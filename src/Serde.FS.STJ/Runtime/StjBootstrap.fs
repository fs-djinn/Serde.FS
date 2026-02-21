namespace Serde.FS.STJ

open Serde.FS

[<AutoOpen>]
module StjBootstrap =
    do Serde.DefaultBackend <- StjBackend() :> ISerdeBackend
