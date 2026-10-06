using System;

namespace GrassSimulation.UI
{
    [Flags]
    public enum UpgradeStatKinds : byte
    {
        None = 0,
        CutRadius = 1 << 0,
        CuttingPower = 1 << 1,
        Speed = 1 << 2,
    }
}
