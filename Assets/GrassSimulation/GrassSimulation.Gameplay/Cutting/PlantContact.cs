using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct PlantContact(PlantKind Kind, Vector3 Position, int RequiredTier);
}
