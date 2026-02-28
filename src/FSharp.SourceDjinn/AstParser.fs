namespace FSharp.SourceDjinn

open FSharp.SourceDjinn.Types

module AstParser =

    // ── Attribute filtering ──────────────────────────────────────────

    let private shortName (name: string) =
        match name.LastIndexOf('.') with
        | -1 -> name
        | i -> name.Substring(i + 1)

    /// Filter types to only those having at least one attribute whose short name is in the given set.
    let filterByAttributes (attrNames: Set<string>) (types: TypeInfo list) : TypeInfo list =
        types
        |> List.filter (fun ti ->
            ti.Attributes |> List.exists (fun a ->
                attrNames.Contains(shortName a.Name)))

    // ── Type parsing (delegates to TypeKindExtractor) ────────────────

    /// Parse F# source text and return ALL type definitions found.
    let parseSourceAllTypes (filePath: string) (sourceText: string) : TypeInfo list =
        TypeKindExtractor.extractTypes filePath sourceText

    /// Parse an F# source file and return ALL type definitions found.
    let parseFileAllTypes (filePath: string) : TypeInfo list =
        let sourceText = System.IO.File.ReadAllText(filePath)
        TypeKindExtractor.extractTypes filePath sourceText

