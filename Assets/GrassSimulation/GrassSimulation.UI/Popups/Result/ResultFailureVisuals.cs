using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class ResultFailureVisuals
    {
        public static ResultFailureVisual Get(in LevelOutcome outcome)
        {
            if (outcome.TryGetProtectedHits(out var hits, out var limit))
            {
                return new ResultFailureVisual(
                      UiText.FAIL_PROTECTED_TITLE
                    , string.Format(UiText.FAIL_PROTECTED_REASON, hits, limit)
                    , ResultFailureIcon.ShieldX
                    , UiPalette.ProtectedFill
                    , UiPalette.Protected
                );
            }

            return new ResultFailureVisual(
                  UiText.FAIL_TIME_UP_TITLE
                , UiText.FAIL_TIME_UP_REASON
                , ResultFailureIcon.Clock
                , UiPalette.TimeUpFill
                , UiPalette.TimeUp
            );
        }
    }
}
