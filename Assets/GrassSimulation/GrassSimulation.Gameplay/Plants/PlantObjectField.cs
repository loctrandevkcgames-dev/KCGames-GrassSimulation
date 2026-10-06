using System;
using System.Collections.Generic;
using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class PlantObjectField : IHasCount, IClearable
    {
        private const int OBJECT_UNITS = 1;

        private readonly PlantDefinition[] _plantByKind = new PlantDefinition[PlantKindExtensions.Length];
        private readonly List<PlantObjectState> _objects = new();

        public PlantObjectField(ReadOnlySpan<PlantDefinition> plants)
        {
            var plantCount = plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                _plantByKind[(int)plants[i].Kind] = plants[i];
            }
        }

        public int Count => _objects.Count;

        public ref readonly PlantDefinition GetPlant(PlantKind kind)
            => ref _plantByKind[(int)kind];

        public PlantObjectState Get(int index)
            => _objects[index];

        public int Add(PlantKind kind, Vector2 position, int fruitCount)
        {
            var state = new PlantObjectState {
                Kind = kind,
                Position = position,
                Radius = _plantByKind[(int)kind].CutZoneRadius,
                FruitCount = fruitCount > 0 ? fruitCount : _plantByKind[(int)kind].FruitCount,
            };

            _objects.Add(state);
            return _objects.Count - 1;
        }

        public void Clear()
        {
            _objects.Clear();
        }

        public void ResetProgress()
        {
            var count = _objects.Count;

            for (var i = 0; i < count; i++)
            {
                var state = _objects[i];

                state.Progress = 0f;
                state.Harvested = false;
                _objects[i] = state;
            }
        }

        public int CountHarvested(PlantKind kind)
        {
            var harvested = 0;
            var count = _objects.Count;

            for (var i = 0; i < count; i++)
            {
                if (_objects[i].Kind == kind && _objects[i].Harvested)
                {
                    harvested++;
                }
            }

            return harvested;
        }

        public void Cut(
              in CutStroke stroke
            , ObstacleField obstacles
            , List<PlantHarvest> harvested
            , PlantContactReport contacts
        )
        {
            var count = _objects.Count;

            if (count == 0)
            {
                return;
            }

            var start = new Vector2(stroke.From.x, stroke.From.z);
            var end = new Vector2(stroke.To.x, stroke.To.z);

            for (var i = 0; i < count; i++)
            {
                var state = _objects[i];

                if (state.Harvested)
                {
                    continue;
                }

                var reach = stroke.Radius + state.Radius;
                var coverage = CutMath.ContactCoverage(start, end, state.Position, reach);

                if (coverage <= 0f)
                {
                    continue;
                }

                var blade = CutMath.ClosestPoint(start, end, state.Position);

                if (obstacles != null && obstacles.IsOccluded(blade, state.Position))
                {
                    continue;
                }

                CutObject(i, state, coverage, in stroke, harvested, contacts);
            }
        }

        private void CutObject(
              int index
            , PlantObjectState state
            , float coverage
            , in CutStroke stroke
            , List<PlantHarvest> harvested
            , PlantContactReport contacts
        )
        {
            ref readonly var plant = ref _plantByKind[(int)state.Kind];
            var position = new Vector3(state.Position.x, 0f, state.Position.y);

            if (plant.RequiredTier > stroke.Tier)
            {
                contacts?.AddLocked(state.Kind, position, plant.RequiredTier);
                return;
            }

            var centerSpeed = CutMath.CenterSpeed(stroke.Radius, state.Radius, stroke.CuttingPower, plant.Toughness);

            contacts?.AddCut(state.Kind, position, in stroke, centerSpeed);

            state.Progress += coverage * stroke.DeltaTime * stroke.CuttingPower / plant.Toughness;

            if (state.Progress >= 1f)
            {
                state.Progress = 1f;
                state.Harvested = true;

                harvested.Add(new PlantHarvest(
                      index
                    , state.Kind
                    , plant.Xp
                    , OBJECT_UNITS
                    , state.FruitCount
                    , position
                ));
            }

            _objects[index] = state;
        }
    }
}
