using System;
using UnityEngine;

namespace GrassSimulation.Audio
{
    [Serializable]
    public struct SoundCue
    {
        [field: SerializeField]
        public SoundId Id { get; private set; }

        [field: SerializeField]
        public AudioClip[] Clips { get; private set; }

        [field: SerializeField]
        public float Volume { get; private set; }

        [field: SerializeField]
        public Vector2 Pitch { get; private set; }

        [field: SerializeField]
        public float Cooldown { get; private set; }

        [field: SerializeField]
        public CueVariantOrder Order { get; private set; }

        [field: SerializeField]
        public AudioBus Bus { get; private set; }

        public bool HasClips => Clips != null && Clips.Length > 0;
    }
}
