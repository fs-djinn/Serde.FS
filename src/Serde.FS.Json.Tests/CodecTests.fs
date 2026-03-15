module Serde.FS.Json.Tests.CodecTests

open System
open NUnit.Framework
open Serde.FS.Json.Codec

// -- JsonValue construction tests --

[<Test>]
let ``JsonValue Null constructs correctly`` () =
    let v = Null
    Assert.AreEqual("null", JsonValue.toString v)

[<Test>]
let ``JsonValue Bool constructs correctly`` () =
    Assert.AreEqual("true", JsonValue.toString (Bool true))
    Assert.AreEqual("false", JsonValue.toString (Bool false))

[<Test>]
let ``JsonValue Number constructs correctly`` () =
    Assert.AreEqual("42", JsonValue.toString (Number 42m))

[<Test>]
let ``JsonValue String constructs correctly`` () =
    Assert.AreEqual("\"hello\"", JsonValue.toString (String "hello"))

[<Test>]
let ``JsonValue Array constructs correctly`` () =
    let v = Array [ Number 1m; String "two"; Bool true ]
    Assert.AreEqual("[1, \"two\", true]", JsonValue.toString v)

[<Test>]
let ``JsonValue Object constructs correctly`` () =
    let v = Object [ "name", String "Alice"; "age", Number 30m ]
    Assert.AreEqual("{\"name\": \"Alice\", \"age\": 30}", JsonValue.toString v)

// -- JsonCodec adapter tests --

[<Test>]
let ``JsonCodec fromPair creates working codec`` () =
    let codec = JsonCodec.fromPair PrimitiveCodecs.intEncoder PrimitiveCodecs.intDecoder
    Assert.AreEqual(Number 42m, codec.Encode 42)
    Assert.AreEqual(42, codec.Decode(Number 42m))

[<Test>]
let ``JsonCodec boxCodec wraps typed codec`` () =
    let typed = PrimitiveCodecs.intCodec
    let boxed = JsonCodec.boxCodec typed
    Assert.AreEqual(typeof<int>, boxed.Type)
    Assert.AreEqual(Number 42m, boxed.Encode(box 42))
    Assert.AreEqual(box 42, boxed.Decode(Number 42m))

// -- CodecRegistry tests --

[<Test>]
let ``CodecRegistry add and tryFind work`` () =
    let codec = PrimitiveCodecs.intCodec |> JsonCodec.boxCodec
    let registry = CodecRegistry.empty |> CodecRegistry.add (typeof<int>, codec)

    let found = CodecRegistry.tryFind typeof<int> registry
    Assert.IsTrue(found.IsSome)
    Assert.AreEqual(Number 42m, found.Value.Encode(box 42))

[<Test>]
let ``CodecRegistry tryFind returns None for missing types`` () =
    let found = CodecRegistry.tryFind typeof<string> CodecRegistry.empty
    Assert.IsTrue(found.IsNone)

[<Test>]
let ``CodecRegistry add overwrites existing codec`` () =
    let codec1 = PrimitiveCodecs.intCodec |> JsonCodec.boxCodec
    let codec2 =
        { new IJsonCodec<int> with
            member _.Encode v = JsonValue.String (string v)
            member _.Decode json =
                match json with
                | JsonValue.String s -> int s
                | _ -> failwith "Expected string" }
        |> JsonCodec.boxCodec

    let registry =
        CodecRegistry.empty
        |> CodecRegistry.add (typeof<int>, codec1)
        |> CodecRegistry.add (typeof<int>, codec2)

    let found = CodecRegistry.tryFind typeof<int> registry
    Assert.IsTrue(found.IsSome)
    // Should use the second codec (last write wins)
    Assert.AreEqual(JsonValue.String "42", found.Value.Encode(box 42))

// -- Primitive codec encoding tests --

[<Test>]
let ``PrimitiveCodecs bool encodes correctly`` () =
    Assert.AreEqual(Bool true, PrimitiveCodecs.boolEncoder.Encode true)
    Assert.AreEqual(Bool false, PrimitiveCodecs.boolEncoder.Encode false)

[<Test>]
let ``PrimitiveCodecs string encodes correctly`` () =
    Assert.AreEqual(String "abc", PrimitiveCodecs.stringEncoder.Encode "abc")

[<Test>]
let ``PrimitiveCodecs int encodes correctly`` () =
    Assert.AreEqual(Number 42m, PrimitiveCodecs.intEncoder.Encode 42)

[<Test>]
let ``PrimitiveCodecs int64 encodes correctly`` () =
    Assert.AreEqual(Number 123456789m, PrimitiveCodecs.int64Encoder.Encode 123456789L)

