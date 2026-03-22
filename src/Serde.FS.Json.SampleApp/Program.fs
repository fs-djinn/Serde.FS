module SampleApp

open Serde.FS
open SampleRpc

/// Mock IOrderApi Implementation
type OrderApi() =
    interface IOrderApi with
        member _.GetProduct id =
            async {
                return {
                    Id = { Value = id }
                    Name = $"Product #{id}"
                    Price = decimal id * 9.99m
                    Tags = [ "sample" ]
                } : Product
            }

        member _.PlaceOrder order =
            async {
                let totalItems = order.Lines |> List.sumBy (fun l -> l.Quantity)
                let totalPrice = order.Lines |> List.sumBy (fun l -> decimal l.Quantity * l.Product.Price)
                return { OrderId = order.Id; TotalItems = totalItems; TotalPrice = totalPrice }
            }

        member _.ListProducts() =
            async {
                return [
                    { Id = { Value = 1 }; Name = "Widget"; Price = 9.99m; Tags = ["sale"] }
                    { Id = { Value = 2 }; Name = "Gadget"; Price = 24.50m; Tags = [] }
                ]
            }

open Microsoft.AspNetCore.Builder
open Serde.FS.Json.AspNet

let runWeb (argv: string[]) =
    let builder = WebApplication.CreateBuilder(argv)
    let app = builder.Build()

    app.MapRpcApi<SampleRpc.IOrderApi>(OrderApi()) |> ignore
    app.MapGet("/", System.Func<string>(fun () -> "Serde.FS.Json SampleApp — RPC endpoints at /rpc/{method}")) |> ignore

    printfn "Starting web server..."
    printfn "Use RpcApi.http to test the API"
    app.Run()


[<EntryPoint>]
let run argv =
    if argv |> Array.contains "--web" then
        runWeb (argv |> Array.filter (fun a -> a <> "--web"))
        0
    else
        ConsoleApp.run ()
        0
