namespace Serde.FS.Json.Codec

/// Deterministic JSON AST for codec-based serialization.
type JsonValue =
    | Null
    | Bool of bool
    | Number of decimal
    | String of string
    | Array of JsonValue list
    | Object of (string * JsonValue) list

module JsonValue =
    /// Returns a compact string representation of a JsonValue for debugging.
    let rec toString (value: JsonValue) =
        match value with
        | Null -> "null"
        | Bool b -> if b then "true" else "false"
        | Number n -> string n
        | String s -> $"\"{s}\""
        | Array items ->
            let inner = items |> List.map toString |> String.concat ", "
            $"[{inner}]"
        | Object fields ->
            let inner =
                fields
                |> List.map (fun (k, v) -> $"\"{k}\": {toString v}")
                |> String.concat ", "
            $"{{{inner}}}"
