using System;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    [Serializable]
    public sealed class PlantIconSet
    {
        [SerializeField]
        private Sprite _grass;

        [SerializeField]
        private Sprite _flower;

        [SerializeField]
        private Sprite _bush;

        public Sprite Get(PlantKind kind)
        {
            return PlantVisuals.GetIcon(kind) switch {
                PlantIcon.Flower => _flower,
                PlantIcon.Bush => _bush,
                _ => _grass,
            };
        }
    }
}
