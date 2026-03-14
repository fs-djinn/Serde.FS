namespace Serde.FS.Json.Codec

/// Encodes a value of type 'T into a JsonValue AST.
type IJsonEncoder<'T> =
    abstract Encode : 'T -> JsonValue
