### 📘 Serde.FS.STJ — Nested Record Support & Fully Qualified Type Usage

#### 🎯 Goal

Make `Serde.FS.STJ` correctly handle **records that contain other records** as fields, by:

- Emitting correct property types for nested records
- Reusing the existing **fully qualified type (FQ)** logic
- Ensuring generated code compiles without name collisions or missing types

This is a backend feature spec building on:

- `TypeKind.Record`
- `SerdeTypeInfo` + `SerdeFieldInfo`
- Existing FQ type resolution logic

---

### 1. Problem Summary

Currently:

- A record with a record field, e.g.:

  ```fsharp
  type Address = { Street: string }
  type User = { Name: string; Address: Address }
  ```

- Generates STJ code where the `Address` property type is **not** using the existing FQ logic.
- This causes:
  - unresolved type references
  - or incorrect type references (wrong namespace/module)
  - build breakage

The core issue: **nested record fields are not using the same FQ type resolution as top‑level types.**

---

### 2. Desired Behavior

For any record field whose type is another record (or any non‑primitive, non‑collection type):

- The generated property type must use the **same fully qualified type resolution** as if that type were being emitted at the top level.

Example:

```fsharp
namespace My.App

type Address = { Street: string }
type User = { Name: string; Address: Address }
```

Generated C# (conceptually):

```csharp
public sealed class User
{
    public string Name { get; set; }
    public My.App.Address Address { get; set; }
}
```

Not just `Address`, but `My.App.Address` (or whatever the existing FQ logic resolves to).

---

### 3. Design: Reuse Existing FQ Type Logic

There is already logic somewhere in the STJ emitter (or shared emitter code) that:

- Given a `SerdeTypeInfo` (or structural `TypeInfo`), computes:
  - the correct **fully qualified type name** for that type in generated C#

This logic is currently used for:

- top‑level type declarations
- maybe some property types

#### Requirement

**All field types—including nested records—must use this same FQ resolution path.**

Concretely:

- Introduce or reuse a helper:

  ```fsharp
  val getFqTypeName : TypeInfo -> string
  ```

- For every `SerdeFieldInfo`, when emitting the property:

  ```fsharp
  let fieldTypeName = getFqTypeName field.Type
  ```

- Use `fieldTypeName` as the property type in generated C#.

This must apply to:

- record fields
- union case fields (future)
- nested option/list/array/set/map element types (where relevant)

---

### 4. Nested Record Handling Rules

When `serdeTypeInfo.Raw.Kind` is `Record fields`:

- For each `SerdeFieldInfo` `f`:
  - Determine the field type via `f.Type.Kind`:
    - If primitive → use existing primitive mapping
    - If option/list/array/set/map → use existing collection logic
    - If record/anonymous record/union/opaque → use `getFqTypeName f.Type`
  - Emit the property using that resolved type name.

No special casing is needed for “nested” vs “top‑level” records—**all records are treated uniformly** via FQ resolution.

---

### 5. Tests

Add tests that:

#### 5.1 Simple nested record

```fsharp
type Address = { Street: string }
type User = { Name: string; Address: Address }
```

- Generated C# for `User` has:

  ```csharp
  public My.App.Address Address { get; set; }
  ```

(or equivalent FQ name based on namespace/module).

#### 5.2 Nested record in another namespace/module

```fsharp
namespace Outer

module Inner =
    type Address = { Street: string }

type User = { Address: Inner.Address }
```

- Generated C# uses the correct FQ name for `Inner.Address`.

#### 5.3 Record inside option/list

```fsharp
type Address = { Street: string }
type User = { Addresses: Address option list }
```

- Generated C# uses the correct FQ type for `Address` inside the collection handling.

---

### 6. Out of Scope

This spec does **not** change:

- union handling
- tuple handling
- strict mode behavior
- attribute semantics
- file cleanup

It only ensures:

- nested record fields use the same FQ type resolution as top‑level types
- generated code compiles when records reference other records

---
