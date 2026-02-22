namespace Serde.FS

open System

type Serde =
    static member val DefaultBackend : ISerdeBackend option = None
        with get, set

    /// When true, serialization/deserialization of types without Serde attributes throws.
    static member val Strict : bool = false
        with get, set

    static member private GetBackend() =
        Serde.DefaultBackend
        |> Option.defaultWith (fun () ->
            failwith "No backend registered. Reference a backend package such as Serde.FS.STJ."
        )

    static member inline private EnforceStrict<'T>() =
        if Serde.Strict then
            let ty = typeof<'T>
            let hasSerdeAttr =
                Attribute.IsDefined(ty, typeof<SerdeAttribute>)
                || Attribute.IsDefined(ty, typeof<SerdeSerializeAttribute>)
                || Attribute.IsDefined(ty, typeof<SerdeDeserializeAttribute>)
            if not hasSerdeAttr then
                failwithf
                    "Strict mode is enabled: type '%s' is not marked with [<Serde>], [<SerdeSerialize>], or [<SerdeDeserialize>]. \
                     Call SerdeStj.allowReflectionFallback() to allow reflection-based serialization."
                    ty.FullName

    static member Serialize(value: 'T) =
        Serde.EnforceStrict<'T>()
        Serde.GetBackend().Serialize(value, None)

    static member Serialize(value: 'T, options: ISerdeOptions) =
        Serde.EnforceStrict<'T>()
        Serde.GetBackend().Serialize(value, Some options)

    static member Deserialize<'T>(json: string) =
        Serde.EnforceStrict<'T>()
        Serde.GetBackend().Deserialize<'T>(json, None)

    static member Deserialize<'T>(json: string, options: ISerdeOptions) =
        Serde.EnforceStrict<'T>()
        Serde.GetBackend().Deserialize<'T>(json, Some options)
