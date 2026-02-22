namespace Serde.FS

module SerdeApp =
    let mutable private entryPoint : (string[] -> int) option = None

    let registerEntryPoint fn =
        entryPoint <- Some fn

    let invokeRegisteredEntryPoint argv =
        match entryPoint with
        | Some fn -> fn argv
        | None -> 0
