using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrassSimulation.Gameplay.Tests;

internal sealed class TestAssets : IDisposable
{
    private readonly List<Object> _objects = new();

    public MachineConfig CreateMachine()
    {
        var machine = Track(ScriptableObject.CreateInstance<MachineConfig>());
        var serialized = new SerializedObject(machine);
        var upgrades = serialized.FindProperty("_upgrades");
        upgrades.arraySize = 2;
        SetUpgrade(upgrades.GetArrayElementAtIndex(0), "WideBlade", radius: 0.15f, power: 0f, speed: 0f);
        SetUpgrade(upgrades.GetArrayElementAtIndex(1), "StrongEngine", radius: 0f, power: 0.3f, speed: 0.25f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return machine;
    }

    public LevelDefinition CreateLevel(
          float timeLimit
        , bool failOnProtectedHits
        , params QuotaSettings[] quotas
    )
    {
        var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
        var serialized = new SerializedObject(level);
        serialized.FindProperty("_cellsX").intValue = 8;
        serialized.FindProperty("_cellsZ").intValue = 8;
        serialized.FindProperty("_cellSize").floatValue = 1f;
        serialized.FindProperty("_timeLimit").floatValue = timeLimit;
        serialized.FindProperty("_failOnProtectedHits").boolValue = failOnProtectedHits;
        serialized.FindProperty("_protectedHitLimit").intValue = 3;
        serialized.FindProperty("_protectedHitCooldown").floatValue = 1f;

        var quotaArray = serialized.FindProperty("_quotas");
        quotaArray.arraySize = quotas.Length;

        for (var i = 0; i < quotas.Length; i++)
        {
            var element = quotaArray.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("<Kind>k__BackingField").enumValueIndex = (int)quotas[i].Kind;
            element.FindPropertyRelative("<Amount>k__BackingField").intValue = quotas[i].Amount;
            element.FindPropertyRelative("<IsBonus>k__BackingField").boolValue = quotas[i].IsBonus;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return level;
    }

    public void Dispose()
    {
        var count = _objects.Count;

        for (var i = 0; i < count; i++)
        {
            Object.DestroyImmediate(_objects[i]);
        }

        _objects.Clear();
    }

    private static void SetUpgrade(SerializedProperty upgrade, string id, float radius, float power, float speed)
    {
        upgrade.FindPropertyRelative("<Id>k__BackingField").stringValue = id;
        upgrade.FindPropertyRelative("<CutRadiusDelta>k__BackingField").floatValue = radius;
        upgrade.FindPropertyRelative("<CuttingPowerDelta>k__BackingField").floatValue = power;
        upgrade.FindPropertyRelative("<SpeedDelta>k__BackingField").floatValue = speed;
    }

    private T Track<T>(T value)
        where T : Object
    {
        _objects.Add(value);
        return value;
    }
}
