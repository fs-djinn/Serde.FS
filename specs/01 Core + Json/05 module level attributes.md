# ⭐ **Claude Spec — Add Module‑Level Serde Attribute Support**

This spec extends the Serde attribute system so that:

- `[<Serde>]` applied to a **module** marks all types inside that module as Serde‑enabled.
- Type‑level attributes still override module‑level attributes.
- Nested modules inherit from parent modules unless overridden.

This is a pure SourceGen change.  
No backend changes.  
No core changes.  
No MSBuild changes.

---

# 1. **Update AST Parsing to Detect Module Attributes**

### **Where to modify:**  
`AstParser.fs` (or wherever the type‑collection logic lives).

### **Goal:**  
Detect attributes on:

- `SynModuleOrNamespace`
- `SynModuleDecl.NestedModule`
- `SynModuleDecl.Types`

### **Specifically:**
Add logic to inspect:

```fsharp
SynModuleDecl.Attributes
```

and extract Serde attributes from modules.

---

# 2. **Track “Current Module Serde Capability” During Traversal**

Introduce a new parameter in the recursive traversal:

```fsharp
currentModuleSerdeCapability : SerdeCapability option
```

When entering a module:

1. Check if the module has a Serde attribute.
2. If yes → set `currentModuleSerdeCapability = Some Serde`.
3. If no → inherit the parent’s capability.

This is identical to how Rust Serde handles module‑level derives.

---

# 3. **Apply Module‑Level Capability to Types Inside the Module**

When encountering a type declaration:

- If the type has its own Serde attribute → use that.
- Else if `currentModuleSerdeCapability` is set → apply it.
- Else → skip the type (no Serde).

### **Example:**

```fsharp
[<Serde>]
module Domain

type A = { X: int }   // Serde-enabled
type B = C of string  // Serde-enabled
```

### **Override example:**

```fsharp
[<Serde>]
module Domain

[<NoSerde>]
type A = { X: int }   // NOT Serde-enabled
```

(You don’t need `NoSerde` yet, but the override logic should be future‑proof.)

---

# 4. **Support Nested Modules**

Nested modules should inherit the parent module’s Serde capability unless overridden.

### Example:

```fsharp
[<Serde>]
module Outer

module Inner =
    type A = { X: int }   // Serde-enabled
```

### Override example:

```fsharp
[<Serde>]
module Outer

[<NoSerde>]
module Inner =
    type A = { X: int }   // NOT Serde-enabled
```

---

# 5. **Update Type Collection Logic**

Wherever the parser currently collects types:

```fsharp
match decl with
| SynModuleDecl.Types(typeDefs, _) -> ...
```

Modify this logic so that:

- Each collected type is annotated with the effective Serde capability.
- This capability is derived from:
  1. Type-level attribute (highest priority)
  2. Module-level attribute (fallback)
  3. No attribute (skip)

---

# 6. **Add Tests**

### **New test file:**  
`ModuleAttributeTests.fs`

### **Test cases:**

#### ✔ Module-level Serde applies to all types
```fsharp
[<Serde>]
module Domain
type A = { X: int }
type B = C of string
```
Both A and B should be collected.

#### ✔ Type-level override
```fsharp
[<Serde>]
module Domain
[<NoSerde>]
type A = { X: int }
type B = C of string
```
Only B should be collected.

#### ✔ Nested module inherits
```fsharp
[<Serde>]
module Outer
module Inner =
    type A = { X: int }
```

#### ✔ Nested module override
```fsharp
[<Serde>]
module Outer
[<NoSerde>]
module Inner =
    type A = { X: int }
```

#### ✔ No module attribute → no types collected
```fsharp
module Domain
type A = { X: int }
```

---

# 7. **Acceptance Criteria**

- `[<Serde>]` on a module marks all types inside it.
- Nested modules inherit unless overridden.
- Type-level attributes override module-level attributes.
- No backend changes required.
- No core changes required.
- All new tests pass.
- Existing tests continue to pass.

---
