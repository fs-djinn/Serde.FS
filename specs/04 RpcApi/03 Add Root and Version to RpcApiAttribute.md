# 📘 **CHANGE SPEC — Add `Root` and `Version` Routing to RPC API**

## 🎯 **Goal**

Enhance the RPC routing system so each API interface can optionally specify:

- a **Root** (custom namespace)
- a **Version** (path suffix)

Default behavior remains stable and predictable:

- If `Root` is omitted → use the interface name as-is (e.g., `IOrderApi`)
- If `Version` is omitted → no version segment

This enables clean URLs like:

```
/rpc/IOrderApi/GetProduct
/rpc/Orders/v2/GetProduct
```

…and avoids ambiguity such as interpreting `Root = "v2"` as the entire namespace.

---

# 1. **Update the `[<RpcApi>]` attribute**

Modify the attribute to support two optional parameters:

```fsharp
[<AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)>]
type RpcApiAttribute() =
    inherit Attribute()

    member val Root : string = null with get, set
    member val Version : string = null with get, set
```

Both are optional.

---

# 2. **Determine the effective root**

Inside `MapRpcApi<'TApi>`:

### Compute the root as:

```
if attribute.Root is not null and not empty:
    root = attribute.Root
else:
    root = interfaceName   (used as-is, no stripping)
```

Example:

| Interface | Root attr | Result |
|----------|-----------|--------|
| `IOrderApi` | none | `"IOrderApi"` |
| `IOrderApi` | `"Orders"` | `"Orders"` |
| `IUserApi` | `"v2"` | `"v2"` (explicit override) |

---

# 3. **Determine the effective version**

### Compute version as:

```
if attribute.Version is null or empty:
    versionSegment = ""
else:
    versionSegment = "/" + attribute.Version
```

Examples:

| Version attr | Result |
|--------------|--------|
| none | `""` |
| `"v2"` | `"/v2"` |
| `"beta"` | `"/beta"` |

---

# 4. **Construct the final route prefix**

Inside `MapRpcApi<'TApi>`:

```
let routePrefix = $"/rpc/{root}{versionSegment}"
```

Examples:

| Root | Version | Final prefix |
|------|---------|--------------|
| `"IOrderApi"` | none | `/rpc/IOrderApi` |
| `"Orders"` | `"v2"` | `/rpc/Orders/v2` |
| `"IUserApi"` | `"beta"` | `/rpc/IUserApi/beta` |

---

# 5. **Map each method under the prefix**

Current behavior:

```
/rpc/GetProduct
```

New behavior:

```
/rpc/{root}/{version}/{methodName}
```

Example:

```
/rpc/Orders/v2/GetProduct
```

---

# 6. **`RpcApiBuilder` remains unchanged**

The builder still stores:

```
Dictionary<string, IEndpointConventionBuilder>
```

Keys remain **just the method names**:

```
"GetProduct"
"PlaceOrder"
"ListProducts"
```

Because each builder is scoped to a single API, there is no collision risk.

---

# 7. **`GetRoute` stays simple**

Users still write:

```fsharp
rpc.GetRoute(nameof IOrderApi.GetProduct)
```

No need to include root or version — the builder already knows its namespace.

---

# 8. **Sample usage**

### Default behavior (no attributes)

```fsharp
type IOrderApi =
    abstract GetProduct : ProductId -> Async<Product>
```

Routes:

```
/rpc/IOrderApi/GetProduct
```

### Custom root

```fsharp
[<RpcApi(Root = "Orders")>]
type IOrderApi =
```

Routes:

```
/rpc/Orders/GetProduct
```

### Version only

```fsharp
[<RpcApi(Version = "v2")>]
type IOrderApi =
```

Routes:

```
/rpc/IOrderApi/v2/GetProduct
```

### Root + Version

```fsharp
[<RpcApi(Root = "Orders", Version = "v3")>]
type IOrderApi =
```

Routes:

```
/rpc/Orders/v3/GetProduct
```

---

# 9. **No changes to client‑side invocation**

Clients still call:

```fsharp
rpc.GetRoute(nameof IOrderApi.GetProduct)
```

This remains stable and type‑checked.

---

# 🌟 **Summary**

This change:

- preserves all existing behavior  
- adds clean, explicit routing control  
- supports versioning without ambiguity  
- keeps URLs predictable and ergonomic  
- avoids collisions across APIs  
- requires minimal code changes  
- fits perfectly into your existing architecture  

This is the kind of polish that makes a framework feel *designed*, not accidental.

---
