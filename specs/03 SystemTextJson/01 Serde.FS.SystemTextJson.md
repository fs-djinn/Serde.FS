### Spec 1 — Create `Serde.FS.SystemTextJson` backend project

This spec defines the new **System.Text.Json–based backend** for Serde.FS.  
It is about the **runtime API surface and packaging**, not codegen internals (that’s Spec 2/3).

---

## 1. Project creation and packaging

**Goal:** Create a new backend project `Serde.FS.SystemTextJson` with proper NuGet metadata and references.

- **Project name:** `Serde.FS.SystemTextJson`
- **Assembly name:** `Serde.FS.SystemTextJson`
- **Output type:** `Library`
- **Target frameworks:**  
  - `net8.0` (and any others currently used in the solution, if applicable)

- **Project references:**
  - Reference `Serde.FS` (core)
  - Do **not** reference `Serde.FS.Json`

- **NuGet package references:**
  - `System.Text.Json` (same version as currently used in Serde.FS.Json, if any)
  - Any ASP.NET Core–related package should **not** be referenced directly here; this project should stay pure STJ + Serde.FS.

- **NuGet metadata:**
  - **PackageId:** `Serde.FS.SystemTextJson`
  - **Authors:** same as other Serde.FS packages
  - **Description:**  
    > System.Text.Json backend for Serde.FS — provides a resolver and generated metadata for integrating Serde-based types with System.Text.Json and ASP.NET Core.
  - **Repository URL, license, tags:** mirror existing Serde.FS packages, adding tags like `system-text-json`, `stj`, `aspnetcore`, `serde`.

---

## 2. High-level purpose and boundaries

**This project is:**

- The **System.Text.Json backend** for Serde.FS.
- Responsible for:
  - Consuming Serde metadata
  - Exposing a **resolver** for STJ
  - Integrating with ASP.NET Core `JsonOptions` / `JsonSerializerOptions`
  - Using **generated STJ metadata** (from Spec 2/3)

**This project is *not*:**

- A JSON serializer for general use (that’s `Serde.FS.Json`).
- A codegen host (that’s `Serde.FS.SystemTextJson.SourceGen` + GeneratorHost).
- A place for MSBuild logic.

---

## 3. Public API surface

### 3.1 Core types

Define a namespace:

```fsharp
namespace Serde.FS.SystemTextJson
```

Within it, introduce:

- **`module Resolver`** (or a static class equivalent if needed for C# ergonomics):

  - **Goal:** Provide a `JsonTypeInfoResolver` (or chainable resolver) that exposes generated STJ metadata for Serde-decorated types.

  - **Public members (initial shape):**

    - **`val resolver : System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver`**

      A resolver that:
      - Aggregates all generated STJ metadata from this assembly (and possibly others, depending on how we wire it in Spec 2/3).
      - Can be plugged into `JsonSerializerOptions.TypeInfoResolver`.

    - Optionally, if we want composability:

      - **`val combine : System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver -> System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver`**

        Returns a resolver that first consults Serde-generated metadata, then falls back to the provided resolver.

  - The exact naming (`Resolver`, `SerdeStjResolver`, etc.) can be finalized, but the intent is:

    ```csharp
    options.TypeInfoResolver = Serde.FS.SystemTextJson.Resolver.resolver;
    // or
    options.TypeInfoResolver = Serde.FS.SystemTextJson.Resolver.combine(existingResolver);
    ```

### 3.2 ASP.NET Core integration example (for docs/tests later)

The project should be designed so that users can do:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver =
        Serde.FS.SystemTextJson.Resolver.combine(options.SerializerOptions.TypeInfoResolver);
});
```

No direct ASP.NET Core dependency is required in this project; this is just the intended usage.

---

## 4. Integration with generated STJ metadata

Even though Spec 2/3 will implement the actual generation, this spec must define **how this project expects to consume it**.

### 4.1 Generated metadata shape (contract)

Assume that `Serde.FS.SystemTextJson.SourceGen` will generate:

- One or more types that implement `IJsonTypeInfoResolver`, e.g.:

  ```fsharp
  type GeneratedSerdeStjResolver() =
      interface System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver with
          member _.GetTypeInfo(type, options) = ...
  ```

- These types will live in this assembly (or a companion assembly referenced by it).

### 4.2 Resolver composition strategy

`Resolver.resolver` should:

- Aggregate all generated resolvers (e.g., a single `GeneratedSerdeStjResolver` or multiple partial resolvers).
- Expose them as a single `IJsonTypeInfoResolver`.

Implementation detail (for Claude later):

- Use `JsonTypeInfoResolver.Combine` if available, or implement a custom composite resolver that:
  - Tries generated resolver(s) first
  - Falls back to `null` (or another resolver if `combine` is used)

---

## 5. Internal structure and constraints

- **No direct dependency on `Serde.FS.Json`.**
  - This backend is STJ-only and should not rely on the JSON codec backend.

- **No MSBuild or codegen logic here.**
  - This project only consumes generated metadata.
  - All generation happens in `Serde.FS.SystemTextJson.SourceGen` + GeneratorHost.

- **No Roslyn generators.**
  - Everything is MSBuild Task–driven; this project just uses the emitted code.

---

## 6. Acceptance criteria

This spec is complete when:

1. **Project exists**:
   - `Serde.FS.SystemTextJson` project is added to the solution.
   - It builds successfully.
   - It has correct NuGet metadata and references.

2. **Public API exists**:
   - A `Resolver` module (or equivalent) is defined.
   - It exposes at least:
     - `resolver : IJsonTypeInfoResolver`
     - Optionally `combine : IJsonTypeInfoResolver -> IJsonTypeInfoResolver`

3. **Integration contract is clear**:
   - The project compiles assuming the existence of a `GeneratedSerdeStjResolver` type (to be provided by Spec 2).
   - The resolver is wired to use that generated type (even if it’s a stub for now).

4. **No STJ logic leaks into `Serde.FS.Json`.**
   - `Serde.FS.Json` remains unchanged in this spec.

---
