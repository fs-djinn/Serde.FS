namespace Serde.FS

type ISerdeBackend =
    abstract Serialize : 'T * System.Type * ISerdeOptions option -> string
    abstract Deserialize : string * System.Type * ISerdeOptions option -> 'T
