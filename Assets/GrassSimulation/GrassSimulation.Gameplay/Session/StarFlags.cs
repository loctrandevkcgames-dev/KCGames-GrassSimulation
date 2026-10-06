using System;

namespace GrassSimulation.Gameplay
{
    [Flags]
    public enum StarFlags : byte
    {
        None = 0,
        Goal = 1,
        Clean = 2,
        Side = 4,
    }
}
