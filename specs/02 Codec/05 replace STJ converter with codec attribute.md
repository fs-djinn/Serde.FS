### Spec E — Remove STJ Converters + Introduce Codec Attribute

---

## 1. Goals

- **Remove** System.Text.Json converter usage from the Serde.FS public surface.
- **Introduce** Serde‑native codec configuration at:
  - **type level** via `Serde(Codec = ...)`
  - **field level** via `SerdeField(Codec = ...)`
- **Route** all codec overrides through the Serde codec system and JsonTypeInfo pipeline.
- **Preserve** STJ only as the final backend engine (JsonTypeInfo + JsonSerializer).

---

## 2. Attribute model changes

### 2.1 `SerdeAttribute`

Current:

```fsharp
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct ||| AttributeTargets.Interface ||| AttributeTargets.Enum, AllowMultiple = false)>]
type SerdeAttribute() =
    inherit Attribute()
    member val Converter : obj = null with get, set
```

Changes:

1. **Deprecate and ignore `Converter`**:

```fsharp
[<Obsolete("System.Text.Json converters are no longer supported. Use Serde(Codec = typeof(MyCodec)) instead.")>]
member val Converter : obj = null with get, set
```

- Generator must **ignore** `Converter` completely (no STJ converter wiring).

2. **Add `Codec` property**:

```fsharp
member val Codec : Type = null with get, set
```

Semantics:

- Applied at **type level** only.
- When non‑null, indicates a **type‑level codec override** implementing `IJsonCodec<'T>`.

Example:

```fsharp
[<Serde(Codec = typeof<MyTypeCodec>)>]
type FancyName = { Value : string }
```

---

### 2.2 New `SerdeFieldAttribute`

Add:

```fsharp
[<AttributeUsage(AttributeTargets.Field ||| AttributeTargets.Property, AllowMultiple = false)>]
type SerdeFieldAttribute() =
    inherit Attribute()
    member val Codec : Type = null with get, set
```

Semantics:

- Applied at **field/property level**.
- When `Codec` is non‑null, indicates a **property‑level codec override**.

Example:

```fsharp
type Person =
    { [<SerdeField(Codec = typeof<UppercaseCodec>)>]
      Name : string }
```

Existing field attributes (`SerdeRenameAttribute`, `SerdeSkipAttribute`, etc.) remain unchanged and can coexist with `SerdeField`.

---

## 3. Generator behavior

### 3.1 Type‑level codec (`Serde(Codec = ...)`)

For a type `'T` with:

```fsharp
[<Serde(Codec = typeof<MyCodec>)>]
type T = ...
```

The generator must:

1. **Validate**:

   - `MyCodec` must implement `IJsonCodec<'T>`.
   - `MyCodec` must have a public parameterless constructor.

   On violation: **compile‑time error** with clear message.

2. **Emit internal metadata**:

   - Attach an internal `JsonCodecAttribute(typeof(MyCodec))` (or equivalent) to the generated metadata for `'T`.

3. **Do not emit any STJ converter wiring**.

### 3.2 Field‑level codec (`SerdeField(Codec = ...)`)

For a field/property `p : 'P` with:

```fsharp
[<SerdeField(Codec = typeof<MyCodec>)>]
val p : 'P
```

The generator must:

1. **Validate**:

   - `MyCodec` must implement `IJsonCodec<'P>`.
   - `MyCodec` must have a public parameterless constructor.

   On violation: **compile‑time error**.

2. **Emit internal metadata**:

   - Attach an internal `JsonCodecAttribute(typeof(MyCodec))` (or equivalent) to the field/property metadata.

3. Preserve any other field attributes (`SerdeRename`, `SerdeSkip`, etc.) and combine them as usual.

---

## 4. JsonTypeInfo pipeline integration (Spec D hook)

JsonTypeInfoBuilder (from Spec D) must be updated to:

### 4.1 Type‑level codec precedence

When building `JsonTypeInfo<'T>`:

1. Check for internal `JsonCodecAttribute` at **type level**.
2. If present:
   - Instantiate codec:

     ```fsharp
     let codec = Activator.CreateInstance(codecType) :?> IJsonCodec<'T>
     ```

   - Adapt codec to `JsonTypeInfo<'T>` using existing adapter.
   - **Return** this `JsonTypeInfo<'T>` as the final metadata for `'T`.
   - Optionally register codec in the registry (if Spec C requires it).

3. If absent, fall back to:
   - property‑level overrides
   - generated metadata
   - registry
   - primitives

### 4.2 Property‑level codec precedence

When constructing property metadata:

1. For each property, check for internal `JsonCodecAttribute` at **property level**.
2. If present:
   - Instantiate codec:

     ```fsharp
     let codec = Activator.CreateInstance(codecType) :?> IJsonCodec<'P>
     ```

   - Adapt codec to property’s `JsonPropertyInfo`.
   - Override any default/generic handling for that property.

3. Precedence:

   - **Property codec > Type codec > Generated metadata > Registry > Primitives**

---

## 5. Error handling

### 5.1 Compile‑time errors (generator)

Emit compile‑time errors for:

- Codec type does not implement `IJsonCodec<'T>` / `IJsonCodec<'P>`.
- Codec type has no public parameterless constructor.

Messages should explicitly mention:

- the type/property
- the codec type
- the required interface
- the constructor requirement

### 5.2 Runtime errors (instantiation failures)

If `Activator.CreateInstance` fails at runtime (e.g., exception in constructor):

- Throw a `SerdeCodecException` (or similar) with:
  - codec type
  - target type/property
  - inner exception

---

## 6. STJ converter removal

### 6.1 Public surface

- `SerdeAttribute.Converter`:
  - Marked `[<Obsolete>]`.
  - Ignored by the generator.
  - No STJ `JsonConverter` is ever wired from it.

### 6.2 Internal behavior

- JsonTypeInfoBuilder must **not**:
  - inspect `Converter`
  - attach STJ converters
  - use STJ converter metadata as an extension point

After Spec E, **the only extension point is the codec system**.

---

## 7. Documentation updates

- Replace all examples using `Converter = typeof<...>` with:
  - `Serde(Codec = typeof<...>)` for type‑level
  - `SerdeField(Codec = typeof<...>)` for property‑level
- Add a “Custom codecs” section showing:
  - implementing `IJsonCodec<'T>`
  - using type‑level and property‑level attributes
  - interaction with registry and primitives
- Add a short “Why converters are deprecated” note:
  - determinism
  - backend‑agnostic design
  - Fable compatibility
  - unified extension point

---
