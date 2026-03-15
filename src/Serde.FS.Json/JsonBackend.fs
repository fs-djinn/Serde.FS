namespace Serde.FS.Json

open Serde.FS
open Serde.FS.Json.Codec

type JsonBackend() =
    interface ISerdeBackend with
        member _.Serialize(value, runtimeType, _options) =
            SerdeMetadata.get runtimeType |> ignore
            let codec = CodecResolver.resolve runtimeType GlobalCodecRegistry.Current
            let jsonValue = codec.Encode(value)
            SerdeJsonWriter.writeToString jsonValue

        member _.Deserialize(json, runtimeType, _options) =
            // Spec 36: Detect missing generic type argument for wrapper DUs
            let isTooGeneric =
                runtimeType = typeof<obj>
                || not runtimeType.IsGenericType
                || runtimeType.IsGenericTypeDefinition
                || runtimeType.GetGenericArguments().Length = 0

            if isTooGeneric then
                try
                    let root = SerdeJsonReader.readFromString json
                    match root with
                    | JsonValue.Object [(caseName, _)] ->
                        match SerdeMetadata.tryFindGenericWrapperByCaseName caseName with
                        | Some wrapperName ->
                            let msg =
                                "Serde.FS: Cannot deserialize a generic wrapper type " +
                                "without specifying the closed generic type.\n\n" +
                                $"The JSON represents a value of type '{wrapperName}<_>'.\n" +
                                $"You must call Deserialize<{wrapperName}<ConcreteType>> to deserialize this value."
                            raise (SerdeMissingMetadataException(msg, runtimeType))
                        | None -> ()
                    | _ -> ()
                with
                | :? SerdeMissingMetadataException -> reraise()
                | _ -> () // JSON parse failed or not wrapper-shaped; fall through

            SerdeMetadata.get runtimeType |> ignore
            let codec = CodecResolver.resolve runtimeType GlobalCodecRegistry.Current
            let jsonValue = SerdeJsonReader.readFromString json
            codec.Decode jsonValue :?> 'T
