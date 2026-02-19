namespace Serde.FS.STJ

open System.Text.Json
open Serde.FS

type StjOptions(jsonOptions: JsonSerializerOptions) =
    interface ISerdeOptions
    member _.JsonOptions = jsonOptions

type StjBackend() =
    interface ISerdeBackend with
        member _.Serialize(value, options) =
            let opts =
                match options with
                | Some (:? StjOptions as o) -> o.JsonOptions
                | _ -> JsonSerializerOptions()
            JsonSerializer.Serialize(value, opts)

        member _.Deserialize(json, options) =
            let opts =
                match options with
                | Some (:? StjOptions as o) -> o.JsonOptions
                | _ -> JsonSerializerOptions()
            JsonSerializer.Deserialize<'T>(json, opts)

[<AutoOpen>]
module StjSerdeExtensions =

    type Serde with
        static member Serialize(value: 'T, options: StjOptions) =
            Serde.DefaultBackend.Serialize(value, Some options)

        static member Deserialize<'T>(json: string, options: StjOptions) =
            Serde.DefaultBackend.Deserialize<'T>(json, Some options)
