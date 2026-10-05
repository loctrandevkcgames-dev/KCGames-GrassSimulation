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

        public int ClampIndex(int index)
            => Mathf.Clamp(index, 0, Mathf.Max(_levels.Length - 1, 0));
    }
}
