# UI: Mvvm, PageFlows, MonoPages (Later: when UI work starts)

Verdict: Later. Trigger: first real UI screens (menus, results, shop). Evaluate against plain uGUI/UI Toolkit then, using
the table in SKILL.md. VisualToolkit is Reject (ECS/Latios references, below).

## Module facts

| Module | asmdef references (from asmdef files) | Generator DLLs |
|---|---|---|
| `EncosyTower.Mvvm` | `EncosyTower.Core`, `UniTask`, `UniTask.Addressables`, `Unity.Addressables`, `Unity.Burst`, `Unity.Collections`, `Unity.Localization`, `Unity.Mathematics`, `Unity.ResourceManager`, `Unity.TextMeshPro`; `overrideReferences: false` | `FW/EncosyTower.Mvvm/SourceGenerators/EncosyTower.Mvvm.Generators.dll`, `...CodeRefactors.dll` |
| `EncosyTower.PageFlows` | `EncosyTower.Core`, `EncosyTower.PubSub`, `UniTask` | none |
| `EncosyTower.PageFlows.MonoPages` | `EncosyTower.Core`, `EncosyTower.PageFlows`, `EncosyTower.Processing`, `EncosyTower.PubSub`, `UniTask`, `Unity.Collections`, `UnityEngine.UI` | none |
| `EncosyTower.VisualToolkit` | `EncosyTower.Core/Mvvm/PageFlows/PageFlows.MonoPages`, `Latios.Core/Psyshock/Transforms`, `Unity.Entities`, `Unity.Entities.Hybrid` | Reject: game code must not use ECS |

`EncosyTower.Core.asmdef` already references `UniTask`, `Unity.Addressables`, `Unity.ResourceManager` and `Unity.Localization`
(plus `UniTask.Addressables`), so beyond Core, `EncosyTower.Mvvm` adds only `Unity.TextMeshPro` (and Burst/Collections/Mathematics,
which Core also references).

## Mvvm (`EncosyTower.Mvvm`)

- Attributes: `[ObservableObject]` (class, `FW/EncosyTower.Mvvm/ComponentModel/Annotations/ObservableObjectAttribute.cs`),
  `[ObservableProperty]` / `[ObservableProperty("Name")]` (field or property), `[NotifyPropertyChangedFor(nameof(Other))]`,
  `[NotifyCanExecuteChangedFor]`, `[RelayCommand]` with `CanExecute` name (`Input/Annotations/RelayCommandAttribute.cs`).
- Sample `SAMPLES/EncosyTower.Samples.Mvvm/SampleMvvm.cs` (works on a `MonoBehaviour`):

```csharp
[ObservableObject]
public sealed partial class SampleMvvm : MonoBehaviour
{
    [ObservableProperty]
    public float ScrollPosition { get => Get_ScrollPosition(); set => Set_ScrollPosition(value); }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Status))]
    public bool Stopped { get => Get_Stopped(); set => Set_Stopped(value); }   // backing field _stopped is generated

    [RelayCommand] private void OnStop() { Stopped = !Stopped; }
}
```

- View side: `MonoView` (`ViewBinding.Components/MonoView.cs`, initializes on Awake/Start per `MonoViewSettings`) holds
  `MonoBinder`s and a binding context. Binder classes are generated per UI component:
  `[MonoBinder(typeof(Image), ExcludeObsolete = true)] public partial class ImageBinder { }`, plus
  `[MonoBindingProperty(nameof(GameObject.SetActive))]`, `[MonoBindingCommand(...)]`, `[MonoBindingExclude]`
  (`MonoBinderAttribute.cs`, `MonoBindingPropertyAttribute.cs`; sample `SAMPLES/.../MonoBinders.cs`).
  Those sample binders are Editor-only: **game code must declare its own `[MonoBinder]` partial classes**.
- Adapters convert values: `[Adapter(sourceType, destType, order)]` on a class implementing `IAdapter.Convert(in Variant)`
  (`ViewBinding/Annotations/AdapterAttribute.cs`, `ViewBinding/IAdapter.cs`; sample `StartStopColorAdapter.cs`). Built-in
  adapters live in `ViewBinding.Adapters/`. They convert through `EncosyTower.Variants.Variant`; the audit recommended rejecting Variants (SKILL.md), so
  evaluate the cost of that dependency before adopting adapters.
