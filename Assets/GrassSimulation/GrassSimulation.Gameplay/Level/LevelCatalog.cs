using System;
using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Grass Simulation/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject, IHasCount
    {
        [SerializeField]
        private LevelDefinition[] _levels = Array.Empty<LevelDefinition>();

        public int Count => _levels.Length;

        public LevelDefinition Get(int index)
            => _levels[index];

        public bool TryFind(LevelId id, out LevelDefinition level)
        {
            var count = _levels.Length;

            for (var i = 0; i < count; i++)
            {
                if (_levels[i].Id == id)
                {
                    level = _levels[i];
                    return true;
                }
            }

            level = null;
            return false;
        }

        public int GetIntroducedTier(int levelIndex)
        {
            var last = Mathf.Min(levelIndex, _levels.Length - 1);
            var tier = 1;

            for (var i = 0; i <= last; i++)
            {
                tier = Mathf.Max(tier, _levels[i].MaxTier);
            }

            return tier;
        }

        public int ClampIndex(int index)
            => Mathf.Clamp(index, 0, Mathf.Max(_levels.Length - 1, 0));
    }
}
