using System;
using UnityEngine;

namespace GrassSimulation.Audio
{
    [Serializable]
    public struct AmbienceLayer
    {
        [field: SerializeField]
        public AudioClip Clip { get; private set; }

        [field: SerializeField]
        public float Volume { get; private set; }
    }
}
