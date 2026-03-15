# **Spec B — Primitive Codecs (Object Expression Implementation)**  
**Goal:** Implement JSON codecs for core primitive types using **F# object expressions**, and register them as singletons. Integrate them into `CodecRegistry` and `CodecBuilder`.

All work remains inside:

```
namespace Serde.FS.Json.Codec
```

---

# **1. Types to support in this spec**

Implement codecs for:

- `bool`
- `string`
- `decimal`
- `int`
- `float`
- `byte[]` (Base64)
- `unit`

These will be implemented as **singleton object expressions**, not classes.

---

# **2. Primitive codec implementations (object expressions)**

Create a new file:

```
PrimitiveCodecs.fs
```

Inside it, define a module:

```fsharp
module PrimitiveCodecs =
    let boolEncoder : IJsonEncoder<bool> =
        { new IJsonEncoder<bool> with
            member _.Encode v = JsonValue.Bool v }

    let boolDecoder : IJsonDecoder<bool> =
        { new IJsonDecoder<bool> with
            member _.Decode json =
                match json with
                | JsonValue.Bool b -> b
                | _ -> failwith "Expected JSON bool" }
```

Follow this pattern for:

### ✔ string  
`JsonValue.String`

### ✔ decimal  
`JsonValue.Number`

### ✔ int  
`JsonValue.Number (decimal value)`

### ✔ float  
`JsonValue.Number (decimal value)`

### ✔ unit  
Encode → `JsonValue.Null`  
Decode → `()` for `JsonValue.Null`

### ✔ byte[]  
Encode:
```fsharp
JsonValue.String (Convert.ToBase64String bytes)
```

Decode:
```fsharp
Convert.FromBase64String base64
```

### Requirements
- **All primitive codecs must be implemented as object expressions.**
- **All primitive codecs must be singletons (module-level `let` bindings).**
- No named classes or types should be created.

---

# **3. Register primitives in CodecRegistry**

Extend `CodecRegistry` with:

```fsharp
static member WithPrimitives() : CodecRegistry
```

### Behavior

- Create a new registry.
- Register all primitive encoders/decoders from `PrimitiveCodecs`.
- Return the populated registry.

This ensures every codec registry starts with primitive support.

---

# **4. Integrate primitives into CodecBuilder.BuildAll**

Modify `CodecBuilder.BuildAll`:

- Start with `CodecRegistry.WithPrimitives()`.
- For now, skip generating codecs for user-provided types (until later specs).
- Return the registry containing only primitive codecs.

This gives you a working registry immediately.

---

# **5. Tests for primitive codecs**

Add tests verifying:

### Encoding
- `true` → `JsonValue.Bool true`
- `"abc"` → `JsonValue.String "abc"`
- `42` → `JsonValue.Number 42M`
- `1.5` → `JsonValue.Number 1.5M`
- `()` → `JsonValue.Null`
- `[|1uy;2uy;3uy|]` → Base64 string

### Decoding
- Round-trip for all primitives.

### Registry
- `CodecRegistry.WithPrimitives()` returns encoders/decoders for all primitives.
- Missing types return `None`.

---

# **6. Non-goals for this spec**

Do **not** implement:

- record codecs  
- DU codecs  
- collection codecs  
- metadata walking  
- STJ integration changes  

Those come later.

---

# **7. Acceptance criteria**

Spec B is complete when:

- All primitive codecs exist as **object expression singletons**.
- `CodecRegistry.WithPrimitives()` registers them.
- `CodecBuilder.BuildAll` uses `WithPrimitives()`.
- All primitive round-trip tests pass.
- No existing Serde.FS.Json behavior is changed.

---
