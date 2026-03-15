### Spec 02 — `Serde.FS.SystemTextJson.SourceGen` (STJ emitter)

This spec creates the **System.Text.Json–specific code emitter** that consumes Serde metadata and generates STJ `IJsonTypeInfoResolver` code, targeting the API surface defined in Spec 01.

---

## 1. Project creation

**Goal:** Add a backend-specific source generation project for STJ.

- **Project name:** `Serde.FS.SystemTextJson.SourceGen`
- **Assembly name:** `Serde.FS.SystemTextJson.SourceGen`
- **Output type:** `Library`
- **Target frameworks:** same as `Serde.FS.Json.SourceGen` (likely `net8.0`)

- **Project references:**
  - `Serde.FS` (core)
  - `Serde.FS.SystemTextJson` (to align with types / namespaces if needed)
  - Any existing shared `Serde.FS.SourceGen` project that contains `SerdeGeneratorEngine` and shared metadata logic

- **NuGet references:**
  - `System.Text.Json` (same version as in `Serde.FS.SystemTextJson`)

- **No Roslyn generators.**
  - This project is a pure library used by the MSBuild task/GeneratorHost in Spec 03.

---

## 2. Core responsibility

**This project’s job:**

- Implement an STJ-specific emitter that:
  - Consumes Serde metadata via `SerdeGeneratorEngine`
  - Generates STJ `JsonTypeInfo`/`IJsonTypeInfoResolver` code
  - Produces a concrete implementation of `GeneratedSerdeStjResolver` that matches the stub created in Spec 01

**It does not:**

- Host MSBuild tasks  
- Integrate with the build directly  
- Expose public runtime APIs (that’s `Serde.FS.SystemTextJson`)

---

## 3. Reuse from `Serde.FS.Json.SourceGen`

Claude should **heavily reuse** the existing JSON emitter:

- Use `Serde.FS.Json.SourceGen` as the structural template:
  - Same pattern of:
    - walking Serde metadata
    - handling records, unions, tuples, options, lists, maps, etc.
    - applying naming, skip, rename, flatten rules
    - emitting per-type code + a central registry/resolver

- Copy the emitter structure and adapt it to STJ:
  - Replace JSON codec generation with STJ `JsonTypeInfo` generation
  - Keep the traversal logic and metadata interpretation as intact as possible
  - Keep error/warning reporting patterns

The goal is: **same traversal, different backend.**

---

## 4. Generated code shape

The emitter should generate a type that **replaces the stub** from Spec 01:

```fsharp
namespace Serde.FS.SystemTextJson

open System
open System.Text.Json.Serialization.Metadata

type GeneratedSerdeStjResolver() =
    interface IJsonTypeInfoResolver with
        member _.GetTypeInfo(t: Type, options: JsonSerializerOptions) : JsonTypeInfo =
            // generated dispatch logic
```

### 4.1 Dispatch strategy

- For each Serde-decorated type, generate a `JsonTypeInfo` factory method.
- In `GetTypeInfo`, dispatch on `t`:
  - If `t` matches a known generated type → return its `JsonTypeInfo`
  - Otherwise → return `null`

This matches STJ expectations and the `Resolver.resolver` contract from Spec 01.

---

## 5. Metadata → STJ mapping

For each supported shape, the emitter should:

- **Records / classes:**
  - Generate `JsonTypeInfo` with properties mapped to Serde fields
  - Apply:
    - rename rules
    - skip rules
    - flatten rules (where applicable)
    - nullability / option handling

- **Unions:**
  - Generate STJ metadata that matches Serde’s union tagging strategy (tag + content, etc.)
  - Ensure compatibility with how Serde.FS.Json represents unions conceptually

- **Tuples / tuple-like:**
  - Generate array-like metadata

- **Collections:**
  - Lists, arrays, maps, sets → appropriate STJ collection metadata

- **Options / nullable:**
  - Map `option<'T>` to nullable semantics consistent with Serde rules

The exact STJ APIs used (`JsonTypeInfo.CreateXxx`, `JsonTypeInfo.PropertyInfo`, etc.) should follow current STJ best practices for source-generated metadata.

---

## 6. Integration with `SerdeGeneratorEngine`

The emitter should implement the same interface used by the JSON emitter:

- Implement `Serde.FS.ISerdeCodeEmitter` (or equivalent) with methods like:
  - `EmitType` / `EmitFile` / etc. (match existing JSON emitter)
- Be consumable by:

```fsharp
SerdeGeneratorEngine.generate sourceFiles (StjCodeEmitter() :> ISerdeCodeEmitter)
```

This is what Spec 03’s GeneratorHost will call.

---

## 7. Namespaces, files, and hint names

- Generated types should live under `namespace Serde.FS.SystemTextJson`.
- The main resolver type must be named `GeneratedSerdeStjResolver` to match Spec 01.
- File naming convention:
  - Use a distinct extension pattern, e.g. `*.stj.g.fs` (final decision can be in Spec 03, but emitter should support hint names that align with that).

---

## 8. Acceptance criteria

Spec 02 is complete when:

1. **Project builds successfully** with no warnings/errors.
2. `Serde.FS.SystemTextJson.SourceGen` contains:
   - An STJ-specific emitter (`StjCodeEmitter` or similar) implementing the shared emitter interface.
3. The emitter:
   - Uses `SerdeGeneratorEngine` to traverse metadata.
   - Generates a concrete `GeneratedSerdeStjResolver` that:
     - Implements `IJsonTypeInfoResolver`.
     - Returns non-null `JsonTypeInfo` for known Serde types.
     - Returns `null` otherwise.
4. The generated resolver compiles against:
   - `Serde.FS.SystemTextJson`’s `Resolver` module.
   - `System.Text.Json` APIs.
5. No changes are made to `Serde.FS.Json` in this spec.

---
