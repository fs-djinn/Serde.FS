namespace Serde.FS.Json.Codec

/// Global mutable registry for framework and app-level registration.
module GlobalCodecRegistry =
    let mutable Current : CodecRegistry =
        CodecRegistry.withPrimitives ()
        |> CodecRegistry.addFactory (typedefof<Set<_>>, CollectionCodecs.SetCodecFactory.create)
        |> CodecRegistry.addFactory (typeof<System.Array>, CollectionCodecs.ArrayCodecFactory.create)
        |> CodecRegistry.addFactory (typedefof<list<_>>, CollectionCodecs.ListCodecFactory.create)
        |> CodecRegistry.addFactory (typedefof<Map<_,_>>, CollectionCodecs.MapCodecFactory.create)
        |> CodecRegistry.addFactory (typedefof<Result<_,_>>, CollectionCodecs.ResultCodecFactory.create)
