using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Logging;
using UnityEngine;
using static EncosyTower.Debugging.ValidationDefines;

namespace GrassSimulation.Gameplay
{
    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS)]
        internal static void ThrowIfTooManyQuotas(
              [DoesNotReturnIf(false)] bool isWithinLimit
            , string levelId
            , int maxQuotas
        )
        {
            if (isWithinLimit == false)
            {
                throw CreateException(levelId, maxQuotas);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(string levelId, int maxQuotas)
                => new($"The level '{levelId}' has more than {maxQuotas} quotas, "
                    + "which the HUD snapshot cannot report.");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_MissingObjectPrefab(PlantKind kind)
        {
            StaticLogger.LogError($"The plant catalog has no object prefab for '{kind}'.");
        }
    }
}
