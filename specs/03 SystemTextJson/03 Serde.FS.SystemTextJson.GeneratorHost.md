### Spec 03 — `Serde.FS.SystemTextJson.GeneratorHost` (MSBuild host)

This spec creates the **MSBuild-driven host** that wires `SerdeGeneratorEngine` to the STJ emitter from Spec 02 and emits generated STJ files into a backend-specific folder.

---

## 1. Project creation

**Goal:** Add a host project that can be invoked from MSBuild to generate STJ metadata.

- **Project name:** `Serde.FS.SystemTextJson.GeneratorHost`
- **Output type:** `Exe` (console app, like the existing JSON host)
- **Target framework:** `net8.0` (match existing host)

- **Project references:**
  - `Serde.FS`
  - `Serde.FS.SourceGen` (or wherever `SerdeGeneratorEngine` lives)
  - `Serde.FS.SystemTextJson.SourceGen` (for `StjCodeEmitter`)
  - `Serde.FS.SystemTextJson` (for namespace alignment / types if needed)

- **NuGet references:**
  - `System.Text.Json` (same version as other STJ projects, if required)

- **Packaging:**
  - This project is **not** packaged as a NuGet.
  - It is used as a `PrivateAsset` tool via MSBuild.

---

## 2. Entry point behavior

Create a `Program` module similar in structure to `Serde.FS.Json.GeneratorHost.Program`, but STJ-specific.

### 2.1 Arguments

- **`argv[0]`** — project directory (required)
- **`argv[1]`** — optional output directory override

If no output directory is provided, default to:

```text
obj/serde-stj-generated
```

under the project directory.

### 2.2 Source file discovery

- Discover all `.fs` files in the project directory (top-level only, same as existing host).
- Exclude generated files:
  - Files ending with `.serde.g.fs`
  - Files ending with `.djinn.g.fs`
  - Files ending with `.stj.g.fs`

Read them into the `sourceFiles` list as `(path, content)` pairs.

---

## 3. Wiring to `SerdeGeneratorEngine` and STJ emitter

Instantiate the STJ emitter from Spec 02:

```fsharp
let emitter = StjCodeEmitter() :> Serde.FS.ISerdeCodeEmitter
let result = SerdeGeneratorEngine.generate sourceFiles emitter
```

Then:

- Print warnings to stderr: `WARNING: ...`
- Print errors to stderr: `ERROR: ...`
- If any errors exist → exit code `1`
- Otherwise → proceed to write generated files.

---

## 4. Output files and folder

### 4.1 Output directory

- Use the resolved output directory (default `obj/serde-stj-generated`).
- Ensure the directory exists (create if missing).

### 4.2 Writing generated files

For each `source` in `result.Sources`:

- Compute `outputFile = Path.Combine(outputDir, source.HintName)`
- If `outputFile` exists:
  - Read existing content
  - If content is identical → skip write
- Otherwise:
  - Write `source.Code` to `outputFile`
- Track all written files in a `HashSet<string>`.

### 4.3 File naming / extensions

- STJ-generated files should use a distinct extension, e.g.:

  - `*.stj.g.fs`

The emitter from Spec 02 should already be producing appropriate `HintName` values; the host just writes them.

---

## 5. Cleanup of stale files

After writing all current generated files:

- Enumerate all `*.stj.g.fs` files in the output directory.
- For each file:
  - If it is **not** in the `generatedFiles` set → delete it.

Do **not** touch:

- `*.serde.g.fs`
- `*.djinn.g.fs`

Those belong to other backends/hosts.

---

## 6. Exit codes

- **0** — success, no errors
- **1** — any errors reported by `SerdeGeneratorEngine`
- **>1** — only if there is a fatal host-level issue (e.g., missing project directory argument)

---

## 7. MSBuild integration (light contract)

This spec does **not** need to fully wire MSBuild, but the host must be designed to be callable from an MSBuild task that:

- Passes the project directory as `argv[0]`
- Optionally passes a custom output directory as `argv[1]`

The existing JSON GeneratorHost can be used as the behavioral template.

---

## 8. Acceptance criteria

Spec 03 is complete when:

1. `Serde.FS.SystemTextJson.GeneratorHost` builds successfully.
2. Running the host with a valid project directory:
   - Scans `.fs` files (excluding generated ones).
   - Invokes `SerdeGeneratorEngine` with `StjCodeEmitter`.
   - Writes generated `*.stj.g.fs` files into `obj/serde-stj-generated` (by default).
   - Cleans up stale `*.stj.g.fs` files.
3. Exit codes behave as specified.
4. No changes are made to `Serde.FS.Json` or its host in this spec.

