namespace Serde.FS.STJ

open System.Text.Json
open System.Text.Json.Serialization.Metadata
open Serde.FS

/// Cached JsonSerializerOptions instance used by the STJ backend.
module internal StjOptionsCache =
    let defaultJsonOptions =
        let opts = JsonSerializerOptions()
        opts.TypeInfoResolverChain.Add(DefaultJsonTypeInfoResolver())
        opts
    let generatedResolvers = System.Collections.Generic.List<IJsonTypeInfoResolver>()

/// Registry for generated IJsonTypeInfoResolvers.
/// Generated code calls registerResolver at module init to wire resolvers into STJ.
module SerdeStjResolverRegistry =
    let registerResolver (resolver: IJsonTypeInfoResolver) =
        StjOptionsCache.generatedResolvers.Add(resolver)
        StjOptionsCache.defaultJsonOptions.TypeInfoResolverChain.Insert(0, resolver)

/// Strict mode enforcement using generated resolvers (AOT-safe, no reflection).
module internal StjStrict =
    let enforceStrict (ty: System.Type) =
        if Serde.Strict then
            let opts = StjOptionsCache.defaultJsonOptions
            let found =
                StjOptionsCache.generatedResolvers
                |> Seq.exists (fun resolver ->
                    resolver.GetTypeInfo(ty, opts) <> null
                )
            if not found then
                failwithf
                    "Strict mode is enabled: type '%s' has no generated Serde metadata. \
                     Call SerdeStj.allowReflectionFallback() to allow reflection-based serialization."
                    ty.FullName

type StjBackend() =
    interface ISerdeBackend with
        member _.Serialize(value, runtimeType, _options) =
            StjStrict.enforceStrict(runtimeType)
            JsonSerializer.Serialize(value, runtimeType, StjOptionsCache.defaultJsonOptions)

        member _.Deserialize(json, runtimeType, _options) =
            StjStrict.enforceStrict(runtimeType)
            JsonSerializer.Deserialize(json, runtimeType, StjOptionsCache.defaultJsonOptions) :?> 'T
