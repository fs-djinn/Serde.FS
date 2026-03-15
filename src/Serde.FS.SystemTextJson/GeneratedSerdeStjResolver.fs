namespace Serde.FS.SystemTextJson

open System
open System.Text.Json
open System.Text.Json.Serialization.Metadata

/// Stub resolver to be replaced by generated code from Serde.FS.SystemTextJson.SourceGen (Spec 2).
type GeneratedSerdeStjResolver() =
    interface IJsonTypeInfoResolver with
        member _.GetTypeInfo(_type: Type, _options: JsonSerializerOptions) : JsonTypeInfo =
            null
