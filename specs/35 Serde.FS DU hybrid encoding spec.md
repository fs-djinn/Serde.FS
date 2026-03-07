### Serde.FS DU hybrid encoding spec (final)

---

#### 1. Philosophy

- **Serde.FS.Json chooses the DU encoding.**  
- Encoding is **deterministic, compile‑time–known, reflection‑free**.  
- **No user‑configurable DU encoding** (beyond future explicit attributes).  
- If users need arbitrary/custom DU encodings, they should use a reflection‑based library (e.g., FSharp.SystemTextJson).

---

#### 2. Wrapper DUs

**Definition**

A *wrapper DU* is a discriminated union that:

- **has exactly one case**, and  
- that case has **exactly one field**.

Examples:

```fsharp
type Wrapper<'T> = Wrapper of 'T
type UserId = UserId of int
```

**JSON encoding**

Root JSON is an object with exactly one property:

```json
{ "<CaseName>": <payload> }
```

Examples:

```fsharp
Wrapper { Name = "Jordan" }
→ { "Wrapper": { "Name": "Jordan" } }

UserId 42
→ { "UserId": 42 }
```

**Properties**

- Property name = case name.
- Property value = field payload encoded with normal Serde rules.
- Applies to generic and non‑generic wrapper DUs.

---

#### 3. Multi‑case DUs

**Definition**

Any DU that is **not** a wrapper DU:

- more than one case, or  
- a single case with zero fields, or  
- a single case with more than one field.

Examples:

```fsharp
type Shape =
  | Circle of radius: float
  | Rectangle of width: float * height: float

type Status =
  | Pending
  | Completed of at: System.DateTime
```

**JSON encoding**

Root JSON is an object with `"Case"` and `"Fields"`:

```json
{ "Case": "<CaseName>", "Fields": [ ... ] }
```

Examples:

```fsharp
Circle 2.0
→ { "Case": "Circle", "Fields": [ 2.0 ] }

Rectangle (3.0, 4.0)
→ { "Case": "Rectangle", "Fields": [ 3.0, 4.0 ] }

Pending
→ { "Case": "Pending", "Fields": [] }

Completed someDate
→ { "Case": "Completed", "Fields": [ "<date-encoding>" ] }
```

**Properties**

- `"Case"` is the DU case name.
- `"Fields"` is an array of field values in declaration order.
- Zero‑field cases use an empty array.

---

#### 4. Generator rules

For each `[<Serde>]` DU:

- **Classify DU kind:**
  - **Wrapper DU** if:
    - exactly one case, and  
    - that case has exactly one field.
  - **Multi‑case DU** otherwise.
- Emit metadata including:
  - **DU kind:** `Wrapper` or `MultiCase`.
  - Case names.
  - Field types in order.
  - Any additional info needed by the backend.

This classification is **compile‑time and fixed**.

---

#### 5. Runtime behavior

**Wrapper DU deserialization**

- Expect root JSON to be an object with **exactly one property**.
- Property name → case name.
- Property value → payload.
- Use metadata for the DU and its single case.
- Deserialize payload into the case’s single field type.

**Multi‑case DU deserialization**

- Expect root JSON to be an object with `"Case"` and `"Fields"`.
- `"Case"` → case name.
- `"Fields"` → array of field values.
- Use metadata for the DU and case.
- Deserialize each field in order.

**Shape mismatch**

- If JSON shape does not match the expected DU kind (wrapper vs multi‑case),  
  throw a clear deserialization error indicating:
  - expected DU kind,  
  - actual JSON shape.

---

#### 6. Guarantees and non‑goals

- **Encoding is part of the Serde.FS contract.**
- Encoding is **stable** across versions (barring explicit major breaking changes).
- **No per‑type/per‑case DU encoding customization.**
- All metadata, serializers, and deserializers are generated assuming this hybrid encoding.

---
