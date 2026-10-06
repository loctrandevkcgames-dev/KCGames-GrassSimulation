using UnityEngine;

namespace GrassSimulation.Audio
{
    public static class AudioVolume
    {
        public const float MIN_DB = -80f;

        private const float SILENCE = 1e-4f;
        private const float DB_PER_DECADE = 20f;
        private const float DECADE_BASE = 10f;

        public static float ToDecibels(float linear)
        {
            if (linear <= SILENCE)
            {
                return MIN_DB;
            }

            if (linear >= 1f)
            {
                return 0f;
            }

            return Mathf.Max(DB_PER_DECADE * Mathf.Log10(linear), MIN_DB);
        }

        public static float FromDecibels(float decibels)
        {
            if (decibels <= MIN_DB)
            {
                return 0f;
            }

            return Mathf.Pow(DECADE_BASE, decibels / DB_PER_DECADE);
        }

        public static float BusDecibels(float baseDecibels, float slider, bool muted)
        {
            if (muted)
            {
                return MIN_DB;
            }

            return Mathf.Max(baseDecibels + ToDecibels(slider), MIN_DB);
        }
    }
}
