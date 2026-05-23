module SampleRpc.Server.InventoryApi

open SampleRpc.Shared

type InventoryApi() =
    interface IInventoryApi with
        member _.GetStock productId =
            async {
                return {
                    ProductId = productId
                    Level = { Quantity = 12; ReorderThreshold = 5 }
                    Location = $"Aisle %d{productId.Value % 10}"
                }
            }

        member _.ListLowStock threshold =
            async {
                return [
                    for i in 1 .. 3 ->
                        { ProductId = { Value = i }
                          Level = { Quantity = max 0 (threshold - i); ReorderThreshold = threshold }
                          Location = $"Aisle %d{i}" }
                ]
            }

        member _.GetStockedProduct productId =
            async {
                return {
                    Id = productId
                    Name = $"In-stock product #{productId.Value}"
                    Price = decimal productId.Value * 9.99m
                    Tags = [ "in-stock" ]
                } : Product
            }
