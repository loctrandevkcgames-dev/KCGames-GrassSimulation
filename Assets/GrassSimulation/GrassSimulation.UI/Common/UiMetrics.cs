namespace GrassSimulation.UI
{
    public static class UiMetrics
    {
        public const float DESIGN_WIDTH = 390f;
        public const float REFERENCE_WIDTH = 1080f;

        /// <summary>Converts a value authored in 390-wide design units to canvas reference units.</summary>
        public const float SCALE = REFERENCE_WIDTH / DESIGN_WIDTH;
    }
}
