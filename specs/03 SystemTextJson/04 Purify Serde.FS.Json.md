# **Spec 04 — Purify `Serde.FS.Json` (Remove STJ, Adopt Codec Pipeline)**

This spec transforms `Serde.FS.Json` into a **pure JSON backend** that uses the codec pipeline exclusively and has **zero dependency** on System.Text.Json or STJ‑related codegen.

This is the final step that completes the ecosystem split.

---

# **1. Goals**

### ✔ Remove all System.Text.Json dependencies  
### ✔ Remove all STJ‑related codegen paths  
### ✔ Ensure JSON backend uses codec pipeline exclusively  
### ✔ Ensure JSON backend is fully independent of STJ backend  
### ✔ Update tests to reflect the new architecture  
### ✔ Keep the public API stable where possible  

This spec is a cleanup + modernization pass.

---

# **2. Project cleanup**

### **2.1 Remove STJ package references**
From `Serde.FS.Json.fsproj`:

- Remove any reference to:
  - `System.Text.Json`
  - `System.Text.Json.Serialization`
  - Any STJ‑related packages

### **2.2 Remove STJ‑related code**
Delete or refactor:

- Any STJ converters  
- Any STJ metadata helpers  
- Any STJ fallback logic  
- Any STJ‑specific serialization paths  
- Any code that references `JsonSerializerOptions` or STJ attributes  

After this spec, **Serde.FS.Json must not compile if STJ is referenced anywhere**.

---

# **3. Codegen cleanup**

### **3.1 Remove STJ codegen from JSON SourceGen**
In `Serde.FS.Json.SourceGen`:

- Remove any logic that emits STJ metadata  
- Remove any STJ‑specific hint names  
- Remove any STJ‑specific resolver generation  
- Ensure the emitter only generates:
  - JSON codecs  
  - JSON helpers  
  - JSON type modules  

### **3.2 Ensure JSON emitter uses codec pipeline exclusively**
The JSON emitter should:

- Generate codec definitions  
- Generate encode/decode functions  
- Generate JSON‑specific helpers  
- Use the codec model from `Serde.FS` core  
- Never reference STJ APIs  

---

# **4. Runtime cleanup**

### **4.1 Ensure SerdeJson.serialize / deserialize use codec pipeline**
Refactor:

```fsharp
SerdeJson.serialize : 'T -> string
SerdeJson.deserialize : string -> 'T
```

to use:

- `Serde.Codec.get<'T>`  
- `Serde.Codec.encode`  
- `Serde.Codec.decode`  

No STJ fallback.  
No STJ integration.  
No hybrid behavior.

### **4.2 Remove any STJ‑based helpers**
Delete:

- STJ converters  
- STJ options  
- STJ resolver hooks  

---

# **5. Public API adjustments**

### **5.1 Preserve existing JSON API surface**
Keep:

- `SerdeJson.serialize`
- `SerdeJson.deserialize`
- Any JSON‑specific modules

But ensure they are codec‑driven.

### **5.2 Remove any STJ‑related public APIs**
If any public API references STJ, it must be removed or replaced.

---

# **6. Test suite updates**

### **6.1 Remove STJ‑dependent tests**
Delete or rewrite tests that:

- Expect STJ behavior  
- Use STJ converters  
- Use STJ metadata  
- Compare JSON output to STJ output  

### **6.2 Add tests for codec‑driven JSON behavior**
Ensure tests cover:

- Records  
- Unions  
- Tuples  
- Options  
- Lists / maps  
- Nested types  
- Flattening  
- Renaming  
- Skipping fields  

### **6.3 Ensure JSON output is deterministic**
Add tests verifying:

- Stable field ordering  
- Stable union tagging  
- Stable formatting  

### **6.4 Ensure JSON backend is independent**
Add a test that ensures:

```fsharp
open System.Text.Json
```

is *not* required anywhere in Serde.FS.Json.

---

# **7. Acceptance criteria**

Spec 04 is complete when:

1. **Serde.FS.Json has zero references to System.Text.Json**  
2. **Serde.FS.Json.SourceGen generates only JSON codecs**  
3. **SerdeJson.serialize/deserialize use codec pipeline exclusively**  
4. **All STJ‑related code is removed**  
5. **All STJ‑related tests are removed or rewritten**  
6. **JSON tests validate codec‑driven behavior**  
7. **Build succeeds with no warnings or errors**  
8. **Serde.FS.Json is fully independent of Serde.FS.SystemTextJson**  

---

# **8. Architectural outcome**

After Spec 04:

### ✔ Serde.FS.Json = pure JSON backend  
### ✔ Serde.FS.SystemTextJson = pure STJ backend  
### ✔ No cross‑contamination  
### ✔ Clean boundaries  
### ✔ Clean codegen  
### ✔ Clean runtime  
### ✔ Clean tests  

This is the moment where the ecosystem becomes elegant.

---
