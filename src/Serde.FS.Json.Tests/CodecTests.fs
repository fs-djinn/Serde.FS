module Serde.FS.Json.Tests.CodecTests

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

// -- CodecRegistry tests --

type DummyEncoder() =
    interface IJsonEncoder<int> with
        member _.Encode(x) = Number (decimal x)

type DummyDecoder() =
    interface IJsonDecoder<int> with
        member _.Decode(v) =
            match v with
            | Number n -> int n
            | _ -> failwith "Expected Number"

[<Test>]
let ``CodecRegistry can add and retrieve encoder`` () =
    let registry = CodecRegistry()
    registry.Add(DummyEncoder(), DummyDecoder())

    let enc = registry.TryGetEncoder<int>()
    Assert.IsTrue(enc.IsSome)
    Assert.AreEqual(Number 42m, enc.Value.Encode(42))

[<Test>]
let ``CodecRegistry can add and retrieve decoder`` () =
    let registry = CodecRegistry()
    registry.Add(DummyEncoder(), DummyDecoder())

    let dec = registry.TryGetDecoder<int>()
    Assert.IsTrue(dec.IsSome)
    Assert.AreEqual(42, dec.Value.Decode(Number 42m))

[<Test>]
let ``CodecRegistry returns None for missing types`` () =
    let registry = CodecRegistry()

    let enc = registry.TryGetEncoder<string>()
    let dec = registry.TryGetDecoder<string>()
    Assert.IsTrue(enc.IsNone)
    Assert.IsTrue(dec.IsNone)
