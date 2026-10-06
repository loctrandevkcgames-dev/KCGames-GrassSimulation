using System.Collections.Generic;
using System.Globalization;
using EncosyTower.Logging;
using GrassSimulation.Gameplay;
using UnityEditor;

namespace GrassSimulation.Editor
{
    public static class LevelMenu
    {
        private const string BAKE_ALL = "Grass Simulation/Levels/Bake All";
        private const string BAKE_SELECTED = "Grass Simulation/Levels/Bake Selected";
        private const string VALIDATE_ALL = "Grass Simulation/Levels/Validate All";

        [MenuItem(BAKE_ALL)]
        private static void BakeAll()
        {
            Log(LevelPipeline.BakeAll());
        }

        [MenuItem(BAKE_SELECTED)]
        private static void BakeSelected()
        {
            var orders = new HashSet<int>();
            var selected = Selection.GetFiltered<LevelDefinition>(SelectionMode.Assets);

            for (var i = 0; i < selected.Length; i++)
            {
                var digits = selected[i].Id.Value.Replace("level-", string.Empty);

                if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order))
                {
                    orders.Add(order);
                }
            }

            Log(LevelPipeline.Bake(orders));
        }

        [MenuItem(BAKE_SELECTED, validate = true)]
        private static bool CanBakeSelected()
            => Selection.GetFiltered<LevelDefinition>(SelectionMode.Assets).Length > 0;

        [MenuItem(VALIDATE_ALL)]
        private static void ValidateAll()
        {
            Log(LevelPipeline.ValidateAll());
        }

        private static void Log(LevelPipelineReport report)
        {
            var summary = $"Levels: {report.Passed} passed, {report.Failed} failed.\n{report.Text}";

            if (report.Failed > 0)
            {
                StaticLogger.LogWarning(summary);
            }
            else
            {
                StaticLogger.LogInfo(summary);
            }
        }
    }
}
