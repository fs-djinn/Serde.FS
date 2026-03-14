namespace Serde.FS.Json.Codec

open System

/// Strongly typed codec that combines encoding and decoding for type 'T.
[<Interface>]
type IJsonCodec<'T> =
    abstract Encode : 'T -> JsonValue
    abstract Decode : JsonValue -> 'T

/// Untyped codec interface for registry storage.
[<Interface>]
type IJsonCodec =
    abstract Type : Type
    abstract Encode : obj -> JsonValue
    abstract Decode : JsonValue -> obj

module JsonCodec =
    /// Creates a JsonCodec<'T> from an encoder and decoder pair.
    let fromPair (encoder: IJsonEncoder<'T>) (decoder: IJsonDecoder<'T>) : IJsonCodec<'T> =
        { new IJsonCodec<'T> with
            member _.Encode v = encoder.Encode v
            member _.Decode json = decoder.Decode json }

    /// Wraps a strongly typed JsonCodec<'T> into an untyped IJsonCodec for registry storage.
    let boxCodec<'T> (codec: IJsonCodec<'T>) : IJsonCodec =
        { new IJsonCodec with
            member _.Type = typeof<'T>
            member _.Encode obj = codec.Encode(unbox<'T> obj)
            member _.Decode json = box (codec.Decode json) }
