using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GrassSimulation.Progression
{
    [Preserve]
    public sealed class ProgressSave
    {
        public const int CURRENT_VERSION = 1;

        [Preserve]
        public ProgressSave()
        {
        }

        public int Version { get; set; }

        public List<string> CompletedLevels { get; set; } = new();

        public Dictionary<string, int> BestStars { get; set; } = new();

        public List<string> GrantedRewards { get; set; } = new();

        public int Revision { get; set; }

        public int Coins { get; set; }

        public List<string> OwnedSkins { get; set; } = new();

        public static ProgressSave CreateNew()
            => new() { Version = CURRENT_VERSION };

        public ProgressSave Clone()
            => new() {
                Version = Version,
                CompletedLevels = new List<string>(CompletedLevels),
                BestStars = new Dictionary<string, int>(BestStars),
                GrantedRewards = new List<string>(GrantedRewards),
                Revision = Revision,
                Coins = Coins,
                OwnedSkins = new List<string>(OwnedSkins),
            };
    }
}
