using System;

namespace GrassSimulation.Gameplay
{
    public readonly record struct PlantKindMask(int Bits)
    {
        public static PlantKindMask FromTier(ReadOnlySpan<PlantSettings> plants, int tier)
        {
            var mask = default(PlantKindMask);

            for (var i = 0; i < plants.Length; i++)
            {
                ref readonly var plant = ref plants[i];

                if (plant.Kind != PlantKind.None && plant.RequiredTier == tier)
                {
                    mask = mask.With(plant.Kind);
                }
            }

            return mask;
        }

        public int Count
        {
            get
            {
                var count = 0;

                for (var bits = Bits; bits != 0; bits &= bits - 1)
                {
                    count++;
                }

                return count;
            }
        }

        public bool Contains(PlantKind kind)
        {
            return (Bits & (1 << (int)kind)) != 0;
        }

        public PlantKindMask With(PlantKind kind)
        {
            return new PlantKindMask(Bits | (1 << (int)kind));
        }
    }
}
