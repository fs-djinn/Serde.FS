namespace Serde.FS.Json.Codec

/// Global mutable registry for framework and app-level registration.
/// The initial value is the canonical factory set from `JsonCodecRegistry.create()` —
/// keeping this in lockstep automatically eliminates the "added a factory in one
/// place, forgot the other" bug class.
module GlobalCodecRegistry =
    let mutable Current : CodecRegistry = JsonCodecRegistry.create ()
