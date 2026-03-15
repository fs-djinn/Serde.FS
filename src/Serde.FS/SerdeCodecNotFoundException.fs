namespace Serde.FS

/// Thrown when no codec can be resolved for a given type.
type SerdeCodecNotFoundException(message: string, targetType: System.Type) =
    inherit System.Exception(message)
    member _.TargetType = targetType
