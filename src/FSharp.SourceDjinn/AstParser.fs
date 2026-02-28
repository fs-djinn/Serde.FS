namespace FSharp.SourceDjinn

open FSharp.SourceDjinn.TypeModel

module AstParser =

    /// Parse F# source text and return ALL type definitions found.
    let parseSourceAllTypes (filePath: string) (sourceText: string) : TypeInfo list =
        TypeKindExtractor.extractTypes filePath sourceText

    /// Parse an F# source file and return ALL type definitions found.
    let parseFileAllTypes (filePath: string) : TypeInfo list =
        let sourceText = System.IO.File.ReadAllText(filePath)
        TypeKindExtractor.extractTypes filePath sourceText
