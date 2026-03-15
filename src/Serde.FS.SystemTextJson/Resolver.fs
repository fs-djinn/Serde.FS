namespace Serde.FS.SystemTextJson

open System.Text.Json.Serialization.Metadata

/// Provides an IJsonTypeInfoResolver that exposes generated STJ metadata for Serde-decorated types.
[<RequireQualifiedAccess>]
module Resolver =

    /// A resolver that aggregates all generated STJ metadata from Serde-decorated types.
    let resolver : IJsonTypeInfoResolver =
        GeneratedSerdeStjResolver() :> IJsonTypeInfoResolver

    /// Returns a resolver that first consults Serde-generated metadata,
    /// then falls back to the provided resolver.
    let combine (fallback: IJsonTypeInfoResolver) : IJsonTypeInfoResolver =
        JsonTypeInfoResolver.Combine(resolver, fallback)
