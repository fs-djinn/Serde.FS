namespace FSharp.SourceDjinn

module EntryPointEmitter =

    let emit (info: EntryPointInfo) : string =
        sprintf "namespace FSharp.SourceDjinn.Generated\n\nmodule DjinnEntryPoint =\n\n    [<EntryPoint>]\n    let main argv =\n        %s.%s argv\n"
            info.ModuleName info.FunctionName
