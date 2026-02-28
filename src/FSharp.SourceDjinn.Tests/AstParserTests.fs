module FSharp.SourceDjinn.Tests.AstParserTests

open NUnit.Framework
open Serde.FS
open FSharp.SourceDjinn
open FSharp.SourceDjinn.Types
open Serde.FS.SourceGen

[<Test>]
let ``Parses record with Serde attribute`` () =
    let source = """
namespace MyApp

open Serde.FS

[<Serde>]
type Person = { FName: string; LName: string; Age: int }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    Assert.That(t.Raw.Namespace, Is.EqualTo(Some "MyApp"))
    Assert.That(t.Raw.EnclosingModules, Is.EqualTo(List.empty<string>))
    Assert.That(t.Raw.TypeName, Is.EqualTo("Person"))
    Assert.That(t.Capability, Is.EqualTo(Both))

    let fields = t.Fields.Value
    Assert.That(fields.Length, Is.EqualTo(3))

    Assert.That(fields.[0].Name, Is.EqualTo("FName"))
    Assert.That(typeInfoToFSharpString fields.[0].Type, Is.EqualTo("string"))

    Assert.That(fields.[1].Name, Is.EqualTo("LName"))
    Assert.That(typeInfoToFSharpString fields.[1].Type, Is.EqualTo("string"))

    Assert.That(fields.[2].Name, Is.EqualTo("Age"))
    Assert.That(typeInfoToFSharpString fields.[2].Type, Is.EqualTo("int"))

[<Test>]
let ``Parses record with SerdeSerialize attribute`` () =
    let source = """
namespace MyApp

[<Serde.FS.SerdeSerialize>]
type Point = { X: float; Y: float }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    Assert.That(t.Raw.TypeName, Is.EqualTo("Point"))
    Assert.That(t.Capability, Is.EqualTo(Serialize))

[<Test>]
let ``Parses record with SerdeDeserialize attribute`` () =
    let source = """
namespace MyApp

[<SerdeDeserialize>]
type Config = { Host: string; Port: int }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    Assert.That(t.Raw.TypeName, Is.EqualTo("Config"))
    Assert.That(t.Capability, Is.EqualTo(Deserialize))

[<Test>]
let ``Ignores types without Serde attributes`` () =
    let source = """
namespace MyApp

type NotSerde = { X: int }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(0))

[<Test>]
let ``Parses multiple annotated records in one file`` () =
    let source = """
namespace MyApp

open Serde.FS

[<Serde>]
type Person = { Name: string; Age: int }

[<Serde>]
type Address = { Street: string; City: string; Zip: string }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(2))
    Assert.That(types.[0].Raw.TypeName, Is.EqualTo("Person"))
    Assert.That(types.[1].Raw.TypeName, Is.EqualTo("Address"))

[<Test>]
let ``Parses record with option field`` () =
    let source = """
namespace MyApp

[<Serde>]
type Person = { Name: string; MiddleName: string option }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    let fields = t.Fields.Value
    Assert.That(fields.[1].Name, Is.EqualTo("MiddleName"))
    Assert.That(typeInfoToFSharpString fields.[1].Type, Is.EqualTo("string option"))

[<Test>]
let ``Parses record with various supported types`` () =
    let source = """
namespace MyApp

[<Serde>]
type AllTypes = {
    S: string
    I: int
    I64: int64
    F: float
    D: decimal
    B: bool
    Dt: System.DateTime
    G: System.Guid
}
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    let fields = t.Fields.Value
    Assert.That(fields.Length, Is.EqualTo(8))
    Assert.That(typeInfoToFSharpString fields.[0].Type, Is.EqualTo("string"))
    Assert.That(typeInfoToFSharpString fields.[1].Type, Is.EqualTo("int"))
    Assert.That(typeInfoToFSharpString fields.[2].Type, Is.EqualTo("int64"))
    Assert.That(typeInfoToFSharpString fields.[3].Type, Is.EqualTo("float"))
    Assert.That(typeInfoToFSharpString fields.[4].Type, Is.EqualTo("decimal"))
    Assert.That(typeInfoToFSharpString fields.[5].Type, Is.EqualTo("bool"))
    Assert.That(typeInfoToFSharpString fields.[6].Type, Is.EqualTo("System.DateTime"))
    Assert.That(typeInfoToFSharpString fields.[7].Type, Is.EqualTo("System.Guid"))

[<Test>]
let ``Parses type inside a named module`` () =
    let source = """
module Program

[<Serde>]
type Person = { Name: string; Age: int }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    Assert.That(t.Raw.Namespace, Is.EqualTo(None))
    Assert.That(t.Raw.EnclosingModules, Is.EqualTo(["Program"]))
    Assert.That(t.Raw.TypeName, Is.EqualTo("Person"))

[<Test>]
let ``Parses type inside nested module under namespace`` () =
    let source = """
namespace MyApp

module Domain =
    [<Serde>]
    type Person = { Name: string; Age: int }
"""
    let types = SerdeAstParser.parseSource "/test.fs" source
    Assert.That(types.Length, Is.EqualTo(1))

    let t = types.[0]
    Assert.That(t.Raw.Namespace, Is.EqualTo(Some "MyApp"))
    Assert.That(t.Raw.EnclosingModules, Is.EqualTo(["Domain"]))
    Assert.That(t.Raw.TypeName, Is.EqualTo("Person"))

[<Test>]
let ``Detects EntryPoint attribute on top-level function`` () =
    let source = """
module Program

[<FSharp.SourceDjinn.EntryPoint>]
let run argv = 0
"""
    let result = EntryPointDetector.detect "/test.fs" source
    Assert.That(result.IsSome, Is.True)
    Assert.That(result.Value.ModuleName, Is.EqualTo("Program"))
    Assert.That(result.Value.FunctionName, Is.EqualTo("run"))

[<Test>]
let ``No entry point attribute returns None`` () =
    let source = """
namespace MyApp

type Person = { Name: string; Age: int }
"""
    let result = EntryPointDetector.detect "/test.fs" source
    Assert.That(result.IsNone, Is.True)

[<Test>]
let ``Extracts correct module name and function name`` () =
    let source = """
module MyApp.Program

[<FSharp.SourceDjinn.EntryPoint>]
let main argv = 0
"""
    let result = EntryPointDetector.detect "/test.fs" source
    Assert.That(result.IsSome, Is.True)
    Assert.That(result.Value.ModuleName, Is.EqualTo("MyApp.Program"))
    Assert.That(result.Value.FunctionName, Is.EqualTo("main"))

[<Test>]
let ``Works with short EntryPoint attribute name`` () =
    let source = """
module Program

open FSharp.SourceDjinn.TypeModel

[<EntryPoint>]
let run argv = 0
"""
    let result = EntryPointDetector.detect "/test.fs" source
    Assert.That(result.IsSome, Is.True)
    Assert.That(result.Value.ModuleName, Is.EqualTo("Program"))
    Assert.That(result.Value.FunctionName, Is.EqualTo("run"))
