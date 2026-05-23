module SampleRpc.Client.Program

open System.Net.Http
open Serde.FS.Json
open SampleRpc.Shared

[<Serde.FS.EntryPoint>]
let main _argv =
    use http = new HttpClient()
    http.DefaultRequestHeaders.Add("X-Api-Key", "ABC")

    let orders = RpcClient.create<IOrderApi> http "http://localhost:5050"
    // Second proxy for the second interface — verifies that RpcClient.create
    // routes each interface to its own URL prefix without crossing wires.
    let inventory = RpcClient.create<IInventoryApi> http "http://localhost:5050"

    try
        async {
            printfn "--- GetProduct ---"
            let! product = orders.GetProduct 42
            printfn "Product: %A" product

            printfn ""
            printfn "--- TryGetProduct (Ok) ---"
            let! result = orders.TryGetProduct 42
            printfn "Result: %A" result

            printfn ""
            printfn "--- TryGetProduct (Error) ---"
            let! result = orders.TryGetProduct -1
            printfn "Result: %A" result

            printfn ""
            printfn "--- ListProducts ---"
            let! products = orders.ListProducts()
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
            let! summary = orders.PlaceOrder order
            printfn "Summary: %A" summary

            printfn ""
            printfn "--- IInventoryApi.GetStock ---"
            let! stock = inventory.GetStock { Value = 42 }
            printfn "Stock: %A" stock

            printfn ""
            printfn "--- IInventoryApi.ListLowStock ---"
            let! lowStock = inventory.ListLowStock 10
            for s in lowStock do
                printfn "  %A" s

            printfn ""
            printfn "--- IInventoryApi.GetStockedProduct (shared Product type) ---"
            let! stockedProduct = inventory.GetStockedProduct { Value = 7 }
            printfn "Stocked product: %A" stockedProduct
        }
        |> Async.RunSynchronously
    with ex ->
        printfn "Error: %s" ex.Message

    printfn ""
    printf "Press any key to exit..."
    System.Console.ReadKey(true) |> ignore
    printfn ""
    0
