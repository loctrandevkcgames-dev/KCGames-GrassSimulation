using System.Diagnostics;
using System.Runtime.CompilerServices;
using EncosyTower.Logging;
using UnityEngine;

namespace GrassSimulation.Audio
{
    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning(object message)
        {
            StaticDevLogger.LogWarning(message);
        }
    }
}
