# 📘 **Spec: Remove JSON Resolver Emission When Backend = STJ**

## 🎯 **Goal**
Ensure that when the SystemTextJson backend is active, the generator **does not emit any JSON resolver files**, specifically:

- `~SerdeResolver.serde.g.fs`
- `~SerdeResolverRegistration.djinn.g.fs`
- any file referencing `SerdeJsonResolver`
- any `.djinn.g.fs` file *except* the universal Djinn entry point

This prevents STJ builds from failing due to undefined JSON symbols and ensures clean backend separation.

---

# 🧩 **Background**

The generator currently emits **both**:

### JSON backend artifacts:
- `~SerdeResolver.serde.g.fs`
- `~SerdeResolverRegistration.djinn.g.fs`
- JSON registry bootstrap
- JSON resolver registration

### STJ backend artifacts:
- `*.serde.g.fs` metadata files
- STJ metadata emitters

### Djinn entry point:
- `~~EntryPoint.djinn.g.fs` (backend‑agnostic, should always be emitted)

Because the JSON resolver files are still emitted during STJ builds, the compiler fails with:

```
The value, namespace, type or module 'SerdeJsonResolver' is not defined.
```

---

# 🛠️ **Required Changes**

## 1. **Add backend‑aware branching in the GeneratorHost**

Modify the host so that resolver emission depends on the backend:

```fsharp
match backend with
| Backend.Json ->
    JsonResolverEmitter.emitResolver(...)
    JsonResolverEmitter.emitRegistration(...)
| Backend.Stj ->
    StjResolverEmitter.emitResolver(...)
    StjResolverEmitter.emitBootstrap(...)
```

### ❗ Important:
- JSON resolver files must **not** be emitted when backend = STJ.
- STJ resolver files must **not** be emitted when backend = JSON.

---

## 2. **Always emit the Djinn entry point**

This file:

```
~~EntryPoint.djinn.g.fs
```

is backend‑agnostic and should always be generated.

Do **not** remove or condition this file.

---

## 3. **Suppress JSON resolver files for STJ**

Explicitly prevent emission of:

- `~SerdeResolver.serde.g.fs`
- `~SerdeResolverRegistration.djinn.g.fs`
- any file referencing:
  - `SerdeJsonResolver`
  - `SerdeJsonResolverRegistry`
  - JSON registry bootstrap

These must only appear when backend = JSON.

---

## 4. **Introduce STJ resolver + bootstrap files**

Add two new STJ‑specific files:

### `~SerdeStjResolver.g.fs`
Contains:

- a module that registers all generated `JsonTypeInfo` factories
- a function like:

```fsharp
let registerAll (options: JsonSerializerOptions) =
    options.TypeInfoResolverChain.Add(AddressSerdeStjTypeInfo.addressStjTypeInfo)
    options.TypeInfoResolverChain.Add(WrapperSerdeStjTypeInfo.wrapperStjTypeInfo)
```

### `~SerdeStjBootstrap.g.fs`
Contains:

```fsharp
module Bootstrap =
    let init () =
        Serde.ResolverBootstrap.registerAll <- Some Serde.Generated.Stj.ResolverRegistration.registerAll
```

This mirrors the JSON bootstrap but uses STJ resolver logic.

---

## 5. **Ensure STJ resolver files are written into the STJ output folder**

Write STJ resolver files into:

```
obj/.../serde-stj-generated/
```

and JSON resolver files into:

```
obj/.../serde-json-generated/
```

No cross‑contamination.

---

# 🧪 **Acceptance Criteria**

### ✔ STJ build produces:
- `Address.serde.g.fs`
- `Wrapper.serde.g.fs`
- `~SerdeStjResolver.g.fs`
- `~SerdeStjBootstrap.g.fs`
- `~~EntryPoint.djinn.g.fs`

### ✔ STJ build does **not** produce:
- `~SerdeResolver.serde.g.fs`
- `~SerdeResolverRegistration.djinn.g.fs`
- any file referencing `SerdeJsonResolver`

### ✔ JSON build continues to produce:
- JSON resolver files
- JSON bootstrap
- Djinn entry point

### ✔ SampleApp builds cleanly with both backends referenced  
(no undefined symbol errors)

---
