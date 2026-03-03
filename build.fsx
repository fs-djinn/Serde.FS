#r "nuget: Fun.Build, 1.1.17"

open System.IO
open Fun.Build

// ---------------------------------------------------------------------------
// Paths
// ---------------------------------------------------------------------------

let serdeFSProj   = "src/Serde.FS/Serde.FS.fsproj"
let sourceGenProj = "src/Serde.FS.SourceGen/Serde.FS.SourceGen.fsproj"
let jsonProj      = "src/Serde.FS.Json/Serde.FS.Json.fsproj"
let buildDir      = ".build"

// ---------------------------------------------------------------------------
// Version helper
// ---------------------------------------------------------------------------

let readVersion (projPath: string) =
    let content = File.ReadAllText(projPath)
    let tag = "<Version>"
    let idx = content.IndexOf(tag)
    if idx = -1 then failwith $"No <Version> found in {projPath}"
    let start = idx + tag.Length
    let endIdx = content.IndexOf("</Version>", start)
    content.Substring(start, endIdx - start).Trim()

let version = readVersion serdeFSProj

// ---------------------------------------------------------------------------
// Pipeline: build (default)
// ---------------------------------------------------------------------------

pipeline "build" {
    description "Build and pack Serde.FS, Serde.FS.SourceGen, and Serde.FS.Json"

    stage "Prepare output directory" {
        run (fun _ ->
            if Directory.Exists(buildDir) then
                for pkg in Directory.GetFiles(buildDir, "*.nupkg", SearchOption.AllDirectories) do
                    File.Delete(pkg)
            else
                Directory.CreateDirectory(buildDir) |> ignore
            printfn $"Output directory: {buildDir}"
        )
    }

    stage "Pack Serde.FS.SourceGen" {
        run $"dotnet clean {sourceGenProj}"
        run $"dotnet build {sourceGenProj} -c Release /p:PackageVersion={version} /p:SerdeFSVersion={version}"
        run $"dotnet pack {sourceGenProj} -c Release -o {buildDir} --no-build /p:NoBuild=true /p:BuildProjectReferences=false /p:PackageVersion={version} /p:SerdeFSVersion={version}"
    }

    stage "Pack Serde.FS" {
        run $"dotnet clean {serdeFSProj}"
        run $"dotnet build {serdeFSProj} -c Release /p:PackageVersion={version}"
        run $"dotnet pack {serdeFSProj} -c Release -o {buildDir} --no-build /p:NoBuild=true /p:BuildProjectReferences=false /p:PackageVersion={version}"
    }

    stage "Pack Serde.FS.Json" {
        run $"dotnet restore {jsonProj} /p:SourceGenVersion={version}"
        run $"dotnet clean {jsonProj}"
        run $"dotnet build {jsonProj} -c Release /p:PackageVersion={version} /p:SerdeFSVersion={version} /p:SourceGenVersion={version}"
        run $"dotnet pack {jsonProj} -c Release -o {buildDir} --no-build /p:NoBuild=true /p:BuildProjectReferences=false /p:PackageVersion={version} /p:SerdeFSVersion={version} /p:SourceGenVersion={version}"
    }

    stage "Summary" {
        run (fun _ ->
            printfn ""
            printfn "========================================"
            printfn "  Build Summary"
            printfn "========================================"
            printfn $"  Version:  {version}"
            printfn $"  Output:   {buildDir}/"
            printfn $"  Packages:"
            if Directory.Exists(buildDir) then
                for pkg in Directory.GetFiles(buildDir, "*.nupkg") do
                    printfn $"    {Path.GetFileName(pkg)}"
            printfn "========================================"
            printfn ""
        )
    }

    runIfOnlySpecified false
}

tryPrintPipelineCommandHelp ()
