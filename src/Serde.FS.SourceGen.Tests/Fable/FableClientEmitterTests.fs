/// Snapshot tests for FableClientEmitter. Each test builds synthetic
/// TypeInfo + RpcInterfaceInfo inputs, runs the emitter, and asserts the
/// output matches a checked-in `.expected.fs` snapshot. See SnapshotHarness.fs
/// for the comparison + .actual.fs writeback flow.
///
/// These tests pin the *current* emitter output. The intent is that the
/// step-4 refactor (route everything through TypeInfo, delete parseTypeString)
/// produces byte-identical output, with these tests catching any drift.
module Serde.FS.SourceGen.Tests.Fable.FableClientEmitterTests

open NUnit.Framework
open Serde.FS.Json.SourceGen
open Serde.FS.SourceGen.Tests.Fable
open Serde.FS.SourceGen.Tests.Fable.SyntheticTypes

[<Test>]
let ``record with primitive fields in a namespace`` () =
    let productTi =
        record "Domain" "Product" [
            "Id", int32Ti
            "Name", stringTi
        ]
    let methods = [
        methodOf "GetProduct" int32Ti productTi
    ]
    let iface = interfaceOf "Domain" "IOrderApi" methods true
    let types = [ toSerde productTi ]
    let actual = FableClientEmitter.emit iface types
    SnapshotHarness.assertSnapshot "record_primitives_namespace" actual

[<Test>]
let ``record with primitive fields under a top-level module`` () =
    let productTi =
        record "Domain.Api" "Product" [
            "Id", int32Ti
            "Name", stringTi
        ]
    let methods = [
        methodOf "GetProduct" int32Ti productTi
    ]
    // IsParentNamespace = false → emitter must use the sibling-module shape:
    //   module rec Domain.IOrderApiFableClient
    let iface = interfaceOf "Domain.Api" "IOrderApi" methods false
    let types = [ toSerde productTi ]
    let actual = FableClientEmitter.emit iface types
    SnapshotHarness.assertSnapshot "record_primitives_module" actual

[<Test>]
let ``multi-case union with mixed cases`` () =
    let shapeTi =
        multiUnion "Domain" "Shape" [
            "Circle",    [ floatTi ]
            "Rectangle", [ floatTi; floatTi ]
            "Point",     [ ]
        ]
    let methods = [
        methodOf "GetShape" int32Ti shapeTi
    ]
    let iface = interfaceOf "Domain" "IShapeApi" methods true
    let types = [ toSerde shapeTi ]
    let actual = FableClientEmitter.emit iface types
    SnapshotHarness.assertSnapshot "multi_case_union" actual
