# Processing (EncosyTower.Processing)

Request/response dispatch: register a handler for a typed request, call `TryProcess`, get an `Option<TResult>`. Sibling
of PubSub (one handler, returns a value) and shares its `ApiMode`/`StateMode`. Verdict: Later (results/shop UI queries).

## Reference

- asmdef: `EncosyTower.Processing` (references `EncosyTower.Core`, `UniTask`;
  `FW/EncosyTower.Processing/EncosyTower.Processing.asmdef`). Generator:
  `FW/EncosyTower.Processing/SourceGenerators/EncosyTower.Processing.Generators.dll`
  (source `GEN/EncosyTower.Processing.Generators/`).
- Sample: `SAMPLES/EncosyTower.Samples.Processing/ProcessingManager.cs` (asmdef is Editor-only, like all samples).

## API

```csharp
// FW/EncosyTower.Processing/Annotations/ProcessingAttribute.cs
[Processing(ApiMode mode)] { StateMode State = StateMode.Both; Type Scope; }   // class or struct, AllowMultiple
// FW/EncosyTower.Processing/Contracts/IRequest.cs
IRequest, IRequest<TResult>, IAsyncRequest, IAsyncRequest<TResult>
```

```csharp
[Processing(ApiMode.Both, State = StateMode.Stateful, Scope = typeof(Owner))]
internal readonly partial record struct FormatTextRequest(string Text) : IRequest<string>;

// register (sample Register); keep registries to unregister later
var hub = GlobalProcessor.Instance.UnityScope(this);                 // Processor.UnityHub; GlobalProcessor.Instance is a static Processor
var registrationHub = hub.WithState(this).WithRegistries(_registries); // _registries: List<ProcessRegistry>
FormatTextRequest.Register(in registrationHub, ProcessText);          // static string ProcessText(Owner state, FormatTextRequest r, ProcessingContext c)
FormatTextRequest.Async.Register(in registrationHub, ProcessTextAsync);

// call
var context = ProcessingContext.DropIfNoHandler(logger: logger, token: ct);   // or ProcessingContext.WaitForHandler(...)
Option<string> result = FormatTextRequest.TryProcess(in hub, new(text), context);
if (result.TryGetValue(out var formatted)) { ... }
var asyncResult = await FormatTextRequest.Async.TryProcess(in hub, request, context);
_registries.Unregister();                                             // RegistryCollectionExtensions
```

`Processor.Hub<..>.TryProcess<TRequest, TResult>(request, context)` returns `Option<TResult>`
(`FW/EncosyTower.Processing/Processors/` file `` Processor+Hub`1.cs ``, line 317).

## Gotchas

- `GlobalProcessor.Instance` is a static singleton reset on enter play mode (`Processors/GlobalProcessor.cs`).
- `ProcessingStrategy` is `DropIfNoHandler` or `WaitForHandler` (`Common/ProcessingStrategy.cs`); the sample toggles
  between them. Always handle an empty `Option` result.
- Messages are `partial` record structs; the async variant is the nested `Request.Async` struct (same pattern as PubSub).
- Per-assembly opt-out: `FW/EncosyTower.Processing/Annotations/SkipSourceGeneratorsForAssemblyAttribute.cs`.

## Where it applies in GrassSimulation

Queries where the caller needs an answer rather than a broadcast: results screen asking for the current settlement,
shop UI asking for owned skins. Not needed for the core loop; use PubSub for events. Trigger: first UI that needs
request/response across assemblies.
