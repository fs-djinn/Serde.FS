namespace Serde.FS.SourceGen

open Serde.FS

module SerdeCodeEmitter =
    let emit (emitter: ISerdeCodeEmitter) (info: SerdeTypeInfo) : string =
        emitter.Emit(info)
