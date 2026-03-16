namespace Serde

module ResolverBootstrap =
    let mutable registerAll : (unit -> obj) option = None
