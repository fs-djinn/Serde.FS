namespace Serde.FS.Json.Codec

open System
open System.Collections

module CollectionCodecs =

    /// Creates a typed codec for Set<'T> given an element codec.
    let setCodec (elementCodec: IJsonCodec<'T>) : IJsonCodec<Set<'T>> =
        { new IJsonCodec<Set<'T>> with
            member _.Encode s =
                s
                |> Set.toList
                |> List.map elementCodec.Encode
                |> JsonValue.Array

            member _.Decode json =
                match json with
                | JsonValue.Array items ->
                    items
                    |> List.map elementCodec.Decode
                    |> Set.ofList
                | _ -> failwith "Expected JSON array for Set<'T>" }

    /// Factory for constructing Set<'T> codecs dynamically from the registry.
    module SetCodecFactory =
        let create (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec =
            let elemType = typeArgs[0]
            let elemCodec = CodecResolver.resolve elemType registry

            let setType = typedefof<Set<_>>.MakeGenericType(elemType)
            // FSharpSet<'T>.Create : IComparer<'T> -> seq<'T> -> FSharpSet<'T>
            let setOfSeqCtor = setType.GetConstructor([| typedefof<seq<_>>.MakeGenericType(elemType) |])
            // Use the constructor: new FSharpSet<'T>(seq<'T>)

            { new IJsonCodec with
                member _.Type = setType
                member _.Encode obj =
                    // Set<'T> implements IEnumerable
                    let items =
                        [ for elem in (obj :?> IEnumerable) -> elemCodec.Encode elem ]
                    JsonValue.Array items
                member _.Decode json =
                    match json with
                    | JsonValue.Array items ->
                        let decoded = items |> List.map elemCodec.Decode
                        let arr = Array.CreateInstance(elemType, decoded.Length)
                        decoded |> List.iteri (fun i item -> arr.SetValue(item, i))
                        setOfSeqCtor.Invoke([| arr |])
                    | _ -> failwith "Expected JSON array for Set<'T>" }

    /// Factory for constructing T[] codecs dynamically from the registry.
    module ArrayCodecFactory =
        let create (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec =
            let elemType = typeArgs[0]
            let elemCodec = CodecResolver.resolve elemType registry
            let arrayType = elemType.MakeArrayType()

            { new IJsonCodec with
                member _.Type = arrayType
                member _.Encode obj =
                    let items =
                        [ for elem in (obj :?> IEnumerable) -> elemCodec.Encode elem ]
                    JsonValue.Array items
                member _.Decode json =
                    match json with
                    | JsonValue.Array items ->
                        let decoded = items |> List.map elemCodec.Decode
                        let arr = Array.CreateInstance(elemType, decoded.Length)
                        decoded |> List.iteri (fun i item -> arr.SetValue(item, i))
                        arr :> obj
                    | _ -> failwith $"Expected JSON array for %s{arrayType.Name}" }

    /// Factory for constructing List<'T> codecs dynamically from the registry.
    module ListCodecFactory =
        let create (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec =
            let elemType = typeArgs[0]
            let elemCodec = CodecResolver.resolve elemType registry
            let listType = typedefof<list<_>>.MakeGenericType(elemType)

            // Build an F# list from decoded elements using List.ofSeq via reflection
            let listModule = typedefof<list<_>>.Assembly.GetType("Microsoft.FSharp.Collections.ListModule")
            let listOfArrayMethod = listModule.GetMethod("OfSeq").MakeGenericMethod(elemType)

            { new IJsonCodec with
                member _.Type = listType
                member _.Encode obj =
                    let items =
                        [ for elem in (obj :?> IEnumerable) -> elemCodec.Encode elem ]
                    JsonValue.Array items
                member _.Decode json =
                    match json with
                    | JsonValue.Array items ->
                        let decoded = items |> List.map elemCodec.Decode
                        let arr = Array.CreateInstance(elemType, decoded.Length)
                        decoded |> List.iteri (fun i item -> arr.SetValue(item, i))
                        listOfArrayMethod.Invoke(null, [| arr |])
                    | _ -> failwith $"Expected JSON array for %s{listType.Name}" }

    /// Factory for constructing Map<'K,'V> codecs dynamically from the registry.
    module MapCodecFactory =
        let create (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec =
            let keyType = typeArgs[0]
            let valType = typeArgs[1]
            let keyCodec = CodecResolver.resolve keyType registry
            let valCodec = CodecResolver.resolve valType registry
            let mapType = typedefof<Map<_,_>>.MakeGenericType(keyType, valType)

            // Build an F# Map from key-value pairs using Map.ofSeq via reflection
            let tupleType = typedefof<System.Tuple<_,_>>.MakeGenericType(keyType, valType)
            let tupleCtor = tupleType.GetConstructor([| keyType; valType |])
            let mapOfSeqMethod =
                typeof<Map<_,_>>.Assembly
                    .GetType("Microsoft.FSharp.Collections.MapModule")
                    .GetMethod("OfSeq")
                    .MakeGenericMethod(keyType, valType)

            { new IJsonCodec with
                member _.Type = mapType
                member _.Encode obj =
                    let items =
                        [ for kvp in (obj :?> IEnumerable) ->
                            let kvpType = kvp.GetType()
                            let key = kvpType.GetProperty("Key").GetValue(kvp)
                            let value = kvpType.GetProperty("Value").GetValue(kvp)
                            JsonValue.Array [ keyCodec.Encode key; valCodec.Encode value ] ]
                    JsonValue.Array items
                member _.Decode json =
                    match json with
                    | JsonValue.Array items ->
                        let pairs =
                            items
                            |> List.map (fun item ->
                                match item with
                                | JsonValue.Array [ k; v ] ->
                                    let key = keyCodec.Decode k
                                    let value = valCodec.Decode v
                                    tupleCtor.Invoke([| key; value |])
                                | _ -> failwith "Expected [key, value] pair in Map")
                        let arr = Array.CreateInstance(tupleType, pairs.Length)
                        pairs |> List.iteri (fun i item -> arr.SetValue(item, i))
                        mapOfSeqMethod.Invoke(null, [| arr |])
                    | _ -> failwith $"Expected JSON array for %s{mapType.Name}" }
