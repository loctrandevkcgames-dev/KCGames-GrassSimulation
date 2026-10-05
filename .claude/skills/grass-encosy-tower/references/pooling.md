# Pooling (EncosyTower.Core, namespace `EncosyTower.Pooling`)

asmdef: `EncosyTower.Core`. No generator. Sources: `FW/EncosyTower.Core/Pooling/` and `Pooling.Native/`.

## GameObjectPool (managed; Adopt now for prop drops)

`FW/EncosyTower.Core/Pooling/GameObjectPool.cs`, `GameObjectPrefab.cs`. No shipped sample uses the managed pool (the framework's own use is `MonoPagePool.cs`); the
only pooling sample is the native pool (`SAMPLES/EncosyTower.Samples.Pooling/NativeGameObjectPooler.cs`), so write a
small EditMode/PlayMode test when adopting.

```csharp
var pool = new GameObjectPool {
    Prefab = new GameObjectPrefab {
        Source = propPrefab,               // GameObject; required, else NullReferenceException on Prepool/Rent
        Parent = poolRoot,                 // Transform (Location.Parent)
        InstantiateInWorldSpace = false,
        Scene = activeScene,               // Location.Scene
        PoolScene = poolScene,             // Location.PoolScene
    },
    RentingStrategy = RentingStrategy.Activate,
    ReturningStrategy = ReturningStrategy.Deactivate,
    TrimCloneSuffix = true,
};
pool.Prepool(32);                          // Prepool(int amount) / Prepool(int amount, ReturningStrategy)
var go = pool.RentGameObject(RentingStrategy.Default);   // also RentTransform, RentGameObjectId, RentTransformId
pool.Return(go, ReturningStrategy.Default);              // GameObject, Transform, spans, ids
pool.ReleaseInstances(keep: 8, onReleased: null);        // destroys unused instances beyond `keep`
pool.ReleaseInstances(0); pool.Dispose();            // Dispose alone leaves instances alive
```

- `RentingStrategy` { `Default`, `Activate`, `DoNothing` }; `ReturningStrategy` { `Default`, `Deactivate`, `DoNothing` }
  (`RentingStrategy.cs`, `ReturningStrategy.cs`). The per-call strategy overrides the pool default.
- `UnusedCount` reports idle instances. Rent with an empty pool prepools one automatically
  (`RentGameObject`: `if (UnusedCount < 1) Prepool(1)`). Batch overloads fill `Span<GameObject>`/`Span<Transform>`/id spans.
- Only the single-instance `Instantiate` (used by `Prepool(1)`) falls back to `Parent` (`GameObjectPrefab.cs:64-81`). The batch
  path used by `Prepool(n > 1)` tries `Parent`, `PoolScene`, `Scene` and returns `false` when none is valid
  (`GameObjectPrefab.cs:84-129`; `GameObjectPool.cs:113-121`): `Prepool(32)` with a null `Parent` and default scenes creates nothing.
  Always set at least one valid location.
- `Return` ignores invalid (destroyed) objects. Pooled objects are moved between `PoolScene` and `Scene`.
- `Dispose()` only clears the lists and nulls the prefab; it does NOT destroy instances (`GameObjectPool.cs:960-967`). Call
  `ReleaseInstances(0)` first. Reference pattern in the framework itself: `FW/EncosyTower.PageFlows.MonoPages/Internals/MonoPagePool.cs`
  (pool setup lines 22, 60, 71, 80, 135; `Dispose` at 138-143 calls `ReleaseInstances(0)` then `Dispose()`).
- `Return` does not guard against returning the same object twice.

## SceneObjectPoolBehaviour, NativeGameObjectPool (Later)

- `SceneObjectPoolBehaviour<TKey>` (`Pooling/SceneObjectPoolBehaviour` generic-1 file, compiled only with `UNITY_MATHEMATICS`):
  a `MonoBehaviour` exposing `TransformAccessArray`, `Positions`, `Scales`, `Rotations` (native lists) for job-driven
  transforms; `Initialize(TKey key, Scene scene, uint initialCapacity, int desiredJobCount = -1)` (protected; subclass it) loads the prefab via
  `key.TryLoad()`; `TKey` must satisfy `ITryLoad<GameObject>` and `ITryLoadAsync<GameObject>` (`EncosyTower.Loaders`; constraint in the
  `SceneObjectPoolBehaviour` generic-1 `_Async` file, lines 11-12), which `ResourceKey<GameObject>` and `AddressableKey<GameObject>` satisfy; rent/return by `GameObject`, `GameObjectInfo`, ids. Consider only for large Burst-driven prop sets.
- `NativeGameObjectPool` (`Pooling.Native/`, `UNITY_COLLECTIONS`): built from
  `NativePrefabInfo(templateInstanceId, position, rotation, scale, rentScene, returnScene)` plus capacity; rent with
  `Rent(int amount, NativeList<GameObjectInfo> result, NativeRentingOptions.Everything)`, return with
  `Return(slice, NativeReturningOptions.Everything)`; `Dispose()` (sample lines 60-110). Pairs with Jobs/Burst.

## Other pools

- `StringBuilderPool.Rent(out var sb)` returns a `PooledObject<StringBuilder>` (use `using`);
  `Rent()`/`Return(sb)` (`StringBulilderPool.cs`).
- `QueuePool<T>`, `StackPool<T>`, `ArrayMapPool<TKey,TValue>`: `Get()`, `Get(out value)`, `Release(x)` (`CollectionPools.cs`).

## Gotchas

- `GameObjectPool` holds plain `List`s and Unity objects with no locking: use it from the main thread only.
- A destroyed object returned to the pool is ignored (`Return` checks `IsInvalid()`), but a destroyed idle instance
  would still be in the list: do not `Destroy` pooled objects directly; `Return` them or `ReleaseInstances`.

## Where it applies in GrassSimulation

Prop drops (Adopt now): one `GameObjectPool` per prop prefab, `Prepool` on level load, `Return` on cleanup, `ReleaseInstances(0)` then `Dispose`
on session end. Native/Scene pools are Later (only with Burst-driven grass or prop transforms).
