module SampleRpc.Server.Program

open Microsoft.AspNetCore.Builder
open Serde.FS.Json.AspNet
open SampleRpc.Shared
open SampleRpc.Server.OrderApi

[<Serde.FS.EntryPoint>]
let main (argv: string array) =
    let builder = WebApplication.CreateBuilder(argv)
    let app = builder.Build()

    app.MapRpcApi<IOrderApi>(OrderApi()) |> ignore
    app.MapGet("/", System.Func<string>(fun () -> "SampleRpc.Server — RPC endpoints at /rpc/IOrderApi/{method}")) |> ignore

    printfn "Starting RPC server..."
    printfn "Use RpcApi.http to test the API"
    app.Run()
    0
