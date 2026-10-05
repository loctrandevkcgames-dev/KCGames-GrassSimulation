using System.Diagnostics;
using System.Runtime.CompilerServices;
using EncosyTower.Logging;
using GrassSimulation.Progression;
using UnityEngine;

namespace GrassSimulation.Sandbox
{
    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_WipeFailed(SaveError error)
        {
            StaticLogger.LogError($"Cannot wipe the progress: {error.ToMessage()}");
        }
    }
}
