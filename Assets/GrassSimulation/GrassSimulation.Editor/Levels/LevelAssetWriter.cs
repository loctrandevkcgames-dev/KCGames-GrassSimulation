using GrassSimulation.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Editor
{
    public static class LevelAssetWriter
    {
        private const string KIND = "<Kind>k__BackingField";
        private const string AMOUNT = "<Amount>k__BackingField";
        private const string IS_BONUS = "<IsBonus>k__BackingField";
        private const string POSITION = "<Position>k__BackingField";
        private const string FRUIT_COUNT = "<FruitCount>k__BackingField";
        private const string YAW = "<Yaw>k__BackingField";
        private const string SCALE = "<Scale>k__BackingField";
        private const string LENGTH = "<Length>k__BackingField";

        public static void Apply(LevelDefinition level, LevelSpec spec, BakedLevel baked)
        {
            var serialized = new SerializedObject(level);

            serialized.FindProperty("_id").stringValue = $"level-{spec.Order:00}";
            serialized.FindProperty("_cellsX").intValue = baked.CellsX;
            serialized.FindProperty("_cellsZ").intValue = baked.CellsZ;
            serialized.FindProperty("_cellSize").floatValue = baked.CellSize;
            serialized.FindProperty("_seed").intValue = baked.Seed;
            serialized.FindProperty("_baseKind").enumValueIndex = (int)PlantKind.None;
            serialized.FindProperty("_zones").arraySize = 0;
            serialized.FindProperty("_spawn").vector2Value = baked.Spawn;
            serialized.FindProperty("_spawnClearing").floatValue = baked.SpawnClearing;
            serialized.FindProperty("_timeLimit").floatValue = spec.IsTimed ? spec.SuggestedTimer : 0f;
            serialized.FindProperty("_type").enumValueIndex = (int)spec.Type;
            serialized.FindProperty("_maxTier").intValue = spec.MaxTier;
            serialized.FindProperty("_displayName").stringValue = spec.Name;
            serialized.FindProperty("_decision").stringValue = spec.Decision;

            WriteKinds(serialized.FindProperty("_bakedKinds"), baked.Kinds);
            WriteBeds(serialized.FindProperty("_bakedBeds"), baked.Beds);
            WritePlants(serialized.FindProperty("_plants"), baked.Plants);
            WriteObstacles(serialized.FindProperty("_obstacles"), baked.Obstacles);
            WriteQuotas(serialized.FindProperty("_quotas"), spec.Quotas);
            WriteUnlock(serialized.FindProperty("_unlock"), spec.Unlock);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
        }

        public static void WriteCatalog(LevelCatalog catalog, LevelDefinition[] levels)
        {
            var serialized = new SerializedObject(catalog);
            var array = serialized.FindProperty("_levels");

            array.arraySize = levels.Length;

            for (var i = 0; i < levels.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void WriteKinds(SerializedProperty array, byte[] kinds)
        {
            array.arraySize = kinds.Length;

            for (var i = 0; i < kinds.Length; i++)
            {
                array.GetArrayElementAtIndex(i).intValue = kinds[i];
            }
        }

        private static void WriteBeds(SerializedProperty array, RectInt[] beds)
        {
            array.arraySize = beds.Length;

            for (var i = 0; i < beds.Length; i++)
            {
                array.GetArrayElementAtIndex(i).rectIntValue = beds[i];
            }
        }

        private static void WritePlants(SerializedProperty array, PlantPlacement[] plants)
        {
            array.arraySize = plants.Length;

            for (var i = 0; i < plants.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);

                element.FindPropertyRelative(KIND).enumValueIndex = (int)plants[i].Kind;
                element.FindPropertyRelative(POSITION).vector2Value = plants[i].Position;
                element.FindPropertyRelative(FRUIT_COUNT).intValue = plants[i].FruitCount;
                element.FindPropertyRelative(YAW).floatValue = plants[i].Yaw;
                element.FindPropertyRelative(SCALE).floatValue = plants[i].Scale;
            }
        }

        private static void WriteObstacles(SerializedProperty array, ObstaclePlacement[] obstacles)
        {
            array.arraySize = obstacles.Length;

            for (var i = 0; i < obstacles.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);

                element.FindPropertyRelative(KIND).enumValueIndex = (int)obstacles[i].Kind;
                element.FindPropertyRelative(POSITION).vector2Value = obstacles[i].Position;
                element.FindPropertyRelative(YAW).floatValue = obstacles[i].Yaw;
                element.FindPropertyRelative(LENGTH).floatValue = obstacles[i].Length;
            }
        }

        private static void WriteQuotas(SerializedProperty array, QuotaSettings[] quotas)
        {
            array.arraySize = quotas.Length;

            for (var i = 0; i < quotas.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);

                element.FindPropertyRelative(KIND).enumValueIndex = (int)quotas[i].Kind;
                element.FindPropertyRelative(AMOUNT).intValue = quotas[i].Amount;
                element.FindPropertyRelative(IS_BONUS).boolValue = quotas[i].IsBonus;
            }
        }

        private static void WriteUnlock(SerializedProperty unlock, UnlockSettings settings)
        {
            unlock.FindPropertyRelative(KIND).enumValueIndex = (int)settings.Kind;
            unlock.FindPropertyRelative(AMOUNT).intValue = settings.Amount;
        }
    }
}
