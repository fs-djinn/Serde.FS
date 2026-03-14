namespace Serde.FS

open System

/// Specifies a codec type to use for serialization and deserialization.
/// Can be applied at the type level or property level.
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct ||| AttributeTargets.Property ||| AttributeTargets.Field, AllowMultiple = false)>]
type JsonCodecAttribute(codecType: Type) =
    inherit Attribute()
    member _.CodecType = codecType
