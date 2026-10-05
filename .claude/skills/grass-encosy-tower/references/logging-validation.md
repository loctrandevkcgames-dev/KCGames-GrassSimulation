# Logging and validation (EncosyTower.Core: `EncosyTower.Logging`, `EncosyTower.Debugging`)

asmdef: `EncosyTower.Core`. No generator. Not a new decision: existing CODING-CONVENTIONS section 10 (errors/validation) and 12.2 (logging) already require this.
Sources: `FW/EncosyTower.Core/Logging/`, `FW/EncosyTower.Core/Debugging/` (`ThrowHelper.cs`, `Checks.cs`, `ValidationDefines.cs`).

## Logging

- `StaticLogger` (Editor, Development and Release): `LogException`, `LogInfo`, `LogWarning`, `LogError`, `Log*Format`,
  `Log*Slim`, overloads taking a `UnityEngine.Object context`, and `LogFixed*` for fixed strings
  (`Logging/StaticLogger.cs`).
- `StaticDevLogger`: same shape, every method carries `[Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]`,
  so calls and their arguments are stripped from Release builds (`Logging/StaticDevLogger.cs`).
- Instance loggers implement `ILogger`: `DevLogger.Default`, `Logger` (sample uses `Logging.Logger.Default`),
  `SlimLogger.Default`, `StringBuilderLogger` (capture text, `OnLogEntryWritten`), `UnityObjectLogger`.
  APIs that take a logger (PubSub `PublishingContext`, Persistence) default to `DevLogger.Default`.
- Never call `UnityEngine.Debug.Log*` directly (CODING-CONVENTIONS 12.2).

## ThrowHelper wrapper pattern (CODING-CONVENTIONS 10.1, 12.2)

Every authored `Throw*`/`Log*`/`Create*Exception` helper lives in the owning module's `internal static class ThrowHelper`,
with `[HideInCallstack, StackTraceHidden]`; cold log helpers also `[MethodImpl(NoInlining)]`. Existing game example:
`Assets/GrassSimulation/GrassSimulation.Progression/ThrowHelper.cs`.

```csharp
using System.Diagnostics;                         // StackTraceHidden, Conditional
using System.Runtime.CompilerServices;
using EncosyTower.Logging;
using UnityEngine;                                // HideInCallstack

namespace GrassSimulation.Progression
{
    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_InvalidSave(string path)
            => StaticLogger.LogWarning($"The progress save '{path}' is invalid and was skipped.");
    }
}
```

Dev-only variant: call `StaticDevLogger.LogInfo(message)` from the helper so it compiles away in release.

## Core guards (`EncosyTower.Debugging.ThrowHelper`, public static, `Debugging/ThrowHelper.cs`)

`ThrowIfNull(object)`, `ThrowIfNullOrEmpty(string)`, `ThrowIfUnityObjectInvalid`, `ThrowIfNullOrUnityObjectInvalid<T>`,
`ThrowIfNotCreated<T>`, `ThrowIfNotInitialized<T>`: these are not `[Conditional]`; they always throw (`[CallerArgumentExpression]` names
the parameter). Module-owned conditional guards use `[Conditional(UNITY_EDITOR), Conditional(DEBUG), Conditional(RUNTIME_CHECKS)]`
(see CODING-CONVENTIONS 10.4).

## ValidationDefines (`Debugging/ValidationDefines.cs`, `using static EncosyTower.Debugging.ValidationDefines;`)

Constants for `[Conditional(...)]`: `UNITY_EDITOR`, `DEBUG`, `RUNTIME_CHECKS` (= `ENCOSY_RUNTIME_CHECKS`), `COLLECTIONS_CHECKS`,
`UNITY_COLLECTIONS_CHECKS`, `PUBSUB_CHECKS`, `PROCESSING_CHECKS`, `STATS_CHECKS`, `PERSISTENCE_CHECKS`, `MVVM_CHECKS`.
`DISABLE_ENCOSY_CHECKS` disables all, `DISABLE_ENCOSY_RUNTIME_CHECKS` and `DISABLE_ENCOSY_EDITOR_CHECKS` disable subsets
(they map the constants to a reserved undefined symbol). Policy lives here; do not copy formulas (CODING-CONVENTIONS 10.3).
Framework-side checks are on in Editor/Development (for example PubSub: `UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS ||
ENCOSY_PUBSUB_RUNTIME_CHECKS`, `Publishers/MessagePublisher+Publisher.cs` header).

## Gotchas

- An omitted `[Conditional]` call also omits argument evaluation: never put required side effects inside log arguments.
- Conditional attributes are resolved per caller assembly: a game assembly decides by its own defines.
- Expected failures are `Option`/`Result`/`Success` (common-results.md), not exceptions or logs.

## Where it applies in GrassSimulation

All game modules: one `ThrowHelper` per assembly, `StaticLogger` for player-visible-in-release warnings (save problems),
`StaticDevLogger` for diagnostics. Required by the existing CODING-CONVENTIONS section 10 and 12.2, not a new decision.
