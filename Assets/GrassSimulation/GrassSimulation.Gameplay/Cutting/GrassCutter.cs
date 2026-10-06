using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassCutter
    {
        private const int NO_BED = -1;

        private readonly PlantDefinition[] _plantByKind = new PlantDefinition[PlantKindExtensions.Length];
        private readonly FieldGrid _grid;
        private readonly FieldFeedback _feedback;
        private readonly ObstacleField _obstacles;
        private readonly int[] _bedByCell;
        private readonly float _maxZoneRadius;

        public GrassCutter(
              FieldGrid grid
            , FieldFeedback feedback
            , ReadOnlySpan<PlantDefinition> plants
            , ReadOnlySpan<RectInt> beds = default
            , ObstacleField obstacles = null
        )
        {
            _grid = grid;
            _feedback = feedback;
            _obstacles = obstacles;

            var plantCount = plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = plants[i];

                if (plant.Representation == PlantRepresentation.Object)
                {
                    continue;
                }

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

        private void ReportLocked(PlantContactReport contacts, PlantKind kind, int index, int requiredTier)
        {
            if (contacts != null)
            {
                contacts.AddLocked(kind, _grid.ToWorld(_grid.CellCenter(index)), requiredTier);
            }
        }

        private void ReportCut(
              PlantContactReport contacts
            , PlantKind kind
            , int index
            , in PlantDefinition plant
            , in CutStroke stroke
        )
        {
            if (contacts == null)
            {
                return;
            }

            var centerSpeed = CutMath.CenterSpeed(
                  stroke.Radius
                , plant.CutZoneRadius
                , stroke.CuttingPower
                , plant.Toughness
            );

            contacts.AddCut(kind, _grid.ToWorld(_grid.CellCenter(index)), in stroke, centerSpeed);
        }

        private bool IsOccluded(Vector2 start, Vector2 end, Vector2 target)
        {
            var blade = CutMath.ClosestPoint(start, end, target);

            return _obstacles.IsOccluded(blade, target);
        }

        private static Vector2 Heading(in CutStroke stroke)
            => new(stroke.To.x - stroke.From.x, stroke.To.z - stroke.From.z);

        public ref readonly PlantDefinition GetPlant(PlantKind kind)
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
            Cut(in stroke, harvested, touchedBeds, contacts: null);
        }

        public void Cut(
              in CutStroke stroke
            , List<int> harvested
            , List<int> touchedBeds
            , PlantContactReport contacts
        )
        {
            var radius = stroke.Radius;
            var start = _grid.ToLocal(stroke.From);
            var end = _grid.ToLocal(stroke.To);
            var extent = Vector2.one * (radius + _maxZoneRadius);
            var min = Vector2.Min(start, end) - extent;
            var max = Vector2.Max(start, end) + extent;

            _grid.GetCellRange(min, max, out var xMin, out var zMin, out var xMax, out var zMax);

            var origin = _grid.Origin;
            var isObstructed = _obstacles != null && _obstacles.HasObstacleNear(min + origin, max + origin);

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

                    var center = _grid.CellCenter(x, z);
                    var reach = radius + _plantByKind[(int)kind].CutZoneRadius;
                    var coverage = CutMath.ContactCoverage(start, end, center, reach);

                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    if (isObstructed && IsOccluded(start + origin, end + origin, center + origin))
                    {
                        continue;
                    }

                    CutCell(index, kind, coverage * stroke.DeltaTime, stroke, harvested, touchedBeds, contacts);
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
            , PlantContactReport contacts
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
                ReportLocked(contacts, kind, index, plant.RequiredTier);
                return;
            }

            _feedback.Shake(index);
            ReportCut(contacts, kind, index, in plant, in stroke);
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
