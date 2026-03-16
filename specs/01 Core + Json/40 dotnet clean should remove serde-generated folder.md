# ✅ **SPEC: Add MSBuild Clean behavior to delete `obj/serde-generated`**

## **Goal**
Ensure that running **Clean** on a project removes all generated Serde files by deleting the entire folder:

```
$(IntermediateOutputPath)serde-generated\
```

This cleanup must be:

- **owned by Serde.FS** (the core package, which is a direct dependency of all backends)
- **independent of JSON/STJ backends**
- **triggered automatically by MSBuild Clean**
- **automatically available** to all consumers since every backend depends on Serde.FS

---

# 1. Create a new `.targets` file in Serde.FS

Inside the **Serde.FS** project, create:

```
src/Serde.FS/buildTransitive/Serde.FS.targets
```

This file will be included in the NuGet package under:

```
buildTransitive/Serde.FS.targets
```

Using `buildTransitive/` ensures the target is auto-imported by transitive consumers of Serde.FS (e.g., projects that reference Serde.FS.Json or Serde.FS.SystemTextJson). This bypasses the default NuGet behavior of excluding `Build` assets from transitive dependencies.

---

# 2. Add a Clean target that removes the folder

The `.targets` file must contain exactly this:

```xml
<Project>
  <Target Name="SerdeSourceGenClean" AfterTargets="Clean">
    <RemoveDir Directories="$(IntermediateOutputPath)serde-generated" />
  </Target>
</Project>
```

### Requirements:

- The target must run **AfterTargets="Clean"**
- It must delete the entire folder, not individual files
- It must not reference JSON or STJ

---

# 3. Update Serde.FS.fsproj to include the `.targets` file in the NuGet package

In `Serde.FS.fsproj`, add:

```xml
<ItemGroup>
  <None Include="buildTransitive\Serde.FS.targets" Pack="true" PackagePath="buildTransitive\" />
</ItemGroup>
```

This ensures the `.targets` file is shipped with the package and flows to transitive consumers.

---

# 4. Do **not** modify JSON or STJ packages

- JSON and STJ must **not** include this `.targets` file
- JSON and STJ must **not** delete the folder
- JSON and STJ continue to do **suffix‑based stale cleanup only**

---

# 5. Expected behavior after implementation

### ✔ Running **Build**:
- JSON/STJ/SourceGen emit files into `obj/serde-generated`

### ✔ Running **Clean**:
- MSBuild deletes the entire `serde-generated` folder
- No stale entry point files remain
- No stale JSON/STJ files remain
- No backend-specific cleanup needed

### ✔ Removing all Serde.FS packages:
- Running Clean removes all generated files
- Project returns to a pristine state

---

# 6. Non‑goals (explicit)

Claude must **not**:

- Add cleanup logic to JSON or STJ
- Add cleanup logic inside generator hosts
- Add cleanup logic inside SerdeGeneratorEngine
- Add a new generator host for SourceGen

Cleanup belongs **only** in the `buildTransitive/` folder of the core Serde.FS package.

---
