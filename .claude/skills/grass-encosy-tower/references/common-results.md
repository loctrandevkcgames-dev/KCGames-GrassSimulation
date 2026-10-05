# Option, Result, Success, Error and capability interfaces (EncosyTower.Core, namespace `EncosyTower.Common`)

asmdef: `EncosyTower.Core`. Sources: `FW/EncosyTower.Core/Common/Option.cs`, `Result.cs`, `Success.cs`, `Error.cs`.
No generator needed. Convention: CODING-CONVENTIONS 8.2 (use `Option<T>`/`TryXxx` for found-or-not, `Result`/`Success`
when callers must act on why; no `Try` prefix for the latter).

## Option<T> (`Option.cs`)

- Construct: `Option.Some(value)`, `Option.SomeIf(cond, value)`, `Option.None`, `Option<T>.None`, implicit from `T`
  and from the non-generic `Option` (`Option.None`). A null reference, or a destroyed `UnityEngine.Object`, yields no value (`Option.cs:185-188`).
- Consume: `HasValue`, `TryGetValue(out T)`, `GetValueOrDefault(T default = default)`, `GetValueOrThrow()`,
  `Deconstruct(out bool hasValue, out T value)`. Extensions: `AsNullable`, `GetValueOrNull`.

## Result<TValue, TError> (`Result.cs`)

```csharp
public Result<LevelSettlement, SettleError> Settle(LevelId level, in LevelResult result)
{
    if (isInvalid) return SettleError.Unknown(level);          // implicit from TError (a [PolyEnumStruct] case via its wrapper)
    return new LevelSettlement(...);                            // implicit from TValue
}

var r = Settle(id, in result);
if (r.TryGetValue(out var settlement)) { /* success */ }
else if (r.TryGetError(out var error)) { StaticLogger.LogWarning(error); }
```

- Construct: `new Result<,>(value)`, `new Result<,>(error)`, `Result<,>.Succeed(value)`, `.Err(error)`, implicit from
  `TValue` and `TError`.
- Consume: `IsValid` (false for `default`), `IsSuccess`, `IsError`, `Value` (`Option<TValue>`), `Error` (`Error<TError>`),
  `TryGetValue`, `TryGetError`, `GetValueOrDefault`, `GetValueOrThrow`, `GetErrorOrDefault`, `GetErrorOrThrow`,
  `Deconstruct(out Option<TValue>, out Option<TError>)`.
- `TValue` and `TError` must differ (`ThrowHelper.ThrowIfResultValueAndErrorHaveSameType`, `Common/ThrowHelper.cs:64`,
  Editor/debug/`ENCOSY_RUNTIME_CHECKS` builds). `default(Result)` is neither success nor error.
- `Result<TValue>` (single parameter) carries `Error<StringOrException>` (string or exception) via
  `Succeed(value)` / `Err(StringOrException)`; use it only for untyped failures (for example `JsonHelper.Serialize`).
- Sample accessor using the typed form: `SAMPLES/EncosyTower.Samples.Persistence/Persistences/PlayerDataAccessor.cs`
  (`Result<OnItemAmountUpdatedMsg, Error>`; success value is the message to publish).

## Success<TFailure> (`Success.cs`)

For operations with no value, only a typed failure.

```csharp
public Success<SaveError> Save(ProgressSave progress)
{
    if (failed) return SaveError.IoFailed(path);                // implicit from TFailure
    return Success.Yes;                                          // implicit from non-generic Success
}
// Success.No(failure), Success.NoIf(cond, failure), Success<T>.Yes
var outcome = Save(progress);
if (outcome.TryGetFailure(out var failure)) { ... }              // IsFailure is a public ByteBool field (Success.cs:75); also IsSuccess, GetFailureOrDefault, GetFailureOrThrow
```

## Error<T> (`Error.cs`)

Option-like wrapper for an error value: `Error.Err(value)`, `Error.ErrIf(cond, value)`, `Error<T>.None`, `HasValue`,
`TryGetValue`, implicit from `T`, implicit to `Option<T>`. Mostly seen as `Result.Error`.

## Capability interfaces (reuse, do not invent members)

| Interface | Member | Namespace and file |
|---|---|---|
| `IIsValid` | `bool IsValid { get; }` | `EncosyTower.Common`, `Common/IIsValid.cs` |
| `IIsCreated` | `bool IsCreated { get; }` | `EncosyTower.Common`, `Common/IIsCreated.cs` |
| `IHasValue` | `bool HasValue { get; }` | `EncosyTower.Common`, `Common/IHasValue.cs` |
| `IHasCount` | `int Count { get; }` | `EncosyTower.Collections`, `Collections.Contracts/IHasLength.cs` (also `IHasLength`) |
| `IHasCapacity` | `int Capacity { get; }` | `EncosyTower.Collections`, `Collections.Contracts/IHasCapacity.cs` |
| `IClearable` | `void Clear()` | `EncosyTower.Collections`, `Collections.Contracts/IClearable.cs` |
| `IInitializable` | `void Initialize()` | `EncosyTower.Initialization`, `Initialization/IInitializable.cs` |
| `IDeinitializable` | `void Deinitialize()` | same namespace, `Initialization/IDeinitializable.cs` |
| `IIsInitialized` | `bool IsInitialized { get; }` | same namespace, `Initialization/IIsInitialized.cs` |

More contracts (`IToArray<T>`, `IAsSpan<T>`, `IIndexer<T>`, ...) are in `FW/EncosyTower.Core/Collections.Contracts/`.

## Where it applies in GrassSimulation

Failure reasons for settle/save/session operations (typed `[PolyEnumStruct]` errors, see codegen-types.md); found-or-not
lookups (level catalog) as `Option<T>`; game types implement `IHasCount`/`IClearable`/`IIsValid`/`IInitializable`
instead of ad-hoc members. Decided: Adopt now.