[<Test>]
let ``PrimitiveCodecs float encodes correctly`` () =
    Assert.AreEqual(Number 1.5m, PrimitiveCodecs.floatEncoder.Encode 1.5)

[<Test>]
let ``PrimitiveCodecs decimal encodes correctly`` () =
    Assert.AreEqual(Number 3.14m, PrimitiveCodecs.decimalEncoder.Encode 3.14m)

[<Test>]
let ``PrimitiveCodecs unit encodes correctly`` () =
    Assert.AreEqual(Null, PrimitiveCodecs.unitEncoder.Encode ())

[<Test>]
let ``PrimitiveCodecs byte array encodes as Base64`` () =
    let bytes = [| 1uy; 2uy; 3uy |]
    let expected = String (Convert.ToBase64String bytes)
    Assert.AreEqual(expected, PrimitiveCodecs.byteArrayEncoder.Encode bytes)

[<Test>]
let ``PrimitiveCodecs Guid encodes as string`` () =
    let g = Guid("12345678-1234-1234-1234-123456789abc")
    Assert.AreEqual(String "12345678-1234-1234-1234-123456789abc", PrimitiveCodecs.guidEncoder.Encode g)

[<Test>]
let ``PrimitiveCodecs DateTime encodes as ISO 8601`` () =
    let dt = DateTime(2026, 3, 14, 10, 30, 0, DateTimeKind.Utc)
    let encoded = PrimitiveCodecs.dateTimeEncoder.Encode dt
    match encoded with
    | String s -> Assert.IsTrue(s.StartsWith("2026-03-14"))
    | _ -> Assert.Fail("Expected JSON string")

[<Test>]
let ``PrimitiveCodecs DateTimeOffset encodes as ISO 8601`` () =
    let dto = DateTimeOffset(2026, 3, 14, 10, 30, 0, TimeSpan.Zero)
    let encoded = PrimitiveCodecs.dateTimeOffsetEncoder.Encode dto
    match encoded with
    | String s -> Assert.IsTrue(s.StartsWith("2026-03-14"))
    | _ -> Assert.Fail("Expected JSON string")

[<Test>]
let ``PrimitiveCodecs DateOnly encodes as ISO 8601`` () =
    let d = DateOnly(2026, 3, 14)
    let encoded = PrimitiveCodecs.dateOnlyEncoder.Encode d
    match encoded with
    | String s -> Assert.AreEqual("2026-03-14", s)
    | _ -> Assert.Fail("Expected JSON string")

[<Test>]
let ``PrimitiveCodecs TimeOnly encodes as ISO 8601`` () =
    let t = TimeOnly(10, 30, 0)
    let encoded = PrimitiveCodecs.timeOnlyEncoder.Encode t
    match encoded with
    | String s -> Assert.IsTrue(s.StartsWith("10:30:00"))
    | _ -> Assert.Fail("Expected JSON string")

// -- Primitive codec round-trip tests --

