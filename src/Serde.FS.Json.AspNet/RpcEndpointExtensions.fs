namespace Serde.FS.Json.AspNet

open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

module private Helpers =
    let readBodyAsString (ctx: HttpContext) =
        task {
            use sr = new System.IO.StreamReader(ctx.Request.Body)
            return! sr.ReadToEndAsync()
        }

    let writeJson (ctx: HttpContext) (json: string) =
        ctx.Response.ContentType <- "application/json"
        ctx.Response.WriteAsync(json)

/// Wraps the route group and per-method endpoint builders returned by MapRpcApi.
type RpcApiBuilder =
    {
        Group: IEndpointRouteBuilder
        Endpoints: Dictionary<string, IEndpointConventionBuilder>
    }
    /// Look up a route by method name for fluent configuration (e.g., RequireAuthorization).
    /// Use with nameof: rpc.GetRoute(nameof Unchecked.defaultof<IOrderApi>.GetProduct)
    member this.GetRoute(methodName: string) =
        match this.Endpoints.TryGetValue(methodName) with
        | true, builder -> builder
        | false, _ ->
            let available = System.String.Join(", ", this.Endpoints.Keys)
            failwith $"RPC method '%s{methodName}' not found. Available methods: %s{available}"

[<AutoOpen>]
module RpcEndpointExtensions =

    type IEndpointRouteBuilder with
        member this.MapRpcApi<'TApi>(impl: 'TApi) =
            let apiType = typeof<'TApi>
            let apiName = apiType.Name
            let rpcModule = RpcReflection.loadModule apiName

            // Read [<RpcApi>] attribute for Root and Version
            let rpcAttr =
                apiType.GetCustomAttributes(typeof<Serde.FS.RpcApiAttribute>, false)
                |> Array.tryHead
                |> Option.map (fun a -> a :?> Serde.FS.RpcApiAttribute)

            let root =
                match rpcAttr |> Option.bind (fun a -> Option.ofObj a.Root) with
                | Some r when r.Length > 0 -> r
                | _ -> apiName

            let versionSegment =
                match rpcAttr |> Option.bind (fun a -> Option.ofObj a.Version) with
                | Some v when v.Length > 0 -> $"/%s{v}"
                | _ -> ""

            let routePrefix = $"/rpc/%s{root}%s{versionSegment}"
            let group = this.MapGroup(routePrefix)
            let endpoints = Dictionary<string, IEndpointConventionBuilder>()

            for methodName in RpcReflection.getMethods rpcModule do
                let builder =
                    group.MapPost(methodName, RequestDelegate(fun ctx ->
                        task {
                            let! body = Helpers.readBodyAsString ctx
                            let input = RpcReflection.deserializeDynamic rpcModule methodName body
                            let! output = RpcReflection.invoke rpcModule (impl :> obj) methodName input
                            let json = RpcReflection.serializeDynamic rpcModule methodName output
                            return! Helpers.writeJson ctx json
                        } :> System.Threading.Tasks.Task
                    ))
                endpoints.[methodName] <- builder

            { Group = this; Endpoints = endpoints }
