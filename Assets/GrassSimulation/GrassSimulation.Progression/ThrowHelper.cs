using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using EncosyTower.Logging;
using UnityEngine;

namespace GrassSimulation.Progression
{
    internal static class ThrowHelper
    {
        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_SaveFailed(string path, Exception exception)
        {
            StaticLogger.LogError($"Cannot write the progress save '{path}': {exception.Message}");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_DeleteFailed(string path, Exception exception)
        {
            StaticLogger.LogError($"Cannot delete the progress save '{path}': {exception.Message}");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_SerializeFailed(string path)
        {
            StaticLogger.LogError($"Cannot serialize the progress save for '{path}'.");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_ReadFailed(string path, Exception exception)
        {
            StaticLogger.LogWarning($"Cannot read the progress save '{path}': {exception.Message}");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_InvalidSave(string path)
        {
            StaticLogger.LogWarning($"The progress save '{path}' is invalid and was skipped.");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_Quarantined(string path, string quarantinePath)
        {
            StaticLogger.LogWarning($"The progress save '{path}' is corrupt and was moved to '{quarantinePath}'.");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_QuarantineFailed(string path, Exception exception)
        {
            StaticLogger.LogWarning($"Cannot quarantine the corrupt progress save '{path}': {exception.Message}");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_StoreThrew(string operation, Exception exception)
        {
            StaticLogger.LogError($"The progress store threw during {operation}: {exception.Message}");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogError_SettleNotSaved(string levelId)
        {
            StaticLogger.LogError($"The result of level '{levelId}' was not saved and its progress was rolled back.");
        }

        [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogWarning_SettleStoreUnavailable(string levelId)
        {
            StaticLogger.LogWarning($"Level '{levelId}' was not settled: the progress store is read-only.");
        }
    }
}
