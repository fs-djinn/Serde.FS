namespace Serde.FS.Json.Codec

/// Global mutable registry for framework and app-level registration.
module GlobalCodecRegistry =
    let mutable Current : CodecRegistry =
        CodecRegistry.withPrimitives ()
        |> CodecRegistry.addFactory (typedefof<Set<_>>, CollectionCodecs.SetCodecFactory.create)
