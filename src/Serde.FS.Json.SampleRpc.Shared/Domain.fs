namespace SampleRpc.Shared

open Serde.FS

// DTOs — no [<Serde>] needed, discovered via [<RpcApi>] interface

// Type abbreviations: must be expanded by the source generator so that
// codecs are emitted for the underlying types (e.g., int) rather than
// the alias names.
type PageSize = int
type PageNumber = int

[<Struct>]
type ProductId = { Value: int }

type Product = {
    Id: ProductId
    Name: string
    Price: decimal
    Tags: string list
}

type OrderLine = {
    Product: Product
    Quantity: int
}

type Order = {
    Id: int
    Lines: OrderLine list
    Notes: string option
}

type OrderSummary = {
    OrderId: int
    TotalItems: int
    TotalPrice: decimal
}

// Inventory-only DTOs — exercise per-interface codec discovery (these types
// should NOT appear in IOrderApi's generated codecs and vice versa for
// IOrderApi's unique types like Order/OrderLine/OrderSummary).
[<Struct>]
type StockLevel = { Quantity: int; ReorderThreshold: int }

type StockEntry = {
    ProductId: ProductId      // shared with IOrderApi — codec must dedupe across interfaces
    Level: StockLevel
    Location: string
}

// RPC API contracts — two interfaces in the same Shared project exercise
// multi-RpcApi support: per-interface route segregation on the server,
// per-interface file emission on the Fable client, and codec deduplication
// for types referenced by both interfaces (ProductId, Product).

[<RpcApi>]
type IOrderApi =
    abstract GetProduct : int -> Async<Product>
    abstract TryGetProduct : int -> Async<Result<Product, string>>
    abstract PlaceOrder : Order -> Async<OrderSummary>
    abstract ListProducts : unit -> Async<Product list>
    /// Multi-arg method using type abbreviations; exercises both alias resolution
    /// and the multi-arg interface override path in the Fable client emitter.
    abstract ListProductsPage : PageSize * PageNumber -> Async<Product list>

[<RpcApi>]
type IInventoryApi =
    abstract GetStock : ProductId -> Async<StockEntry>
    abstract ListLowStock : int -> Async<StockEntry list>
    /// Returns a type SHARED with IOrderApi — exercises whether ProductCodec
    /// gets emitted once or duplicated across interface codec modules.
    abstract GetStockedProduct : ProductId -> Async<Product>
