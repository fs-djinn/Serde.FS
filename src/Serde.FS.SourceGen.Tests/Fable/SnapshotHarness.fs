/// Plain string-equality snapshot harness for the Fable emitter.
/// On mismatch, writes an `<name>.actual.fs` next to the checked-in
/// `<name>.expected.fs` so the diff is a normal git/IDE operation —
/// no external diff tool required.
module Serde.FS.SourceGen.Tests.Fable.SnapshotHarness

open System.IO
open NUnit.Framework

/// Directory holding checked-in `*.expected.fs` files. Located relative to
/// this source file via `__SOURCE_DIRECTORY__` so tests find snapshots
/// regardless of where the test assembly was copied at runtime.
let private snapshotsDir = Path.Combine(__SOURCE_DIRECTORY__, "Snapshots")

/// Normalise line endings so a Windows-checked-in CRLF expected file matches
/// LF-produced actual output (and vice-versa).
let private normalise (s: string) = s.Replace("\r\n", "\n")

/// Assert that `actual` matches the checked-in snapshot `<name>.expected.fs`.
/// On mismatch (or when no expected file exists yet), writes
/// `<name>.actual.fs` next to the expected file so the dev can:
///   1. open it,
///   2. diff against `<name>.expected.fs`,
///   3. if the change is intentional, rename .actual over .expected.
let assertSnapshot (name: string) (actual: string) =
    if not (Directory.Exists snapshotsDir) then
        Directory.CreateDirectory snapshotsDir |> ignore

    let expectedPath = Path.Combine(snapshotsDir, name + ".expected.fs")
    let actualPath = Path.Combine(snapshotsDir, name + ".actual.fs")

    if not (File.Exists expectedPath) then
        File.WriteAllText(actualPath, actual)
        Assert.Fail (
            sprintf "No expected snapshot for '%s'. Wrote actual to:\n  %s\nReview, and if correct, rename to '%s.expected.fs'."
                name actualPath name)
    else
        let expected = File.ReadAllText expectedPath
        if normalise expected = normalise actual then
            // Match — clean up any stale .actual file from a previous run.
            if File.Exists actualPath then File.Delete actualPath
        else
            File.WriteAllText(actualPath, actual)
            Assert.Fail (
                sprintf "Snapshot mismatch for '%s'. Compare:\n  expected: %s\n  actual:   %s"
                    name expectedPath actualPath)
