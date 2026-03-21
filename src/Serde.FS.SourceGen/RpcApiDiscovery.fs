namespace Serde.FS.SourceGen

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FSharp.SourceDjinn.TypeModel.Types
open Serde.FS

/// Discovers types transitively referenced from [<RpcApi>] interfaces.
/// Uses FSharpChecker directly because SourceDjinn does not parse interfaces.
module internal RpcApiDiscovery =

    let private checker = FSharpChecker.Create()

    let private identToString (idents: LongIdent) =
        idents |> List.map (fun i -> i.idText) |> String.concat "."

    let private rpcApiAttrNames = set [ "RpcApi"; "RpcApiAttribute" ]

    /// Names of types to skip during closure computation (primitives, wrappers, collections).
    let private skipTypeNames =
        set [
            "unit"; "bool"; "string"; "int"; "int8"; "int16"; "int32"; "int64"
            "uint8"; "uint16"; "uint32"; "uint64"; "byte"
            "float"; "float32"; "double"; "single"; "decimal"
            "sbyte"
            "Guid"; "System.Guid"
            "DateTime"; "System.DateTime"
            "DateTimeOffset"; "System.DateTimeOffset"
            "TimeSpan"; "System.TimeSpan"
            "DateOnly"; "System.DateOnly"
            "TimeOnly"; "System.TimeOnly"
            "Async"; "Task"; "System.Threading.Tasks.Task"
            "option"; "Option"
            "list"; "List"
            "array"; "Array"
            "Set"; "Map"
            "seq"; "Seq"
            "Result"
        ]

    /// Recursively extract all type names referenced in a SynType.
    /// Unwraps Async<T>, Task<T>, and function types (A -> B).
    let rec private collectTypeNames (synType: SynType) : string list =
        match synType with
        | SynType.LongIdent(SynLongIdent(id = idents)) ->
            let name = identToString idents
            if skipTypeNames.Contains name then []
            else [ name ]

        | SynType.App(typeName, _, typeArgs, _, _, _, _) ->
            let baseName =
                match typeName with
                | SynType.LongIdent(SynLongIdent(id = idents)) -> identToString idents
                | _ -> ""
            // If this is Async<T>, Task<T>, option<T>, list<T>, etc. — unwrap and collect from args only
            if skipTypeNames.Contains baseName then
                typeArgs |> List.collect collectTypeNames
            else
                // User-defined generic type — collect the base name + args
                [ baseName ] @ (typeArgs |> List.collect collectTypeNames)

        | SynType.Fun(argType, returnType, _, _) ->
            collectTypeNames argType @ collectTypeNames returnType

        | SynType.Tuple(_, segments, _) ->
            segments
            |> List.collect (fun seg ->
                match seg with
                | SynTupleTypeSegment.Type t -> collectTypeNames t
                | _ -> [])

        | SynType.Paren(innerType, _) ->
            collectTypeNames innerType

        | SynType.Array(_, elementType, _) ->
            collectTypeNames elementType

        | SynType.Var _ -> [] // generic parameter like 'T — skip
        | _ -> []

    /// Extract type names from an abstract member's SynValSig.
    let private extractFromValSig (valSig: SynValSig) : string list =
        let (SynValSig(synType = synType)) = valSig
        collectTypeNames synType

    /// Check if a SynComponentInfo has the [<RpcApi>] attribute.
    let private hasRpcApiAttr (synComponentInfo: SynComponentInfo) : bool =
        let (SynComponentInfo(attributes = attrs)) = synComponentInfo
        attrs
        |> List.exists (fun attrList ->
            attrList.Attributes
            |> List.exists (fun attr ->
                match attr.TypeName with
                | SynLongIdent(id = idents) ->
                    let name = identToString idents
                    rpcApiAttrNames.Contains name))

    /// Walk a parsed AST to find [<RpcApi>] interfaces and extract type names from their members.
    let private findRpcApiTypeNames (filePath: string) (sourceText: string) : string list =
        let source = SourceText.ofString sourceText
        let parsingOptions = { FSharpParsingOptions.Default with SourceFiles = [| filePath |] }
        let parseResults = checker.ParseFile(filePath, source, parsingOptions) |> Async.RunSynchronously

        let typeNames = ResizeArray<string>()

        let rec walkTypeDefn (typeDefn: SynTypeDefn) =
            let (SynTypeDefn(typeInfo = synComponentInfo; typeRepr = typeRepr; members = members)) = typeDefn
            if hasRpcApiAttr synComponentInfo then
                // Extract from ObjectModel representation (interface members)
                match typeRepr with
                | SynTypeDefnRepr.ObjectModel(_, memberDefns, _) ->
                    for memberDefn in memberDefns do
                        match memberDefn with
                        | SynMemberDefn.AbstractSlot(slotSig, _, _, _) ->
                            typeNames.AddRange(extractFromValSig slotSig)
                        | _ -> ()
                | _ -> ()
                // Also check augmentation members
                for memberDefn in members do
                    match memberDefn with
                    | SynMemberDefn.AbstractSlot(slotSig, _, _, _) ->
                        typeNames.AddRange(extractFromValSig slotSig)
                    | _ -> ()

        let rec walkDecls (decls: SynModuleDecl list) =
            for decl in decls do
                match decl with
                | SynModuleDecl.Types(typeDefns, _) ->
                    for td in typeDefns do walkTypeDefn td
                | SynModuleDecl.NestedModule(decls = nestedDecls) ->
                    walkDecls nestedDecls
                | _ -> ()

        match parseResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(contents = modules)) ->
            for SynModuleOrNamespace(decls = decls) in modules do
                walkDecls decls
        | _ -> ()

        Seq.toList typeNames

    /// Build a lookup map from short type name to TypeInfo.
    let private buildLookup (allTypeInfos: TypeInfo list) : Map<string, TypeInfo> =
        allTypeInfos
        |> List.map (fun ti -> ti.TypeName, ti)
        |> Map.ofList

    /// Recursively collect all type names from a TypeInfo's fields and union cases.
    let rec private collectTransitiveTypeNames (lookup: Map<string, TypeInfo>) (visited: Set<string>) (typeName: string) : Set<string> =
        if visited.Contains typeName then visited
        else
            match Map.tryFind typeName lookup with
            | None -> visited
            | Some ti ->
                let visited = visited.Add typeName
                let fieldTypes =
                    match ti.Kind with
                    | Record fields | AnonymousRecord fields ->
                        fields |> List.collect (fun f -> extractTypeNamesFromTypeInfo f.Type)
                    | Union cases ->
                        cases |> List.collect (fun c -> c.Fields |> List.collect (fun f -> extractTypeNamesFromTypeInfo f.Type))
                    | _ -> []
                fieldTypes
                |> List.filter (fun n -> not (skipTypeNames.Contains n))
                |> List.fold (fun acc n -> collectTransitiveTypeNames lookup acc n) visited

    /// Extract type names from a SourceDjinn TypeInfo, unwrapping collections/options.
    and private extractTypeNamesFromTypeInfo (ti: TypeInfo) : string list =
        match ti.Kind with
        | Primitive _ | GenericParameter _ | GenericTypeDefinition _ -> []
        | Record _ | Union _ | Enum _ | AnonymousRecord _ ->
            if skipTypeNames.Contains ti.TypeName then []
            else [ ti.TypeName ]
        | Option inner | List inner | Array inner | Set inner ->
            extractTypeNamesFromTypeInfo inner
        | Map (k, v) ->
            extractTypeNamesFromTypeInfo k @ extractTypeNamesFromTypeInfo v
        | Tuple fields ->
            fields |> List.collect (fun f -> extractTypeNamesFromTypeInfo f.Type)
        | ConstructedGenericType ->
            let baseName =
                if skipTypeNames.Contains ti.TypeName then []
                else [ ti.TypeName ]
            baseName @ (ti.GenericArguments |> List.collect extractTypeNamesFromTypeInfo)

    /// Discover all types transitively referenced from [<RpcApi>] interfaces.
    /// Returns SerdeTypeInfo list for types that need codec generation.
    let discover (allTypeInfos: TypeInfo list) (sourceFiles: (string * string) list) : SerdeTypeInfo list =
        // Step 1: Find all type names referenced in [<RpcApi>] interface members
        let rootTypeNames =
            sourceFiles
            |> List.collect (fun (filePath, sourceText) ->
                if filePath.EndsWith(".fs") then
                    try findRpcApiTypeNames filePath sourceText
                    with _ -> []
                else [])
            |> List.distinct

        if rootTypeNames.IsEmpty then []
        else
            // Step 2: Build lookup and compute transitive closure
            let lookup = buildLookup allTypeInfos
            let allDiscoveredNames =
                rootTypeNames
                |> List.filter (fun n -> not (skipTypeNames.Contains n))
                |> List.fold (fun acc name -> collectTransitiveTypeNames lookup acc name) Set.empty

            // Step 3: Build SerdeTypeInfo for each discovered type
            allDiscoveredNames
            |> Set.toList
            |> List.choose (fun name -> Map.tryFind name lookup)
            |> List.filter (fun ti ->
                match ti.Kind with
                | Record _ | Union _ | Enum _ | AnonymousRecord _ -> true
                | _ -> false)
            |> List.map SerdeMetadataBuilder.buildSerdeTypeInfo
