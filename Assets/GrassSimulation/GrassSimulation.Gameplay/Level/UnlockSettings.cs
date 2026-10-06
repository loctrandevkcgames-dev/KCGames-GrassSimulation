using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct UnlockSettings
    {
        [field: SerializeField]
        public UnlockKind Kind { get; set; }

        [field: SerializeField]
        public int Amount { get; set; }
    }
}
