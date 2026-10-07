using System;

namespace GrassSimulation.Gameplay
{
    public readonly record struct BoosterSnapshot(BoosterSlot Turbo, BoosterSlot ExtraTime, bool IsHudShown)
    {
        public static BoosterSnapshot From(LevelSession session, Func<BoosterKind, int> getStock)
        {
            var state = session.State;
            var isHudShown = state == LevelState.Playing || state == LevelState.UpgradeChoice;

            return new BoosterSnapshot(
                  ReadSlot(session, BoosterKind.Turbo, getStock)
                , ReadSlot(session, BoosterKind.ExtraTime, getStock)
                , isHudShown
            );
        }

        public BoosterSlot GetSlot(BoosterKind kind)
        {
            return kind switch {
                BoosterKind.Turbo => Turbo,
                BoosterKind.ExtraTime => ExtraTime,
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
        }

        private static BoosterSlot ReadSlot(LevelSession session, BoosterKind kind, Func<BoosterKind, int> getStock)
        {
            var boosters = session.Boosters;
            var isAllowed = BoosterRules.IsAllowed(kind, session.Rules);
            var state = isAllowed ? boosters.GetState(kind) : BoosterSlotState.NotEquipped;

            return new BoosterSlot(
                  state
                , boosters.IsEquipped(kind)
                , isAllowed
                , getStock(kind)
                , boosters.GetRemaining(kind)
                , boosters.GetDuration(kind)
            );
        }
    }
}
