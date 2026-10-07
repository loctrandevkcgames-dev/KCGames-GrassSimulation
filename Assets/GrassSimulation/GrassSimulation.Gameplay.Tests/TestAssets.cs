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

    public MachineConfig CreateMachine(string id, float cutRadius, float speed)
    {
        var machine = CreateMachine();
        var serialized = new SerializedObject(machine);

        serialized.FindProperty("_id").stringValue = id;
        serialized.FindProperty("_displayName").stringValue = id;
        serialized.FindProperty("_baseCutRadius").floatValue = cutRadius;
        serialized.FindProperty("_baseSpeed").floatValue = speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return machine;
    }

    public MachineCatalog CreateMachineCatalog(params MachineConfig[] machines)
    {
        var catalog = Track(ScriptableObject.CreateInstance<MachineCatalog>());
        var serialized = new SerializedObject(catalog);
        var array = serialized.FindProperty("_machines");

        array.arraySize = machines.Length;

        for (var i = 0; i < machines.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = machines[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return catalog;
    }

    public static GameRulesValues CreateRules(bool failOnProtectedHits)
    {
        var mode = failOnProtectedHits ? ProtectedMode.Fail : ProtectedMode.Warn;

        return GameRulesValues.Default with { ProtectedMode = mode };
    }

    public LevelDefinition CreateLevel(float timeLimit, params QuotaSettings[] quotas)
        => CreateLevel(LevelType.Normal, timeLimit, quotas);

    public LevelDefinition CreateLevel(LevelType type, float timeLimit, params QuotaSettings[] quotas)
    {
        var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
        var serialized = new SerializedObject(level);
        serialized.FindProperty("_cellsX").intValue = 8;
        serialized.FindProperty("_cellsZ").intValue = 8;
        serialized.FindProperty("_cellSize").floatValue = 1f;
        serialized.FindProperty("_timeLimit").floatValue = timeLimit;
        serialized.FindProperty("_type").enumValueIndex = (int)type;

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

    public void SetMaxTier(LevelDefinition level, int maxTier)
    {
        var serialized = new SerializedObject(level);

        serialized.FindProperty("_maxTier").intValue = maxTier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public LevelCatalog CreateCatalog(params LevelDefinition[] levels)
    {
        var catalog = Track(ScriptableObject.CreateInstance<LevelCatalog>());
        var serialized = new SerializedObject(catalog);
        var array = serialized.FindProperty("_levels");

        array.arraySize = levels.Length;

        for (var i = 0; i < levels.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return catalog;
    }

    public void SetBeds(LevelDefinition level, params RectInt[] beds)
    {
        var serialized = new SerializedObject(level);
        var zones = serialized.FindProperty("_zones");

        zones.arraySize = beds.Length;

        for (var i = 0; i < beds.Length; i++)
        {
            var zone = zones.GetArrayElementAtIndex(i);
            var kind = zone.FindPropertyRelative("<Kind>k__BackingField");

            kind.enumValueIndex = (int)PlantKind.ProtectedFlower;
            zone.FindPropertyRelative("<Cells>k__BackingField").rectIntValue = beds[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
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
