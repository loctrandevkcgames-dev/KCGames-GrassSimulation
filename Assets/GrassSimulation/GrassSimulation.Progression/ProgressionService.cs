using System;
using System.Collections.Generic;
using EncosyTower.Common;
using EncosyTower.Initialization;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public sealed class ProgressionService : IInitializable
    {
        private readonly IProgressStore _store;
        private readonly List<RewardGrant> _grants = new();

        private ProgressSave _save = ProgressSave.CreateNew();
        private LoadError _loadFailure;
        private bool _isReadOnly;

        public ProgressionService(IProgressStore store)
        {
            _store = store;
        }

        public int Coins => _save.Coins;

        public bool IsReadOnly => _isReadOnly;

        public LoadError LoadFailure => _loadFailure;

        public void Initialize()
        {
            Reload();
        }

        public bool IsCompleted(LevelId level)
            => _save.CompletedLevels.Contains(level.Value);

        public int GetBestStars(LevelId level)
            => _save.BestStars.TryGetValue(level.Value, out var stars) ? stars : 0;

        public bool IsGranted(RewardId id)
            => _save.GrantedRewards.Contains(id.ToKey());

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
        {
            if (_isReadOnly)
            {
                ThrowHelper.LogWarning_SettleStoreUnavailable(level.Value);
                return Result<LevelSettlement, SettleError>.Err(new SettleError.StoreUnavailable(_loadFailure));
            }

            var isWin = result.Outcome.IsSuccess;
            var earnedStars = 0;

            if (isWin && result.TryGetStars(out var stars))
            {
                earnedStars = Math.Min(Math.Max(stars, 0), RewardRules.MAX_STARS);
            }

            _grants.Clear();
            RewardCalculator.Collect(level, isWin, earnedStars, _save.GrantedRewards, _grants);

            var grantCount = _grants.Count;
            var coins = 0;

            for (var i = 0; i < grantCount; i++)
            {
                coins += _grants[i].Coins;
            }

            var isNewBest = isWin && earnedStars > GetBestStars(level);
            var isFirstCompletion = isWin && IsCompleted(level) == false;
            var settlement = new LevelSettlement(coins, earnedStars, isNewBest, isFirstCompletion);

            if (grantCount == 0 && isNewBest == false && isFirstCompletion == false)
            {
                return Result<LevelSettlement, SettleError>.Succeed(settlement);
            }

            var snapshot = _save.Clone();

            for (var i = 0; i < grantCount; i++)
            {
                _save.GrantedRewards.Add(_grants[i].Id.ToKey());
            }

            _save.Revision++;
            _save.Coins += coins;

            if (isNewBest)
            {
                _save.BestStars[level.Value] = earnedStars;
            }

            if (isFirstCompletion)
            {
                _save.CompletedLevels.Add(level.Value);
            }

            var saved = SaveSafely(_save);

            if (saved.TryGetFailure(out var failure))
            {
                _save = snapshot;
                ThrowHelper.LogError_SettleNotSaved(level.Value);
                return Result<LevelSettlement, SettleError>.Err(new SettleError.NotSaved(failure));
            }

            return Result<LevelSettlement, SettleError>.Succeed(settlement);
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
