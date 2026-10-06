using GrassSimulation.Gameplay;
using UnityEngine.Scripting;

namespace GrassSimulation.Progression
{
    [Preserve]
    public sealed class LevelRecord
    {
        [Preserve]
        public LevelRecord()
        {
        }

        public StarFlags Stars { get; set; }

        public int Attempts { get; set; }

        public bool Completed { get; set; }

        public LevelRecord Clone()
            => new() { Stars = Stars, Attempts = Attempts, Completed = Completed };
    }
}
