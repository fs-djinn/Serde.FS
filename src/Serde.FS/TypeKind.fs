namespace Serde.FS

module TypeKindTypes =

    type PrimitiveKind =
        | Unit
        | Bool
        | Int8 | Int16 | Int32 | Int64
        | UInt8 | UInt16 | UInt32 | UInt64
        | Float32 | Float64
        | Decimal
        | String
        | Guid
        | DateTime
        | DateTimeOffset
        | TimeSpan
        | DateOnly
        | TimeOnly

    type TypeKind =
        | Primitive of PrimitiveKind
        | Record of fields: FieldInfo list
        | Tuple of elements: FieldInfo list
        | Option of inner: TypeInfo
        | List of inner: TypeInfo
        | Array of inner: TypeInfo
        | Set of inner: TypeInfo
        | Map of key: TypeInfo * value: TypeInfo
        | Enum of namesAndValues: (string * int) list
        | AnonymousRecord of fields: FieldInfo list
        | Union of cases: UnionCase list

    and TypeInfo = {
        Namespace: string option
        EnclosingModules: string list
        TypeName: string
        Kind: TypeKind
    }

    and FieldInfo = {
        Name: string
        Type: TypeInfo
    }

    and UnionCase = {
        CaseName: string
        Fields: FieldInfo list
        Tag: int option
    }
