module SampleRpc.Server.Program

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication
open SampleRpc.Shared
open SampleRpc.Server.OrderApi
open Serde.FS.Json.AspNet

module Handlers =
    open System.Security.Claims

    type ApiKeyAuthHandler(options, logger, encoder) =
        inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)

        override this.HandleAuthenticateAsync() =
            let request = this.Request
            task {
                match request.Headers.TryGetValue("X-Api-Key") with
                | true, v when v.ToString() = "ABC" ->
                    let claims = [| Claim(ClaimTypes.Name, "ApiKeyUser") |]
                    let identity = ClaimsIdentity(claims, "ApiKey")
                    let principal = ClaimsPrincipal(identity)
                    let ticket = AuthenticationTicket(principal, "ApiKey")
                    return AuthenticateResult.Success(ticket)
                | _ ->
                    return AuthenticateResult.Fail("Missing or invalid X-Api-Key header")
            }


[<Serde.FS.EntryPoint>]
let main (argv: string array) =
    let builder = WebApplication.CreateBuilder(argv)

    builder.Services
        .AddAuthentication("ApiKey")
        .AddScheme<AuthenticationSchemeOptions, Handlers.ApiKeyAuthHandler>("ApiKey", ignore)
    |> ignore

    builder.Services.AddAuthorization(fun options ->
        options.AddPolicy("ApiKeyPolicy", fun policy ->
            policy.RequireAuthenticatedUser() |> ignore
        )
    ) |> ignore

    let app = builder.Build()
    app.UseAuthentication() |> ignore
    app.UseAuthorization() |> ignore

    let rpc = app.MapRpcApi<IOrderApi>(OrderApi())

    rpc.GetRoute(nameof Unchecked.defaultof<IOrderApi>.PlaceOrder)
        .RequireAuthorization("ApiKeyPolicy") |> ignore

    app.MapGet("/", System.Func<string>(fun () -> "Serde.FS.Json SampleApp — RPC endpoints at /rpc/{method}")) |> ignore

    printfn "Starting web server..."
    printfn "Use RpcApi.http to test the API"
    app.Run()

    0
