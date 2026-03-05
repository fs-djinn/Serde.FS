# Implementation Spec: Generic Type Support in Serde.FS

Target: Work against `FSharp.SourceDjinn` / `TypeModel` v0.2.0.

## 1. High‑level behavior

Serde.FS should:

- Treat `[<Serde>]` generic types as **Serde definitions**.
- Generate serializers for **closed constructed types** (e.g., `Wrapper<Person>`).
- Use Djinn’s generic model:
  - `TypeInfo.GenericParameters`
  - `TypeInfo.GenericArguments`
  - `TypeInfo.instantiate`
  - `IsGenericDefinition` / `IsConstructedGeneric`

Non‑generic behavior remains unchanged.

---

## 2. Where to make changes

### 2.1 Discovery / analysis phase

Touch points (names approximate, adjust to actual code):

- `Serde.FS.SourceGen.TypeDiscovery`
- `Serde.FS.SourceGen.MetadataBuilder`
- Any module that:
  - Walks `TypeInfo` from Djinn
  - Filters by `[<Serde>]`
  - Builds internal `SerdeTypeInfo` / `SerdeRecord` / `SerdeUnion`

Add generic awareness here.

---

## 3. Internal metadata model changes

Assume you have something like:

```fsharp
type SerdeTypeInfo =
    | Record of SerdeRecord
    | Union of SerdeUnion
    // ...
```

### 3.1 Add generic context

Extend your core metadata type(s) to carry generic info:

```fsharp
type SerdeGenericContext =
    {
        DefinitionType: TypeInfo
        GenericParameters: GenericParameterInfo list
        GenericArguments: TypeInfo list
    }

type SerdeRecord =
    {
        Type: TypeInfo
        Fields: SerdeField list
        GenericContext: SerdeGenericContext option
        // existing fields...
    }

type SerdeUnion =
    {
        Type: TypeInfo
        Cases: SerdeCase list
        GenericContext: SerdeGenericContext option
        // existing fields...
    }
```

Rules:

- **Non‑generic Serde types:** `GenericContext = None`.
- **Generic definitions:** `GenericContext = Some` with:
  - `DefinitionType = definition TypeInfo`
  - `GenericParameters = definition.GenericParameters`
  - `GenericArguments = []`
- **Constructed generics:** `GenericContext = Some` with:
  - `DefinitionType = definition TypeInfo`
  - `GenericParameters = definition.GenericParameters`
  - `GenericArguments = constructed.GenericArguments`

---

## 4. Discovery logic

### 4.1 Identify Serde definitions

In your “collect Serde types” phase:

1. For each `TypeInfo` from Djinn:
   - Check for `[<Serde>]` attribute (existing logic).
   - If present:
     - If `ti.IsGenericDefinition`:
       - Register as **generic Serde definition**.
     - Else:
       - Register as **non‑generic Serde definition** (existing behavior).

You’ll likely want a map:

```fsharp
// key: (namespace, name, arity)
type SerdeDefinitionKey = string option * string * int

let definitionKey (ti: TypeInfo) =
    let arity = ti.GenericParameters.Length
    (ti.Namespace, ti.Name, arity)

let serdeDefinitions : Map<SerdeDefinitionKey, TypeInfo> = ...
```

### 4.2 Identify types that need serializers

You probably already have a “root set” of types that need serializers (Serde types + their transitive dependencies).

Extend that logic to:

- Include **constructed generics** encountered in fields / union cases.
- For each constructed generic, ensure its **definition** is in `serdeDefinitions`.

---

## 5. Matching constructed generics to definitions

Add a helper:

```fsharp
let tryFindDefinitionForConstructed
    (definitions: Map<SerdeDefinitionKey, TypeInfo>)
    (constructed: TypeInfo)
    : (TypeInfo * TypeInfo list) option =
    
    if not constructed.IsConstructedGeneric then None
    else
        let key =
            let arity = constructed.GenericArguments.Length
            (constructed.Namespace, constructed.Name, arity)
        match Map.tryFind key definitions with
        | None -> None
        | Some def -> Some (def, constructed.GenericArguments)
```

Usage:

- If `None` → error: “Constructed generic X<...> has no Serde definition.”
- If `Some (def, args)` → proceed to instantiation.

---

## 6. Instantiation and concrete shape

### 6.1 Use `TypeInfo.instantiate`

When you have `(definition, args)`:

```fsharp
let instantiated : TypeInfo =
    TypeInfo.instantiate definition args
```

Invariants (from Djinn):

