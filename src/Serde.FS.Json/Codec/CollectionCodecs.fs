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
            let elemCodec =
                match CodecRegistry.tryFind elemType registry with
                | Some c -> c
                | None -> failwith $"No codec found for Set element type '{elemType.FullName}'"

            let setType = typedefof<Set<_>>.MakeGenericType(elemType)
            // FSharpSet<'T>.Create : IComparer<'T> -> seq<'T> -> FSharpSet<'T>
            // Use the constructor: new FSharpSet<'T>(seq<'T>)
            let setOfSeqCtor = setType.GetConstructor([| typedefof<seq<_>>.MakeGenericType(elemType) |])

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