[<Test>]
let ``PrimitiveCodecs bool round-trips`` () =
    let v = true
    Assert.AreEqual(v, PrimitiveCodecs.boolDecoder.Decode(PrimitiveCodecs.boolEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs string round-trips`` () =
    let v = "hello"
    Assert.AreEqual(v, PrimitiveCodecs.stringDecoder.Decode(PrimitiveCodecs.stringEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs int round-trips`` () =
    let v = 99
    Assert.AreEqual(v, PrimitiveCodecs.intDecoder.Decode(PrimitiveCodecs.intEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs int64 round-trips`` () =
    let v = 9876543210L
    Assert.AreEqual(v, PrimitiveCodecs.int64Decoder.Decode(PrimitiveCodecs.int64Encoder.Encode v))

[<Test>]
let ``PrimitiveCodecs float round-trips`` () =
    let v = 2.718
    Assert.AreEqual(v, PrimitiveCodecs.floatDecoder.Decode(PrimitiveCodecs.floatEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs decimal round-trips`` () =
    let v = 123.456m
    Assert.AreEqual(v, PrimitiveCodecs.decimalDecoder.Decode(PrimitiveCodecs.decimalEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs unit round-trips`` () =
    let encoded = PrimitiveCodecs.unitEncoder.Encode ()
    let decoded = PrimitiveCodecs.unitDecoder.Decode encoded
    Assert.AreEqual(Null, encoded)
    Assert.IsTrue((decoded = ()))

[<Test>]
let ``PrimitiveCodecs byte array round-trips`` () =
    let v = [| 1uy; 2uy; 3uy |]
    Assert.AreEqual(v, PrimitiveCodecs.byteArrayDecoder.Decode(PrimitiveCodecs.byteArrayEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs Guid round-trips`` () =
    let v = Guid.NewGuid()
    Assert.AreEqual(v, PrimitiveCodecs.guidDecoder.Decode(PrimitiveCodecs.guidEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs DateTime round-trips`` () =
    let v = DateTime(2026, 3, 14, 10, 30, 0, DateTimeKind.Utc)
    Assert.AreEqual(v, PrimitiveCodecs.dateTimeDecoder.Decode(PrimitiveCodecs.dateTimeEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs DateTimeOffset round-trips`` () =
    let v = DateTimeOffset(2026, 3, 14, 10, 30, 0, TimeSpan.Zero)
    Assert.AreEqual(v, PrimitiveCodecs.dateTimeOffsetDecoder.Decode(PrimitiveCodecs.dateTimeOffsetEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs DateOnly round-trips`` () =
    let v = DateOnly(2026, 3, 14)
    Assert.AreEqual(v, PrimitiveCodecs.dateOnlyDecoder.Decode(PrimitiveCodecs.dateOnlyEncoder.Encode v))

[<Test>]
let ``PrimitiveCodecs TimeOnly round-trips`` () =
    let v = TimeOnly(10, 30, 45, 123)
    Assert.AreEqual(v, PrimitiveCodecs.timeOnlyDecoder.Decode(PrimitiveCodecs.timeOnlyEncoder.Encode v))

// -- CodecRegistry.withPrimitives tests --

[<Test>]
let ``withPrimitives registers all primitive codecs`` () =
    let registry = CodecRegistry.withPrimitives ()
    Assert.IsTrue((CodecRegistry.tryFind typeof<bool> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<string> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<decimal> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<int> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<int64> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<float> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<unit> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<byte[]> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<Guid> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<DateTime> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<DateTimeOffset> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<DateOnly> registry).IsSome)
    Assert.IsTrue((CodecRegistry.tryFind typeof<TimeOnly> registry).IsSome)

[<Test>]
let ``withPrimitives returns None for unregistered types`` () =
    let registry = CodecRegistry.withPrimitives ()
    Assert.IsTrue((CodecRegistry.tryFind typeof<char> registry).IsNone)

// -- Set codec tests --

[<Test>]
let ``setCodec encodes Set<int> as JSON array`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.intCodec
    let result = codec.Encode (Set.ofList [3; 1; 2])
    Assert.AreEqual(Array [Number 1m; Number 2m; Number 3m], result)

[<Test>]
let ``setCodec decodes JSON array to Set<int>`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.intCodec
    let result = codec.Decode (Array [Number 1m; Number 2m; Number 3m])
    Assert.AreEqual(Set.ofList [1; 2; 3], result)

[<Test>]
let ``setCodec round-trips Set<int>`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.intCodec
    let original = Set.ofList [1; 2; 3]
    Assert.AreEqual(original, codec.Decode(codec.Encode original))

[<Test>]
let ``setCodec round-trips Set<string>`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.stringCodec
    let original = Set.ofList ["apple"; "banana"; "cherry"]
    Assert.AreEqual(original, codec.Decode(codec.Encode original))

[<Test>]
let ``setCodec round-trips empty set`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.intCodec
    let original = Set.empty<int>
    Assert.AreEqual(original, codec.Decode(codec.Encode original))

[<Test>]
let ``setCodec throws on non-array JSON`` () =
    let codec = CollectionCodecs.setCodec PrimitiveCodecs.intCodec
    Assert.Throws<exn>(fun () ->
        codec.Decode (String "not an array") |> ignore
    ) |> ignore

// -- GlobalCodecRegistry tests --

[<Test>]
let ``GlobalCodecRegistry Current is initialized with primitives`` () =
    Assert.IsTrue((CodecRegistry.tryFind typeof<int> GlobalCodecRegistry.Current).IsSome)

[<Test>]
let ``GlobalCodecRegistry Current can be updated`` () =
    let original = GlobalCodecRegistry.Current
    try
        let customCodec =
            { new IJsonCodec<char> with
                member _.Encode v = JsonValue.String (string v)
                member _.Decode json =
                    match json with
                    | JsonValue.String s -> s.[0]
                    | _ -> failwith "Expected string" }
            |> JsonCodec.boxCodec

        GlobalCodecRegistry.Current <-
            GlobalCodecRegistry.Current
            |> CodecRegistry.add (typeof<char>, customCodec)

        let found = CodecRegistry.tryFind typeof<char> GlobalCodecRegistry.Current
        Assert.IsTrue(found.IsSome)
        Assert.AreEqual(JsonValue.String "A", found.Value.Encode(box 'A'))
    finally
        GlobalCodecRegistry.Current <- original
