namespace GrassSimulation.Progression
{
    internal static class ProgressMigration
    {
        internal static bool TryUpgrade(ProgressSave save)
            => save.Version == ProgressSave.CURRENT_VERSION;
    }
}
