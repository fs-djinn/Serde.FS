namespace Serde.FS.Json.Codec

module JsonCodecRegistry =
    /// Creates a fresh registry with primitive codecs installed.
    let create () =
        CodecRegistry.withPrimitives ()
        |> CodecRegistry.addFactory (typedefof<Set<_>>, CollectionCodecs.SetCodecFactory.create)
        |> CodecRegistry.addFactory (typedefof<Map<_,_>>, CollectionCodecs.MapCodecFactory.create)
        |> CodecRegistry.addFactory (typeof<System.Array>, CollectionCodecs.ArrayCodecFactory.create)
        |> CodecRegistry.addFactory (typedefof<List<_>>, CollectionCodecs.ListCodecFactory.create)
