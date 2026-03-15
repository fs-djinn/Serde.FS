namespace Serde.FS

/// Thrown when JSON parsing fails. Wraps the underlying parse error with Serde context.
type SerdeJsonException(message: string, inner: System.Exception) =
    inherit System.Exception(message, inner)
    new(message: string) = SerdeJsonException(message, null)
