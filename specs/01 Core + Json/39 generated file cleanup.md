### ✅ High-level goals

1. All generated files go into a **single folder**:  
   `$(IntermediateOutputPath)serde-generated\`
2. There are **three generators**:
   - **Serde.FS.SourceGen** → Djinn entry point (`*.djinn.g.fs`)
   - **Serde.FS.Json** → JSON codecs (`*.json.g.fs`)
   - **Serde.FS.SystemTextJson** → STJ codecs/resolvers (`*.stj.g.fs`)
3. **Each generator host only cleans up its own files** (stale-file cleanup).
4. No host ever deletes another backend’s files.
5. No broad `*.g.fs` patterns, no legacy `*.serde.g.fs`.

---

## 1. Folder unification

### 1.1 GeneratorHosts

For **both** GeneratorHost projects:

- `src/Serde.FS.Json.GeneratorHost/Program.fs`
- `src/Serde.FS.SystemTextJson.GeneratorHost/Program.fs`

Ensure the output directory is:

```fsharp
let outputDir = Path.Combine(projectDir, "obj", "serde-generated")
```

(or equivalent using `IntermediateOutputPath`—but the effective folder must be `obj/serde-generated`).

Remove any use of:

- `serde-json-generated`
- `serde-stj-generated`
- any other backend-specific folder names.

### 1.2 .targets files

Update all three `.targets` files:

- `src/Serde.FS.Json/Serde.FS.Json.targets`
- `src/Serde.FS.SystemTextJson/Serde.FS.SystemTextJson.targets`
- `src/Serde.FS.SourceGen/Serde.FS.SourceGen.targets`

**Changes:**

1. Replace backend-specific folders with:

   ```xml
   $(IntermediateOutputPath)serde-generated\
   ```

2. Ensure generated files are included via:

   ```xml
   <Compile Include="$(IntermediateOutputPath)serde-generated\**\*.fs" />
   ```

3. Remove any references to:

   - `serde-json-generated`
   - `serde-stj-generated`
   - `serde-djinn-generated`
   - or similar.

---

## 2. Stale-file cleanup (per host, suffix-based)

### 2.1 Shared behavior

All hosts use the same pattern:

- Track `generatedFiles` in a `HashSet<string>`.
- After generation, enumerate files in `outputDir` matching **host-specific patterns**.
- Delete only files that:
  - match the pattern, and  
  - are **not** in `generatedFiles`.

No host should delete files it did not generate.

---

### 2.2 Serde.FS.Json.GeneratorHost

File: `src/Serde.FS.Json.GeneratorHost/Program.fs`

**Responsibility:** JSON codec file(s), suffix: `*.json.g.fs`.

**Spec:**

- Set cleanup patterns to:

  ```fsharp
  for ext in [ "*.json.g.fs" ] do
      for existingFile in Directory.GetFiles(outputDir, ext) do
          if not (generatedFiles.Contains existingFile) then
              File.Delete existingFile
  ```

- Remove cleanup of:
  - `"*.serde.g.fs"` (legacy)
  - `"*.djinn.g.fs"`
  - `"*.stj.g.fs"`
  - `"*.g.fs"` (too broad)
  - any hard-coded legacy filenames.

---

### 2.3 Serde.FS.SystemTextJson.GeneratorHost

File: `src/Serde.FS.SystemTextJson.GeneratorHost/Program.fs`

**Responsibility:** STJ resolver/codec files, suffix: `*.stj.g.fs`.

**Spec:**

- Set cleanup patterns to:

  ```fsharp
  for ext in [ "*.stj.g.fs" ] do
      for existingFile in Directory.GetFiles(outputDir, ext) do
          if not (generatedFiles.Contains existingFile) then
              File.Delete existingFile
  ```

- Remove cleanup of:
  - `"*.serde.g.fs"` (legacy)
  - `"*.djinn.g.fs"`
  - `"*.json.g.fs"`
  - `"*.g.fs"` (too broad)
  - any hard-coded legacy filenames like `~SerdeResolver.serde.g.fs`, `~SerdeResolverRegistration.djinn.g.fs`.

---

## 3. Invariants to preserve

1. **Single folder**: all generated files live under `obj/serde-generated`.
2. **Suffix-based ownership**:
   - JSON → `*.json.g.fs`
   - STJ → `*.stj.g.fs`
   - SourceGen → `*.djinn.g.fs`
3. **No cross-backend deletion**:
   - JSON never deletes `.stj.g.fs` or `.djinn.g.fs`.
   - STJ never deletes `.json.g.fs` or `.djinn.g.fs`.
   - SourceGen never deletes `.json.g.fs` or `.stj.g.fs`.
4. **No broad patterns** like `*.g.fs` or `*.serde.g.fs`.

---

## 4. Validation checklist

After Claude applies this spec:

- `obj/serde-generated/` exists and contains:
  - `~SerdeJsonCodecs.json.g.fs` (JSON)
  - `~SerdeStjResolver.stj.g.fs` (STJ, if referenced)
  - `~~EntryPoint.djinn.g.fs` (when attribute is present)
- Removing JSON removes stale `*.json.g.fs` but leaves STJ and Djinn files.
- Removing STJ removes stale `*.stj.g.fs` but leaves JSON and Djinn files.
- No `serde-json-generated` or `serde-stj-generated` folders remain.

---
