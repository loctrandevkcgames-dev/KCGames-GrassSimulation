# Persistence (EncosyTower.Persistence)

Owner decision (2026-10-05): **deferred; the game keeps its own `FileProgressStore`**. Everything else in this file is audit
findings: the blockers B1 to B3 below and the re-evaluation triggers (a second persisted document such as skins/shop,
anti-tamper via `AesEncryption` in `FW/EncosyTower.Core/Encryption/`, or cloud save). Until then, copy only the sample
accessor pattern (below).

## How it works

- asmdef: `EncosyTower.Persistence` (references `EncosyTower.Core`, `UniTask`; `FW/EncosyTower.Persistence/EncosyTower.Persistence.asmdef`).
  Generator: `FW/EncosyTower.Persistence/SourceGenerators/EncosyTower.Persistence.Generators.dll`
  (source `GEN/EncosyTower.Persistence.Generators/`, snapshots `GEN/EncosyTower.SourceGen.Tests/Persistence/`).
  Needs Newtonsoft for JSON via the game-supplied (de)serialize funcs (sample uses `JsonHelper`, `UNITY_NEWTONSOFT_JSON`).
- Attributes (namespace `EncosyTower.Persistences`, `FW/EncosyTower.Persistence/Annotations/`):
  `[Persist]` on a data class/struct (generator adds `IPersist` members `string Id`, `int Version`),
  `[Persistence]` on a `static partial class` (generates nested `Persistence`, `PersistDirectory`, load/save for all
  `[Persist]` types), `[PersistAccessor(typeof(TPersistence))]` on an `IPersistAccessor` class.
- Sample: `SAMPLES/EncosyTower.Samples.Persistence/Persistences/` (`PlayerPersistence.cs`, `PlayerDataAccessor.cs`,
  `PlayerDataError.cs`, `PlayerMessages.cs`, `PersistenceAPI.cs`, `NonEncryption.cs`) and `Shared/JsonArrayMap.cs`.

```csharp
[Serializable, Persist]
internal partial class PlayerData { public JsonArrayMap<ItemId, int> ItemAmounts { get; set; } = new(); }   // JSON needs writable props

[Persistence] public static partial class PlayerPersistence { ... }     // wiring in sample PlayerPersistence.cs:
//   s_vault = new Persistence(StringVault.Default, new NonEncryption(logger), logger, ArrayPool<UnityTask>.Shared, userId);
//   await s_vault.TryLoadAsync(logger, userId, SourcePriority.OnlyLocal, SaveDestination.Local, token);
//   partial class PersistDirectory { static partial void GetIgnoreEncryption(ref bool ignore); private static partial PersistStoreArgs GetStoreArgs<TData,TStore>(Func<TData> createFunc) ... }
```

`GetStoreArgs` returns `PersistStoreDefault<TData>.Args(createFunc, PersistSourceLocal<TData>.Args(RootPath, SerializeFunc,
DeserializeFunc, FileExtension, MakeFilePathFunc))` (sample `PersistenceAPI.cs`). The file name comes from a `StringId` key
resolved through a `StringVault` (`PersistSourceLocal.cs:216`).

## Sample accessor pattern to copy now (game-side, no framework dependency)

`SAMPLES/.../PlayerDataAccessor.cs`: a sealed accessor over the store with mutations returning
`Result<TMessage, TError>`; on success the caller publishes the message through PubSub.

```csharp
public Result<OnItemAmountUpdatedMsg, Error> AddSingletonItem(ItemId id)
{
    var error = Error.ItemAlreadyAcquired(id).Prefix(nameof(AddSingletonItem));   // PolyEnumStruct error via factory wrapper
    var result = Add(Data.ItemAmounts, id, true, error);
    MarkDirtyIfSuccess(result);
    return result.TryGetValue(out var changed) ? new OnItemAmountUpdatedMsg(id, changed) : result.GetErrorOrDefault();
}
```

Message (`PlayerMessages.cs`): `[PubSub(ApiMode.Sync, State = StateMode.Stateful, Scope = typeof(PlayerAccessorScope))] readonly partial record struct OnItemAmountUpdatedMsg(ItemId Id, Changed<int> Value);`
Error type: `PlayerDataError.cs` (see codegen-types.md).

## Blockers (verified in source)

- **B1 non-atomic write.** `PersistSourceLocal.OnSaveAsync` writes straight to the final path:
  `await File.WriteAllTextAsync(filePath, raw, Encoding.UTF8, token);` (`FW/EncosyTower.Persistence/PersistSources/PersistSourceLocal.cs:147`).
  A crash or cancellation mid-write leaves a truncated save; no temp file or backup.
- **B2 save success not reported, dirty cleared first.** `PersistSourceBase.SaveAsync` sets `IsDirty = false` (line 67)
  before `await OnSaveAsync(data, token)` (line 69). `OnSaveAsync` returns `UnityTask` with no status (`PersistSourceLocal.cs:124`),
  skips the write silently when serialization fails (line 137) and swallows exceptions with `Logger.LogException(ex)`
  (lines 150-153). Callers cannot tell failure from success and the data is no longer marked dirty, so it is not retried.
- **B3 failed load saves defaults over the file.** `PersistSourceLocal.TryLoadAsync` returns `Option.None` on any failure
  (missing, unreadable, undecodable; lines 156-196). `PersistStoreDefault.LoadAsync` then keeps data null (lines 145-153).
  `PersistenceBase.TryLoadAsync` calls `PersistDirectory.CreateDataIfNotExist()` (`Persistences/PersistenceBase.cs:50`; the
  generator emits it at `GEN/EncosyTower.Persistence.Generators/Generators/PersistenceDeclaration+WriteCode.cs:1374`), sets
  `_markDirtyBeforeSaving = true` (line 31), then `await SaveAsync(...)` (line 59) marks everything dirty (lines 78-82) and writes
  the defaults over the unreadable file. It still returns `true` because `OnTryLoadAsync` defaults to true (lines 54-61, 87-98).

## Minimal framework-fix scope if ever adopted (commit separately, general, no game types)

- F1 (runtime only): in `PersistSourceLocal.OnSaveAsync` write to a temp file in the same directory, replace the target
  (`File.Replace`/move), keep a `.bak` of the previous file; fall back to the `.bak` on load.
- F2 (generator change): report save status (for example `UnityTask<bool>`/status enum through `PersistSourceBase.SaveAsync`,
  `PersistStoreDefault.SaveAsync`, generated `SaveEntireDirectoryAsync`) and keep `IsDirty` set when the write fails.
- F3 (generator change): report load status (loaded / not found / corrupt) and do not call `CreateDataIfNotExist` + mark
  dirty + save after a corrupt or unreadable file; quarantine instead.
- F2 and F3 touch generated code: follow PROJECT-CONVENTIONS section 4 (use `GEN/EncosyTower.SourceGen.slnx`, refresh the
  shipped DLL under `FW/EncosyTower.Persistence/SourceGenerators/`, commit it with the generator change).

## Where it applies in GrassSimulation

`GrassSimulation.Progression` (`FileProgressStore`). Mutations such as settle/unlock should follow the accessor pattern now
(Result + typed error + PubSub message). Do not migrate storage to this module until a re-evaluation trigger fires and
F1 to F3 (or equivalent) are done.
