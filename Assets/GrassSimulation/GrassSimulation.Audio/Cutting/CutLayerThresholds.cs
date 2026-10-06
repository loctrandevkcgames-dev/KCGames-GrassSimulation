using System;
using UnityEngine;

namespace GrassSimulation.Audio
{
    [Serializable]
    public struct CutLayerThresholds
    {
        [field: SerializeField]
        public float Light { get; private set; }

        [field: SerializeField]
        public float Medium { get; private set; }

        [field: SerializeField]
        public float Dense { get; private set; }

        public CutLayerThresholds(float light, float medium, float dense)
        {
            Light = light;
            Medium = medium;
            Dense = dense;
        }
    }
}
