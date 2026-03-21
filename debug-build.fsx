#r "nuget: Fun.Build, 1.1.17"

open System
open System.IO
open Fun.Build

// ---------------------------------------------------------------------------
// Paths
// ---------------------------------------------------------------------------

let serdeFSProj       = "src/Serde.FS/Serde.FS.fsproj"
let sourceGenProj     = "src/Serde.FS.SourceGen/Serde.FS.SourceGen.fsproj"
let generatorHostProj    = "src/Serde.FS.Json.GeneratorHost/Serde.FS.Json.GeneratorHost.fsproj"
let stjGeneratorHostProj = "src/Serde.FS.SystemTextJson.GeneratorHost/Serde.FS.SystemTextJson.GeneratorHost.fsproj"
let stjProj           = "src/Serde.FS.Json/Serde.FS.Json.fsproj"
let stjSystemTextJsonProj = "src/Serde.FS.SystemTextJson/Serde.FS.SystemTextJson.fsproj"
let sampleRpcProj     = "src/Serde.FS.Json.SampleRpc/Serde.FS.Json.SampleRpc.fsproj"
let sampleAppProj     = "src/Serde.FS.Json.SampleApp/Serde.FS.Json.SampleApp.fsproj"
let sourceGenTestProj = "src/Serde.FS.SourceGen.Tests/Serde.FS.SourceGen.Tests.fsproj"
let jsonTestProj      = "src/Serde.FS.Json.Tests/Serde.FS.Json.Tests.fsproj"
let nugetLocalDir     = ".nuget-local"

// ---------------------------------------------------------------------------
// Version helpers (read from Directory.Build.props)
// ---------------------------------------------------------------------------

let readProp (propName: string) =
    let content = File.ReadAllText("Directory.Build.props")
    let tag = $"<{propName}>"
    let idx = content.IndexOf(tag)
    if idx = -1 then failwith $"No <{propName}> found in Directory.Build.props"
    let start = idx + tag.Length
    let endIdx = content.IndexOf($"</{propName}>", start)
    content.Substring(start, endIdx - start).Trim()

let stableSerdeFSVersion     = readProp "SerdeFSVersion"
let stableSourceGenVersion   = readProp "SourceGenVersion"
let stableSerdeJsonVersion   = readProp "SerdeJsonVersion"
let stableSerdeStjVersion    = readProp "SerdeStjVersion"

let timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmss")

// All debug packages share the same version
let debugVersion = $"{stableSerdeFSVersion}.debug.{timestamp}"

// ---------------------------------------------------------------------------
// Pipeline: debug (default)
// ---------------------------------------------------------------------------

