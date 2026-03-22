# 📘 **SPEC: Add `GetRoute<'TApi>` to Serde.FS.Json.AspNet**

## 1. **Modify `MapRpcApi` to return an `RpcApiBuilder`**

### Requirements
- Wrap the existing route group and endpoint builders in a new type `RpcApiBuilder`.
- Store a mapping of `methodName → RouteHandlerBuilder`.
- Return this `RpcApiBuilder` instead of the raw `IEndpointRouteBuilder`.

### Structure

```fsharp
type RpcApiBuilder =
    {
        Group : IEndpointRouteBuilder
        Endpoints : Dictionary<string, RouteHandlerBuilder>
    }
```

### Behavior
- `MapRpcApi<'TApi>` should:
  - Create a route group (e.g., `/rpc`)
  - For each RPC method:
    - Map a POST endpoint
    - Store the resulting `RouteHandlerBuilder` in `Endpoints`
  - Return an `RpcApiBuilder` instance

---

## 2. **Add `GetRoute<'TApi>` extension method**

### Purpose
Allow strongly‑typed lookup of RPC endpoints using a lambda:

```fsharp
rpc.GetRoute<IOrderApi>(fun r -> r.GetProduct)
```

### Requirements
- Add an extension method on `RpcApiBuilder`.
- Accept a function `'TApi -> 'a`.
- Extract the method name using `selector.Method.Name`.
- Return the corresponding `RouteHandlerBuilder`.

### Implementation sketch

```fsharp
type RpcApiBuilder with
    member this.GetRoute<'TApi>(selector: 'TApi -> 'a) =
        let methodName = selector.Method.Name
        this.Endpoints.[methodName]
```

### Notes
- No reflection beyond `selector.Method.Name`.
- No quotations required.
- Works for all interface methods.

---

## 3. **Add API‑key authorization policy to SampleApp**

### Requirements
- Add an authorization policy named `"ApiKeyABC"`.
- Policy must require header `X-Api-Key` with value `"ABC"`.

### Implementation sketch (in `runWeb` before `builder.Build()`)

```fsharp
builder.Services.AddAuthorization(fun options ->
    options.AddPolicy("ApiKeyABC", fun policy ->
        policy.RequireAssertion(fun ctx ->
            let http = ctx.Resource :?> HttpContext
            match http.Request.Headers.TryGetValue("X-Api-Key") with
            | true, v when v.ToString() = "ABC" -> true
            | _ -> false
        )
    )
)
```

---

## 4. **Apply the policy to one RPC method**

### Requirements
- In SampleApp, after calling `MapRpcApi<IOrderApi>`, apply the policy to `GetProduct`.

### Example

```fsharp
let rpc = app.MapRpcApi<IOrderApi>(OrderApi())

rpc.GetRoute<IOrderApi>(fun r -> r.GetProduct)
   .RequireAuthorization("ApiKeyABC")
```

---

## 5. **Update `RpcApi.http` test file**

### Requirements
- For the `GetProduct` request, add:

```
X-Api-Key: ABC
```

### Example updated request

```
### Get Product
POST http://localhost:5050/rpc/GetProduct
Content-Type: application/json
Accept: application/json
X-Api-Key: ABC

{
  "Id": { "Value": 42 }
}
```

Other RPC calls (PlaceOrder, ListProducts) remain unchanged unless you choose to secure them.

---

# 🌟 **Final Deliverable Summary (for Claude)**

Claude should:

1. Modify `MapRpcApi` to return a new `RpcApiBuilder` containing:
   - The route group
   - A dictionary of methodName → RouteHandlerBuilder

2. Implement `RpcApiBuilder.GetRoute<'TApi>(selector)`:
   - Extract method name from selector
   - Return the corresponding endpoint builder

3. Add `"ApiKeyABC"` authorization policy to SampleApp

4. Apply the policy to `GetProduct` using:
   ```fsharp
   rpc.GetRoute<IOrderApi>(fun r -> r.GetProduct)
      .RequireAuthorization("ApiKeyABC")
   ```

5. Update `RpcApi.http` to include `X-Api-Key: ABC` for the GetProduct request

---
