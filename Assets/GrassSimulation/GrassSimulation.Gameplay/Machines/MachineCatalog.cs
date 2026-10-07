using System;
using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "MachineCatalog", menuName = "Grass Simulation/Machine Catalog")]
    public sealed class MachineCatalog : ScriptableObject, IHasCount
    {
        [SerializeField]
        private MachineConfig[] _machines = Array.Empty<MachineConfig>();

        public int Count => _machines.Length;

        public MachineConfig Standard => TryFind(MachineIds.Standard, out var machine) ? machine : _machines[0];

        public MachineConfig Get(int index)
            => _machines[index];

        public bool TryFind(MachineId id, out MachineConfig machine)
        {
            var count = _machines.Length;

            for (var i = 0; i < count; i++)
            {
                if (_machines[i].Id == id)
                {
                    machine = _machines[i];
                    return true;
                }
            }

            machine = null;
            return false;
        }
    }
}
