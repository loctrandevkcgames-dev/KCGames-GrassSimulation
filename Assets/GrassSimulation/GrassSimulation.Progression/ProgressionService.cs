using System;
using EncosyTower.Common;
using EncosyTower.Initialization;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public sealed class ProgressionService : IInitializable
    {
        private readonly IProgressStore _store;

        private ProgressSave _save = ProgressSave.CreateNew();
        private LoadError _loadFailure;
        private bool _isReadOnly;

        public ProgressionService(IProgressStore store)
        {
            _store = store;
        }

        public bool IsReadOnly => _isReadOnly;

        public LoadError LoadFailure => _loadFailure;

        public void Initialize()
        {
            Reload();
        }

        public bool IsCompleted(LevelId level)
            => _save.Levels.TryGetValue(level.Value, out var record) && record.Completed;

        public StarFlags GetStars(LevelId level)
            => _save.Levels.TryGetValue(level.Value, out var record) ? record.Stars : StarFlags.None;

        public int GetStarCount(LevelId level)
            => StarRules.Count(GetStars(level));

        public MachineId SelectedMachine => new(_save.SelectedMachine);

        public int OwnedMachineCount => _save.OwnedMachines.Count;

        public bool IsMachineOwned(MachineId machine)
            => _save.OwnedMachines.Contains(machine.Value);

        public bool IsUnlockGranted(LevelId level, UnlockKind kind)
            => _save.GrantedUnlocks.Contains(CreateUnlockId(level, kind));

        public int GetAttempts(LevelId level)
            => _save.Levels.TryGetValue(level.Value, out var record) ? record.Attempts : 0;

        public int FindFirstIncomplete(LevelCatalog catalog)
        {
            var count = catalog.Count;

            for (var i = 0; i < count; i++)
            {
                if (IsCompleted(catalog.Get(i).Id) == false)
                {
                    return i;
                }
            }

            return catalog.ClampIndex(count - 1);
        }

        public Result<LevelSettlement, SettleError> Settle(LevelId level, in LevelResult result)
            => Settle(level, in result, unlock: default);

        public Result<LevelSettlement, SettleError> Settle(LevelId level, in LevelResult result, UnlockSettings unlock)
        {
            if (_isReadOnly)
            {
                ThrowHelper.LogWarning_SettleStoreUnavailable(level.Value);
                return Result<LevelSettlement, SettleError>.Err(new SettleError.StoreUnavailable(_loadFailure));
            }

            var isWin = result.Outcome.IsSuccess;
            var earned = isWin ? result.Stars : StarFlags.None;
            var isFirstCompletion = isWin && IsCompleted(level) == false;
            var newStars = earned & ~GetStars(level);
            var snapshot = _save.Clone();

            if (_save.Levels.TryGetValue(level.Value, out var record) == false)
            {
                record = new LevelRecord();
                _save.Levels[level.Value] = record;
            }

            _save.Revision++;
            record.Attempts++;
            record.Stars |= earned;
            record.Completed |= isWin;

            var unlocks = isWin ? GrantUnlock(level, unlock) : Array.Empty<UnlockSettings>();
            var settlement = new LevelSettlement(earned, newStars, isFirstCompletion, unlocks);

            var saved = SaveSafely(_save);

            if (saved.TryGetFailure(out var failure))
            {
                _save = snapshot;
                ThrowHelper.LogError_SettleNotSaved(level.Value);
                return Result<LevelSettlement, SettleError>.Err(new SettleError.NotSaved(failure));
            }

            return Result<LevelSettlement, SettleError>.Succeed(settlement);
        }

        public Success<SelectMachineError> SelectMachine(MachineId machine)
        {
            if (_isReadOnly)
            {
                return Success.No<SelectMachineError>(new SelectMachineError.StoreUnavailable(_loadFailure));
            }

            if (IsMachineOwned(machine) == false)
            {
                return Success.No<SelectMachineError>(new SelectMachineError.NotOwned(machine));
            }

            if (_save.SelectedMachine == machine.Value)
            {
                return Success.Yes;
            }

            var snapshot = _save.Clone();

            _save.Revision++;
            _save.SelectedMachine = machine.Value;

            var saved = SaveSafely(_save);

            if (saved.TryGetFailure(out var failure))
            {
                _save = snapshot;
                return Success.No<SelectMachineError>(new SelectMachineError.NotSaved(failure));
            }

            return Success.Yes;
        }

        public Success<SaveError> Wipe()
        {
            Success<SaveError> deleted;

            try
            {
                deleted = _store.Delete();
            }
            catch (Exception exception)
            {
                ThrowHelper.LogError_StoreThrew(nameof(IProgressStore.Delete), exception);
                deleted = Success.No<SaveError>(new SaveError.WriteFailed(string.Empty, exception.Message));
            }

            if (deleted.IsSuccess)
            {
                _save = ProgressSave.CreateNew();
                _loadFailure = default;
                _isReadOnly = false;
            }

            return deleted;
        }

        public void Reload()
        {
            Result<ProgressSave, LoadError> loaded;

            try
            {
                loaded = _store.Load();
            }
            catch (Exception exception)
            {
                ThrowHelper.LogError_StoreThrew(nameof(IProgressStore.Load), exception);
                loaded = Result<ProgressSave, LoadError>.Err(new LoadError.Unreadable(string.Empty, exception.Message));
            }

            _isReadOnly = false;
            _loadFailure = default;

            if (loaded.TryGetValue(out var save))
            {
                _save = save;
                return;
            }

            _save = ProgressSave.CreateNew();

            if (loaded.TryGetError(out var error))
            {
                _isReadOnly = error.BlocksWrites;
                _loadFailure = error;
            }
        }

        private static string CreateUnlockId(LevelId level, UnlockKind kind)
            => $"{level.Value}:{kind}";

        private UnlockSettings[] GrantUnlock(LevelId level, UnlockSettings unlock)
        {
            if (unlock.Kind == UnlockKind.None || _save.GrantedUnlocks.Add(CreateUnlockId(level, unlock.Kind)) == false)
            {
                return Array.Empty<UnlockSettings>();
            }

            if (MachineIds.TryFromUnlock(unlock.Kind, out var machine))
            {
                _save.OwnedMachines.Add(machine.Value);
            }

            return new[] { unlock };
        }

        private Success<SaveError> SaveSafely(ProgressSave save)
        {
            try
            {
                return _store.Save(save);
            }
            catch (Exception exception)
            {
                ThrowHelper.LogError_StoreThrew(nameof(IProgressStore.Save), exception);
                return Success.No<SaveError>(new SaveError.WriteFailed(string.Empty, exception.Message));
            }
        }
    }
}
