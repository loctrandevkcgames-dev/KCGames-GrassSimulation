# PubSub (EncosyTower.PubSub)

Typed publish/subscribe messaging with generated, allocation-light message APIs.

## Reference

- UniTask reference: sync-only generated code never references `UnityTask`/`UniTask` (the Async section of the Scope
  snapshot starts at ~line 361), and the `Messenger` ctor's `ArrayPool<UnityTask>` type lives in Core, so a `UniTask` asmdef
  reference is likely unnecessary for sync-only use. Unverified: confirm with one compile.
- asmdef: `EncosyTower.PubSub` (references `EncosyTower.Core`, `UniTask`; `FW/EncosyTower.PubSub/EncosyTower.PubSub.asmdef`).
  The sample asmdef references `EncosyTower.Core`, `EncosyTower.PubSub`, `UniTask` (`SAMPLES/EncosyTower.Samples.PubSub/EncosyTower.Samples.PubSub.asmdef`).
  Game assemblies need `EncosyTower.Core` and `EncosyTower.PubSub`; add `UniTask` if async APIs are used.
- Generator: `FW/EncosyTower.PubSub/SourceGenerators/EncosyTower.PubSub.Generators.dll`; source in
  `GEN/EncosyTower.PubSub.Generators/Generators/`; snapshots of generated output in
  `GEN/EncosyTower.SourceGen.Tests/PubSub/Snapshots/`.
- Opt out of generators per assembly: `[assembly: EncosyTower.PubSub.SkipSourceGeneratorsForAssembly]`
  (`FW/EncosyTower.PubSub/Annotations/SkipSourceGeneratorsForAssemblyAttribute.cs`). Do not do this in game code.
- Sample: `SAMPLES/EncosyTower.Samples.PubSub/PubSubManager.cs`. Larger API tour:
  `GEN/Samples/Samples.PubSub/PubSubSamples.cs`.

## Declaring a message

```csharp
// FW/EncosyTower.PubSub/Annotations/PubSubAttribute.cs
[PubSub(ApiMode mode)] { StateMode State = StateMode.Both; Type Scope; }   // AllowMultiple = true, class or struct
// ApiMode: Sync = 0, Async = 1, Both = 2.  StateMode: Stateless = 0, Stateful = 1, Both = 2. (FW/EncosyTower.Core/CodeGen/)
```

```csharp
using EncosyTower.CodeGen;     // ApiMode, StateMode
using EncosyTower.PubSub;

[PubSub(ApiMode.Sync, State = StateMode.Stateful)]
public readonly partial record struct LevelCompletedMsg(int Stars);
```

Messages are `partial` and implement `IMessage` through the generator (strict mode; `ENCOSY_PUBSUB_RELAX_MODE`
removes the requirement, `FW/EncosyTower.PubSub/Contracts/IMessage.cs`). Without `Scope =`, the message uses
`GlobalScope` and the generator emits static shortcuts that use `GlobalMessenger`
(`GEN/EncosyTower.SourceGen.Tests/PubSub/Snapshots/PubSubMessageGeneratorTests.Scope.verified.cs`).

## Generated members (static, on the message type)

- Sync: `Publish(in Publisher<TScope> p, TMsg msg, PublishingContext ctx = default)`, `Publish(msg, ctx)` (global),
  `Cache(...)`, and many `Subscribe` overloads with handlers `Action`, `Action<TMsg>`, `Action<PublishingContext>`,
  `Action<TMsg, PublishingContext>` plus optional `int order = 0`, `ILogger logger = null`. Overloads WITHOUT a token return
  `ISubscription`; the separate overloads that add a required `CancellationToken unsubscribeToken` return `void`
  (snapshot `GEN/EncosyTower.SourceGen.Tests/PubSub/Snapshots/PubSubMessageGeneratorTests.Scope.verified.cs:40-46`).
  Stateful handlers (`Action<TState>`, `Action<TState, TMsg>`, `Action<TState, TMsg, PublishingContext>`) come either as
  `Subscribe<TState>(in Subscriber<TScope,TState> s, handler, ...)` (snapshot line 88) or as
  `Subscribe<TState>(in Subscriber<TScope> s, TState state, handler, ...)` (line 144); the global shortcut is
  `Msg.Subscribe(state, handler)`.
- Async (`ApiMode.Async` or `Both`): nested `Message.Async` struct (implicit conversion to and from the message),
  `Message.Async.Publish(...)` returning `UnityTask`, `Message.Async.Subscribe(...)` with `Func<..., UnityTask>` handlers.
- Subscribe returns `ISubscription`; `ISubscription.Unsubscribe()` (= `Dispose`,
  `FW/EncosyTower.PubSub/Contracts/ISubscription.cs`). `ICollection<ISubscription>.Unsubscribe()` clears a list
  (`Common/SubscriptionCollectionExtensions.cs`).

