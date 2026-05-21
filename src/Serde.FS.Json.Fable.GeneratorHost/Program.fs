module Serde.FS.Json.Fable.GeneratorHost.Program

open System.IO
open Serde.FS.SourceGen
open Serde.FS.Json.Fable.SourceGen

// NB: do NOT `open Serde.FS` at the top of this file. The Serde.FS namespace
// exports its own `EntryPointAttribute` (used by the codec runtime), which
// would shadow F#'s `Microsoft.FSharp.Core.EntryPointAttribute` and silently
// break the `[<EntryPoint>]` annotation below — the compiler emits FS0988
// "Main module of program is empty" and the host exe becomes a no-op.
// Field types from Serde.FS (RpcInterfaceInfo, RpcMethodInfo) resolve through
// inference on `discovery.Interfaces` without needing the open.

// Entry point invoked from the Serde.FS.Json.Fable buildTransitive target.
// Args:
//   argv.[0] = projectDir     the consumer Fable project's directory.
//   argv.[1] = outputDir      absolute path to <projectDir>/fable-generated;
//                             we write `~<InterfaceName>.fable.g.fs` here.
//   argv.[2] = refSourceList  semicolon-separated .fs paths from
//                             directly-referenced projects (Shared etc.).
[<EntryPoint>]
let main (argv: string array) =
    if argv.Length = 0 then
        eprintfn "Expected project directory argument"
        1
    else
        let projectDir = argv.[0]
        let outputDir =
            if argv.Length > 1 then argv.[1]
            else Path.Combine(projectDir, "fable-generated")

        if not (Directory.Exists outputDir) then
            Directory.CreateDirectory outputDir |> ignore

        let isGenerated (name: string) =
            name.EndsWith(".serde.g.fs")
            || name.EndsWith(".djinn.g.fs")
            || name.EndsWith(".json.g.fs")
            || name.EndsWith(".fable.g.fs")

        let localSourceFiles =
            Directory.GetFiles(projectDir, "*.fs", SearchOption.TopDirectoryOnly)
            |> Array.filter (fun f -> not (isGenerated (Path.GetFileName f)))
            |> Array.map (fun f -> f, File.ReadAllText f)
            |> Array.toList

        let localFullPaths =
            localSourceFiles
            |> List.map (fun (f, _) -> Path.GetFullPath(f).ToLowerInvariant())
            |> Set.ofList

        let refSourceFiles =
            if argv.Length > 2 && not (System.String.IsNullOrWhiteSpace(argv.[2])) then
                argv.[2].Split(';', System.StringSplitOptions.RemoveEmptyEntries)
                |> Array.filter (fun f -> f.EndsWith(".fs") && File.Exists(f))
                |> Array.filter (fun f -> not (isGenerated (Path.GetFileName f)))
                |> Array.filter (fun f ->
                    not (localFullPaths.Contains(Path.GetFullPath(f).ToLowerInvariant())))
                |> Array.map (fun f -> f, File.ReadAllText f)
                |> Array.toList
            else []

        let sourceFiles = localSourceFiles @ refSourceFiles

        // Reuse the same discovery used by the server-side generator so the
        // Fable client stays in lockstep with what the server expects.
        let allTypeInfos =
            sourceFiles
            |> List.collect (fun (path, src) ->
                if path.EndsWith ".fs"
                then SerdeAstParser.parseSourceAllTypes path src
                else [])

        let discovery = RpcApiDiscovery.discover allTypeInfos sourceFiles

        let generatedFiles =
            System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        let mutable hadErrors = false

        for rpc in discovery.Interfaces do
            // SerdeFS102: every method's input/output TypeInfo must be
            // resolved or we can't compute codec references safely. The
            // validation logic lives on FableClientEmitter so the same
            // diagnostic is unit-testable outside this host.
            match FableClientEmitter.validateInterfaceTypes rpc with
            | Some err ->
                hadErrors <- true
                eprintfn "%s" err
            | None ->
                let code = FableClientEmitter.emit rpc discovery.DiscoveredTypes
                let outputFile = Path.Combine(outputDir, FableClientEmitter.outputFileName rpc)
                let existing =
                    if File.Exists outputFile then Some (File.ReadAllText outputFile)
                    else None
                match existing with
                | Some prev when prev = code -> ()
                | _ -> File.WriteAllText(outputFile, code)
                generatedFiles.Add outputFile |> ignore

        // Self-ignoring .gitignore so generated files don't appear in git.
        let gitignorePath = Path.Combine(outputDir, ".gitignore")
        if not (File.Exists gitignorePath) then
            File.WriteAllText(gitignorePath, "*\n")

        // Delete any stale .fs in the folder (the generator owns it).
        if Directory.Exists outputDir then
            for existingFile in Directory.GetFiles(outputDir, "*.fs") do
                if not (generatedFiles.Contains existingFile) then
                    File.Delete existingFile

        if hadErrors then 1 else 0
