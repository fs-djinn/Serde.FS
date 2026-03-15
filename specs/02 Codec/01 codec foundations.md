# **Spec A — Codec Foundations (Serde.FS.Json.Codec)**  
**Goal:** Introduce the foundational types and structures required for the new codec system. This spec is *additive* and must not modify or break any existing System.Text.Json integration.

This spec creates the **Codec namespace**, the **JsonValue AST**, the **encoder/decoder interfaces**, the **registry**, and a **skeleton CodecBuilder**. No actual encoding logic is implemented yet.

---

# **1. Project & Namespace Requirements**

All code must be added to the existing project:

```
Serde.FS.Json
```

Under a new namespace:

```
namespace Serde.FS.Json.Codec
```

This namespace must be self‑contained and must not interfere with existing STJ code.

---

# **2. Implement `JsonValue` (deterministic JSON AST)**

Create a new file `JsonValue.fs` with the following discriminated union:

```fsharp
type JsonValue =
    | Null
    | Bool of bool
    | Number of decimal
    | String of string
    | Array of JsonValue list
    | Object of (string * JsonValue) list
```

### Requirements
- Must be immutable and deterministic.
- Must not include any additional primitives.
- Add simple helper functions for debugging (e.g., `toString`), but no full JSON serializer.

---

# **3. Define Codec Interfaces**

Create two new files:

### `IJsonEncoder.fs`
```fsharp
type IJsonEncoder<'T> =
    abstract Encode : 'T -> JsonValue
```

### `IJsonDecoder.fs`
```fsharp
type IJsonDecoder<'T> =
    abstract Decode : JsonValue -> 'T
```

### Requirements
- No default implementations.
- No constraints on `'T`.
- These interfaces must be public.

---

# **4. Implement `CodecRegistry`**

Create a new file `CodecRegistry.fs`.

### Responsibilities
- Store encoders and decoders keyed by `System.Type`.
- Provide type‑safe retrieval helpers.
- Provide a factory method for creating a registry from a list of types (empty for now).

### Required API
```fsharp
type CodecRegistry =
    new : unit -> CodecRegistry

    member Add<'T> :
        encoder : IJsonEncoder<'T> *
        decoder : IJsonDecoder<'T> -> unit

    member TryGetEncoder<'T> : unit -> IJsonEncoder<'T> option
    member TryGetDecoder<'T> : unit -> IJsonDecoder<'T> option

    static member Create : types : System.Type list -> CodecRegistry
```

### Implementation Notes
- Use two dictionaries internally:
  - `Dictionary<Type, obj>` for encoders
  - `Dictionary<Type, obj>` for decoders
- Store boxed interface instances.
- Retrieval must unbox safely using `:?` pattern.
- `Create` should return an empty registry for now (CodecBuilder will populate it later).

---

# **5. Implement `CodecBuilder` (skeleton only)**

Create a new file `CodecBuilder.fs`.

### Responsibilities
- Provide the structure for building codecs from Serde metadata.
- No actual encoding logic is implemented in this spec.

### Required API
```fsharp
type CodecBuilder =
    static member BuildCodec<'T> :
        metadata : Serde.FS.TypeModel.TypeInfo ->
        IJsonEncoder<'T> * IJsonDecoder<'T>

    static member BuildAll :
        types : System.Type list ->
        CodecRegistry
```

### Implementation Notes
- `BuildCodec` should throw `NotImplementedException` for now.
- `BuildAll` should:
  - create a new `CodecRegistry`
  - iterate the types
  - call `BuildCodec` for each
  - add them to the registry
- No actual codec generation logic yet.

---

# **6. Testing Requirements**

Add a new test file in the Serde.FS.Json test project:

- Verify that `JsonValue` constructs correctly.
- Verify that `CodecRegistry` can:
  - add encoders/decoders
  - retrieve them
  - return `None` for missing types
- No tests for CodecBuilder logic yet.

---

# **7. Non‑Goals for This Spec**

This spec must **not** implement:

- primitive codecs  
- record codecs  
- DU codecs  
- collection codecs  
- metadata walking  
- JSON serialization  
- STJ integration changes  

Those will be implemented in later specs.

---

# **8. Acceptance Criteria**

This spec is complete when:

- The `Serde.FS.Json.Codec` namespace exists.
- `JsonValue`, `IJsonEncoder`, `IJsonDecoder`, `CodecRegistry`, and `CodecBuilder` compile.
- `CodecBuilder` contains only scaffolding.
- Tests for the registry and JsonValue pass.
- No existing code is modified or broken.
- The new namespace is fully additive.

---
