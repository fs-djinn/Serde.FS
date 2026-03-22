module SampleRpc.Client.Program

open System.Net.Http
open Serde.FS.Json
open SampleRpc.Shared

[<Serde.FS.EntryPoint>]
let main _argv =
    let http = new HttpClient()
    let orders = RpcClient.Create<IOrderApi> "http://localhost:5050" http

    let run = task {
        printfn "--- GetProduct ---"
        let! product = orders.GetProduct 42 |> Async.StartAsTask
        printfn "Product: %A" product

        printfn ""
        printfn "--- ListProducts ---"
        let! products = orders.ListProducts() |> Async.StartAsTask
        for p in products do
            printfn "  %A" p

        printfn ""
        printfn "--- PlaceOrder ---"
        let order : Order = {
            Id = 1
            Lines = [
                { Product = { Id = { Value = 42 }; Name = "Widget"; Price = 9.99m; Tags = [] }; Quantity = 3 }
            ]
            Notes = Some "Rush delivery"
        }
        let! summary = orders.PlaceOrder order |> Async.StartAsTask
        printfn "Summary: %A" summary
    }

    try
        run.Wait()
    with ex ->
        printfn "Error: %s" ex.InnerException.Message

    printfn ""
    printf "Press any key to exit..."
    System.Console.ReadKey(true) |> ignore
    printfn ""
    0
