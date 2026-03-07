namespace Serde.FS.SourceGen

open System.IO
open Serde.FS
open Microsoft.Build.Utilities
open Microsoft.Build.Framework

type SerdeGeneratorTask() =
    inherit Task()

    [<Required>]
    member val SourceFiles : ITaskItem array = [||] with get, set

    member val OutputDir : string = "" with get, set
    member val EmitterAssemblyPath : string = "" with get, set
    member val EmitterTypeName : string = "" with get, set

    member private this.ResolveEmitter() : ISerdeCodeEmitter =
        if not (System.String.IsNullOrEmpty(this.EmitterTypeName)) then
            let alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(System.Reflection.Assembly.GetExecutingAssembly())
            let fullPath = System.IO.Path.GetFullPath(this.EmitterAssemblyPath)
            let asm = alc.LoadFromAssemblyPath(fullPath)
            let emitterType = asm.GetType(this.EmitterTypeName)
            System.Activator.CreateInstance(emitterType) :?> ISerdeCodeEmitter
        else
            match SerdeCodegenRegistry.getDefaultEmitter() with
            | Some e -> e
            | None -> failwith "No Serde code emitter registered. Provide EmitterTypeName or call SerdeCodegenRegistry.setDefaultEmitter()."

    override this.Execute() =
        try
            if not (Directory.Exists(this.OutputDir)) then
                Directory.CreateDirectory(this.OutputDir) |> ignore

            let emitter = this.ResolveEmitter()

            // Read source files into (filePath, sourceText) pairs for the engine
            let sourceFiles =
                this.SourceFiles
                |> Array.choose (fun item ->
                    let filePath = item.ItemSpec
                    if File.Exists(filePath) && filePath.EndsWith(".fs") then
                        Some (filePath, File.ReadAllText(filePath))
                    else None)
                |> Array.toList

            let result = SerdeGeneratorEngine.generate sourceFiles emitter

            // Report warnings and errors
            for warning in result.Warnings do
                this.Log.LogWarning(warning)
            for error in result.Errors do
                this.Log.LogError(error)

            if not (List.isEmpty result.Errors) then
                false
            else
                let generatedFiles = System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)

                // Write generated sources to output directory
                for source in result.Sources do
                    let outputFile = Path.Combine(this.OutputDir, source.HintName)
                    let existingContent =
                        if File.Exists(outputFile) then Some (File.ReadAllText(outputFile))
                        else None
                    match existingContent with
                    | Some existing when existing = source.Code -> ()
                    | _ -> File.WriteAllText(outputFile, source.Code)
                    generatedFiles.Add(outputFile) |> ignore
                    this.Log.LogMessage(MessageImportance.Low, "Serde: Generated {0}", outputFile)

                // Remove stale generated files
                for existingFile in Directory.GetFiles(this.OutputDir, "*.serde.g.fs") do
                    if not (generatedFiles.Contains(existingFile)) then
                        File.Delete(existingFile)
                        this.Log.LogMessage(MessageImportance.Low, "Serde: Removed stale {0}", existingFile)
                for existingFile in Directory.GetFiles(this.OutputDir, "*.djinn.g.fs") do
                    if not (generatedFiles.Contains(existingFile)) then
                        File.Delete(existingFile)
                        this.Log.LogMessage(MessageImportance.Low, "Serde: Removed stale {0}", existingFile)

                true
        with ex ->
            this.Log.LogErrorFromException(ex)
            false
