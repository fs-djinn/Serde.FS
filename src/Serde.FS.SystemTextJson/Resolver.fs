namespace Serde.FS.SystemTextJson

open System.Text.Json.Serialization.Metadata

/// Collects IJsonTypeInfoResolver instances registered by IBootstrap implementations at startup.
[<RequireQualifiedAccess>]
module Resolver =

    let private resolvers = ResizeArray<IJsonTypeInfoResolver>()

    let register (r: IJsonTypeInfoResolver) =
        resolvers.Add(r)

    let get () =
        JsonTypeInfoResolver.Combine(resolvers.ToArray())
