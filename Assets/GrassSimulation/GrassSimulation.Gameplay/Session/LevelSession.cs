using EncosyTower.PubSub;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class LevelSession
    {
        private const float HARVEST_BATCH_INTERVAL = 0.1f;

        private readonly LevelDefinition _level;
        private readonly LevelRules _rules;
        private readonly int _cuttableCells;
        private readonly MessagePublisher.Publisher<GameplayScope> _publisher;
        private readonly HarvestBatcher _harvestBatcher = new(HARVEST_BATCH_INTERVAL);

        private int _clearedCells;
        private int _lockedKinds;
        private int _slowKinds;

        public LevelSession(
              LevelDefinition level
            , MachineConfig machine
            , int cuttableCells
            , MessagePublisher.Publisher<GameplayScope> publisher
        )
            : this(level, machine, cuttableCells, publisher, GameRulesValues.Default)
        {
        }

        public LevelSession(
              LevelDefinition level
            , MachineConfig machine
            , int cuttableCells
            , MessagePublisher.Publisher<GameplayScope> publisher
            , GameRulesValues rules
        )
        {
            ThrowHelper.ThrowIfTooManyQuotas(
                  level.Quotas.Length <= LevelSnapshot.MAX_QUOTAS
                , level.Id.Value
                , LevelSnapshot.MAX_QUOTAS
            );

            _level = level;
            _rules = LevelRules.Resolve(level, in rules);
            _cuttableCells = cuttableCells;
            _publisher = publisher;
            Machine = machine;
            Growth = new MowerGrowth(machine);
            Objectives = new LevelObjectives(level.Quotas);
            Protection = new ProtectedRule(_rules.Retrigger, level.Beds.Length);
            ResetState();
        }

        public MachineConfig Machine { get; private set; }

        public MowerGrowth Growth { get; private set; }

        public LevelObjectives Objectives { get; }

        public ProtectedRule Protection { get; }

        public LevelState State { get; private set; }

        public bool IsPaused { get; private set; }

        public bool IsAssisted { get; set; }

        public float RemainingTime { get; private set; }

        public float ElapsedTime { get; private set; }

        public LevelResult Result { get; private set; }

        public bool IsSimulating => IsPaused == false && (State == LevelState.Playing || State == LevelState.Cleanup);

        public LevelRules Rules => _rules;

        public bool CountsProtectedHits => _rules.FailsOnProtectedHits;

        public int ProtectedHitLimit => _rules.FailLimit;

        public float ClearedFraction => _cuttableCells > 0 ? (float)_clearedCells / _cuttableCells : 1f;

        public void Reset()
        {
            ResetState();
            PublishState();
        }

        public bool TryOpenLoadout()
        {
            if (State != LevelState.Preview)
            {
                return false;
            }

            State = LevelState.Loadout;
            PublishState();
            return true;
        }

        public bool BackToPreview()
        {
            if (State != LevelState.Loadout)
            {
                return false;
            }

            State = LevelState.Preview;
            PublishState();
            return true;
        }

        public bool TryBegin()
            => TryBegin(Machine);

        public bool TryBegin(MachineConfig machine)
        {
            if (State != LevelState.Preview && State != LevelState.Loadout)
            {
                return false;
            }

            if (machine != Machine)
            {
                Machine = machine;
                Growth = new MowerGrowth(machine);
            }

            State = LevelState.Playing;
            LevelStartedMsg.Publish(in _publisher, new LevelStartedMsg(_level.Id, machine.Id));
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
            RecordHarvest(kind, xp, units: 1);
        }

        public void RecordHarvest(PlantKind kind, int xp, int units)
        {
            if (IsSimulating == false)
            {
                return;
            }

            _clearedCells += units;

            if (State != LevelState.Playing)
            {
                _harvestBatcher.Add(cells: units, xp: 0, ElapsedTime);
                return;
            }

            Objectives.Record(kind, units);
            _harvestBatcher.Add(cells: units, xp, ElapsedTime);

            var tiersGained = Growth.AddXp(xp);

            PublishCompletedQuotas(kind, units);
            PublishTierUps(tiersGained);
        }

        public bool RecordLockedTouch(PlantKind kind, int requiredTier)
        {
            if (IsSimulating == false || MarkFirst(ref _lockedKinds, kind) == false)
            {
                return false;
            }

            LockedPlantTouchedMsg.Publish(in _publisher, new LockedPlantTouchedMsg(kind, requiredTier));
            return true;
        }

        public bool RecordSlowHint(PlantKind kind)
        {
            if (IsSimulating == false || MarkFirst(ref _slowKinds, kind) == false)
            {
                return false;
            }

            SlowHintMsg.Publish(in _publisher, new SlowHintMsg(kind));
            return true;
        }

        public void RecordXp(int xp)
        {
            if (State == LevelState.Playing && IsPaused == false)
            {
                _harvestBatcher.Add(cells: 0, xp, ElapsedTime);
                PublishTierUps(Growth.AddXp(xp));
            }
        }

        public bool RecordProtectedTouch(int bed)
        {
            if (State != LevelState.Playing || IsPaused)
            {
                return false;
            }

            var isNewHit = Protection.Touch(bed, ElapsedTime);

            if (isNewHit)
            {
                var message = new ProtectedHitMsg(Protection.Hits, _rules.FailLimit, _rules.FailsOnProtectedHits);

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

            if (_rules.IsTimed)
            {
                RemainingTime = Mathf.Max(RemainingTime - deltaTime, 0f);
            }

            var hasTooManyHits = _rules.FailsOnProtectedHits && Protection.Hits > _rules.FailLimit;

            if (hasTooManyHits)
            {
                Finish(new LevelOutcome.TooManyProtectedHits(Protection.Hits, _rules.FailLimit));
                return;
            }

            if (Objectives.IsComplete)
            {
                Finish(new LevelOutcome.Success(EvaluateStars()));
                return;
            }

            if (_rules.IsTimed && RemainingTime <= 0f)
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
            Result = new LevelResult(outcome, RemainingTime, Protection.Hits, IsAssisted);

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

        private static bool MarkFirst(ref int mask, PlantKind kind)
        {
            var bit = 1 << (int)kind;
            var isFirst = (mask & bit) == 0;

            mask |= bit;
            return isFirst;
        }

        private void PublishCompletedQuotas(PlantKind kind, int units)
        {
            var count = Objectives.QuotaCount;
            var harvested = Objectives.GetHarvested(kind);

            for (var i = 0; i < count; i++)
            {
                ref readonly var quota = ref Objectives.GetQuota(i);

                var isNewlyMet = harvested - units < quota.Amount && harvested >= quota.Amount;

                if (quota.Kind == kind && isNewlyMet)
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

        private StarFlags EvaluateStars()
        {
            var isSideMet = Objectives.HasBonusQuota
                ? Objectives.AreBonusQuotasMet
                : ClearedFraction >= StarRules.SIDE_SWEEP_FRACTION;

            return StarRules.Evaluate(
                  isWin: true
                , isAssisted: IsAssisted
                , isTimed: _rules.IsTimed
                , remainingTime: RemainingTime
                , timeLimit: _rules.TimeLimit
                , star2TimeLeft: _rules.Star2TimeLeft
                , protectedHits: Protection.Hits
                , isSideMet: isSideMet
            );
        }

        private void ResetState()
        {
            Growth.Reset();
            Objectives.Reset();
            Protection.Reset();
            State = LevelState.Preview;
            IsPaused = false;
            IsAssisted = false;
            RemainingTime = _rules.TimeLimit;
            ElapsedTime = 0f;
            Result = default;
            _clearedCells = 0;
            _lockedKinds = 0;
            _slowKinds = 0;
            _harvestBatcher.Clear();
        }
    }
}
