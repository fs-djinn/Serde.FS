module Program

open Serde.FS
open Serde.FS.STJ

[<Serde>]
type Color =
    | Red = 1
    | Green = 2
    | Blue = 3

[<Serde>]
type Address = { Street: string; City: string }

[<Serde>]
type Pet = { Name: string; Species: string }

[<Serde>]
type Shape =
    | Circle of radius: float
    | Rectangle of width: float * height: float
    | Point

[<Serde>]
type Person = {
    Name: string
    Age: int
    Address: Address option
    LuckyNumbers: int Set
    Colors: Color[]
    Pets: Pet list
    Position: float * float
    PetMap: Map<string, Pet>
    Shapes: Shape list
}

let run argv =
    SerdeStj.useAsDefault()
    let pets = [
        { Name = "Fido"; Species = "Dog" }
        { Name = "Whiskers"; Species = "Cat" }
    ]
    let person = {
        Name = "John"
        Age = 30
        Address = Some { Street = "123 Main St"; City = "Springfield" }
        LuckyNumbers = Set [ 1; 2; 3 ]
        Colors = [| Color.Red; Color.Green; Color.Blue |]
        Pets = pets
        Position = 10.5, 20.5
        PetMap = pets |> List.map (fun p -> p.Name, p) |> Map.ofList
        Shapes = [ Shape.Circle(3.14); Shape.Rectangle(10.0, 20.0); Shape.Point ]
    }
    let json = Serde.Serialize person
    printfn "Serialized: %s" json
    let deserialized: Person = Serde.Deserialize json
    printfn "Deserialized: %A" deserialized

    let shapes = [ Shape.Circle(3.14); Shape.Rectangle(10.0, 20.0); Shape.Point ]
    for shape in shapes do
        let shapeJson = Serde.Serialize shape
        printfn "Shape: %s" shapeJson
        let back: Shape = Serde.Deserialize shapeJson
        printfn "Back: %A" back
    0

SerdeApp.entryPoint run
