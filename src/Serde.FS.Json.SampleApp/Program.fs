module MyApp

open Serde.FS
open Serde.FS.Json

[<Serde>]
type Wrapper<'T> = Wrapper of 'T

[<Serde>]
type Person = { Name: string }

[<Serde>]
type Shape =
    | Circle of radius: float
    | Point

[<FSharp.SourceDjinn.TypeModel.EntryPoint>]
let main argv =

    SerdeJson.useAsDefault()

    let json = Serde.Serialize<Wrapper<Person>>(Wrapper { Name = "Jordan"})
    let person = Serde.Deserialize<Wrapper<Person>> json

    printfn "Wrapper JSON: %s" json
    printfn "Wrapper roundtrip: %A" person

    let circleJson = Serde.Serialize<Shape>(Circle 2.5)
    let circle = Serde.Deserialize<Shape> circleJson

    printfn "Circle JSON: %s" circleJson
    printfn "Circle roundtrip: %A" circle

    let pointJson = Serde.Serialize<Shape>(Point)
    let point = Serde.Deserialize<Shape> pointJson

    printfn "Point JSON: %s" pointJson
    printfn "Point roundtrip: %A" point

    0 