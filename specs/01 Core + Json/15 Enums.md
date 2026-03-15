## Enum behavior in .NET and STJ

Enums in .NET are integer‑backed types with named constants. STJ supports two encoding modes:

- **Numeric** (default): `Color.Red` → `1`
- **String** (with `JsonStringEnumConverter`): `Color.Red` → `"Red"`

Serde ecosystems (Rust, Kotlin, Swift) overwhelmingly prefer **string enums** because they are stable and human‑readable. Numeric enums are brittle and unsafe across versions.

Serde.FS should follow the Serde convention: **string enums by default**.

---

## Structural detection

You already have:

```fsharp
TypeKind.Enum of (string * int) list
```

The STJ backend should detect enums via:

```fsharp
match serdeTypeInfo.Raw.Kind with
| Enum cases -> emitEnum serdeTypeInfo cases
```

Each `(name, value)` pair is the raw structural metadata.

---

## Serde semantics for enums

SerdeTypeInfo should interpret attributes:

- `[<SerdeRename("Foo")>]` on the enum type → rename the type (rare)
- `[<SerdeRename("Bar")>]` on enum members → rename the case
- `[<SerdeSkip>]` on enum members → skip the case (rare but allowed)
- `[<SerdeSkipSerialize>]` / `[<SerdeSkipDeserialize>]` → capability restrictions

Serde.FS should compute:

- `SerdeUnionCaseInfo`‑like metadata for enum members
- Effective case name (rename or original)
- Capability for each case

Enums are structurally simpler than unions, but semantically similar.

---

## JSON encoding rules

Enums must serialize as **strings**, not numbers.

### Serialization

Given:

```fsharp
type Color =
    | Red = 1
    | Blue = 2
```

Serialize:

```json
"Red"
```

If renamed:

```fsharp
[<SerdeRename("Crimson")>]
| Red = 1
```

Serialize:

```json
"Crimson"
```

### Deserialization

Given `"Red"` or `"Crimson"`:

- Look up the matching case by effective name
- Construct the enum value using its underlying integer

If the string does not match any case:

- In strict mode → fail
- In non‑strict mode → fail (STJ cannot recover)

Serde.FS should not introduce fallback behavior.

---

## Capability handling

Respect `SerdeTypeInfo.Capability`:

- Serialize‑only → only emit serialization logic
- Deserialize‑only → only emit deserialization logic
- Both → emit both

Case‑level skip attributes override type‑level capability.

---

## Fully qualified type resolution

Generated C# must reference the enum using the same FQ logic as records and tuples.

Example:

```fsharp
namespace My.App
type Color = Red = 1 | Blue = 2
```

Generated C# must use:

```csharp
My.App.Color
```

not just `Color`.

---

## STJ integration

Add a new branch in the STJ emitter:

```fsharp
| Enum cases -> emitEnum serdeTypeInfo cases
```

Implement `emitEnum` to:

- Generate a converter or `JsonTypeInfo` node that:
  - Serializes enum values as strings
  - Deserializes strings back to enum values
  - Uses the effective case names from SerdeAttributes
- Register the converter in the resolver

This should mirror how STJ’s `JsonStringEnumConverter` works, but with Serde rename/skip semantics.

---

## Strict mode behavior

Strict mode applies to enums:

- If any case is skipped or renamed → allowed
- If a JSON string does not match any case → strict mode must reject
- If the enum type is opaque → strict mode must reject (but enums are never opaque)

Enums are always safe in strict mode because they have complete metadata.

---

## Tests

### Basic enum

```fsharp
type Color = Red = 1 | Blue = 2
```

Serialize:

```json
"Red"
```

Deserialize:

```fsharp
"Red" → Color.Red
```

### Renamed case

```fsharp
type Color =
    | [<SerdeRename("Crimson")>] Red = 1
```

Serialize:

```json
"Crimson"
```

### Enum inside record

```fsharp
type User = { Favorite: Color }
```

Generated C# must use FQ type for `Color`.

### Enum inside option/list/map/tuple

```fsharp
type T = { Items: Color list }
```

Serialize:

```json
{ "Items": ["Red", "Blue"] }
```

### Strict mode

- `"Unknown"` → fail
- `"Red"` → succeed

---

## Out of scope

This spec does not include:

- Union support  
- Union tagging  
- Numeric enum mode  
- EnumMemberAttribute support (future optional feature)  
- Naming conventions beyond rename attributes  

---
