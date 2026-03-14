namespace Serde.FS.Json.Codec

open System

module PrimitiveCodecs =
    let boolEncoder : IJsonEncoder<bool> =
        { new IJsonEncoder<bool> with
            member _.Encode v = JsonValue.Bool v }

    let boolDecoder : IJsonDecoder<bool> =
        { new IJsonDecoder<bool> with
            member _.Decode json =
                match json with
                | JsonValue.Bool b -> b
                | _ -> failwith "Expected JSON bool" }

    let stringEncoder : IJsonEncoder<string> =
        { new IJsonEncoder<string> with
            member _.Encode v = JsonValue.String v }

    let stringDecoder : IJsonDecoder<string> =
        { new IJsonDecoder<string> with
            member _.Decode json =
                match json with
                | JsonValue.String s -> s
                | _ -> failwith "Expected JSON string" }

    let decimalEncoder : IJsonEncoder<decimal> =
        { new IJsonEncoder<decimal> with
            member _.Encode v = JsonValue.Number v }

    let decimalDecoder : IJsonDecoder<decimal> =
        { new IJsonDecoder<decimal> with
            member _.Decode json =
                match json with
                | JsonValue.Number n -> n
                | _ -> failwith "Expected JSON number" }

    let intEncoder : IJsonEncoder<int> =
        { new IJsonEncoder<int> with
            member _.Encode v = JsonValue.Number (decimal v) }

    let intDecoder : IJsonDecoder<int> =
        { new IJsonDecoder<int> with
            member _.Decode json =
                match json with
                | JsonValue.Number n -> int n
                | _ -> failwith "Expected JSON number" }

    let int64Encoder : IJsonEncoder<int64> =
        { new IJsonEncoder<int64> with
            member _.Encode v = JsonValue.Number (decimal v) }

    let int64Decoder : IJsonDecoder<int64> =
        { new IJsonDecoder<int64> with
            member _.Decode json =
                match json with
                | JsonValue.Number n -> int64 n
                | _ -> failwith "Expected JSON number" }

    let floatEncoder : IJsonEncoder<float> =
        { new IJsonEncoder<float> with
            member _.Encode v = JsonValue.Number (decimal v) }

    let floatDecoder : IJsonDecoder<float> =
        { new IJsonDecoder<float> with
            member _.Decode json =
                match json with
                | JsonValue.Number n -> float n
                | _ -> failwith "Expected JSON number" }

    /// Generic adapter that wraps a decode function. Used to work around the F#
    /// limitation that prevents implementing IJsonDecoder<unit> via object expression.
    type private FuncDecoder<'T>(decode: JsonValue -> obj) =
        interface IJsonDecoder<'T> with
            member _.Decode json = decode json |> unbox<'T>

    let unitEncoder : IJsonEncoder<unit> =
        { new IJsonEncoder<unit> with
            member _.Encode _ = JsonValue.Null }

    let unitDecoder : IJsonDecoder<unit> =
        FuncDecoder<unit>(fun json ->
            match json with
            | JsonValue.Null -> box ()
            | _ -> failwith "Expected JSON null")
        :> IJsonDecoder<unit>

    let byteArrayEncoder : IJsonEncoder<byte[]> =
        { new IJsonEncoder<byte[]> with
            member _.Encode bytes = JsonValue.String (Convert.ToBase64String bytes) }

    let byteArrayDecoder : IJsonDecoder<byte[]> =
        { new IJsonDecoder<byte[]> with
            member _.Decode json =
                match json with
                | JsonValue.String base64 -> Convert.FromBase64String base64
                | _ -> failwith "Expected JSON string (Base64)" }

    let guidEncoder : IJsonEncoder<Guid> =
        { new IJsonEncoder<Guid> with
            member _.Encode v = JsonValue.String (v.ToString("D")) }

    let guidDecoder : IJsonDecoder<Guid> =
        { new IJsonDecoder<Guid> with
            member _.Decode json =
                match json with
                | JsonValue.String s -> Guid.Parse s
                | _ -> failwith "Expected JSON string (Guid)" }

    let dateTimeEncoder : IJsonEncoder<DateTime> =
        { new IJsonEncoder<DateTime> with
            member _.Encode v = JsonValue.String (v.ToString("O")) }

    let dateTimeDecoder : IJsonDecoder<DateTime> =
        { new IJsonDecoder<DateTime> with
            member _.Decode json =
                match json with
                | JsonValue.String s -> DateTime.Parse(s, null, Globalization.DateTimeStyles.RoundtripKind)
                | _ -> failwith "Expected JSON string (DateTime)" }

    let dateTimeOffsetEncoder : IJsonEncoder<DateTimeOffset> =
        { new IJsonEncoder<DateTimeOffset> with
            member _.Encode v = JsonValue.String (v.ToString("O")) }

    let dateTimeOffsetDecoder : IJsonDecoder<DateTimeOffset> =
        { new IJsonDecoder<DateTimeOffset> with
            member _.Decode json =
                match json with
                | JsonValue.String s -> DateTimeOffset.Parse(s, null, Globalization.DateTimeStyles.RoundtripKind)
                | _ -> failwith "Expected JSON string (DateTimeOffset)" }

    // -- Combined codecs --

    let boolCodec = JsonCodec.fromPair boolEncoder boolDecoder
    let stringCodec = JsonCodec.fromPair stringEncoder stringDecoder
    let decimalCodec = JsonCodec.fromPair decimalEncoder decimalDecoder
    let intCodec = JsonCodec.fromPair intEncoder intDecoder
    let int64Codec = JsonCodec.fromPair int64Encoder int64Decoder
    let floatCodec = JsonCodec.fromPair floatEncoder floatDecoder
    let unitCodec = JsonCodec.fromPair unitEncoder unitDecoder
    let byteArrayCodec = JsonCodec.fromPair byteArrayEncoder byteArrayDecoder
    let guidCodec = JsonCodec.fromPair guidEncoder guidDecoder
    let dateTimeCodec = JsonCodec.fromPair dateTimeEncoder dateTimeDecoder
    let dateTimeOffsetCodec = JsonCodec.fromPair dateTimeOffsetEncoder dateTimeOffsetDecoder
