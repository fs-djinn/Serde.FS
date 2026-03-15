### Spec F — Codec‑Driven Serialization & Deserialization

---

## 1. Goals

- Make **codecs the actual engine** for JSON serialization/deserialization.
- Keep **System.Text.Json** only as:
  - UTF‑8 reader/writer
  - optional integration via `JsonTypeInfo`
- Provide **Serde‑native** `serialize`/`deserialize` APIs that:
  - use the codec registry
  - respect type/field codec overrides
  - are deterministic and reflection‑free.

---

## 2. Public API surface

Add a new module in `Serde.FS.Json` (name can be refined, but assume `SerdeJson`):

```fsharp
module SerdeJson =
    val serialize<'T> : 'T -> string
    val serializeToUtf8<'T> : 'T -> byte[]
    val deserialize<'T> : string -> 'T
    val deserializeFromUtf8<'T> : byte[] -> 'T
```

Semantics:

- All four functions are **codec‑driven**.
- They **do not** use `JsonSerializer.Serialize/Deserialize` for behavior.
- They **do not** use STJ converters or reflection.

---

## 3. Serialization pipeline

### 3.1 High‑level flow

For `serialize<'T> value`:

1. **Resolve codec** for `'T`:
   - property‑level `SerdeField(Codec = ...)`
   - type‑level `Serde(Codec = ...)`
   - registry
   - primitives
   - generated codecs

2. **Encode to `JsonValue`**:
   - Call `IJsonEncoder<'T>.Encode value` → `JsonValue`.

3. **Write JSON**:
   - Use `Utf8JsonWriter` (or equivalent) to emit JSON from `JsonValue`.
   - `serialize` → string (via `Encoding.UTF8.GetString`).
   - `serializeToUtf8` → raw `byte[]`.

### 3.2 JsonValue → Utf8JsonWriter mapping

Implement a function:

```fsharp
val writeJsonValue : Utf8JsonWriter -> JsonValue -> unit
```

Behavior:

- `Null` → `writer.WriteNullValue()`
- `Bool b` → `writer.WriteBooleanValue(b)`
- `Number n` → write as JSON number (respect existing numeric policy)
- `String s` → `writer.WriteStringValue(s)`
- `Array xs` → `writer.WriteStartArray()` / recurse / `WriteEndArray()`
- `Object props` → `writer.WriteStartObject()` / write name + value / `WriteEndObject()`

No STJ converters, no reflection.

---

## 4. Deserialization pipeline

### 4.1 High‑level flow

For `deserialize<'T> json`:

1. **Parse JSON into `JsonValue`**:
   - Use `Utf8JsonReader` (or `JsonDocument`) to build a `JsonValue` tree.
   - This is a mechanical JSON→`JsonValue` parser.

2. **Resolve codec** for `'T` (same precedence as serialization).

3. **Decode**:
   - Call `IJsonDecoder<'T>.Decode jsonValue` → `'T`.

### 4.2 Utf8JsonReader → JsonValue mapping

Implement a function:

```fsharp
val readJsonValue : Utf8JsonReader byref -> JsonValue
```

(or via `JsonDocument` if simpler initially).

Behavior:

- Parse full JSON payload into a `JsonValue` tree.
- Support all JSON types (null, bool, number, string, array, object).
- No STJ converters, no `JsonTypeInfo`.

---

## 5. Codec resolution rules

For both serialize and deserialize, codec resolution must follow:

1. **Property‑level codec** (`SerdeField(Codec = ...)`)  
2. **Type‑level codec** (`Serde(Codec = ...)`)  
3. **Registry** (manually registered codecs)  
4. **Generated codecs** (from Serde generator)  
5. **Primitive codecs** (bool, string, int, DateTime, etc.)

If no codec is found:

- Throw a clear exception (e.g., `SerdeCodecNotFoundException` with type info).

---

## 6. Interaction with JsonTypeInfo / STJ

- `SerdeJson.serialize` / `deserialize` **must not**:
  - use `JsonSerializer.Serialize/Deserialize`
  - use `JsonTypeInfo<'T>`
  - use STJ converters

- `JsonTypeInfoBuilder` from Spec D remains for:
  - integration with external STJ consumers
  - compatibility scenarios

But **SerdeJson** is the preferred, Serde‑native path.

---

## 7. Error behavior

- **Codec resolution failure**:
  - Throw `SerdeCodecNotFoundException` (or similar) with:
    - target type
    - hint about missing Serde attributes or registration.

- **Decode failure**:
  - `IJsonDecoder<'T>.Decode` should already return/throw according to existing design.
  - Wrap or propagate with clear context (path if available).

- **JSON parse failure**:
  - Throw a Serde‑specific exception wrapping STJ parse errors, not raw STJ exceptions.

---

## 8. Documentation updates

- Introduce `SerdeJson.serialize` / `deserialize` as the **primary API**.
- Clearly state:
  - behavior is codec‑driven
  - deterministic
  - reflection‑free
  - independent of STJ converters
- Show examples:
  - simple types
  - types with `Serde(Codec = ...)`
  - fields with `SerdeField(Codec = ...)`
- Mark STJ‑based `JsonSerializer` integration as:
  - “for interoperability/legacy only”
  - not the preferred path.

---
