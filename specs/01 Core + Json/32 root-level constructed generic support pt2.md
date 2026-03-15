# **Specification: Generic Serde Type Closure for Serde.FS**

## **Purpose**
Enable Serde.FS to correctly serialize and deserialize **closed constructed generic types** such as:

```
Wrapper<Person>
Wrapper<Order>
Wrapper<Wrapper<Person>>
```

whenever:

- the generic type definition is marked `[<Serde>]`, and  
- the type arguments are Serde‑enabled, and  
- the closed constructed type appears anywhere in the user’s code (including call sites).

This removes the current limitation where generic Serde types cannot be used as root types.

---

## **High‑Level Behavior**
When the generator encounters a Serde‑annotated generic type definition:

```fsharp
[<Serde>]
type Wrapper<'T> = Wrapper of 'T
```

it must treat this as a **generic Serde template**.

Whenever a *closed constructed instance* of this generic appears in the program, the generator must:

1. **Resolve the closed type** (e.g., `Wrapper<Person>`).
2. **Verify that all type arguments are Serde‑enabled**.
3. **Generate Serde metadata for the closed type**.
4. **Emit the metadata into the generated assembly**.

This must happen even if the closed type appears **only** at:

- a `Serialize<...>` call site  
- a `Deserialize<...>` call site  
- a type annotation  
- a let‑binding  
- a function parameter or return type  

---

## **Detection Rules**

### **A closed constructed generic type must be generated when:**

1. The generic type definition is marked `[<Serde>]`.
2. The closed type appears anywhere in the untyped AST as:
   - a type argument to `Serialize<...>` or `Deserialize<...>`
   - a type annotation (`let x : Wrapper<Person> = ...`)
   - a function parameter type
   - a function return type
   - a field type inside another Serde type
3. All type arguments are Serde‑enabled types (either:
   - marked `[<Serde>]`, or  
   - themselves closed constructed generics that satisfy this rule).

### **Do NOT generate metadata when:**

- The generic type definition is not marked `[<Serde>]`.
- Any type argument is not Serde‑enabled.
- The type is a type abbreviation (ignored by F#; cannot carry attributes).
- The type appears only inside generated code.

---

## **Metadata Generation Requirements**

For each closed constructed type (e.g., `Wrapper<Person>`), Claude must:

- Generate a full Serde metadata record exactly as if the user had written:

  ```fsharp
  [<Serde>]
  type WrapperOfPerson = Wrapper<Person>
  ```

- Use the generic definition’s serialization logic, substituting the closed type arguments.
- Ensure that nested constructed generics are recursively generated.

Example:

If the generator sees:

```
Wrapper<Person>
Wrapper<Order>
Wrapper<Wrapper<Person>>
```

it must generate metadata for all three.

---

## **Call‑Site Driven Type Closure**

### **Serialize**
When the generator sees:

```fsharp
Serde.Serialize<Wrapper<Person>> value
```

it must:

1. Parse the type argument `Wrapper<Person>`.
2. Resolve it to a closed constructed type.
3. Generate metadata for that type.
4. Use that metadata for code generation.

### **Deserialize**
When the generator sees:

```fsharp
Serde.Deserialize<Wrapper<Person>> json
```

it must perform the same steps.

---

## **Type Resolution Rules**

Claude must resolve closed constructed types using only the untyped AST:

- `Wrapper<Person>` is a `SynType.App` node.
- The generic definition `Wrapper<'T>` is known to be Serde‑annotated.
- The type argument `Person` is a Serde‑annotated concrete type.
- Therefore the closed type is valid and must be generated.

No type inference is required or available.

---

## **Error Conditions**

If a closed constructed type is encountered where any type argument is not Serde‑enabled, emit a diagnostic:

```
Serde.FS error: The generic type '<FULLY_QUALIFIED_TYPE>' cannot be used with Serde because its type argument '<ARG>' is not Serde-enabled.
```

Include:

- fully qualified type  
- relative file path  
- line/column  

---

## **Interaction with the Root‑Level Constructed Generic Diagnostic**

The previously defined diagnostic still applies:

- If the user calls `Serialize value` with a constructed generic, require `<T>`.
- Once `<T>` is provided, the generator must generate metadata for that closed type.

These two behaviors complement each other.

---

## **Acceptance Criteria**

The feature is complete when:

- `Serialize<Wrapper<Person>>` works.
- `Deserialize<Wrapper<Person>>` works.
- No type abbreviations are required.
- No extra Serde attributes are required.
- Nested generics work (`Wrapper<Wrapper<Person>>`).
- Metadata is generated for all closed constructed types that appear in the program.
- Errors are emitted when type arguments are not Serde‑enabled.
- The behavior is stable and deterministic.

---
