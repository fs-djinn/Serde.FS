module Serde.FS.STJ.Tests.SerdeTests

open NUnit.Framework
open Serde.FS

type Person = { FName: string; LName: string }

[<Serde>]
type MarkedPerson = { FName: string; LName: string }

[<SetUp>]
let Setup () =
    Serde.FS.STJ.SerdeStj.useAsDefault()
    // Existing tests use reflection (no source gen), so allow it.
    Serde.FS.STJ.SerdeStj.allowReflectionFallback()

[<Test>]
let ``Serialize and deserialize a record`` () =
    let json = Serde.Serialize { FName = "Jordan"; LName = "Marr" }
    json |> string =! """{"FName":"Jordan","LName":"Marr"}"""

    let person : Person = Serde.Deserialize json
    person.FName =! "Jordan"
    person.LName =! "Marr"

[<Test>]
let ``Strict mode throws on serialize for unmarked type`` () =
    Serde.Strict <- true
    try
        let mutable threw = false
        try
            Serde.Serialize<Person>({ FName = "Jordan"; LName = "Marr" }) |> ignore
        with _ ->
            threw <- true
        Assert.That(threw, Is.True, "Expected strict mode to throw for unmarked Person type")
    finally
        Serde.Strict <- false

[<Test>]
let ``Strict mode throws on deserialize for unmarked type`` () =
    Serde.Strict <- true
    try
        let mutable threw = false
        try
            Serde.Deserialize<Person>("""{"FName":"Jordan","LName":"Marr"}""") |> ignore
        with _ ->
            threw <- true
        Assert.That(threw, Is.True, "Expected strict mode to throw for unmarked Person type")
    finally
        Serde.Strict <- false

[<Test>]
let ``Strict mode succeeds after allowReflectionFallback`` () =
    Serde.Strict <- true
    Serde.FS.STJ.SerdeStj.allowReflectionFallback()
    let json = Serde.Serialize { FName = "Jordan"; LName = "Marr" }
    json |> string =! """{"FName":"Jordan","LName":"Marr"}"""

[<Test>]
let ``Strict mode does not throw for Serde-attributed type`` () =
    Serde.Strict <- true
    try
        let json = Serde.Serialize { MarkedPerson.FName = "Jordan"; LName = "Marr" }
        json |> string =! """{"FName":"Jordan","LName":"Marr"}"""

        let person : MarkedPerson = Serde.Deserialize json
        person.FName =! "Jordan"
        person.LName =! "Marr"
    finally
        Serde.Strict <- false
