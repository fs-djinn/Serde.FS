# **Serde.FS — Strict, Deterministic Serialization for F#**

[![Serde.FS](https://img.shields.io/nuget/vpre/Serde.FS.svg?label=Serde.FS)](https://www.nuget.org/packages/Serde.FS/)
[![Serde.FS.SourceGen](https://img.shields.io/nuget/vpre/Serde.FS.SourceGen.svg?label=Serde.FS.SourceGen)](https://www.nuget.org/packages/Serde.FS.SourceGen/)
[![Serde.FS.Json](https://img.shields.io/nuget/vpre/Serde.FS.Json.svg?label=Serde.FS.Json)](https://www.nuget.org/packages/Serde.FS.Json/)

Serde.FS is a reflection‑free, compile‑time validated serialization and RPC framework for F#.  
It brings Rust‑style determinism to .NET and adds a **zero‑boilerplate RPC layer** on top.

---

## 🚀 RPC in Three Blocks

### Shared.fsproj
```fsharp
[<RpcApi>]
type IOrderApi =
    abstract GetProduct : ProductId -> Async<Product>
    abstract PlaceOrder : Order -> Async<OrderSummary>
```

### Server.fsproj
```fsharp
app.MapRpc<IOrderApi>(OrderApi())
```

### Client.fsproj
```fsharp
let! product = client.GetProduct(ProductId 42)
```

That’s the entire workflow:  
**Define an interface → Implement it → Call it.**  

All routing, serialization, and client code is fully generated at compile time by the same deterministic engine that powers Serde.FS — no reflection, no runtime inference, no surprises.

---

## 📦 NuGet Packages

Serde.FS is composed of several focused packages:

| Package | Description |
|--------|-------------|
| **Serde.FS** | Core metadata + attributes used by all backends |
| **Serde.FS.Json** | Deterministic, reflection‑free JSON backend |
| **Serde.FS.Json.AspNet** | Integrates Serde.FS.Json into ASP.NET for RPC servers |
| **Serde.FS.Json.Fable** 🚧 | Fable RPC client integration (in development) |

Most users will install:

- `Serde.FS.Json.AspNet` (for RPC servers)
- `Serde.FS.Json` (for serialization)

---

## 🌐 Coming Soon: Serde.FS.Json.Fable

A first‑class Fable RPC client is in development.
It will let you consume any `[<RpcApi>]` interface directly from a Fable project with the same zero‑boilerplate workflow:

```fsharp
let! product = client.GetProduct(ProductId 42)
```

---

## 🚀 Getting Started

This is the smallest possible Serde.FS RPC setup.  
It uses three projects — **Shared**, **Server**, and **Client** — following the familiar SAFE‑style structure.

---

### 📁 1. Create the Shared project

Define your RPC interface and domain types.

```fsharp
namespace SampleRpc.Shared

open Serde.FS

// DTOs — no [<Serde>] needed, discovered via [<RpcApi>] interface

[<Struct>]
type ProductId = ProductId of int

type Product =
    { Id: ProductId
      Name: string }

[<RpcApi>]
type IOrderApi =
    abstract GetProduct : ProductId -> Async<Product>
```

This is the only place where the interface lives.  
Both the server and client reference this project.

See the full shared example here:
[src/Serde.FS.Json.SampleRpc.Shared/Shared.fs](src/Serde.FS.Json.SampleRpc.Shared/Domain.fs)

---

### 🖥️ 2. Create the Server project

A minimal ASP.NET RPC server:

```fsharp
open Microsoft.AspNetCore.Builder
open SampleRpc.Shared
open Serde.FS.Json.AspNet

type OrderApi() =
    interface IOrderApi with
        member _.GetProduct(ProductId id) =
            async { return { Id = ProductId id; Name = $"Product %d{id}" } }

[<Serde.FS.EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder(argv)
    let app = builder.Build()

    app.MapRpcApi<IOrderApi>(OrderApi()) |> ignore

    app.Run()
    0
```

That’s it — no authentication, no policies, no extra endpoints.  
Just a clean RPC server.

See the full server example here:
[src/Serde.FS.Json.SampleRpc.Server/Program.fs](src/Serde.FS.Json.SampleRpc.Server/Program.fs)

---

### 💻 3. Create the Client project

Consume the RPC API with a generated client:

```fsharp
open SampleRpc.Shared
open Serde.FS.Json

[<Serde.FS.EntryPoint>]
let main _ =
    async {
        use http = new HttpClient()
        let orders = RpcClient.create<IOrderApi> http "http://localhost:5000"

        let! product = orders.GetProduct(ProductId 42)
        printfn $"Product: %A{product}"
    }
    |> Async.RunSynchronously

    0
```

The client is fully generated at compile time —  
no reflection, no runtime inference, no DTO drift.

See the full client example here:
[src/Serde.FS.Json.SampleRpc.Client/Program.fs](src/Serde.FS.Json.SampleRpc.Client/Program.fs)


---

### 🎉 That’s the entire workflow

**Define an interface → Implement it → Call it.**

All routing, serialization, and client code is generated at compile time by the same deterministic engine that powers Serde.FS.

---

## 🚀 Quick Start (the whole system in 20 seconds)

### 1. Install the JSON backend

```bash
dotnet add package Serde.FS.Json
```

### 2. Annotate your types

```fsharp
open Serde.FS

[<Serde>]
type Person = {
    Name : string
    Age  : int
}
```

### 3. Serialize and deserialize

```fsharp
open Serde.FS.Json

let json = SerdeJson.serialize { Name = "Gustens"; Age = 14 }
let person : Person = SerdeJson.deserialize json
```

That’s the entire workflow: **opt in → generate → serialize**.

### 4. Add EntryPoint

If you are using Serde.FS in an app that needs an entry point, you must use the special `Serde.FS.EntryPoint` attribute:

```fsharp
[<Serde.FS.EntryPoint>]
let main argv = ...
```

---

## ✨ Key Features

- **Strict, Serde‑style serialization** — Only `[<Serde>]` types participate.  
- **Compile‑time validation** — Nested types must also be annotated.  
- **Deterministic code generation** — Stable, predictable serializers.  
- **Fast runtime backend** — F# Source Generated code, no reflection.  
- **Custom converters** — Override behavior per‑type without weakening strictness.  
- **Backend‑agnostic design** — JSON today, TOML/YAML tomorrow.

---

## 🔒 Strictness Model

Serde.FS enforces strictness at two levels:

### **1. Root strictness (runtime)**
Serializing a type without `[<Serde>]` throws a strict violation.

### **2. Nested strictness (build‑time)**
If a Serde‑annotated type contains a nested type that lacks `[<Serde>]`, the generator fails the build.

This mirrors Rust Serde:

> If a nested type doesn’t derive Serialize, the parent type cannot derive Serialize.

---

## 🧩 Custom Converters

Serde.FS supports attribute‑level codecs that override serialization for specific types.

### 1. Implement a custom codec

```fsharp
open Serde.FS
open Serde.FS.Json.Codec

// Custom codec that uppercases on encode and lowercases on decode
type FancyNameCodec() =
    interface IJsonCodec<FancyName> with
        member _.Encode(n: FancyName) =
            JsonValue.String(n.Value.ToUpperInvariant())

        member _.Decode(json: JsonValue) =
            match json with
            | JsonValue.String s -> { Value = s.ToLowerInvariant() }
            | _ -> failwith "Expected JSON string for FancyName"

```

### 2. Attach it to a type

```fsharp
and 
    [<Serde(Codec = typeof<FancyNameCodec>)>]
    FancyName = { Value : string }
```

### 3. Use it normally

```fsharp
let fancy = { Value = "Jordan" }
let json = SerdeJson.serialize fancy
// => "JORDAN"

let back : FancyName = SerdeJson.deserialize json
// => { Value = "jordan" }
```

Converters are explicit, compile‑time validated, and do not introduce fallback or reflection.

---

## 🏁 Custom EntryPoint for CLI apps  
F# requires the real `[<EntryPoint>]` function to appear last in compilation order.  
Because Serde.FS uses source generation, it cannot safely generate the real entry point directly.  
 
To solve this, mark your intended entry point with:  
 
```fsharp
[<Serde.FS.EntryPoint>]
let main argv = ...
```  
 
Serde.FS will generate the actual `[<EntryPoint>]` wrapper in a separate file so it appears in the correct place in the compilation order.

---

## 🧱 Design Philosophy

- **Explicitness** — Only annotated types participate.  
- **Determinism** — No runtime inference or fallback.  
- **Compile‑time validation** — Errors surface early.  
- **Backend independence** — Metadata is backend‑agnostic.

---


## 🧠 Mental Model

Serde.FS is not a general‑purpose .NET serializer. It is a **compile‑time, explicit, deterministic system** inspired by Rust Serde.

- Only annotated types participate.  
- No runtime inference or fallback.  
- Errors surface early and predictably.  
- Backends follow Serde semantics, not their own.  

Ideal for stable domain models, configuration files, deterministic logs, and interop formats.

Not designed for dynamic JSON, schema‑drifting storage, partial deserialization, or runtime‑mutable behavior.

---

## 🎯 When Serde.FS is the right tool

Serde.FS is designed for developers who want:

### **Compile‑time validated serialization**
Serde.FS generates metadata and serializers at build time.  
If your types are invalid or incomplete, you find out *before* your program runs.

### **Deterministic, reflection‑free JSON**
No runtime shape inference.  
No reflection.  
No surprises.

The JSON encoding is stable, predictable, and part of the contract.

### **High performance**
All serializers and deserializers are generated ahead of time.  
No runtime metadata discovery.

### **Schema‑aware systems**
If you care about correctness, versioning, or long‑term stability, Serde.FS gives you a rock‑solid foundation.

### **Interop with Serde‑style ecosystems**
The design mirrors Rust Serde: metadata‑driven, explicit, principled.

If your goal is:

> “Make my JSON encoding correct, stable, and compile‑time validated — and don’t let me shoot myself in the foot.”

Serde.FS is the right tool.

---

## 🛑 When Serde.FS is *not* the right tool

Serde.FS intentionally does **not** support:

### **Custom DU encodings**
If you need to shape JSON for SQL Server, legacy systems, or custom wire formats, Serde.FS is not designed for that.

### **Runtime configuration**
Serde.FS does not allow per‑type or per‑case overrides of encoding rules.  
The encoding is fixed and deterministic.

### **Dynamic or untyped JSON**
If you need to deserialize into `obj`, dictionaries, or unknown shapes, a reflection‑based library is a better fit.

### **Highly flexible or ad‑hoc JSON**
If your JSON shape is not under your control, or you need to adapt to many different formats, use a dynamic library.

### **Recommended alternative**
If you need flexible, customizable, runtime‑driven JSON encoding, consider:

- **Thoth.Json** — excellent for hand‑crafted decoders/encoders and custom DU shapes.

Serde.FS is not trying to replace Thoth.  
It’s a different tool for a different philosophy.

---

## 🧭 Choosing the Right JSON Library for Your F# Project

| Scenario / Requirement | **Serde.FS.Json** | **FSharp.SystemTextJson** | **Thoth.Json** |
|------------------------|-------------------|----------------------------|-----------------|
| **Compile‑time validated serialization** | ⭐ **Best choice** | ❌ No | ❌ No |
| **Deterministic, reflection‑free encoding** | ⭐ **Yes** | ❌ Reflection‑based | ❌ Manual decoders |
| **High performance, ahead‑of‑time generation** | ⭐ **Yes** | ⚠️ Mixed | ❌ No |
| **Custom DU encoding** | ❌ Fixed (not customizable) | ⭐ **Yes** | ⭐ **Yes** |
| **Dynamic / untyped JSON** | ❌ No | ⭐ **Yes** | ⭐ **Yes** |
| **Interop with legacy JSON formats** | ❌ No | ⭐ **Yes** | ⭐ **Yes** |
| **Hand‑crafted decoders / domain‑driven parsing** | ❌ No | ⚠️ Possible | ⭐ **Best choice** |
| **Strict schema stability (generated at build-time)** | ⭐ **Yes** | ❌ No | ❌ No |
| **Zero reflection (AOT‑friendly)** | ⭐ **Yes** | ❌ No | ⭐ **Yes** |
| **Best for SQL‑shaped JSON** | ❌ No | ⭐ **Yes** | ⭐ **Yes** |
| **Best for F# domain models** | ⭐ **Yes** | ⚠️ Good | ⭐ **Good** |
| **Best for dynamic JSON APIs** | ❌ No | ⭐ **Best choice** | ⭐ **Best choice** |

### Summary

- **Choose Serde.FS.Json** if you want correctness, determinism, and compile‑time guarantees.  
- **Choose FSharp.SystemTextJson** if you need flexibility, custom DU encodings, or interop with existing JSON.  
- **Choose Thoth.Json** if you want explicit, hand‑crafted decoders and total control over parsing.

---

## 📚 Roadmap

- Additional backends (TOML, YAML)  
- Field‑level overrides (`rename`, `skip`, `flatten`)  
- Improved diagnostics  
- Optional compile‑time schema generation  

---

## ❤️ Acknowledgements

Serde.FS is inspired by the elegance and rigor of Rust Serde, adapted to the F# ecosystem with a focus on clarity, determinism, and developer experience.

---

Serde.FS.Json is powered by [FSharp.SourceDjinn](https://github.com/serde-fs/FSharp.SourceDjinn), a lightweight source generator engine.
