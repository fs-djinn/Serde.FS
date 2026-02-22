namespace Serde.FS.STJ

open Serde.FS

type SerdeStjOptions() =
    interface ISerdeOptions with
        /// Gets or sets strict mode. Mirrors Serde.Strict.
        member _.Strict
            with get () = Serde.Strict
            and set v = Serde.Strict <- v
    member this.Strict
        with get () = (this :> ISerdeOptions).Strict
        and set v = (this :> ISerdeOptions).Strict <- v

module internal SerdeStjDefaults =
    let options = SerdeStjOptions()
