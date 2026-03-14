namespace Serde.FS.Json.Codec

open System
open System.Collections.Generic

/// Immutable codec registry that maps types to codecs.
type CodecRegistry =
    { Codecs: Dictionary<Type, IJsonCodec> }

module CodecRegistry =
    /// Creates an empty registry.
    let empty : CodecRegistry =
        { Codecs = Dictionary<Type, IJsonCodec>() }

    /// Registers a codec for the given type. Last write wins.
    let add (ty: Type, codec: IJsonCodec) (registry: CodecRegistry) : CodecRegistry =
        let newCodecs = Dictionary<Type, IJsonCodec>(registry.Codecs)
        newCodecs[ty] <- codec
        { Codecs = newCodecs }

    /// Looks up a codec by type.
    let tryFind (ty: Type) (registry: CodecRegistry) : IJsonCodec option =
        match registry.Codecs.TryGetValue(ty) with
        | true, codec -> Some codec
        | _ -> None

    /// Creates a registry pre-populated with all primitive codecs.
    let withPrimitives () : CodecRegistry =
        empty
        |> add (typeof<bool>, PrimitiveCodecs.boolCodec |> JsonCodec.boxCodec)
        |> add (typeof<string>, PrimitiveCodecs.stringCodec |> JsonCodec.boxCodec)
        |> add (typeof<decimal>, PrimitiveCodecs.decimalCodec |> JsonCodec.boxCodec)
        |> add (typeof<int>, PrimitiveCodecs.intCodec |> JsonCodec.boxCodec)
        |> add (typeof<int64>, PrimitiveCodecs.int64Codec |> JsonCodec.boxCodec)
        |> add (typeof<float>, PrimitiveCodecs.floatCodec |> JsonCodec.boxCodec)
        |> add (typeof<unit>, PrimitiveCodecs.unitCodec |> JsonCodec.boxCodec)
        |> add (typeof<byte[]>, PrimitiveCodecs.byteArrayCodec |> JsonCodec.boxCodec)
        |> add (typeof<Guid>, PrimitiveCodecs.guidCodec |> JsonCodec.boxCodec)
        |> add (typeof<DateTime>, PrimitiveCodecs.dateTimeCodec |> JsonCodec.boxCodec)
        |> add (typeof<DateTimeOffset>, PrimitiveCodecs.dateTimeOffsetCodec |> JsonCodec.boxCodec)

/// Global mutable registry for framework and app-level registration.
module GlobalCodecRegistry =
    let mutable Current : CodecRegistry = CodecRegistry.withPrimitives ()
