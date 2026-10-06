using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassCutter
    {
        private const float MIN_SEGMENT = 1e-4f;
        private const int NO_BED = -1;

        private readonly PlantSettings[] _plantByKind = new PlantSettings[PlantKindExtensions.Length];
        private readonly FieldGrid _grid;
        private readonly FieldFeedback _feedback;
        private readonly int[] _bedByCell;
        private readonly float _maxZoneRadius;

        public GrassCutter(
              FieldGrid grid
            , FieldFeedback feedback
            , PlantSettings[] plants
            , ReadOnlySpan<RectInt> beds = default
        )
        {
            _grid = grid;
            _feedback = feedback;

            var plantCount = plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = plants[i];
                _plantByKind[(int)plant.Kind] = plant;
                _maxZoneRadius = Mathf.Max(_maxZoneRadius, plant.CutZoneRadius);
            }

            _bedByCell = BuildBedIndex(grid, beds);
        }

        private static int[] BuildBedIndex(FieldGrid grid, ReadOnlySpan<RectInt> beds)
        {
            var bedByCell = new int[grid.Count];

            Array.Fill(bedByCell, NO_BED);

            for (var bed = 0; bed < beds.Length; bed++)
            {
                var cells = beds[bed];

                for (var z = Mathf.Max(cells.yMin, 0); z < Mathf.Min(cells.yMax, grid.CellsZ); z++)
                {
                    for (var x = Mathf.Max(cells.xMin, 0); x < Mathf.Min(cells.xMax, grid.CellsX); x++)
                    {
                        var index = grid.IndexOf(x, z);

                        if (grid.GetKind(index) == PlantKind.ProtectedFlower)
                        {
                            bedByCell[index] = bed;
                        }
                    }
                }
            }

            return bedByCell;
        }

        private static float ContactCoverage(Vector2 from, Vector2 to, Vector2 point, float radius)
        {
            var segment = to - from;
            var length = segment.magnitude;

            if (length < MIN_SEGMENT)
            {
                return (point - to).sqrMagnitude <= radius * radius ? 1f : 0f;
            }

            var direction = segment / length;
            var offset = point - from;
            var along = Vector2.Dot(offset, direction);
            var across = Mathf.Abs(direction.x * offset.y - direction.y * offset.x);

            if (across > radius)
            {
                return 0f;
            }

            var halfChord = Mathf.Sqrt(radius * radius - across * across);
            var enter = Mathf.Max(along - halfChord, 0f);
            var exit = Mathf.Min(along + halfChord, length);
            return Mathf.Max(exit - enter, 0f) / length;
        }

        private static Vector2 Heading(in CutStroke stroke)
            => new(stroke.To.x - stroke.From.x, stroke.To.z - stroke.From.z);

        public ref readonly PlantSettings GetPlant(PlantKind kind)
            => ref _plantByKind[(int)kind];

        public int CountCuttableCells()
        {
            var cuttable = 0;
            var count = _grid.Count;

            for (var i = 0; i < count; i++)
            {
                var kind = _grid.GetKind(i);

                if (kind != PlantKind.None && _plantByKind[(int)kind].IsProtected == false)
                {
                    cuttable++;
                }
            }

            return cuttable;
        }

        public void Cut(in CutStroke stroke, List<int> harvested, List<int> touchedBeds)
        {
            var radius = stroke.Radius;
            var start = _grid.ToLocal(stroke.From);
            var end = _grid.ToLocal(stroke.To);
            var extent = Vector2.one * (radius + _maxZoneRadius);
            var min = Vector2.Min(start, end) - extent;
            var max = Vector2.Max(start, end) + extent;

            _grid.GetCellRange(min, max, out var xMin, out var zMin, out var xMax, out var zMax);

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var index = _grid.IndexOf(x, z);
                    var kind = _grid.GetKind(index);

                    if (kind == PlantKind.None)
                    {
                        continue;
                    }

                    var reach = radius + _plantByKind[(int)kind].CutZoneRadius;
                    var coverage = ContactCoverage(start, end, _grid.CellCenter(x, z), reach);

                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    CutCell(index, kind, coverage * stroke.DeltaTime, stroke, harvested, touchedBeds);
                }
            }
        }

        private void CutCell(
              int index
            , PlantKind kind
            , float contactTime
            , in CutStroke stroke
            , List<int> harvested
            , List<int> touchedBeds
        )
        {
            ref readonly var plant = ref _plantByKind[(int)kind];

            if (plant.IsProtected)
            {
                var bed = Mathf.Max(_bedByCell[index], 0);

                _feedback.FlashProtected(index);

                if (touchedBeds.Contains(bed) == false)
                {
                    touchedBeds.Add(bed);
                }

                return;
            }

            var progress = _grid.GetProgress(index);

            if (progress >= 1f)
            {
                return;
            }

            if (plant.RequiredTier > stroke.Tier)
            {
                _feedback.FlashLocked(index);
                return;
            }

            _feedback.Shake(index);
            progress += contactTime * stroke.CuttingPower / plant.Toughness;

            if (progress >= 1f)
            {
                progress = 1f;
                harvested.Add(index);
                _feedback.MarkCut(index, Heading(stroke));
            }

            _grid.SetProgress(index, progress);
        }
    }
}
