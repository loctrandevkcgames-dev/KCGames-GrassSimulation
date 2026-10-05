---
name: grass-encosy-tower
description: Mandatory evaluation and usage guide for the embedded EncosyTower framework in GrassSimulation. Use before planning or implementing any GrassSimulation feature, when choosing data types, events, errors, pools, saves, UI binding, screens or tables, and whenever code would reimplement something EncosyTower may already provide (messaging, results, ids, enums, pooling, persistence, config keys, logging, collections).
---

# EncosyTower in GrassSimulation

Project Owner rule (2026-10-05, start of session): for every feature, look at EncosyTower source and samples to see what
can be applied before building it. Prefer USING existing framework modules over game-side reimplementation. When a module
would need a CHANGE, follow [PROJECT-CONVENTIONS.md section 3](../../../docs/conventions/PROJECT-CONVENTIONS.md): prefer
solving the need in game code first; change the framework only when the game needs it, keep the change minimal and general
(no game-specific types), never mix it with game changes, and commit it first under its `EncosyTower.<Module>` topic through
the `grass-commit` skill. Generator changes must also refresh the shipped DLLs (section 4).

Paths used below: `FW` = `Packages/com.laicasaane.encosy-tower`, `SAMPLES` =
`Assets/Samples/Encosy Tower/0.1.8-preview.3`, `GEN` = `Plugins/SourceGenerator` (repo root; generator sources).
The reference files were written from source and samples. Re-read the cited source before relying on a signature
you have not seen in this session.

## Mandatory evaluation procedure

1. List the feature's needs, one line each (events, failure reasons, ids, variant state, pooling, saving, tables,
   UI, settings, logging, collections).
2. Walk the module index below. For every module that could touch a need, open its reference file, then read the
   framework source and the sample it cites. Do not decide from the module name alone.
3. Fill this table for every candidate and include it in the plan shown to the Project Owner:

   | Module | Where in our code | Benefit / cost | Verdict |
   |---|---|---|---|
   | e.g. EncosyTower.PubSub | `GrassSimulation.Gameplay` level events | Benefit: replaces hand-written events. Cost: asmdef refs, codegen (`[PubSub]`), UniTask/async pipeline, allocations, IL2CPP | Adopt now / Later (trigger) / Reject (concrete blocker; would a minimal general framework fix remove it?) |

   The cost column must state: asmdef references to add, source generators and `partial` rules, async/UniTask use,
   allocation behavior, IL2CPP/AOT concerns, extra packages or defines.
4. Reject needs a concrete, verified blocker (file and line), not taste, plus whether a minimal general framework
   fix would remove it. Later needs a named trigger.
5. After adding asmdef references or generated types, compile through the
   [grass-unity-editor](../grass-unity-editor/SKILL.md) skill. Never run `dotnet build` on Unity projects.

## Module index

| Module / asmdef | Provides | Reference |
|---|---|---|
| `EncosyTower.PubSub` | `[PubSub]` generated messages, `GlobalMessenger`/`Messenger`, scopes, interceptors | [pubsub.md](references/pubsub.md) |
| `EncosyTower.Core` (generators) | `[PolyEnumStruct]`, `[WrapRecord]`/`[WrapType]`, `[EnumExtensions]`, `[UnionId]` | [codegen-types.md](references/codegen-types.md) |
| `EncosyTower.Core` (Common) | `Option<T>`, `Result<TValue,TError>`, `Success<TFailure>`, `Error<T>`, capability interfaces | [common-results.md](references/common-results.md) |
| `EncosyTower.Core` (Pooling) | `GameObjectPool`, `SceneObjectPoolBehaviour`, `NativeGameObjectPool`, `StringBuilderPool` | [pooling.md](references/pooling.md) |
| `EncosyTower.Persistence` | `[Persist]`, `[Persistence]`, `[PersistAccessor]`, local file source | [persistence.md](references/persistence.md) |
| `EncosyTower.Processing` | `[Processing]` request/response, `GlobalProcessor` | [processing.md](references/processing.md) |
| `EncosyTower.Core` (Collections) | `ArrayMap`, `ArraySet`, `ListFast`, `Shared*`, native collections | [collections.md](references/collections.md) |
| `EncosyTower.Core` (Logging, Debugging) | `StaticLogger`, `StaticDevLogger`, `ValidationDefines`, `ThrowHelper` | [logging-validation.md](references/logging-validation.md) |
| `EncosyTower.Mvvm`, `EncosyTower.PageFlows`, `EncosyTower.PageFlows.MonoPages` | `[ObservableObject]`, `[RelayCommand]`, `[MonoBinder]`, page flows | [ui-mvvm-pageflows.md](references/ui-mvvm-pageflows.md) |
| `EncosyTower.Data`, `EncosyTower.Databases.Authoring`, `EncosyTower.Databases.Settings` | `[Data]`, `[Database]`, `[Table]`, BakingSheet authoring | [data-config.md](references/data-config.md) |
| `EncosyTower.Core` (ConfigKeys, Settings) | `ConfigKey<T>` PlayerPrefs, `Settings<T>` | [data-config.md](references/data-config.md) |
| `EncosyTower.VisualToolkit`, `EncosyTower.Entities`, `EncosyTower.Entities.Stats` | UI Toolkit bindings, ECS helpers | Reject: asmdefs reference `Unity.Entities` (VisualToolkit also Latios); game code must not use ECS |
| Core misc (not detailed here) | `UnityTask` (UniTask or Awaitable), `Id`/`Id2`/`Id3`, `StringId`/`StringVault`, `Vaults`, `Variants`, `Encryption`, `Loaders`, `Scenes`, `Localization`, `Buffers`, `TypeFlags` | read `FW/EncosyTower.Core/<folder>` when a need matches |

