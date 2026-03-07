# 📘 **SPEC: Add a Console‑App Generator Host and Wire It Into Serde.FS.Json**

This spec defines the new out‑of‑process generator host for Serde.FS.  
Follow it exactly.  
Do not modify any existing engine logic.

---

# 1. **Create a new project: `Serde.FS.GeneratorHost`**

**Location:**

```
src/Serde.FS.GeneratorHost/
```

**Project file: `Serde.FS.GeneratorHost.fsproj`**

### Requirements:

- TFM: `net8.0`
- Output type: `Exe`
- Nullable: enable
- LangVersion: latest
- No implicit usings
- No analyzers
- No MSBuild tasks

### Project references:

- `Serde.FS.SourceGen`
- `Serde.FS.Json`
- Any other engine dependencies required for generation

### NuGet references:

None unless required by the engine.

---

# 2. **Program.fs**

Create a minimal entry point:

```fsharp
open Serde.FS.SourceGen

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        eprintfn "Expected project directory argument"
        1
    else
        let projectDir = argv[0]
        SerdeGeneratorEngine.generate projectDir
        0
```

### Behavior:

- Accepts exactly one argument: the project directory
- Calls the existing engine’s `generate` function
- Exits with code 0 on success, non‑zero on failure
- Does not print anything except errors

---

# 3. **Package the host inside the Serde.FS.Json NuGet package**

Modify `Serde.FS.Json.fsproj`:

Add this inside an `<ItemGroup>`:

```xml
<Content Include="..\Serde.FS.GeneratorHost\bin\$(Configuration)\net8.0\Serde.FS.GeneratorHost.dll"
         PackagePath="tools/net8.0/"
         CopyToOutputDirectory="Never"
         Visible="false" />
```

This ensures the host is included in the final `.nupkg` under:

```
tools/net8.0/Serde.FS.GeneratorHost.dll
```

---

# 4. **Replace the MSBuild task with an Exec‑based .targets file**

Modify or replace `Serde.FS.Json.targets` with:

```xml
<Project>
  <Target Name="SerdeGenerate" BeforeTargets="CoreCompile">
    <Exec Command="dotnet &quot;$(MSBuildThisPackageRoot)tools/net8.0/Serde.FS.GeneratorHost.dll&quot; &quot;$(ProjectDir)&quot;" />

    <ItemGroup>
      <Compile Include="$(IntermediateOutputPath)serde-generated/**/*.fs"
               Condition="Exists('$(IntermediateOutputPath)serde-generated/')" />
    </ItemGroup>
  </Target>
</Project>
```

### Requirements:

- Remove all `<UsingTask>` elements  
- Remove all MSBuild task references  
- Only use `<Exec>`  
- The generator host must run **before** F# compilation  
- Generated files must be included via `<Compile Include>`  

---

# 5. **Remove the old MSBuild task project**

Delete:

- Any project implementing `ITask`
- Any `.targets` file referencing `<UsingTask>`
- Any analyzer‑based generator invocation

The new system must rely **only** on the console host.

---

# 6. **Ensure the engine remains untouched**

Claude must **not** modify:

- `Serde.FS.SourceGen`
- `Serde.FS.Json`
- The type model
- The emitters
- The engine logic

Only the hosting mechanism changes.

---

# 7. **Acceptance criteria**

After implementation:

- `SampleApp` builds successfully in Visual Studio  
- `SampleApp` builds successfully via CLI  
- No MSBuild warnings about fallback execution  
- No MSBuild tasks are loaded  
- The generator runs via `dotnet Serde.FS.GeneratorHost.dll`  
- Generated files appear under `obj/serde-generated/`  
- The system works identically on all machines  

---
