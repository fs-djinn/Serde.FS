namespace FSharp.SourceDjinn

open FSharp.SourceDjinn.TypeModel
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

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

    // ── Call detection (generic) ─────────────────────────────────────

    let private checker = FSharpChecker.Create()

    /// Check if a long ident matches a dot-separated fully-qualified name.
    let private identMatches (idents: LongIdent) (fullName: string) =
        let parts = fullName.Split('.')
        let names = idents |> List.map (fun i -> i.idText)
        names = List.ofArray parts

    /// Check if an expression is a reference to any of the given fully-qualified names.
    let private isCallTo (fullNames: string list) (expr: SynExpr) =
        match expr with
        | SynExpr.LongIdent(_, SynLongIdent(id = idents), _, _) ->
            fullNames |> List.exists (identMatches idents)
        | _ -> false

    /// Recursively check if an expression contains a reference to any of the given names.
    let rec private exprContainsCallTo (fullNames: string list) (expr: SynExpr) =
        if isCallTo fullNames expr then true
        else
            match expr with
            | SynExpr.App(_, _, funcExpr, argExpr, _) ->
                exprContainsCallTo fullNames funcExpr
                || exprContainsCallTo fullNames argExpr
            | SynExpr.Sequential(expr1 = e1; expr2 = e2) ->
                exprContainsCallTo fullNames e1
                || exprContainsCallTo fullNames e2
            | SynExpr.Do(expr = e) ->
                exprContainsCallTo fullNames e
            | SynExpr.Paren(expr = e) ->
                exprContainsCallTo fullNames e
            | SynExpr.LetOrUse(body = body) ->
                exprContainsCallTo fullNames body
            | _ -> false

    /// Walk declarations looking for references to any of the given names.
    let rec private declsContainCallTo (fullNames: string list) (decls: SynModuleDecl list) =
        decls |> List.exists (fun decl ->
            match decl with
            | SynModuleDecl.Expr(expr, _) ->
                exprContainsCallTo fullNames expr
            | SynModuleDecl.NestedModule(decls = nestedDecls) ->
                declsContainCallTo fullNames nestedDecls
            | SynModuleDecl.Let(_, bindings, _) ->
                bindings |> List.exists (fun (SynBinding(expr = expr)) ->
                    exprContainsCallTo fullNames expr)
            | _ -> false)

    /// Check if source text contains a call to any of the given fully-qualified names.
    let sourceContainsCallTo (fullNames: string list) (filePath: string) (sourceText: string) : bool =
        let source = SourceText.ofString sourceText

        let parsingOptions =
            { FSharpParsingOptions.Default with
                SourceFiles = [| filePath |] }

        let parseResults =
            checker.ParseFile(filePath, source, parsingOptions)
            |> Async.RunSynchronously

        match parseResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(contents = modules)) ->
            modules |> List.exists (fun (SynModuleOrNamespace(decls = decls)) ->
                declsContainCallTo fullNames decls)
        | _ -> false

    /// Check if a source file contains a call to any of the given fully-qualified names.
    let fileContainsCallTo (fullNames: string list) (filePath: string) : bool =
        let sourceText = System.IO.File.ReadAllText(filePath)
        sourceContainsCallTo fullNames filePath sourceText