## Owner decision (2026-10-05)

The Project Owner approved the audit's recommendations as a whole: the Persistence deferral (keep the game-side
`FileProgressStore`) and the adopt now / later / reject lists below. Everything under "Audit findings" is evidence and
rationale from the audit, not separate Owner decisions.

- **Adopt now:** PubSub for gameplay events; `[PolyEnumStruct]` for variant state and typed errors;
  `Result`/`Success` (CODING-CONVENTIONS 8.2); `[WrapRecord]` for `LevelId`; `[EnumExtensions]`; capability
  interfaces (`IHasCount`, `IClearable`, `IIsValid`, `IInitializable`); `GameObjectPool` for prop drops.
- **Later:** Processing (results/shop UI queries); `ConfigKey<bool>` player prefs (sound/haptics); Data/Databases
  (balancing tables); `[UnionId]` (skins catalog); `SharedArray`/native collections (if Burst);
  `SceneObjectPoolBehaviour`; Mvvm + PageFlows/MonoPages (when UI work starts).
- **Reject:** Vaults, `StringId` for persisted ids, `Settings<T>` for player settings, Variants.
- **Persistence:** deferred; keep `FileProgressStore`.

## Audit findings (evidence)

- **Persistence blockers** (lines in [persistence.md](references/persistence.md)): B1 non-atomic write, B2 save success
  not reported and `IsDirty` cleared before the write, B3 a failed load creates defaults and saves over the file.
  Re-evaluation triggers: a second persisted document (skins/shop), anti-tamper (`AesEncryption`) or cloud save. Meanwhile
  copy the sample accessor pattern: `Result<..., Error>` mutations, `[PolyEnumStruct]` error, publish a message on success.
- **`StringId` for persisted ids:** ids are sequential interning indexes assigned by the vault (`new Id(index)` at
  `FW/EncosyTower.Core/StringIds/StringVault.cs:184-186, 199-201`), so they depend on registration order and are not stable
  across sessions or vault instances.
- **`Settings<T>`:** a ScriptableObject singleton loaded with `Resources.Load` at runtime
  (`FW/EncosyTower.Core/Settings/Settings.cs:30, 56`): fine for project configuration, not for mutable per-player settings.
- **Vaults** (service locator; use the composition root plus PubSub) and **Variants** (no dynamic values): these two rejections
  are audit judgement, not a verified blocker.

## Gotchas

- PubSub sync `Publish` is `PublishAsync(...).Forget()`
  (`FW/EncosyTower.PubSub/Publishers/MessagePublisher+Publisher.cs:156`) and brokers take a `lock`
  (`lock (_orderToHandlerMap)` in `FW/EncosyTower.PubSub/Internals/`, the `MessageBroker` generic-1 file) with
  pooled task arrays. Publish discrete or batched
  events only, never per grass cell or per frame.
- `GlobalMessenger` is static and is reset on enter play mode
  (`FW/EncosyTower.PubSub/Messengers/GlobalMessenger.cs`). Tests build their own
  `new Messenger(ArrayPool<UnityTask>.Shared)` (ctor in `Messengers/Messenger.cs`) and dispose it.
- `JsonHelper` is guarded by `UNITY_NEWTONSOFT_JSON` and its `ContractResolver` keeps only writable properties
  (`FW/EncosyTower.Core/Serialization.NewtonsoftJson/JsonHelper.cs:56-65`): get-only properties are not serialized.
- Types receiving generated members must be `partial` (messages, poly enums, wrappers, `[Persist]` types). Case
  structs of a `[PolyEnumStruct]` are its nested `partial` structs.
- Sample asmdefs are Editor-only (`defineConstraints: UNITY_EDITOR || INCLUDE_ENCOSY_SAMPLES_*`). Sample types
  (uGUI MonoBinders, `JsonArrayMap`, `NonEncryption`, `PersistenceAPI`) are not available to game assemblies:
  re-declare what you need in game code, modeled on the sample file.
- `EncosyTower.Entities`, `.Entities.Stats` and `.VisualToolkit` reference ECS/Latios. Game code must not use ECS.
- `Result<TValue,TError>` with identical `TValue` and `TError` types is invalid (checked in Editor/debug builds,
  `FW/EncosyTower.Core/Common/ThrowHelper.cs:64`).
- Game asmdefs reference modules by asmdef name (`EncosyTower.Core`, `EncosyTower.PubSub`, ...), as
  `Assets/GrassSimulation/GrassSimulation.Gameplay/GrassSimulation.Gameplay.asmdef` does with `EncosyTower.Core`.
- Do not edit `Samples~` or bump framework versions (PROJECT-CONVENTIONS section 3).
- New game asmdefs that use record structs (messages, `[WrapRecord]`, results) need a `csc.rsp` next to the asmdef containing
  `-langversion:10` (as `Assets/GrassSimulation/GrassSimulation.Gameplay/csc.rsp` does).
