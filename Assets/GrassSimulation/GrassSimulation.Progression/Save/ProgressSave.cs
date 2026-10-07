using System.Collections.Generic;
using GrassSimulation.Gameplay;
using UnityEngine.Scripting;

namespace GrassSimulation.Progression
{
    [Preserve]
    public sealed class ProgressSave
    {
        public const int CURRENT_VERSION = 3;

        [Preserve]
        public ProgressSave()
        {
        }

        public int Version { get; set; }

        public int Revision { get; set; }

        public Dictionary<string, LevelRecord> Levels { get; set; } = new();

        public HashSet<string> OwnedMachines { get; set; } = new();

        public string SelectedMachine { get; set; }

        public HashSet<string> GrantedUnlocks { get; set; } = new();

        // Version 1 fields. They are read only to migrate a version 1 save and are null afterwards.
        public List<string> CompletedLevels { get; set; }

        public Dictionary<string, int> BestStars { get; set; }

        public static ProgressSave CreateNew()
        {
            var save = new ProgressSave { Version = CURRENT_VERSION };

            save.EnsureMachines();
            return save;
        }

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
                OwnedMachines = OwnedMachines == null ? null : new HashSet<string>(OwnedMachines),
                SelectedMachine = SelectedMachine,
                GrantedUnlocks = GrantedUnlocks == null ? null : new HashSet<string>(GrantedUnlocks),
                CompletedLevels = CompletedLevels == null ? null : new List<string>(CompletedLevels),
                BestStars = BestStars == null ? null : new Dictionary<string, int>(BestStars),
            };
        }

        internal void EnsureMachines()
        {
            OwnedMachines ??= new HashSet<string>();
            GrantedUnlocks ??= new HashSet<string>();
            OwnedMachines.Add(MachineIds.Standard.Value);

            if (string.IsNullOrEmpty(SelectedMachine) || OwnedMachines.Contains(SelectedMachine) == false)
            {
                SelectedMachine = MachineIds.Standard.Value;
            }
        }
    }
}
