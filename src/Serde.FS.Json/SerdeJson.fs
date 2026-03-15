module Serde.FS.Json.SerdeJson

open System
open System.Buffers
open System.Text
open System.Text.Json
open Serde.FS
open Serde.FS.Json.Codec

/// Sets System.Text.Json as the default backend for Serde.FS.
/// Call once at application startup.
let private triggerBootstrap () =
    match global.Serde.ResolverBootstrap.registerAll with
    | Some _ -> ()
    | None ->
        let asm = System.Reflection.Assembly.GetEntryAssembly()
        if not (isNull asm) then
            match asm.GetType("Djinn.Generated.Bootstrap") with
            | null -> ()
            | ty ->
                match ty.GetMethod("init", System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.Static) with
                | null -> ()
                | m -> m.Invoke(null, [||]) |> ignore

let useAsDefault () =
    triggerBootstrap ()
    match global.Serde.ResolverBootstrap.registerAll with
    | Some f -> f()
    | None -> ()
    match Serde.DefaultBackend with
    | Some (:? JsonBackend) -> ()
    | _ -> Serde.DefaultBackend <- Some (JsonBackend() :> ISerdeBackend)

/// The global JSON backend options instance.
let options = SerdeJsonDefaults.options

/// Apply a configuration function to the global JSON backend options.
let configure (f: SerdeJsonOptions -> unit) = f options

// ---------------------------------------------------------------------------
// Codec-driven serialization / deserialization
// ---------------------------------------------------------------------------

/// Resolves the codec for 'T and encodes the value to a JsonValue.
let private encodeToJsonValue<'T> (value: 'T) : JsonValue =
    let codec = CodecResolver.resolve typeof<'T> GlobalCodecRegistry.Current
    codec.Encode(box value)

/// Resolves the codec for 'T and decodes a JsonValue to 'T.
let private decodeFromJsonValue<'T> (jsonValue: JsonValue) : 'T =
    let codec = CodecResolver.resolve typeof<'T> GlobalCodecRegistry.Current
    codec.Decode jsonValue :?> 'T

/// Parses a JSON string into a JsonValue tree.
let private parseJsonValue (json: string) : JsonValue =
    try
        let bytes = Encoding.UTF8.GetBytes(json)
        let mutable reader = Utf8JsonReader(ReadOnlySpan<byte>(bytes))
        reader.Read() |> ignore
        JsonValueBridge.readJsonValue &reader
    with
    | :? SerdeJsonException -> reraise()
    | :? JsonException as ex ->
        raise (SerdeJsonException("Failed to parse JSON input.", ex))
    | ex ->
        raise (SerdeJsonException("Failed to parse JSON input.", ex))

/// Parses a UTF-8 byte array into a JsonValue tree.
let private parseJsonValueFromUtf8 (bytes: byte[]) : JsonValue =
    try
        let mutable reader = Utf8JsonReader(ReadOnlySpan<byte>(bytes))
        reader.Read() |> ignore
        JsonValueBridge.readJsonValue &reader
    with
    | :? SerdeJsonException -> reraise()
    | :? JsonException as ex ->
        raise (SerdeJsonException("Failed to parse JSON input.", ex))
    | ex ->
        raise (SerdeJsonException("Failed to parse JSON input.", ex))

/// Writes a JsonValue to a UTF-8 byte array.
let private writeJsonValueToUtf8 (jsonValue: JsonValue) : byte[] =
    let buffer = ArrayBufferWriter<byte>()
    use writer = new Utf8JsonWriter(buffer)
    JsonValueBridge.writeJsonValue writer jsonValue
    writer.Flush()
    buffer.WrittenSpan.ToArray()

/// Codec-driven serialization of a value to a JSON string.
/// Does not use JsonSerializer, STJ converters, or reflection.
let serialize<'T> (value: 'T) : string =
    let jsonValue = encodeToJsonValue<'T> value
    let bytes = writeJsonValueToUtf8 jsonValue
    Encoding.UTF8.GetString(bytes)

/// Codec-driven serialization of a value to a UTF-8 byte array.
/// Does not use JsonSerializer, STJ converters, or reflection.
let serializeToUtf8<'T> (value: 'T) : byte[] =
    let jsonValue = encodeToJsonValue<'T> value
    writeJsonValueToUtf8 jsonValue

/// Codec-driven deserialization of a JSON string to a value of type 'T.
/// Does not use JsonSerializer, STJ converters, or reflection.
let deserialize<'T> (json: string) : 'T =
    let jsonValue = parseJsonValue json
    decodeFromJsonValue<'T> jsonValue

/// Codec-driven deserialization of a UTF-8 byte array to a value of type 'T.
/// Does not use JsonSerializer, STJ converters, or reflection.
let deserializeFromUtf8<'T> (bytes: byte[]) : 'T =
    let jsonValue = parseJsonValueFromUtf8 bytes
    decodeFromJsonValue<'T> jsonValue