- `instantiated.GenericParameters = []`
- `instantiated.GenericArguments = args`
- All fields / nested types are concrete (no `GenericParameter`).

### 6.2 Build Serde metadata from instantiated shape

Where you currently build `SerdeRecord` / `SerdeUnion` from a `TypeInfo`:

- For **non‑generic types**: keep existing behavior.
- For **constructed generics**:
  - Use `instantiated` as the `Type` for structural inspection (fields, cases).
  - Attach a `GenericContext` that remembers:
    - `DefinitionType = definition`
    - `GenericParameters = definition.GenericParameters`
    - `GenericArguments = args`

This lets codegen know:

- The concrete shape (from `instantiated`).
- The generic template (from `definition`).
- The mapping `'T -> Person`, etc.

---

## 7. Codegen changes

### 7.1 Serializer naming

To avoid collisions, serializers for constructed generics should have names that encode type arguments.

Example strategy:

```fsharp
// Pseudocode
let serializerName (ti: TypeInfo) =
    if ti.IsConstructedGeneric then
        // e.g., Wrapper_Person, Wrapper_Person_Address, etc.
        let argNames = ti.GenericArguments |> List.map (fun a -> a.Name) |> String.concat "_"
        $"{ti.Name}_{argNames}_Serde"
    else
        $"{ti.Name}_Serde"
```

You can refine this later (e.g., use fully qualified names, hashing, etc.), but this is enough to start.

### 7.2 Generation pipeline

Where you currently:

1. Build `SerdeTypeInfo` from `TypeInfo`.
2. Emit serializers/deserializers.

Extend to:

- Accept `SerdeTypeInfo` that may have `GenericContext`.
- For constructed generics:
  - Use the **instantiated** `TypeInfo` for field/case traversal.
  - Use `GenericContext` only for naming / error messages / future features.

### 7.3 Caching

Maintain a cache keyed by the **instantiated TypeInfo** or a stable key:

```fsharp
type SerdeKey = string // e.g., fully qualified name + generic args

let serdeKey (ti: TypeInfo) : SerdeKey = ...

let generated : HashSet<SerdeKey> = HashSet()

let ensureSerializerGenerated ti =
    let key = serdeKey ti
    if not (generated.Contains key) then
        generated.Add key |> ignore
        // run codegen for ti
```

This prevents duplicate serializers for the same closed generic.

---

## 8. Error handling

### 8.1 Constructed generic without Serde definition

Example:

```fsharp
type Wrapper<'T> = Wrapper of 'T

[<Serde>]
type Person = { Name: string }

let value : Wrapper<Person> = ...
```

Error:

- “Type `Wrapper<Person>` is used in serialization, but `Wrapper<'T>` is not marked with `[<Serde>]`.”

### 8.2 Generic Serde type instantiated with non‑Serde type

Example:

```fsharp
[<Serde>]
type Wrapper<'T> = Wrapper of 'T

type NotSerde = { X: int }

let value : Wrapper<NotSerde> = ...
```

Error:

- “Type `Wrapper<NotSerde>` is used in serialization, but `NotSerde` does not have Serde metadata.”

Implementation:

- During traversal of `instantiated` TypeInfo, when you hit a field type that is not:
  - A primitive / built‑in supported type, or
  - A Serde type (definition or constructed),
- Emit a diagnostic with both the constructed generic and the missing type.

---

## 9. Tests

Add tests under something like `Serde.FS.Tests/Generics.fs`.

### 9.1 Basic generic record

```fsharp
[<Serde>]
type Wrapper<'T> = Wrapper of 'T

[<Serde>]
type Person = { Name: string }

let value = Wrapper { Name = "Jordan" }
```

- Round‑trip `Wrapper<Person>`.

### 9.2 Nested generics

```fsharp
[<Serde>]
type Wrapper<'T> = Wrapper of 'T

[<Serde>]
type Person = { Name: string }

let value = Wrapper (Wrapper { Name = "Jordan" })
```

- Round‑trip `Wrapper<Wrapper<Person>>`.

### 9.3 Non‑Serde argument error

```fsharp
[<Serde>]
type Wrapper<'T> = Wrapper of 'T

type NotSerde = { X: int }

let value = Wrapper { X = 1 }
```

- Assert diagnostic mentions `Wrapper<NotSerde>` and `NotSerde`.

### 9.4 Non‑Serde generic definition error

```fsharp
type Wrapper<'T> = Wrapper of 'T

[<Serde>]
type Person = { Name: string }

let value = Wrapper { Name = "Jordan" }
```

- Assert diagnostic mentions missing `[<Serde>]` on `Wrapper<'T>`.

---
