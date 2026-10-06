using UnityEngine;

namespace GrassSimulation.Audio
{
    public static class TimerTicks
    {
        public static TimerTick Evaluate(
              float previousRemaining
            , float remaining
            , float warningSeconds
            , float accentSeconds
        )
        {
            if (remaining <= 0f || remaining > previousRemaining || remaining > warningSeconds)
            {
                return TimerTick.None;
            }

            if (Mathf.FloorToInt(remaining) >= Mathf.FloorToInt(previousRemaining))
            {
                return TimerTick.None;
            }

            return remaining <= accentSeconds ? TimerTick.Accent : TimerTick.Tick;
        }
    }
}
