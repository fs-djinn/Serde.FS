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
            let apiName = typeof<'TApi>.Name
            let rpcModule = RpcReflection.loadModule apiName

            let group = this.MapGroup("/rpc")
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