## Wiring (modeled on the sample)

The snippet compiles only if the message declares `Scope = typeof(Owner)` (the sample's `LogMsg` uses
`Scope = typeof(PubSubManager)`). A `GlobalScope` message (no `Scope =`) is used with `Subscriber.Global()` /
`Publisher.Global()` or the static shortcuts `Msg.Subscribe(state, handler)` / `Msg.Publish(msg)`; the shortcuts always
target `GlobalMessenger`.

```csharp
// Unity-object scope + state, subscriptions collected for later cleanup (PubSubManager.cs Subscribe/Publish)
var subscriber = GlobalMessenger.Subscriber.UnityScope(this).WithState(this).WithSubscriptions(_subscriptions);
LogMsg.Subscribe(in subscriber, Handle);                       // static void Handle(Owner state, LogMsg msg, PublishingContext ctx)
var publisher = GlobalMessenger.Publisher.UnityScope(this);
LogMsg.Publish(in publisher, new LogMsg("x"), PublishingContext.DropIfNoSubscriber(logger: logger, token: ct));
_subscriptions.Unsubscribe();                                   // using EncosyTower.PubSub
```

- Scopes: `Publisher.Global()`, `Scope<TScope>()` (`TScope : struct`), `Scope(TScope scope)`, `UnityScope(obj)`
  (`TScope : UnityEngine.Object`) on `MessagePublisher`/`MessageSubscriber` (`Publishers/MessagePublisher.cs`,
  `Subscribers/MessageSubscriber.cs`). Subscriber adds `WithState(state)` (`TState : class`) and `WithSubscriptions(ICollection<ISubscription>)`
  (`Subscribers/MessageSubscriber+Subscriber` generic-2 file).
- `PublishingContext` (`Common/PublishingContext.cs`): `Default(warnNoSubscriber, logger, callerInfo, token)` (a method, `PublishingContext.cs:26`), `DropIfNoSubscriber(warnNoSubscriber, logger, callerInfo, token)`,
  `WaitForSubscriber(logger, callerInfo, token)`; `PublishingStrategy` is `DropIfNoSubscriber` or `WaitForSubscriber`.
- Interceptors: `GlobalMessenger.Interceptors.AddInterceptor(x)` / `RemoveInterceptor(x)` with
  `IMessageInterceptor<TMsg>.InterceptAsync(TMsg msg, PublishingContext context, PublishContinuation<TMsg> continuation)`
  returning `UnityTask` (sample `PubSubManager.cs`, `LogInterceptor`).
- Custom instance (tests, isolated buses): `var messenger = new Messenger(ArrayPool<UnityTask>.Shared);` then
  `messenger.Publisher.Global()`, `messenger.Subscriber.Global().WithState(owner)`, and `messenger.Dispose()`. Store the
  publisher/subscriber in a local before passing it with `in`:
  `var pub = messenger.Publisher.Global(); Msg.Publish(in pub, msg);`
  (`Messengers/Messenger.cs`).

## Gotchas

- Sync `Publish` calls `PublishAsync(message, context).Forget()` (`Publishers/MessagePublisher+Publisher.cs:151-156`);
  brokers lock and rent pooled lists/arrays. Publish discrete or batched events, never per cell or per frame.
- `GlobalMessenger` instance is static; the reset on enter play mode is Editor-only (`#if UNITY_EDITOR`,
  `[InitializeOnEnterPlayMode]`, `Messengers/GlobalMessenger.cs`). Tests must not share it; use a `Messenger` instance.
- State handlers are `static` methods taking the state first; keep subscriptions in a list and unsubscribe on
  destroy to avoid dangling handlers.
- A scope that is a Unity object must be alive: `UnityScope`/`Scope` call `DebuggingThrowHelper.ThrowIfUnityObjectInvalid` /
  `ThrowIfNullOrUnityObjectInvalid` (`Publishers/MessagePublisher.cs`).
- Diagnostics: `SG_PUBSUB_0001` invalid scope, `0002` repeated scope, `0003` unsupported declaration,
  `0004`/`0005` invalid mode values (`GEN/EncosyTower.PubSub.Generators/Analyzers/PubSubMessageAnalyzer+Diagnostics.cs`).

## Where it applies in GrassSimulation

Gameplay events (level started, cell cleared, level completed, prop dropped, settlement saved). Decided: Adopt now.
Use a stateless/global message per event, a composition-root-owned `Messenger` (or `GlobalMessenger`) and tests with
their own `Messenger`. After a successful accessor mutation, publish the resulting message (see persistence.md
sample pattern).
