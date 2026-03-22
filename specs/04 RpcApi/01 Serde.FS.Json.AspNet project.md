# 📘 **Spec: RPC Dispatch + ASP.NET Integration**

This spec defines two deliverables:

1. **Extend Serde.FS.Json.SourceGen** to emit RPC dispatch modules  
2. **Create Serde.FS.Json.AspNet** to wire ASP.NET to SerdeJson for RPC routes  

Both parts are required for the RPC system to function.

---

# ============================================================
# **PART A — Extend Serde.FS.Json.SourceGen**
# ============================================================

## **Purpose**
For every interface marked with `[<RpcApi>]`, the generator must emit a module:

```
module SerdeGenerated.Rpc.<ApiName>
```

This module provides:

- A list of RPC method names  
- Dynamic serializers/deserializers  
- A dynamic invoker that calls the implementation  

This module is consumed by the ASP.NET glue layer.

---

# **A.1 Output Module Shape**

For an interface:

```fsharp
[<RpcApi>]
type IOrderApi =
    abstract GetProduct : int -> Async<Product>
    abstract PlaceOrder : Order -> Async<OrderSummary>
```

Generate:

```fsharp
namespace SerdeGenerated.Rpc

open System.Threading.Tasks

module IOrderApi =
    val methods : string list

    val deserializeDynamic : methodName:string -> json:string -> obj
    val serializeDynamic : methodName:string -> value:obj -> string

    val invoke : impl:obj -> methodName:string -> input:obj -> Task<obj>
```

---

# **A.2 Required Behavior**

### **1. `methods : string list`**
List of abstract method names in the interface, in declaration order.

Example:

```fsharp
let methods = [ "GetProduct"; "PlaceOrder" ]
```

---

### **2. `deserializeDynamic`**

Given:

- method name  
- JSON string  

Return the deserialized input object using the generated codec for that method’s input type.

Example:

```fsharp
match methodName with
| "GetProduct" -> SerdeJson.deserialize<int>(json) :> obj
| "PlaceOrder" -> SerdeJson.deserialize<Order>(json) :> obj
```

---

### **3. `serializeDynamic`**

Given:

- method name  
- output object  

Return JSON using the generated codec for that method’s return type.

Example:

```fsharp
match methodName with
| "GetProduct" -> SerdeJson.serialize<Product>(value :?> Product)
| "PlaceOrder" -> SerdeJson.serialize<OrderSummary>(value :?> OrderSummary)
```

---

### **4. `invoke`**

Given:

- implementation object  
- method name  
- input object  

Call the method and return a `Task<obj>`.

**Async<'T> must be converted to Task<'T> using `Async.StartAsTask`.**

Example:

```fsharp
match methodName with
| "GetProduct" ->
    let api = impl :?> IOrderApi
    let arg = input :?> int
    task {
        let! result = api.GetProduct arg |> Async.StartAsTask
        return result :> obj
    }
```

---

# **A.3 Constraints**

Claude must:

- NOT use reflection to inspect DTO types  
- NOT generate codecs (already done)  
- NOT modify existing codec files  
- NOT introduce new abstractions  
- ONLY generate the RPC dispatch module  

---

# ============================================================
# **PART B — Serde.FS.Json.AspNet**
# ============================================================

## **Purpose**
Provide ASP.NET Core integration that routes RPC calls to SerdeJson serialization and the generated RPC dispatch module.

This package contains **no codegen** and **no type‑model logic**.

---

# **B.1 Project Requirements**

### **Project: `Serde.FS.Json.AspNet`**
- Target: `net8.0`
- Dependencies:
  - `Serde.FS.Json`
  - `Microsoft.AspNetCore.App` (implicit)
- No Fable support
- No MSBuild targets
- No reflection except to load the generated RPC module by name

---

# **B.2 Public API**

Expose exactly one extension method:

```fsharp
type IEndpointRouteBuilder with
    member MapRpcApi<'TApi>(impl: 'TApi) : IEndpointRouteBuilder
```

This registers:

```
POST /rpc/{methodName}
```

for each RPC method.

---

# **B.3 Runtime Behavior**

### **1. Route Mapping**

For interface:

```fsharp
[<RpcApi>]
type IOrderApi =
    abstract GetProduct : int -> Async<Product>
```

Map:

```
POST /rpc/GetProduct
```

---

### **2. Request Handling**

Each endpoint must:

1. Read request body as raw text  
2. Call `deserializeDynamic`  
3. Call `invoke`  
4. Call `serializeDynamic`  
5. Write JSON response  

No System.Text.Json involvement.

---

# **B.4 Implementation Details**

## **File: RpcReflection.fs**

A tiny helper to load the generated module:

```fsharp
module RpcReflection =
    let loadModule (apiName: string) : System.Type =
        let fullName = $"SerdeGenerated.Rpc.%s{apiName}"
        System.Type.GetType(fullName, throwOnError = true)

    let getMethods (t: System.Type) : string list =
        t.GetProperty("methods").GetValue(null) :?> string list

    let deserializeDynamic (t: System.Type) (methodName: string) (json: string) : obj =
        t.GetMethod("deserializeDynamic").Invoke(null, [| methodName; json |])

    let serializeDynamic (t: System.Type) (methodName: string) (value: obj) : string =
        t.GetMethod("serializeDynamic").Invoke(null, [| methodName; value |]) :?> string

    let invoke (t: System.Type) (impl: obj) (methodName: string) (input: obj) : Task<obj> =
        t.GetMethod("invoke").Invoke(null, [| impl; methodName; input |]) :?> Task<obj>
```

---

## **File: RpcEndpointExtensions.fs**

```fsharp
namespace Serde.FS.Json.AspNet

open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Http
open System.Threading.Tasks

module private Helpers =
    let readBodyAsString (ctx: HttpContext) =
        task {
            use sr = new System.IO.StreamReader(ctx.Request.Body)
            return! sr.ReadToEndAsync()
        }

    let writeJson (ctx: HttpContext) (json: string) =
        ctx.Response.ContentType <- "application/json"
        ctx.Response.WriteAsync(json)

type IEndpointRouteBuilder with
    member this.MapRpcApi<'TApi>(impl: 'TApi) =
        let apiName = typeof<'TApi>.Name
        let rpcModule = RpcReflection.loadModule apiName

        let group = this.MapGroup("/rpc")

        for methodName in RpcReflection.getMethods rpcModule do
            group.MapPost(methodName, RequestDelegate(fun ctx ->
                task {
                    let! body = Helpers.readBodyAsString ctx
                    let input = RpcReflection.deserializeDynamic rpcModule methodName body
                    let! output = RpcReflection.invoke rpcModule (impl :> obj) methodName input
                    let json = RpcReflection.serializeDynamic rpcModule methodName output
                    return! Helpers.writeJson ctx json
                }
            ))
            |> ignore

        this
```

---

# ============================================================
# **DELIVERABLES**
# ============================================================

Claude must produce:

### **1. Source generator extension**
- Emits `SerdeGenerated.Rpc.<ApiName>` modules  
- Implements:
  - `methods`
  - `deserializeDynamic`
  - `serializeDynamic`
  - `invoke` (Async → Task conversion)

### **2. New project: Serde.FS.Json.AspNet**
- `Serde.FS.Json.AspNet.fsproj`
- `RpcReflection.fs`
- `RpcEndpointExtensions.fs`

### **3. No other public API**

---
