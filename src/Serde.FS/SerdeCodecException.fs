namespace Serde.FS

/// Thrown when a codec fails during construction or execution.
type SerdeCodecException(message: string, inner: System.Exception) =
    inherit System.Exception(message, inner)
    new(message: string) = SerdeCodecException(message, null)
