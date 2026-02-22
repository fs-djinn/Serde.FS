# Serde.FS

## Rebuild local debug NuGet package (for testing SampleApp)

1. Build the source generator:
   ```
   dotnet build src/Serde.FS.SourceGen/Serde.FS.SourceGen.fsproj
   ```

2. Increment the `-debug.N` suffix in `src/Serde.FS.STJ/Serde.FS.STJ.fsproj` (e.g. `0.1.1-debug.1` -> `0.1.1-debug.2`). Do not bump the release version for local testing.

3. Build the STJ project (copies SourceGen DLL into analyzers/ and generates the .nupkg):
   ```
   dotnet build src/Serde.FS.STJ/Serde.FS.STJ.fsproj
   ```

4. Update the `Serde.FS.STJ` package version in `src/Serde.FS.STJ.SampleApp/Serde.FS.STJ.SampleApp.fsproj` to match.

5. Restore and build the SampleApp:
   ```
   dotnet restore src/Serde.FS.STJ.SampleApp/Serde.FS.STJ.SampleApp.fsproj
   dotnet build src/Serde.FS.STJ.SampleApp/Serde.FS.STJ.SampleApp.fsproj
   ```

The version bump is required because NuGet caches packages by version. The local feed is configured in `nuget.config` pointing to `src/Serde.FS.STJ/bin/Debug`.
