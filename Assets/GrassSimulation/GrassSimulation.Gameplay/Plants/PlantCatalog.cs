using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "PlantCatalog", menuName = "Grass Simulation/Plant Catalog")]
    public sealed class PlantCatalog : ScriptableObject
    {
        [SerializeField]
        private PlantDefinition[] _plants = Array.Empty<PlantDefinition>();

        public ReadOnlySpan<PlantDefinition> Plants => _plants;

        public bool TryGet(PlantKind kind, out PlantDefinition plant)
        {
            var count = _plants.Length;

            for (var i = 0; i < count; i++)
            {
                if (_plants[i].Kind == kind)
                {
                    plant = _plants[i];
                    return true;
                }
            }

            plant = default;
            return false;
        }
    }
}
