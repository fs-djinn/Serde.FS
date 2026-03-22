module SampleRpc.Server.OrderApi

open SampleRpc.Shared

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
