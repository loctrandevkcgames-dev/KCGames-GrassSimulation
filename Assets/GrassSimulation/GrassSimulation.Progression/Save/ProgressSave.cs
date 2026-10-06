using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GrassSimulation.Progression
{
    [Preserve]
    public sealed class ProgressSave
    {
        public const int CURRENT_VERSION = 2;

        [Preserve]
        public ProgressSave()
        {
        }

        public int Version { get; set; }

        public int Revision { get; set; }

        public Dictionary<string, LevelRecord> Levels { get; set; } = new();

        // Version 1 fields. They are read only to migrate a version 1 save and are null afterwards.
        public List<string> CompletedLevels { get; set; }

        public Dictionary<string, int> BestStars { get; set; }

        public static ProgressSave CreateNew()
            => new() { Version = CURRENT_VERSION };

        public ProgressSave Clone()
        {
            var levels = new Dictionary<string, LevelRecord>(Levels.Count);

            foreach (var pair in Levels)
            {
                levels[pair.Key] = pair.Value.Clone();
            }

            return new ProgressSave {
                Version = Version,
                Revision = Revision,
                Levels = levels,
                CompletedLevels = CompletedLevels == null ? null : new List<string>(CompletedLevels),
                BestStars = BestStars == null ? null : new Dictionary<string, int>(BestStars),
            };
        }
    }
}
