namespace Serde.FS

type ISerdeCodeEmitter =
    abstract member Emit : SerdeTypeInfo -> string
    /// File suffix for per-type generated files (e.g. "json", "stj"). Produces "{TypeName}.{suffix}.g.fs".
    abstract member HintNameSuffix : string

type ISerdeResolverEmitter =
    abstract member EmitResolver : SerdeTypeInfo list -> string option
    /// The hint name for the resolver file (e.g. "~SerdeResolver.serde.g.fs" or "~SerdeStjResolver.g.fs").
    abstract member ResolverHintName : string
    /// Additional files to emit after the resolver (e.g. registration + bootstrap). Returns (hintName, code) pairs.
    abstract member EmitRegistrationFiles : unit -> (string * string) list
    /// Whether this backend emits per-type files (true) or consolidates all codecs into the resolver (false).
    abstract member EmitPerTypeFiles : bool

/// Metadata for a single RPC method discovered from an [<RpcApi>] interface.
type RpcMethodInfo = {
    MethodName: string
    /// F# type expression for the input parameter (e.g., "int", "SampleRpc.Order")
    InputType: string
    /// F# type expression for the return type, unwrapped from Async/Task (e.g., "SampleRpc.Product")
    OutputType: string
}

/// Metadata for an [<RpcApi>] interface.
type RpcInterfaceInfo = {
    /// Fully qualified interface name (e.g., "SampleRpc.IOrderApi")
    FullName: string
    /// Short interface name (e.g., "IOrderApi")
    ShortName: string
    /// Methods in declaration order
    Methods: RpcMethodInfo list
}

/// Result of RPC API discovery.
type RpcDiscoveryResult = {
    /// Types that need codec generation
    DiscoveredTypes: SerdeTypeInfo list
    /// Interface metadata for RPC dispatch module generation
    Interfaces: RpcInterfaceInfo list
}

type ISerdeRpcEmitter =
    /// Emit RPC dispatch modules for [<RpcApi>] interfaces.
    /// Returns (hintName, code) pairs for each interface.
    abstract member EmitRpcModules : RpcInterfaceInfo list -> (string * string) list

module SerdeCodegenRegistry =
    let mutable private defaultEmitter : ISerdeCodeEmitter option = None
    let setDefaultEmitter emitter = defaultEmitter <- Some emitter
    let getDefaultEmitter () = defaultEmitter
