using EncosyTower.PubSub;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class LevelSession
    {
        public const float SECOND_STAR_TIME_FRACTION = 0.2f;
        public const float TIMER_WARNING_SECONDS = 15f;

        private const float HARVEST_BATCH_INTERVAL = 0.1f;

        private readonly LevelDefinition _level;
        private readonly int _cuttableCells;
        private readonly MessagePublisher.Publisher<GameplayScope> _publisher;
        private readonly HarvestBatcher _harvestBatcher = new(HARVEST_BATCH_INTERVAL);

        private int _clearedCells;

        public LevelSession(
              LevelDefinition level
            , MachineConfig machine
            , int cuttableCells
            , MessagePublisher.Publisher<GameplayScope> publisher
        )
        {
            ThrowHelper.ThrowIfTooManyQuotas(
                  level.Quotas.Length <= LevelSnapshot.MAX_QUOTAS
                , level.Id.Value
                , LevelSnapshot.MAX_QUOTAS
            );

            _level = level;
            _cuttableCells = cuttableCells;
            _publisher = publisher;
            Growth = new MowerGrowth(machine);
            Objectives = new LevelObjectives(level.Quotas);
            Protection = new ProtectedRule(level.ProtectedHitCooldown);
            ResetState();
        }

        public MowerGrowth Growth { get; }

        public LevelObjectives Objectives { get; }

        public ProtectedRule Protection { get; }

        public LevelState State { get; private set; }

        public bool IsPaused { get; private set; }

        public float RemainingTime { get; private set; }

        public float ElapsedTime { get; private set; }

        public LevelResult Result { get; private set; }

        public bool IsSimulating => IsPaused == false && (State == LevelState.Playing || State == LevelState.Cleanup);

        public bool CountsProtectedHits => _level.FailOnProtectedHits;

        public int ProtectedHitLimit => _level.ProtectedHitLimit;

        public float ClearedFraction => _cuttableCells > 0 ? (float)_clearedCells / _cuttableCells : 1f;

        public void Reset()
        {
            ResetState();
            PublishState();
        }

        public bool TryBegin()
        {
            if (State != LevelState.Preview)
            {
                return false;
            }

            State = LevelState.Playing;
            LevelStartedMsg.Publish(in _publisher, new LevelStartedMsg(_level.Id));
            PublishState();
            PublishAlreadyMetQuotas();
            return true;
        }

        public void Pause()
        {
            var canPause = State == LevelState.Playing || State == LevelState.Cleanup;

            if (IsPaused == canPause)
            {
                return;
            }

            IsPaused = canPause;
            PublishState();
        }

        public void Resume()
        {
            if (IsPaused == false)
            {
                return;
            }

            IsPaused = false;
            PublishState();
        }

        public void RecordHarvest(PlantKind kind, int xp)
        {
            if (IsSimulating == false)
            {
                return;
            }

            _clearedCells++;

            if (State != LevelState.Playing)
            {
                _harvestBatcher.Add(cells: 1, xp: 0, ElapsedTime);
                return;
            }

            Objectives.Record(kind);
            _harvestBatcher.Add(cells: 1, xp, ElapsedTime);

            var tiersGained = Growth.AddXp(xp);

            PublishCompletedQuotas(kind);
            PublishTierUps(tiersGained);
        }

        public void RecordXp(int xp)
        {
            if (State == LevelState.Playing && IsPaused == false)
            {
                _harvestBatcher.Add(cells: 0, xp, ElapsedTime);
                PublishTierUps(Growth.AddXp(xp));
            }
        }

        public bool RecordProtectedTouch()
        {
            if (State != LevelState.Playing || IsPaused)
            {
                return false;
            }

            var isNewHit = Protection.Touch(ElapsedTime);

            if (isNewHit)
            {
                var message = new ProtectedHitMsg(
                      Protection.Hits
                    , _level.ProtectedHitLimit
                    , _level.FailOnProtectedHits
                );

                ProtectedHitMsg.Publish(in _publisher, message);
            }

            return isNewHit;
        }

        public void EndTick(float deltaTime)
        {
            if (IsSimulating == false)
            {
                return;
            }

            ElapsedTime += deltaTime;
            FlushHarvestBatch(force: false);

            if (State != LevelState.Playing)
            {
                return;
            }

            RemainingTime = Mathf.Max(RemainingTime - deltaTime, 0f);

            var hasTooManyHits = _level.FailOnProtectedHits && Protection.Hits > _level.ProtectedHitLimit;

            if (hasTooManyHits)
            {
                Finish(new LevelOutcome.TooManyProtectedHits(Protection.Hits, _level.ProtectedHitLimit));
                return;
            }

            if (Objectives.IsComplete)
            {
                Finish(new LevelOutcome.Success(CountStars()));
                return;
            }

            if (RemainingTime <= 0f)
            {
                Finish(new LevelOutcome.TimeUp(Objectives.RemainingMainQuota));
                return;
            }

            if (Growth.PendingUpgrades > 0)
            {
                State = LevelState.UpgradeChoice;
                PublishState();
            }
        }

        public bool TryChooseUpgrade(int option)
        {
            if (State != LevelState.UpgradeChoice || Growth.TryChooseUpgrade(option) == false)
            {
                return false;
            }

            var resolvedTier = Growth.Tier - Growth.PendingUpgrades;
            var message = new UpgradeChosenMsg(option, Growth.GetUpgrade(option).Id, resolvedTier);

            if (Growth.PendingUpgrades == 0)
            {
                State = LevelState.Playing;
            }

            if (State == LevelState.Playing)
            {
                PublishState();
            }

            UpgradeChosenMsg.Publish(in _publisher, message);

            return true;
        }

        public bool TryEnterCleanup()
        {
            if (State != LevelState.Success)
            {
                return false;
            }

            State = LevelState.Cleanup;
            PublishState();
            return true;
        }

        private void Finish(LevelOutcome outcome)
        {
            var isSuccess = outcome.IsSuccess;

            State = isSuccess ? LevelState.Success : LevelState.Failure;
            Result = new LevelResult(outcome, RemainingTime, Protection.Hits);

            var finished = new LevelFinishedMsg(_level.Id, Result, ElapsedTime);

            FlushHarvestBatch(force: true);
            LevelFinishedMsg.Publish(in _publisher, finished);
            PublishState();
        }

        private void PublishState()
        {
            LevelStateChangedMsg.Publish(in _publisher, new LevelStateChangedMsg(State, IsPaused));
        }

        private void PublishTierUps(int tiersGained)
        {
            var firstTier = Growth.Tier - tiersGained + 1;

            for (var i = 0; i < tiersGained; i++)
            {
                TierUpMsg.Publish(in _publisher, new TierUpMsg(firstTier + i));
            }
        }

        private void PublishCompletedQuotas(PlantKind kind)
        {
            var count = Objectives.QuotaCount;
            var harvested = Objectives.GetHarvested(kind);

            for (var i = 0; i < count; i++)
            {
                ref readonly var quota = ref Objectives.GetQuota(i);

                if (quota.Kind == kind && harvested == quota.Amount)
                {
                    QuotaCompletedMsg.Publish(in _publisher, new QuotaCompletedMsg(i, kind, quota.IsBonus));
                }
            }
        }

        private void PublishAlreadyMetQuotas()
        {
            var count = Objectives.QuotaCount;

            for (var i = 0; i < count; i++)
            {
                ref readonly var quota = ref Objectives.GetQuota(i);

                if (quota.Amount <= 0)
                {
                    QuotaCompletedMsg.Publish(in _publisher, new QuotaCompletedMsg(i, quota.Kind, quota.IsBonus));
                }
            }
        }

        private void FlushHarvestBatch(bool force)
        {
            if (_harvestBatcher.TryFlush(ElapsedTime, force, out var message))
            {
                HarvestBatchedMsg.Publish(in _publisher, message);
            }
        }

        private int CountStars()
        {
            var hasTimeStar = RemainingTime >= _level.TimeLimit * SECOND_STAR_TIME_FRACTION;
            var hasCleanStar = Protection.Hits == 0 && Objectives.AreBonusQuotasMet;
            var stars = 1;

            if (hasTimeStar)
            {
                stars++;
            }

            if (hasCleanStar)
            {
                stars++;
            }

            return stars;
        }

        private void ResetState()
        {
            Growth.Reset();
            Objectives.Reset();
            Protection.Reset();
            State = LevelState.Preview;
            IsPaused = false;
            RemainingTime = _level.TimeLimit;
            ElapsedTime = 0f;
            Result = default;
            _clearedCells = 0;
            _harvestBatcher.Clear();
        }
    }
}
