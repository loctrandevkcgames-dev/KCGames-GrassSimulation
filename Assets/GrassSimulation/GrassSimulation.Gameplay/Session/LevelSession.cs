using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class LevelSession
    {
        private const float SECOND_STAR_TIME_FRACTION = 0.2f;

        private readonly LevelDefinition _level;
        private readonly int _cuttableCells;

        private int _clearedCells;

        public LevelSession(LevelDefinition level, MachineConfig machine, int cuttableCells)
        {
            _level = level;
            _cuttableCells = cuttableCells;
            Growth = new MowerGrowth(machine);
            Objectives = new LevelObjectives(level.Quotas);
            Protection = new ProtectedRule(level.ProtectedHitCooldown);
            Reset();
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
            Growth.Reset();
            Objectives.Reset();
            Protection.Reset();
            State = LevelState.Preview;
            IsPaused = false;
            RemainingTime = _level.TimeLimit;
            ElapsedTime = 0f;
            Result = default;
            _clearedCells = 0;
        }

        public bool TryBegin()
        {
            if (State != LevelState.Preview)
            {
                return false;
            }

            State = LevelState.Playing;
            return true;
        }

        public void Pause()
        {
            var canPause = State == LevelState.Playing || State == LevelState.Cleanup;
            IsPaused = canPause;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void RecordHarvest(PlantKind kind, int xp)
        {
            if (IsSimulating == false)
            {
                return;
            }

            _clearedCells++;

            if (State == LevelState.Playing)
            {
                Objectives.Record(kind);
                Growth.AddXp(xp);
            }
        }

        public void RecordXp(int xp)
        {
            if (State == LevelState.Playing && IsPaused == false)
            {
                Growth.AddXp(xp);
            }
        }

        public bool RecordProtectedTouch()
        {
            if (State != LevelState.Playing || IsPaused)
            {
                return false;
            }

            return Protection.Touch(ElapsedTime);
        }

        public void EndTick(float deltaTime)
        {
            if (IsSimulating == false)
            {
                return;
            }

            ElapsedTime += deltaTime;

            if (State != LevelState.Playing)
            {
                return;
            }

            RemainingTime = Mathf.Max(RemainingTime - deltaTime, 0f);

            var hasTooManyHits = _level.FailOnProtectedHits && Protection.Hits > _level.ProtectedHitLimit;

            if (hasTooManyHits)
            {
                Finish(LevelOutcome.TooManyProtectedHits);
                return;
            }

            if (Objectives.IsComplete)
            {
                Finish(LevelOutcome.Success);
                return;
            }

            if (RemainingTime <= 0f)
            {
                Finish(LevelOutcome.TimeUp);
                return;
            }

            if (Growth.PendingUpgrades > 0)
            {
                State = LevelState.UpgradeChoice;
            }
        }

        public bool TryChooseUpgrade(int option)
        {
            if (State != LevelState.UpgradeChoice || Growth.TryChooseUpgrade(option) == false)
            {
                return false;
            }

            if (Growth.PendingUpgrades == 0)
            {
                State = LevelState.Playing;
            }

            return true;
        }

        public bool TryEnterCleanup()
        {
            if (State != LevelState.Success)
            {
                return false;
            }

            State = LevelState.Cleanup;
            return true;
        }

        private void Finish(LevelOutcome outcome)
        {
            var isSuccess = outcome == LevelOutcome.Success;
            var stars = isSuccess ? CountStars() : 0;

            State = isSuccess ? LevelState.Success : LevelState.Failure;
            Result = new LevelResult(outcome, stars, RemainingTime, Protection.Hits);
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
    }
}
