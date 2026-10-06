using GrassSimulation.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Progression.Tests;

internal static class TestCatalogs
{
    public static LevelCatalog Create(string first, string second, string third, out Object[] objects)
    {
        var ids = new[] { first, second, third };
        var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        var levels = new Object[ids.Length];

        for (var i = 0; i < ids.Length; i++)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var serializedLevel = new SerializedObject(level);

            serializedLevel.FindProperty("_id").stringValue = ids[i];
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();
            levels[i] = level;
        }

        var serializedCatalog = new SerializedObject(catalog);
        var array = serializedCatalog.FindProperty("_levels");

        array.arraySize = levels.Length;

        for (var i = 0; i < levels.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

        objects = new Object[levels.Length + 1];
        levels.CopyTo(objects, 0);
        objects[levels.Length] = catalog;
        return catalog;
    }

    public static void DestroyAll(Object[] objects)
    {
        var count = objects.Length;

        for (var i = 0; i < count; i++)
        {
            Object.DestroyImmediate(objects[i]);
        }
    }
}