- Editor tooling lives in `FW/EncosyTower.Editor.Mvvm/` (`ViewBinding.Components`, `ViewBinding.Contexts`).

## PageFlows and MonoPages

- Concepts: `PageFlowScope(Id3)` identifies a flow; flow messages are PubSub messages scoped by `PageFlowScope`
  (`FW/EncosyTower.PageFlows/PubSub/PageFlowPubSub.cs`: `AttachPageMessage`, `DetachPageMessage`, `BeginTransitionMessage`,
  `EndTransitionMessage`; `EncosyTower.PageFlows.MonoPages/PubSub/MonoPageFlowPubSub.cs`: `PrepoolPageMessage`, `TrimPoolMessage`,
  `AddPageMessage`, `ShowPageMessage(string AssetKey, PageContext)`, `ShowPageAtIndexMessage`, `HidePageAtIndexMessage`,
  `HideActivePageMessage(PageContext)`). Queries via Processing (`MonoPageFlowProcessing.cs`: `GetCurrentPageRequest`, ...).
- Flow kinds: `MonoPageFlowKind` { `SinglePageStack`, `MultiPageStack`, `SinglePageList`, `MultiPageList` }.
- A `MonoPageCodex` component (RectTransform; `_flows` array of `FlowDefinition` with identifier/kind/sorting) creates flows;
  your own `MonoBehaviour : IMonoPageCodexOnInitialize` supplies a `PageFlowScopeCollectionApplier<TScopes>` and
  `OnInitializeAsync(MonoPageCodex)`. Scopes are a struct implementing `IPageFlowScopeCollection` with public get/set `PageFlowScope`
  properties (the codex logs an error if none exist, `MonoPageCodex.cs:393-405`); the sample declares `Screen`, `Popup`, `FreeTop`
  and marks them `[Preserve]`. Verified (`MonoPageCodex.cs:143-175`): the set of `PageFlowScope` property names must equal the
  set of flow-definition identifiers exactly, otherwise initialization fails; lines 284-288 log an error for a property with no
  matching flow.
- Pages derive from `MonoPageBase<TScopes>` (`Subscriber`, `Publisher`, `FlowScope`, `FlowScopeCollection` as `Option<TScopes>`).
- Sample `SAMPLES/EncosyTower.Samples.MonoPages/` (`GamePageCodex.cs`, `GamePageFlowScopes.cs`, `ScreenRed.cs`, `PopupGray.cs`):

```csharp
public struct GamePageFlowScopes : IPageFlowScopeCollection { [Preserve] public PageFlowScope Screen { get; set; } /* Popup, FreeTop */ }
public class ScreenRed : MonoPageBase<GamePageFlowScopes> { /* on click: */
    var publisher = Publisher.Scope(scopes.Screen);
    _ = ShowPageMessage.Async.Publish(in publisher, new ShowPageMessage("prefab-screen-blue", default)); }  // asset key of a prefab
```

## Gotchas

- Page prefabs are loaded by asset key (`ShowPageMessage.AssetKey`), with a `MonoPagePool` internally (`Internals/MonoPagePool.cs`);
  `MonoPageLoaderStrategy` is `Addressables` or `Resources` (`Context/MonoPageLoaderStrategy.cs`): decide before adopting.
- Everything is async over PubSub/UniTask: view code must tolerate async show/hide and transitions
  (`MonoPageTransition*` components).
- `[ObservableProperty]` properties call generated `Get_X()`/`Set_X(value)` helpers and live in a `partial` class marked
  `[ObservableObject]` (sample `SampleMvvm.cs`).
- Do not use `EncosyTower.VisualToolkit` (references `Unity.Entities`, Latios).

## Where it applies in GrassSimulation

Menus, level select, results, shop and settings screens once UI work starts. Until then, keep game-logic classes UI-free so a
`[ObservableObject]` presenter layer can be added without changing them.
