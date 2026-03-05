# **Specification: Add Compile‑Time Diagnostic for Root‑Level Constructed Generics in Serde.FS**

## **Purpose**
Ensure that calls to `Serde.Serialize` and `Serde.Deserialize` that pass a **root‑level constructed generic type** without an explicit type argument produce a **build‑time error** with:

- the **fully qualified type name** of the constructed generic,
- a **copy‑pasteable fix**, and
- the **relative file path + line/column** of the call site.

This eliminates silent failures and teaches users the correct usage pattern.

---

## **Scope**
Claude must modify **Serde.FS.SourceGen** only.  
No changes to Djinn, Serde runtime, or the public API.

---

## **Invariants**
Claude must follow these invariants exactly:

- Diagnostics must be **errors**, not warnings.
- Diagnostics must include **relative file paths**.
- Diagnostics must include **fully qualified type names**.
- Diagnostics must include **generic arguments** when known.
- When generic arguments are unknown, use `'T` in the type display and `_` in the suggested fix.
- Diagnostics must be **copy‑pasteable**.
- Only trigger for **root‑level constructed generics**.
- Never trigger for:
  - non‑generic types,
  - generic definitions inside `[<Serde>]` types,
  - nested generics inside records/unions,
  - calls that already specify `<T>`.

---

## **Detection Rules**
Claude must implement the following detection logic:

### **Trigger when ALL of the following are true:**
1. The call is to `Serde.Serialize` or `Serde.Deserialize`.
2. The call has **no explicit type argument** (`SynExpr.TypeApp` absent).
3. The argument expression resolves to a **constructed generic type**:
   - e.g., `Wrapper<Guid>`, `Result<int, string>`, etc.
4. The type is **not discoverable** from a `[<Serde>]` type definition at the call site.
5. The type is **not** a simple generic definition (e.g., `Wrapper<'T>` without instantiation).

### **Do NOT trigger when:**
- The user already wrote `Serde.Serialize<SomeType>(...)`.
- The type is non‑generic.
- The type is a generic definition inside a Serde‑annotated type.
- The call is inside generated code.

---

## **Diagnostic Message Format**

Claude must emit diagnostics in this exact format:

### **When generic arguments are known**
```
Serde.FS error: The value passed to `Serialize` has a constructed generic type:
    <FULLY_QUALIFIED_TYPE>

at: <RELATIVE_PATH>(<LINE>,<COLUMN>)

Root‑level constructed generics require an explicit type argument.
Use:
    Serde.Serialize<<FULLY_QUALIFIED_TYPE>>(value)
```

### **When generic arguments are unknown**
```
Serde.FS error: The value passed to `Serialize` has a constructed generic type:
    <FULLY_QUALIFIED_GENERIC_DEFINITION<'T>>

at: <RELATIVE_PATH>(<LINE>,<COLUMN>)

Root‑level constructed generics require an explicit type argument.
Use:
    Serde.Serialize<<FULLY_QUALIFIED_GENERIC_DEFINITION<_>>>(value)
```

### **Deserialize version**
Replace `Serialize` with `Deserialize` and adjust the wording:

```
Serde.FS error: The return type of `Deserialize` has a constructed generic type:
    <FULLY_QUALIFIED_TYPE>

at: <RELATIVE_PATH>(<LINE>,<COLUMN>)

Root‑level constructed generics require an explicit type argument.
Use:
    Serde.Deserialize<<FULLY_QUALIFIED_TYPE>>(json)
```

---

## **Relative Path Requirements**
Claude must compute the relative path from the project root (the MSBuild project directory).  
Format must match F# compiler diagnostics:

```
src/MyApp/Handlers/UserHandler.fs(42,17)
```

No absolute paths.

---

## **Fully Qualified Type Requirements**
Claude must output:

- Namespace
- All containing modules
- Type name
- Generic arguments (if known)

Example:

```
MyApp.Domain.Models.Wrapper<System.Guid>
```

If unknown:

```
MyApp.Domain.Models.Wrapper<'T>
```

---

## **Copy‑Pasteability Requirements**
The suggested fix must:

- compile as‑is,
- include the fully qualified type,
- include the correct generic arguments or `_`,
- reuse the argument expression **only if it is a simple identifier**.

If the argument is complex, Claude must use `(value)` as a placeholder.

---

## **Implementation Steps**
Claude must:

1. Update the call‑site discovery logic to detect missing type arguments.
2. Resolve the argument expression’s type shape using existing Djinn metadata.
3. Determine whether generic arguments are known or unknown.
4. Construct the fully qualified type string.
5. Construct the relative path.
6. Emit the diagnostic using the existing Roslyn/F# generator diagnostic mechanism.
7. Ensure the diagnostic blocks code generation for that call site.

---

## **Acceptance Criteria**
The feature is complete when:

- A call like `Serde.Serialize x` where `x : Wrapper<Guid>` produces the correct diagnostic.
- The diagnostic includes:
  - fully qualified type,
  - correct generic arguments,
  - relative path,
  - line/column,
  - copy‑pasteable fix.
- Calls with explicit `<T>` do not trigger.
- Nested generics inside Serde types do not trigger.
- Non‑generic types do not trigger.
- The diagnostic appears in IDEs and CLI builds.

---
