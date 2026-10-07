namespace GrassSimulation.Gameplay
{
    public static class MachineIds
    {
        public static readonly MachineId Standard = new("standard");

        public static readonly MachineId Wide = new("wide");

        public static bool TryFromUnlock(UnlockKind kind, out MachineId machine)
        {
            if (kind == UnlockKind.WideMachine)
            {
                machine = Wide;
                return true;
            }

            machine = default;
            return false;
        }
    }
}
