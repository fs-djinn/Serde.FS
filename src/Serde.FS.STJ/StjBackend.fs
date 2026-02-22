namespace Serde.FS.STJ

open System.Text.Json
open Serde.FS

/// Cached JsonSerializerOptions instance used by the STJ backend.
/// Creating new options per call is expensive and breaks metadata caching.
/// This instance is initialized once and reused for all serialization.
module internal StjOptionsCache =
    let defaultJsonOptions =
        let opts = JsonSerializerOptions()
        // TODO: attach generated metadata here
        opts

type StjBackend() =
    interface ISerdeBackend with
        member _.Serialize(value, _options) =
            JsonSerializer.Serialize(value, StjOptionsCache.defaultJsonOptions)

        member _.Deserialize(json, _options) =
            JsonSerializer.Deserialize<'T>(json, StjOptionsCache.defaultJsonOptions)
