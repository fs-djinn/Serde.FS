# 📘 **Serde.FS Runtime Contract: Missing Root‑Level Generic Type Argument on `Deserialize`**

## 1. **Purpose**

This runtime error exists to enforce a core Serde.FS invariant:

> **Generic wrapper types must be deserialized with an explicit closed generic type argument.**

When a user calls:

```fsharp
Serde.Deserialize json
```

without specifying `<T>`, F# infers the return type as `obj`.  
This is legal at compile time, but **invalid at runtime** when the JSON root clearly represents a generic wrapper type.

The goal of this spec is to replace the unhelpful:

```
Missing metadata for type 'System.Object'
```

with a clear, intention‑revealing error.

---

# 2. **When the Error Should Trigger**

The specialized runtime error must be thrown when **all** of the following are true:

### **Condition A — The JSON root represents a wrapper DU**
Using the hybrid encoding:

- JSON root is an object with **exactly one property**, and  
- that property name matches a known Serde‑generated wrapper DU case name.

### **Condition B — The wrapper DU is generic**
Example:

```fsharp
type Wrapper<'T> = Wrapper of 'T
```

### **Condition C — The user did not specify `<T>`**
This is detected when the inferred runtime type is:

- `typeof<obj>`  
- OR a non‑generic type  
- OR a generic type with zero type arguments  
- OR a generic type definition (open generic)

### **Condition D — Metadata exists for the wrapper DU type definition**
Meaning: Serde.FS knows about `Wrapper<_>` at compile time.

If all four conditions are met → throw the specialized error.

---

# 3. **How to Detect Wrapper vs Multi‑Case DU at Runtime**

### **Wrapper DU detection (hybrid encoding)**

A wrapper DU is encoded as:

```json
{ "<CaseName>": <payload> }
```

Runtime detection:

```fsharp
let root = JsonDocument.Parse(json).RootElement
if root.ValueKind = Object then
    let props = root.EnumerateObject()
    if props.MoveNext() && not props.MoveNext() then
        // exactly one property → wrapper encoding
        let caseName = firstProperty.Name
```

### **Multi‑case DU detection**

Multi‑case DUs use:

```json
{ "Case": "...", "Fields": [...] }
```

This spec does **not** apply to multi‑case DUs.

---

# 4. **How to Detect Missing `<T>`**

The runtime receives:

```fsharp
Deserialize<'a>(json, runtimeType = typeof<'a>)
```

If the user calls `Deserialize json` without `<T>`, F# infers:

```fsharp
'a = obj
runtimeType = typeof<obj>
```

The runtime must treat the following as “missing generic argument”:

- `runtimeType = typeof<obj>`
- `runtimeType.IsGenericType = false`
- `runtimeType.IsGenericTypeDefinition = true`
- `runtimeType.GetGenericArguments().Length = 0`

If the JSON root is a wrapper DU AND the runtime type is too generic → throw.

---

# 5. **Error Message (exact text)**

Throw `SerdeMissingMetadataException` with this message:

```
Serde.FS: Cannot deserialize a generic wrapper type without specifying the closed generic type.

The JSON represents a value of type '<Wrapper<_>>'.
You must call Deserialize<Wrapper<ConcreteType>> to deserialize this value.
```

Where `<Wrapper>` is replaced with the actual wrapper case name.

---

# 6. **Implementation Details**

### **Location**

Modify:

```
src/Serde.FS.Json/JsonBackend.fs
```

Inside the `Deserialize` method, **before** calling:

```fsharp
SerdeMetadata.get runtimeType
```

### **Steps**

1. Parse JSON root.
2. If root is a wrapper DU (single property object):
   - Extract `caseName`.
3. Look up metadata for a wrapper DU whose case name matches `caseName`.
4. If the DU is generic:
   - Check if `runtimeType` is too generic (see Section 4).
5. If so:
   - Throw the specialized error.

If any step fails (e.g., JSON not wrapper‑shaped), fall back to normal metadata lookup.

---

# 7. **Test Cases**

Add tests in:

```
src/Serde.FS.Json.Tests/SerdeTests.fs
```

### **Test 1 — Serialize<T>, Deserialize<T>**
Should succeed.

### **Test 2 — Serialize missing <T>, Deserialize<T>**
Compile‑time error (unchanged).

### **Test 3 — Serialize<T>, Deserialize missing <T>**
JSON:

```json
{ "Wrapper": { "Name": "Jordan" } }
```

Call:

```fsharp
Deserialize<obj>(json, typeof<obj>, None)
```

Expect:

- `SerdeMissingMetadataException`
- Message contains “Cannot deserialize a generic wrapper type”

### **Test 4 — Both missing <T>**
Same JSON, same expectation.

---
