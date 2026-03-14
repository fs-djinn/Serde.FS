namespace Serde.FS.Json.Codec

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.Json.Serialization.Metadata
open Serde.FS

/// Constructs a JsonTypeInfo<'T> using the deterministic pipeline:
/// 1. Codec registry lookup (+ type-level [<JsonCodec>] attribute)
/// 2. Generated metadata
/// 3. STJ fallback
/// 4. Error
module JsonTypeInfoBuilder =

    /// Checks for a [<JsonCodec>] attribute on the type and returns the codec if found.
    let private tryGetTypeCodecAttribute (ty: Type) (registry: CodecRegistry) : IJsonCodec option =
        match ty.GetCustomAttributes(typeof<JsonCodecAttribute>, false) with
        | [| attr |] ->
            let codecAttr = attr :?> JsonCodecAttribute
            let codecType = codecAttr.CodecType
            // Check if the codec type is already in the registry
            match CodecRegistry.tryFind codecType registry with
            | Some codec -> Some codec
            | None ->
                // Instantiate the codec from the attribute type
                try
                    let instance = Activator.CreateInstance(codecType)
                    match instance with
                    | :? IJsonCodec as codec -> Some codec
                    | _ -> None
                with ex ->
                    raise (SerdeCodecException($"Failed to instantiate codec type '{codecType.FullName}' from [<JsonCodec>] attribute.", ex))
        | _ -> None

    /// Creates a JsonTypeInfo<'T> from an untyped IJsonCodec by wrapping it in a CodecConverter.
    let private codecToJsonTypeInfo<'T> (codec: IJsonCodec) (options: JsonSerializerOptions) : JsonTypeInfo<'T> =
        let converter =
            // Create a typed CodecConverter<'T> using the codec's encode/decode
            { new JsonConverter<'T>() with
                member _.Read(reader, _typeToConvert, _options) =
                    let jsonValue = JsonValueBridge.readJsonValue &reader
                    codec.Decode jsonValue :?> 'T
                member _.Write(writer, value, _options) =
                    let jsonValue = codec.Encode(box value)
                    JsonValueBridge.writeJsonValue writer jsonValue }
        let typeInfo = JsonMetadataServices.CreateValueInfo<'T>(options, converter)
        typeInfo

    /// Applies property-level [<JsonCodec>] overrides to a JsonTypeInfo.
    let private applyPropertyCodecOverrides (registry: CodecRegistry) (typeInfo: JsonTypeInfo<'T>) : JsonTypeInfo<'T> =
        if typeInfo.Kind = JsonTypeInfoKind.Object then
            for prop in typeInfo.Properties do
                match prop.AttributeProvider with
                | null -> ()
                | attrProvider ->
                    let attrs = attrProvider.GetCustomAttributes(typeof<JsonCodecAttribute>, false)
                    if attrs.Length > 0 then
                        let codecAttr = attrs[0] :?> JsonCodecAttribute
                        let codecType = codecAttr.CodecType
                        try
                            let codecInstance =
                                match CodecRegistry.tryFind codecType registry with
                                | Some codec -> codec
                                | None ->
                                    match Activator.CreateInstance(codecType) with
                                    | :? IJsonCodec as codec -> codec
                                    | other -> failwithf "Type '%s' does not implement IJsonCodec" (other.GetType().FullName)
                            prop.CustomConverter <- UntypedCodecConverter(codecInstance)
                        with
                        | :? SerdeCodecException -> reraise()
                        | ex ->
                            raise (SerdeCodecException($"Failed to apply property-level codec '{codecType.FullName}'.", ex))
        typeInfo

    /// Constructs a JsonTypeInfo<'T> using the deterministic pipeline.
    ///
    /// Pipeline order (strict):
    /// 1. Codec registry lookup (includes type-level [<JsonCodec>] attribute check)
    /// 2. Generated metadata (via registered resolvers)
    /// 3. STJ fallback (DefaultJsonTypeInfoResolver)
    /// 4. Error (SerdeMissingMetadataException)
    let build<'T> (registry: CodecRegistry) (options: JsonSerializerOptions) : JsonTypeInfo<'T> =
        let targetType = typeof<'T>

        // Step 1 — Check type-level [<JsonCodec>] attribute, then codec registry
        let typeAttrCodec = tryGetTypeCodecAttribute targetType registry
        match typeAttrCodec with
        | Some codec ->
            codecToJsonTypeInfo<'T> codec options
            |> applyPropertyCodecOverrides registry
        | None ->
            match CodecRegistry.tryFind targetType registry with
            | Some codec ->
                codecToJsonTypeInfo<'T> codec options
                |> applyPropertyCodecOverrides registry
            | None ->
                // Step 2 — Try generated metadata via resolver chain
                let mutable generatedTypeInfo : JsonTypeInfo = null
                for resolver in options.TypeInfoResolverChain do
                    if generatedTypeInfo = null then
                        try
                            let ti = resolver.GetTypeInfo(targetType, options)
                            if ti <> null then
                                generatedTypeInfo <- ti
                        with _ -> ()

                if generatedTypeInfo <> null then
                    let typed = generatedTypeInfo :?> JsonTypeInfo<'T>
                    applyPropertyCodecOverrides registry typed
                else
                    // Step 3 — STJ fallback
                    try
                        let fallbackResolver = DefaultJsonTypeInfoResolver()
                        let ti = fallbackResolver.GetTypeInfo(targetType, options)
                        if ti <> null then
                            let typed = ti :?> JsonTypeInfo<'T>
                            applyPropertyCodecOverrides registry typed
                        else
                            // Step 4 — Error
                            raise (SerdeMissingMetadataException(
                                $"No codec, generated metadata, or STJ fallback available for type '{targetType.FullName}'.",
                                targetType))
                    with
                    | :? SerdeMissingMetadataException -> reraise()
                    | :? SerdeCodecException -> reraise()
                    | ex ->
                        raise (SerdeMissingMetadataException(
                            $"No codec, generated metadata, or STJ fallback available for type '{targetType.FullName}'.",
                            targetType))
