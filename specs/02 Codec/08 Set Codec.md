# **Spec 05 — Add `Set<'T>` Codec Support to Serde.FS.Json**

This spec adds JSON codec support for the F# collection type `Set<'T>` in the pure codec‑driven JSON backend. This closes a gap revealed after Spec 04, where STJ fallback behavior was removed and `Set<'T>` no longer had an implicit converter.

---

## **1. Goal**

Add a JSON codec for:

```
Microsoft.FSharp.Collections.FSharpSet<'T>
```

so that:

- `SerdeJson.serialize` and `SerdeJson.deserialize` work for types containing sets  
- Generated codecs can encode/decode sets  
- The SampleApp and user code no longer throw `SerdeCodecNotFoundException` for sets  

---

## **2. Where to implement**

Add the codec in:

```
src/Serde.FS.Json/Codec/JsonCodec.fs
```

(or the equivalent file where list/map/option codecs live).

This codec should follow the same structure and conventions as existing collection codecs.

---

## **3. Codec behavior**

### **Encoding**
- Represent a set as a JSON array.
- Use the element codec to encode each element.
- Preserve the natural ordering of the set (FSharpSet is sorted).

### **Decoding**
- Expect a JSON array.
- Decode each element using the element codec.
- Construct a `Set<'T>` from the decoded list.

---

## **4. Codec implementation**

Implement a function:

```fsharp
val setCodec : IJsonCodec<'T> -> IJsonCodec<Set<'T>>
```

The codec must:

- Implement `IJsonCodec<Set<'T>>`
- Encode as `JsonValue.Array`
- Decode from `JsonValue.Array`
- Throw a descriptive error if the JSON is not an array

---

## **5. Registry integration**

Register the codec in the global codec registry so that:

```fsharp
CodecResolver.resolve<Set<int>>()
```

returns the new codec automatically.

Registration should follow the same pattern as list/map/option registration:

- Register a factory for the generic type definition `Set<_>`
- Use the element codec provided by the registry

---

## **6. Tests**

Add tests in the JSON test suite verifying:

### **Encoding**
```fsharp
Set.ofList [1; 2; 3]
```
encodes to:
```json
[1,2,3]
```

### **Decoding**
The JSON array decodes back into a `Set<int>` with the same elements.

### **Round‑trip**
Round‑trip tests for:

- `Set<int>`
- `Set<string>`
- Nested sets inside records/unions

### **Error cases**
- Decoding a non‑array JSON value should throw a clear error.

---

## **7. Acceptance criteria**

Spec 05 is complete when:

1. `Set<'T>` can be serialized and deserialized via `SerdeJson.serialize` / `SerdeJson.deserialize`.
2. `CodecResolver.resolve<Set<'T>>()` returns a working codec.
3. Generated codecs for types containing sets compile and work.
4. All new tests pass.
5. The SampleApp runs successfully without codec‑not‑found errors.

---
