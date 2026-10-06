using System;
using UnityEngine;

namespace GrassSimulation.Audio
{
    [Serializable]
    public struct GrassCutLoopSettings
    {
        [field: SerializeField]
        public AudioClip Clip { get; private set; }

        [field: SerializeField]
        public float Volume { get; private set; }

        [field: SerializeField]
        public Vector2 Pitch { get; private set; }

        [field: SerializeField]
        public float FullCellsPerSecond { get; private set; }
    }
}
