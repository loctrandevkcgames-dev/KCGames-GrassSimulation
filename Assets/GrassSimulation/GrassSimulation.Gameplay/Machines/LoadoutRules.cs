namespace GrassSimulation.Gameplay
{
    public static class LoadoutRules
    {
        public static bool ShouldShow(int ownedMachineCount, bool hasBoosterStock)
        {
            return ownedMachineCount > 1 || hasBoosterStock;
        }
    }
}
