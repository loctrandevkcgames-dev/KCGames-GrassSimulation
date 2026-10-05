# Collections and buffers (EncosyTower.Core, namespace `EncosyTower.Collections`)

asmdef: `EncosyTower.Core` (also references `Unity.Collections`, `Unity.Burst`, `Unity.Mathematics`). No generator.
Sources: `FW/EncosyTower.Core/Collections/`, `Collections.Contracts/`, `Collections.Extensions/`, `Collections.Unsafe/`,
`Buffers/`. No dedicated sample; the Persistence sample uses `ArrayMap` through `Shared/JsonArrayMap.cs`.
Verdict: Later. Approved adopt-now items are only the capability interfaces `IHasCount`, `IClearable`, `IIsValid`, `IInitializable`.
`StringBuilderPool`/`CollectionPools` (pooling.md), `IHasCapacity` and `IAsSpan<T>` are available: adopt when needed.

## Types

| Type | Facts (verified) | File |
|---|---|---|
| `ArrayMap<TKey,TValue>` | Class, `IDisposable`, dense array-backed dictionary. `Add`, `TryAdd`, `TryGetValue`, `ContainsKey`, `Remove`, `GetOrAdd` (ref return), `GetValueByRef`, `Trim`, `Union/Intersect/Exclude`. Does NOT implement `IDictionary`. | `Collections/ArrayMap.cs:49` |
| `ArraySet<T>` | Class, `ICollection<T>`, `IHasCount`, `IClearable`, `IIncreaseCapacity`. | `Collections/ArraySet.cs:44` |
| `ListFast<T>` | `readonly struct` wrapping a `ListExposed<T>` (internals-exposed list); `Find` returns `Option<T>`; `IAsSpan<T>`, `IHasCount`, `IIsCreated`. | `Collections/ListFast.cs:15` |
| `SharedArray<T>` / `SharedArray<T,TNative>` | Managed array that can be viewed as a `NativeArray<TNative>` / slice (alias, `T : unmanaged`, equal size enforced); implicit conversion to `NativeArray`. Also `SharedList`, `SharedQueue`, `SharedStack`, `SharedArrayMap`, `SharedArraySet`, `SharedReference`. | `Collections/SharedArray.cs:69` |
| `ArrayMapNative`, `ListNative`, `QueueNative`, `StackNative`, `SharedArrayNative`, ... | Unmanaged, `[NativeContainer]`, Burst/Jobs friendly; `TKey : unmanaged, IEquatable<TKey>`; compiled under `UNITY_COLLECTIONS`. | `Collections/ArrayMapNative.cs:58` |
| `ListProxy<T>`, `NativeSliceReadOnly`, `DictionaryReadOnly`, `HashSetReadOnly`, `*.ReadOnly` views | Read-only wrappers. | `Collections/` |

Extensions: `AsListFast()`, `IncreaseCapacityBy`, spans, per-type files in `Collections.Extensions/` and `Collections.Extensions.Unsafe/`.
Buffers (`Buffers/`): `BufferManaged`, `BufferNative`, `BufferUnsafe`, `BufferShared<T,TNative>`, `IBuffer`, `IAlloc`.

## Gotchas

- **Unity serialization only.** `ArrayMap`/`ArraySet` serialize through `ISerializationCallbackReceiver`
  (`Collections/ArrayMap+Serialization.cs`, `ArraySet+Serialization.cs`), not JSON. For Newtonsoft persistence subclass and
  add `IDictionary` as the sample does (`SAMPLES/EncosyTower.Samples.Persistence/Shared/JsonArrayMap.cs`, Editor-only asmdef:
  re-declare it in game code). `JsonHelper` keeps only writable properties (see logging-validation.md / persistence.md).
- `ArrayMap`/`ArraySet` implement `IDisposable` (`ArrayMap.cs:49`, `ArraySet.cs:44`): dispose owners.
- Native collections are compiled under `UNITY_COLLECTIONS` and carry safety handles under
  `ENABLE_UNITY_COLLECTIONS_CHECKS`; follow CODING-CONVENTIONS section 13 (Burst/Jobs) when using them.
- `SharedArray` safety handles are active under `ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY`.
- Prefer plain BCL collections when none of the framework features (spans, `Option`-returning finds, native aliasing,
  ref access) are needed; ECS-free Burst code is the main reason to adopt native/shared variants.

## Where it applies in GrassSimulation

Cell/blade state arrays shared between managed code and Burst jobs (`SharedArray`/`ArrayMapNative`), only if/when a Burst
job needs them. Capability interfaces `IHasCount` and `IClearable` are Adopt now for game collection-like types (common-results.md);
`IHasCapacity` and `IAsSpan<T>` are available, adopt when needed.
