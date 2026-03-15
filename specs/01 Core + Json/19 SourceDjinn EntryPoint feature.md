## SourceDjinn Entry Point Generation — Full Implementation Spec

### Overview
The goal is to support a declarative entry‑point model for F# applications using SourceDjinn. Users annotate a function with a custom attribute:

```fsharp
[<FSharp.SourceDjinn.TypeModel.EntryPoint>]
let run argv = ...
```

SourceDjinn detects this at design time and generates a safe wrapper:

```fsharp
[<EntryPoint>]
let main argv = Program.run argv
```

No runtime registration, no reflection, no module initializer forcing, no callback registry, and no additional packages.

---

## 1. Attribute Definition (in FSharp.SourceDjinn.TypeModel)

### File: `EntryPointAttribute.fs`

```fsharp
namespace FSharp.SourceDjinn.TypeModel

[<System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)>]
type EntryPointAttribute() =
    inherit System.Attribute()
```

**Constraints:**
- Must be dependency‑free.
- Must not reference SourceDjinn engine or Serde.
- Must be included in the TypeModel package so it flows transitively to user apps.

---

## 2. Detection Logic (in SourceDjinn Engine)

### Module: `SourceDjinn.EntryPointDetector`

**Responsibilities:**
- Scan the syntax tree for methods annotated with `FSharp.SourceDjinn.TypeModel.EntryPointAttribute`.
- Ensure exactly one such method exists; if more than one, emit a diagnostic.
- Extract:
  - Fully qualified module name (e.g., `Program`)
  - Function name (e.g., `run`)
  - Parameter list (must be `string[] -> int`)

**Output:**
```fsharp
type EntryPointInfo =
    { ModuleName : string
      FunctionName : string }

val detect : SyntaxTree -> EntryPointInfo option
```

**Rules:**
- Only top‑level functions are allowed.
- Only one entry point is allowed.
- Function must have signature `string[] -> int` (or be inferrable as such).
- If signature mismatches, emit a diagnostic.

---

## 3. Emission Logic (in SourceDjinn Engine)

### Module: `SourceDjinn.EntryPointEmitter`

**Responsibilities:**
Generate a file containing:

- A unique module name: `DjinnGeneratedEntryPoint`
- A single function:
  ```fsharp
  [<EntryPoint>]
  let main argv = <ModuleName>.<FunctionName> argv
  ```

**Output:**
```fsharp
val emit : EntryPointInfo -> GeneratedFile
```

**Generated file name:**
```
~EntryPoint.djinn.g.fs
```

**Generated code template:**

```fsharp
namespace <ModuleName>

module DjinnGeneratedEntryPoint =
    [<EntryPoint>]
    let main argv =
        <ModuleName>.<FunctionName> argv
```

**Notes:**
- No reflection.
- No module initializer forcing.
- No callback registry.
- No runtime dependencies.
- Pure static call.

---

## 4. Integration in Serde.FS.SourceGen (or any generator)

### Responsibilities:
- Call `EntryPointDetector.detect` on the syntax tree.
- If an entry point is found:
  - Call `EntryPointEmitter.emit`.
  - Add the generated file to the compilation.

### Pseudocode:

```fsharp
match EntryPointDetector.detect tree with
| Some info ->
    let file = EntryPointEmitter.emit info
    context.AddSource("~EntryPoint.djinn.g.fs", file.Content)
| None -> ()
```

---

## 5. User Experience

### User writes:

```fsharp
module Program

open FSharp.SourceDjinn.TypeModel

[<EntryPoint>]
let run argv =
    printfn "Hello, world."
    0
```

### Generator produces:

```fsharp
namespace Program

module DjinnGeneratedEntryPoint =
    [<EntryPoint>]
    let main argv =
        Program.run argv
```

### Result:
- No boilerplate.
- No runtime helpers.
- No SerdeApp.
- No callback registration.
- No reflection.
- No extra packages.

---

## 6. Safety and Edge Cases

### If multiple entry points are found:
Emit diagnostic:
```
Multiple [<FSharp.SourceDjinn.TypeModel.EntryPoint>] functions found. Only one is allowed.
```

### If signature is wrong:
Emit diagnostic:
```
Entry point function must have type string[] -> int.
```

### If function is nested or not top‑level:
Emit diagnostic:
```
Entry point must be a top-level function.
```

### If no entry point is found:
Do nothing.

---

## 7. Naming and Namespace Rules

- The generated wrapper always lives in the **same namespace** as the user’s module.
- The generated module name is always `DjinnGeneratedEntryPoint`.
- The generated function name is always `main`.
- This avoids collisions and keeps the wrapper predictable.

---

## 8. Removal of Old System

Delete the following from Serde.FS:

- `SerdeApp.entryPoint`
- `SerdeApp.invokeRegisteredEntryPoint`
- Any reflection-based module initializer forcing
- Any generated code referencing SerdeApp
- The old `~EntryPoint.serde.g.fs` template

This system replaces all of it.

---

## 9. Summary of Responsibilities

### TypeModel
- Defines the attribute.

### SourceDjinn (engine)
- Detects the attribute.
- Emits the wrapper.

### Serde.FS.SourceGen
- Orchestrates detection + emission.

### Serde.FS (runtime)
- No longer involved in entry points.

---
