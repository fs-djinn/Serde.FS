namespace Serde.FS.Json.Codec

/// Decodes a JsonValue AST into a value of type 'T.
type IJsonDecoder<'T> =
    abstract Decode : JsonValue -> 'T