pipeline "debug" {
    description "Pack Serde packages and test the STJ backend via local NuGet feed"

    stage "Show versions" {
        run (fun _ ->
            printfn $"Stable Serde.FS:       {stableSerdeFSVersion}"
            printfn $"Stable SourceGen:      {stableSourceGenVersion}"
            printfn $"Stable Serde.FS.Json:  {stableSerdeJsonVersion}"
            printfn $"Stable Serde.FS.STJ:   {stableSerdeStjVersion}"
            printfn $"Timestamp:             {timestamp}"
            printfn $"Debug version:         {debugVersion}"
        )
    }

    stage "Prune local feed and global cache" {
        run (fun _ ->
            if Directory.Exists(nugetLocalDir) then
                for pkg in Directory.GetFiles(nugetLocalDir, "*.nupkg", SearchOption.AllDirectories) do
                    let name = Path.GetFileName(pkg)
                    if name.StartsWith("Serde.", StringComparison.OrdinalIgnoreCase) then
                        printfn $"  Deleting {pkg}"
                        File.Delete(pkg)
                    else
                        printfn $"  Keeping  {pkg}"
            else
                Directory.CreateDirectory(nugetLocalDir) |> ignore

            printfn "Local feed pruned."

            let globalPkgs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            for pkgName in [ "serde.fs"; "serde.fs.sourcegen"; "serde.fs.json"; "serde.fs.systemtextjson" ] do
                let pkgDir = Path.Combine(globalPkgs, pkgName)
                if Directory.Exists(pkgDir) then
                    for versionDir in Directory.GetDirectories(pkgDir) do
                        if Path.GetFileName(versionDir).Contains("debug") then
                            try
                                printfn $"  Clearing global cache: {versionDir}"
                                Directory.Delete(versionDir, true)
                            with :? UnauthorizedAccessException ->
                                printfn $"  Skipped (locked): {versionDir}"

            printfn "Global cache debug versions cleared."
        )
    }

    stage "Pack Serde.FS.SourceGen" {
        run $"dotnet clean {sourceGenProj}"
        run $"dotnet restore {sourceGenProj} --source https://api.nuget.org/v3/index.json --source {Path.GetFullPath(nugetLocalDir)}"
        run $"dotnet build {sourceGenProj} -c Debug --no-restore /p:PackageVersion={debugVersion} /p:SourceGenVersion={debugVersion} /p:SerdeFSVersion={debugVersion}"
        run $"dotnet pack {sourceGenProj} -c Debug -o {nugetLocalDir} /p:PackageVersion={debugVersion} /p:SourceGenVersion={debugVersion} /p:SerdeFSVersion={debugVersion}"
    }

    stage "Pack Serde.FS" {
        run $"dotnet clean {serdeFSProj}"
        run $"dotnet build {serdeFSProj} -c Debug /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion}"
        run $"dotnet pack {serdeFSProj} -c Debug -o {nugetLocalDir} /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion}"
    }

    stage "Publish GeneratorHosts" {
        run $"dotnet publish {generatorHostProj} -c Debug"
        run $"dotnet publish {stjGeneratorHostProj} -c Debug"
    }

    stage "Pack Serde.FS.Json" {
        run $"dotnet restore {stjProj} --no-cache /p:SourceGenVersion={debugVersion}"
        run $"dotnet clean {stjProj}"
        run $"dotnet build {stjProj} -c Debug /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion} /p:SourceGenVersion={debugVersion}"
        run $"dotnet pack {stjProj} -c Debug -o {nugetLocalDir} /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion} /p:SourceGenVersion={debugVersion}"
    }

    stage "Pack Serde.FS.SystemTextJson" {
        run $"dotnet restore {stjSystemTextJsonProj} --no-cache /p:SerdeFSVersion={debugVersion}"
        run $"dotnet clean {stjSystemTextJsonProj}"
        run $"dotnet build {stjSystemTextJsonProj} -c Debug /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion} /p:SerdeStjVersion={debugVersion}"
        run $"dotnet pack {stjSystemTextJsonProj} -c Debug -o {nugetLocalDir} /p:PackageVersion={debugVersion} /p:SerdeFSVersion={debugVersion} /p:SerdeStjVersion={debugVersion}"
    }

    stage "Run tests" {
        run $"dotnet test {sourceGenTestProj} -c Debug --no-restore"
        run $"dotnet test {jsonTestProj} -c Debug --no-restore"
    }

    stage "Write SampleApp version props" {
        run (fun _ ->
            // SampleRpc references Serde.FS
            let rpcPropsPath = Path.Combine(Path.GetDirectoryName(sampleRpcProj), "Directory.Build.props")
            let rpcContent = $"""<Project>
  <PropertyGroup>
    <SerdeFSVersion>{debugVersion}</SerdeFSVersion>
  </PropertyGroup>
</Project>
"""
            File.WriteAllText(rpcPropsPath, rpcContent)
            printfn $"  Wrote {rpcPropsPath} with SerdeFSVersion={debugVersion}"

            // SampleApp references Serde.FS.Json
            let propsPath = Path.Combine(Path.GetDirectoryName(sampleAppProj), "Directory.Build.props")
            let content = $"""<Project>
  <PropertyGroup>
    <SerdeJsonVersion>{debugVersion}</SerdeJsonVersion>
    <SerdeStjVersion>{debugVersion}</SerdeStjVersion>
  </PropertyGroup>
</Project>
"""
            File.WriteAllText(propsPath, content)
            printfn $"  Wrote {propsPath} with SerdeJsonVersion={debugVersion}, SerdeStjVersion={debugVersion}"
        )
    }

    stage "Restore SampleRpc and SampleApp" {
        run $"dotnet restore {sampleRpcProj} --no-cache"
        run $"dotnet restore {sampleAppProj} --no-cache"
    }

    stage "Build and run SampleApp" {
        run $"dotnet build {sampleAppProj} --no-restore"
        run $"dotnet run --project {sampleAppProj} --no-build"
    }

    stage "Summary" {
        run (fun _ ->
            printfn ""
            printfn "========================================"
            printfn "  Serde Debug Pipeline Summary"
            printfn "========================================"
            printfn $"  Debug version:      {debugVersion}"
            printfn $"  Packed:"
            printfn $"    Serde.FS                  {debugVersion}"
            printfn $"    Serde.FS.SourceGen        {debugVersion}"
            printfn $"    Serde.FS.Json             {debugVersion}"
            printfn $"    Serde.FS.SystemTextJson   {debugVersion}"
            printfn $"  Restore source:     .nuget-local (--no-cache)"
            printfn $"  SampleApp resolved: {debugVersion}"
            printfn "========================================"
            printfn ""
        )
    }

    runIfOnlySpecified false
}

tryPrintPipelineCommandHelp ()
