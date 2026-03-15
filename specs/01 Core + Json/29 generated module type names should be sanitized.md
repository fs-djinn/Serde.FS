## 🛠️ Fix invalid module/type names in generated serializers

### **Problem**
When generating serializers for constructed generic types whose generic arguments come from namespaces (e.g., `System.Guid`), the generator currently uses the *raw CLR full name* to construct module and type identifiers.

Example of invalid output:

```fsharp
module rec Serde.Generated.Wrapper_System.Guid
```

F# interprets the `.` as a namespace separator, causing:

```
FS0534: A module abbreviation must be a simple name, not a path
```

This breaks serialization for any constructed generic whose type argument contains a dot.

---

## 🎯 **Goal**
Ensure all generated module names, type names, and identifiers are **valid F# identifiers**, even when generic arguments come from namespaces or contain other illegal characters.

---

## 🧩 **Required Change**
Introduce a **single sanitization function** that converts any CLR type name into a safe F# identifier.

### **Sanitization rules**
Replace the following characters with `_`:

- `.`
- `+` (nested types)
- `` ` `` (generic arity)
- `<` and `>`
- `,`
- `[`, `]`
- Any other non‑identifier character

Optionally collapse multiple `_` into a single `_` (not required but cleaner).

### **Example implementation**

```fsharp
let sanitize (name: string) =
    name
        .Replace(".", "_")
        .Replace("+", "_")
        .Replace("`", "_")
        .Replace("<", "_")
        .Replace(">", "_")
        .Replace(",", "_")
        .Replace("[", "_")
        .Replace("]", "_")
```

---

## 🧱 **Where to apply sanitization**
Apply `sanitize` to **every place** where the generator constructs an identifier from a type name:

- module names  
- converter type names  
- JsonTypeInfo function names  
- any internal helper modules  
- any generated static fields or values  
- any generated file names (optional but recommended)

### Example transformation

Input type:

```
Wrapper<System.Guid>
```

Sanitized identifier:

```
Wrapper_System_Guid
```

Generated module:

```fsharp
module rec Serde.Generated.Wrapper_System_Guid
```

Generated converter:

```fsharp
type internal Wrapper_System_GuidConverter() = ...
```

Generated JsonTypeInfo:

```fsharp
let wrapper_System_GuidJsonTypeInfo options = ...
```

---

## 🧪 **Test cases Claude should verify**

### Valid after fix:
- `Wrapper<System.Guid>`
- `Wrapper<System.DateTime>`
- `Wrapper<System.Uri>`
- `Wrapper<System.Collections.Generic.List<int>>`
- `Wrapper<Wrapper<System.Guid>>`
- `Wrapper<Wrapper<Wrapper<System.Guid>>>`

### Still invalid (by design):
- Using unmarked generic definitions  
- Using non‑Serde type arguments  

---

## 📌 **Important constraints**
- Do **not** use backticks in generated code.  
- Do **not** generate nested module paths.  
- Do **not** change any Serde semantics.  
- Only sanitize identifiers — no behavioral changes.  

---
