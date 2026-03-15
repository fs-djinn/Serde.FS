# **Spec G — Collection Codec Factories (Array, List, Map)**

## **1. Purpose**

Extend the Serde.FS.Json backend with **generic codec factories** for the following collection types:

- `T[]` (F# array)
- `List<T>` (F# list)
- `Map<K,V>` (F# map)

These factories allow the codec resolver to dynamically construct codecs for any instantiation of these types at runtime, using the same pattern as the existing `Set<'T>` factory and the new `Wrapper<'T>` factory.

This spec does **not** modify the JSON backend itself — only the codegen and registry.

---

# **2. Architectural Principles**

### ✔ Pure JSON backend  
No bootstrap, no global resolver registry logic beyond codec registration.

### ✔ Deterministic codec resolution  
Factories must be registered in the generated resolver so that:

```
CodecResolver.resolve typeof<Color[]> registry
```

returns a working codec.

### ✔ No reflection fallback  
Factories must explicitly construct typed codecs using the inner codecs.

### ✔ Consistent with existing patterns  
Factories follow the same shape as:

- `Set<'T>` factory  
- `Wrapper<'T>` factory  

---

# **3. Factory Requirements**

## **3.1 Array Factory (`T[]`)**

### **Factory signature**

```fsharp
let arrayCodecFactory (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec
```

### **Behavior**

1. Extract the element type:  
   `let elemTy = typeArgs.[0]`

2. Resolve the element codec:  
   `let elemCodec = CodecResolver.resolve elemTy registry`

3. Construct the array type:  
   `let arrayTy = elemTy.MakeArrayType()`

4. Instantiate the typed codec:  
   `ArrayCodec(elemCodec, arrayTy)`

5. Return as `IJsonCodec`.

### **Registration**

In the generated resolver:

```fsharp
addFactory (typedefof<_[]>, arrayCodecFactory)
```

---

## **3.2 List Factory (`List<T>`)**

### **Factory signature**

```fsharp
let listCodecFactory (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec
```

### **Behavior**

1. Extract element type  
2. Resolve element codec  
3. Construct `List<T>` type  
4. Instantiate `ListCodec<'T>`  
5. Return as `IJsonCodec`

### **Registration**

```fsharp
addFactory (typedefof<List<_>>, listCodecFactory)
```

---

## **3.3 Map Factory (`Map<K,V>`)**

### **Factory signature**

```fsharp
let mapCodecFactory (typeArgs: Type[]) (registry: CodecRegistry) : IJsonCodec
```

### **Behavior**

1. Extract key and value types  
2. Resolve key and value codecs  
3. Construct `Map<K,V>` type  
4. Instantiate `MapCodec<'K,'V>`  
5. Return as `IJsonCodec`

### **Registration**

```fsharp
addFactory (typedefof<Map<_,_>>, mapCodecFactory)
```

---

# **4. Codegen Requirements**

### **4.1 Where to emit factories**

Factories should be emitted in the same location as:

- `emitGenericWrapperFactory`
- `emitResolver`

Specifically:

- Add new factory modules under `Serde.Generated.<TypeName>`.
- Add factory registration in `Serde.Generated.SerdeJsonResolver.register()`.

### **4.2 Do NOT generate concrete codecs for arrays/lists/maps**

These types are always handled by factories.

---

# **5. Resolver Emission**

In `emitResolver`, add:

```fsharp
GlobalCodecRegistry.Current <-
    CodecRegistry.addFactory (typedefof<_[]>, arrayCodecFactory) GlobalCodecRegistry.Current

GlobalCodecRegistry.Current <-
    CodecRegistry.addFactory (typedefof<List<_>>, listCodecFactory) GlobalCodecRegistry.Current

GlobalCodecRegistry.Current <-
    CodecRegistry.addFactory (typedefof<Map<_,_>>, mapCodecFactory) GlobalCodecRegistry.Current
```

Order does not matter.

---

# **6. Testing Requirements**

Add tests or SampleApp fields to validate:

- `Color[]`
- `List<Shape>`
- `Map<string, Pet>`
- Nested: `List<Map<string, Color[]>>`

All must:

- serialize without `SerdeCodecNotFoundException`
- round‑trip correctly

---

# **7. Out of Scope**

- Sequence codecs (`seq<'T>`)
- ResizeArray codecs
- Immutable collections
- Multi-case generic DUs (future spec)

---
