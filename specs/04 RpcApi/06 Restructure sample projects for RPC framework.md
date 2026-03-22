# ✅ **SPEC: Restructure Sample Projects for RPC Framework**

## **Goal**
Restructure the sample solution to clearly demonstrate the canonical RPC architecture:

```
Serde.FS.Json.SampleRpc.Shared   (contract)
Serde.FS.Json.SampleRpc.Server   (WebRpc host)
Serde.FS.Json.SampleRpc.Client   (RPC consumer)
Serde.FS.Json.SampleApp          (Serde attribute testing only)
```

This replaces the current mixed responsibilities and creates a clean, real‑world example of how users should structure their own RPC applications.

---

# 1. **Rename `Serde.FS.Json.SampleRpc` → `Serde.FS.Json.SampleRpc.Shared`**

### **Actions**
- Rename the project folder  
- Rename the `.fsproj` file  
- Update the project name inside the `.fsproj`  
- Update namespaces to `Serde.FS.Json.SampleRpc.Shared`  
- Update all references in other projects to point to the new name  

### **Contents of Shared project**
- `Domain.fs` containing:
  - DTO types  
  - `[<RpcApi>]` interface(s)  
- No server code  
- No client code  
- No ASP.NET dependencies  

---

# 2. **Create new project: `Serde.FS.Json.SampleRpc.Server`**

### **Project type**
- ASP.NET Core Web project (minimal API)

### **Dependencies**
- `Serde.FS`
- `Serde.FS.Json`
- `Serde.FS.Json.AspNet`
- Reference to `Serde.FS.Json.SampleRpc.Shared`

### **Contents**
- `Program.fs` with:
  - WebApplication builder
  - `OrderApi()` implementation (or whatever API is in Shared)
  - `app.MapRpcApi<IOrderApi>(OrderApi())`
  - Basic logging + CORS if needed

### **Namespace**
`Serde.FS.Json.SampleRpc.Server`

### **Notes**
- Remove all `[<Serde>]` testing code from this project  
- This project is *only* the RPC host  

---

# 3. **Create new project: `Serde.FS.Json.SampleRpc.Client`**

### **Project type**
- Console app

### **Dependencies**
- `Serde.FS`
- `Serde.FS.Json`
- Reference to `Serde.FS.Json.SampleRpc.Shared`

### **Contents**
- `Program.fs` demonstrating:
  ```fsharp
  open System.Net.Http
  open Serde.FS.Json
  open Serde.FS.Json.SampleRpc.Shared

  let http = new HttpClient()
  let orders = RpcClient.Create<IOrderApi>("http://localhost:5050", http)

  let run = task {
      let! p = orders.GetProduct(ProductId 42)
      printfn "Product: %A" p

      let! all = orders.ListProducts()
      printfn "Products: %A" all
  }

  run.Wait()
  ```

### **Namespace**
`Serde.FS.Json.SampleRpc.Client`

---

# 4. **Simplify `Serde.FS.Json.SampleApp`**

### **Goal**
Return SampleApp to its original purpose:  
**testing `[<Serde>]` and codec generation only.**

### **Actions**
- Remove all RPC‑related code  
- Remove ASP.NET hosting  
- Remove RPC interfaces  
- Remove RPC DTOs  
- Remove `MapRpcApi` usage  
- Keep only:
  - `[<Serde>]` attribute tests  
  - Codec generation tests  
  - Serialization/deserialization demos  

### **Namespace**
`Serde.FS.Json.SampleApp`

---

# 5. **Solution Updates**

### **Add to solution**
- `Serde.FS.Json.SampleRpc.Server`
- `Serde.FS.Json.SampleRpc.Client`

### **Rename in solution**
- `Serde.FS.Json.SampleRpc` → `Serde.FS.Json.SampleRpc.Shared`

### **Update project references**
- Server → Shared  
- Client → Shared  
- SampleApp → no RPC references  

---

# 6. **Directory Structure After Changes**

```
/Serde.FS.slnx
  /Serde.FS
  /Serde.FS.Json
  /Serde.FS.Json.Codec
  /Serde.FS.Json.AspNet
  /Serde.FS.Json.SourceGen
  /Serde.FS.Json.SampleApp
  /Serde.FS.Json.SampleRpc.Shared
  /Serde.FS.Json.SampleRpc.Server
  /Serde.FS.Json.SampleRpc.Client
```

---

# 7. **No code changes required to RPC system**
This spec is **purely structural**.  
The RPC framework already supports this layout perfectly.

---
