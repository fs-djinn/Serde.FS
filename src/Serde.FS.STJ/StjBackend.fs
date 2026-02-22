namespace Serde.FS.STJ

open System.Text.Json
open Serde.FS

type StjBackend() =
    interface ISerdeBackend with
        member _.Serialize(value, _options) =
            JsonSerializer.Serialize(value, JsonSerializerOptions())

        member _.Deserialize(json, _options) =
            JsonSerializer.Deserialize<'T>(json, JsonSerializerOptions())
