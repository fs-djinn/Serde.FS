namespace Serde.FS.Json.Codec

open System
open System.Text.Json
open System.Text.Json.Serialization

/// Converts between the codec JsonValue AST and STJ's Utf8JsonWriter/Utf8JsonReader.
module JsonValueBridge =

    /// Writes a JsonValue to a Utf8JsonWriter.
    let rec writeJsonValue (writer: Utf8JsonWriter) (value: JsonValue) =
        match value with
        | JsonValue.Null -> writer.WriteNullValue()
        | JsonValue.Bool b -> writer.WriteBooleanValue(b)
        | JsonValue.Number n -> writer.WriteNumberValue(n)
        | JsonValue.String s -> writer.WriteStringValue(s)
        | JsonValue.Array items ->
            writer.WriteStartArray()
            for item in items do
                writeJsonValue writer item
            writer.WriteEndArray()
        | JsonValue.Object fields ->
            writer.WriteStartObject()
            for (name, fieldValue) in fields do
                writer.WritePropertyName(name)
                writeJsonValue writer fieldValue
            writer.WriteEndObject()

    /// Reads a JsonValue from a Utf8JsonReader.
    let rec readJsonValue (reader: byref<Utf8JsonReader>) : JsonValue =
        match reader.TokenType with
        | JsonTokenType.Null -> JsonValue.Null
        | JsonTokenType.True -> JsonValue.Bool true
        | JsonTokenType.False -> JsonValue.Bool false
        | JsonTokenType.Number -> JsonValue.Number(reader.GetDecimal())
        | JsonTokenType.String -> JsonValue.String(reader.GetString())
        | JsonTokenType.StartArray ->
            let mutable items = []
            while reader.Read() && reader.TokenType <> JsonTokenType.EndArray do
                items <- items @ [ readJsonValue &reader ]
            JsonValue.Array items
        | JsonTokenType.StartObject ->
            let mutable fields = []
            while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
                let name = reader.GetString()
                reader.Read() |> ignore
                let value = readJsonValue &reader
                fields <- fields @ [ (name, value) ]
            JsonValue.Object fields
        | _ ->
            failwithf "Unexpected JSON token: %A" reader.TokenType

/// A JsonConverter<'T> that delegates to an IJsonCodec<'T> via the JsonValue AST bridge.
type CodecConverter<'T>(codec: IJsonCodec<'T>) =
    inherit JsonConverter<'T>()

    override _.Read(reader, _typeToConvert, _options) =
        let jsonValue = JsonValueBridge.readJsonValue &reader
        codec.Decode jsonValue

    override _.Write(writer, value, _options) =
        let jsonValue = codec.Encode value
        JsonValueBridge.writeJsonValue writer jsonValue

/// A JsonConverter that delegates to an untyped IJsonCodec via the JsonValue AST bridge.
type UntypedCodecConverter(codec: IJsonCodec) =
    inherit JsonConverter<obj>()

    override _.Read(reader, _typeToConvert, _options) =
        let jsonValue = JsonValueBridge.readJsonValue &reader
        codec.Decode jsonValue

    override _.Write(writer, value, _options) =
        let jsonValue = codec.Encode value
        JsonValueBridge.writeJsonValue writer jsonValue
