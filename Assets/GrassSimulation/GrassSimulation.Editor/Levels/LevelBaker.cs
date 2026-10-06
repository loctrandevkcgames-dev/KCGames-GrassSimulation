using System;
using System.Collections.Generic;
using EncosyTower.Common;
using GrassSimulation.Gameplay;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace GrassSimulation.Editor
{
    public static class LevelBaker
    {
        public const float CELL_SIZE = 0.5f;
        public const float SPAWN_CLEARING = 1f;

        private const float OBJECT_GAP = 0.2f;
        private const float OBJECT_INSET = 0.3f;
        private const float OBSTACLE_DISTANCE = 0.3f;
        private const float EDGE_NOISE = 1.2f;
        private const int CANDIDATES = 24;
        private const int ATTEMPTS = 400;
        private const int CELLS_PER_SEED = 100;
        private const int MAX_SEEDS = 3;
        private const int STAGE_OBSTACLES = 1;
        private const int STAGE_FIELD = 100;
        private const int STAGE_OBJECTS = 200;
        private const int STAGE_SHUFFLE = 300;

        private static readonly (char Letter, PlantKind Kind)[] s_fieldLetters = {
            (LayoutText.GRASS, PlantKind.Grass),
            (LayoutText.FLOWER, PlantKind.HarvestFlower),
            (LayoutText.THICK, PlantKind.ThickGrass),
        };

        private static readonly (char Letter, PlantKind Kind)[] s_objectLetters = {
            (LayoutText.BUSH_LOW, PlantKind.BushLow),
            (LayoutText.VEGETABLE, PlantKind.Vegetable),
            (LayoutText.BUSH_BIG, PlantKind.BushBig),
            (LayoutText.MELON, PlantKind.Melon),
            (LayoutText.TREE, PlantKind.FruitTree),
            (LayoutText.GIANT, PlantKind.GiantFruit),
        };

        public static Result<BakedLevel, LevelBuildError> Bake(
              LevelSpec spec
            , LayoutText layout
            , ReadOnlySpan<PlantDefinition> plants
        )
        {
            var seed = layout.Seed != 0 ? layout.Seed : spec.Order;
            var cellsPerMeter = Mathf.RoundToInt(1f / CELL_SIZE);
            var cellsX = layout.Width * cellsPerMeter;
            var cellsZ = layout.Height * cellsPerMeter;
            var spawn = new Vector2(layout.Spawn.x + 0.5f, layout.Spawn.y + 0.5f);
            var obstacles = BuildObstacles(layout, seed);
            var blocked = BuildBlocked(cellsX, cellsZ, spawn, obstacles);
            var kinds = new byte[cellsX * cellsZ];
            var beds = BuildBeds(layout, cellsPerMeter);

            if (TryFillBeds(kinds, blocked, beds, cellsX) == false)
            {
                return Fail(spec.Id, "a protected bed overlaps an obstacle or the spawn clearing.");
            }

            for (var i = 0; i < s_fieldLetters.Length; i++)
            {
                var (letter, kind) = s_fieldLetters[i];
                var region = CollectRegion(layout, letter, blocked, cellsX, cellsPerMeter);
                var count = spec.GetCount(kind);

                if (count > region.Count)
                {
                    return Result<BakedLevel, LevelBuildError>.Err(
                        new LevelBuildError.CapacityExceeded(spec.Id, kind, count, region.Count)
                    );
                }

                PickCells(region, count, cellsX, Hash(seed, STAGE_FIELD + i), kinds, kind);
            }

            var placed = new List<PlantPlacement>();
            var objectResult = PlaceObjects(spec, layout, plants, spawn, obstacles, seed, placed);

            if (objectResult.TryGetFailure(out var failure))
            {
                return Result<BakedLevel, LevelBuildError>.Err(failure);
            }

            Shuffle(placed, Hash(seed, STAGE_SHUFFLE));

            return Result<BakedLevel, LevelBuildError>.Succeed(new BakedLevel(
                  seed
                , cellsX
                , cellsZ
                , CELL_SIZE
                , kinds
                , placed.ToArray()
                , obstacles
                , beds
                , spawn
                , SPAWN_CLEARING
            ));
        }

        private static uint Hash(int seed, int stage)
            => (uint)(seed * 7919 + stage * 104729);

        private static Result<BakedLevel, LevelBuildError> Fail(string id, string reason)
            => Result<BakedLevel, LevelBuildError>.Err(new LevelBuildError.LayoutInvalid(id, reason));

        private static ObstaclePlacement[] BuildObstacles(LayoutText layout, int seed)
        {
            var random = Random.CreateFromIndex(Hash(seed, STAGE_OBSTACLES));
            var obstacles = new List<ObstaclePlacement>();

            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    if (layout.At(x, y) == LayoutText.ROCK)
                    {
                        obstacles.Add(new ObstaclePlacement {
                            Kind = ObstacleKind.Rock,
                            Position = new Vector2(x + 0.5f, y + 0.5f),
                            Yaw = random.NextFloat(0f, 360f),
                            Length = 1f,
                        });
                    }
                }
            }

            AddFenceRuns(layout, obstacles);
            return obstacles.ToArray();
        }

        private static void AddFenceRuns(LayoutText layout, List<ObstaclePlacement> obstacles)
        {
            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var isFence = layout.At(x, y) == LayoutText.FENCE;

                    if (isFence == false)
                    {
                        continue;
                    }

                    var hasLeft = layout.At(x - 1, y) == LayoutText.FENCE;
                    var hasBelow = layout.At(x, y - 1) == LayoutText.FENCE;
                    var hasRight = layout.At(x + 1, y) == LayoutText.FENCE;
                    var hasAbove = layout.At(x, y + 1) == LayoutText.FENCE;

                    if (hasLeft == false && hasRight)
                    {
                        obstacles.Add(MeasureRun(layout, x, y, isHorizontal: true));
                    }

                    if (hasBelow == false && hasAbove)
                    {
                        obstacles.Add(MeasureRun(layout, x, y, isHorizontal: false));
                    }

                    if (hasLeft == false && hasRight == false && hasBelow == false && hasAbove == false)
                    {
                        obstacles.Add(MeasureRun(layout, x, y, isHorizontal: true));
                    }
                }
            }
        }

        private static ObstaclePlacement MeasureRun(LayoutText layout, int x, int y, bool isHorizontal)
        {
            var step = isHorizontal ? Vector2Int.right : Vector2Int.up;
            var length = 1;

            while (layout.At(x + step.x * length, y + step.y * length) == LayoutText.FENCE)
            {
                length++;
            }

            var center = new Vector2(x + 0.5f, y + 0.5f) + (Vector2)step * ((length - 1) * 0.5f);

            return new ObstaclePlacement {
                Kind = ObstacleKind.Fence,
                Position = center,
                Yaw = isHorizontal ? 0f : 90f,
                Length = length,
            };
        }

        private static bool[] BuildBlocked(int cellsX, int cellsZ, Vector2 spawn, ObstaclePlacement[] obstacles)
        {
            var grid = new FieldGrid(cellsX, cellsZ, CELL_SIZE);

            grid.Fill(new RectInt(0, 0, cellsX, cellsZ), PlantKind.Grass);
            grid.ClearCircle(spawn, SPAWN_CLEARING);

            for (var i = 0; i < obstacles.Length; i++)
            {
                var shape = ObstacleShape.From(in obstacles[i], Vector2.zero);

                grid.ClearCapsule(shape.A, shape.B, shape.Radius);
            }

            var blocked = new bool[grid.Count];

            for (var i = 0; i < blocked.Length; i++)
            {
                blocked[i] = grid.GetKind(i) == PlantKind.None;
            }

            return blocked;
        }

        private static RectInt[] BuildBeds(LayoutText layout, int cellsPerMeter)
        {
            var beds = new RectInt[layout.Beds.Length];

            for (var i = 0; i < beds.Length; i++)
            {
                var bed = layout.Beds[i];

                beds[i] = new RectInt(
                      bed.x * cellsPerMeter
                    , bed.y * cellsPerMeter
                    , bed.width * cellsPerMeter
                    , bed.height * cellsPerMeter
                );
            }

            return beds;
        }

        private static bool TryFillBeds(byte[] kinds, bool[] blocked, RectInt[] beds, int cellsX)
        {
            for (var i = 0; i < beds.Length; i++)
            {
                var bed = beds[i];

                for (var z = bed.yMin; z < bed.yMax; z++)
                {
                    for (var x = bed.xMin; x < bed.xMax; x++)
                    {
                        if (blocked[z * cellsX + x])
                        {
                            return false;
                        }

                        kinds[z * cellsX + x] = (byte)PlantKind.ProtectedFlower;
                    }
                }
            }

            return true;
        }

        private static List<int> CollectRegion(
              LayoutText layout
            , char letter
            , bool[] blocked
            , int cellsX
            , int cellsPerMeter
        )
        {
            var region = new List<int>();

            for (var index = 0; index < blocked.Length; index++)
            {
                var x = index % cellsX;
                var z = index / cellsX;

                if (blocked[index] == false && layout.At(x / cellsPerMeter, z / cellsPerMeter) == letter)
                {
                    region.Add(index);
                }
            }

            return region;
        }

        private static void PickCells(
              List<int> region
            , int count
            , int cellsX
            , uint stream
            , byte[] kinds
            , PlantKind kind
        )
        {
            if (count <= 0)
            {
                return;
            }

            var chosen = count == region.Count ? region : SelectCluster(region, count, cellsX, stream);

            for (var i = 0; i < chosen.Count; i++)
            {
                kinds[chosen[i]] = (byte)kind;
            }
        }

        private static List<int> SelectCluster(List<int> region, int count, int cellsX, uint stream)
        {
            var random = Random.CreateFromIndex(stream);
            var seeds = FindSeeds(region, cellsX, Mathf.Clamp(count / CELLS_PER_SEED, 1, MAX_SEEDS));
            var scores = new float[region.Count];

            for (var i = 0; i < region.Count; i++)
            {
                var cell = new Vector2(region[i] % cellsX, region[i] / cellsX);
                var nearest = float.MaxValue;

                for (var s = 0; s < seeds.Count; s++)
                {
                    nearest = Mathf.Min(nearest, Vector2.Distance(cell, seeds[s]));
                }

                scores[i] = nearest + random.NextFloat(0f, EDGE_NOISE);
            }

            var order = region.ToArray();

            Array.Sort(scores, order);
            return new List<int>(new ArraySegment<int>(order, 0, count));
        }

        private static List<Vector2> FindSeeds(List<int> region, int cellsX, int seedCount)
        {
            var centroid = Vector2.zero;

            for (var i = 0; i < region.Count; i++)
            {
                centroid += new Vector2(region[i] % cellsX, region[i] / cellsX);
            }

            centroid /= region.Count;

            var seeds = new List<Vector2>();

            while (seeds.Count < seedCount)
            {
                seeds.Add(FarthestCell(region, cellsX, seeds, centroid));
            }

            return seeds;
        }

        private static Vector2 FarthestCell(List<int> region, int cellsX, List<Vector2> seeds, Vector2 centroid)
        {
            var best = centroid;
            var bestScore = seeds.Count == 0 ? float.MaxValue : -1f;

            for (var i = 0; i < region.Count; i++)
            {
                var cell = new Vector2(region[i] % cellsX, region[i] / cellsX);
                var score = seeds.Count == 0 ? Vector2.Distance(cell, centroid) : MinDistance(cell, seeds);
                var isBetter = seeds.Count == 0 ? score < bestScore : score > bestScore;

                if (isBetter)
                {
                    best = cell;
                    bestScore = score;
                }
            }

            return best;
        }

        private static float MinDistance(Vector2 cell, List<Vector2> seeds)
        {
            var nearest = float.MaxValue;

            for (var i = 0; i < seeds.Count; i++)
            {
                nearest = Mathf.Min(nearest, Vector2.Distance(cell, seeds[i]));
            }

            return nearest;
        }

        private static Success<LevelBuildError> PlaceObjects(
              LevelSpec spec
            , LayoutText layout
            , ReadOnlySpan<PlantDefinition> plants
            , Vector2 spawn
            , ObstaclePlacement[] obstacles
            , int seed
            , List<PlantPlacement> placed
        )
        {
            var radii = new float[PlantKindExtensions.Length];

            for (var i = 0; i < plants.Length; i++)
            {
                radii[(int)plants[i].Kind] = plants[i].CutZoneRadius;
            }

            for (var i = 0; i < s_objectLetters.Length; i++)
            {
                var (letter, kind) = s_objectLetters[i];
                var count = spec.GetCount(kind);
                var random = Random.CreateFromIndex(Hash(seed, STAGE_OBJECTS + i));
                var cells = CollectLetterCells(layout, letter);
                var context = new PlacementContext(layout, letter, kind, radii, spawn, obstacles, cells);

                for (var n = 0; n < count; n++)
                {
                    if (TryPlaceOne(context, placed, ref random, out var placement) == false)
                    {
                        return Success.No<LevelBuildError>(
                            new LevelBuildError.PlacementFailed(spec.Id, kind, n, count)
                        );
                    }

                    placed.Add(placement);
                }
            }

            return Success.Yes;
        }

        private static List<Vector2Int> CollectLetterCells(LayoutText layout, char letter)
        {
            var cells = new List<Vector2Int>();

            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    if (layout.At(x, y) == letter)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        private static bool TryPlaceOne(
              in PlacementContext context
            , List<PlantPlacement> placed
            , ref Random random
            , out PlantPlacement placement
        )
        {
            var found = false;
            var best = Vector2.zero;
            var bestDistance = -1f;

            for (var attempt = 0; attempt < ATTEMPTS && (found == false || attempt < CANDIDATES); attempt++)
            {
                var candidate = PickPoint(in context, ref random);

                var distance = ScoreCandidate(in context, candidate, placed);

                if (distance > bestDistance)
                {
                    found = distance >= 0f;
                    best = candidate;
                    bestDistance = distance;
                }
            }

            placement = new PlantPlacement {
                Kind = context.Kind,
                Position = best,
                FruitCount = context.Layout.FruitCounts.TryGetValue(context.Letter, out var fruits) ? fruits : 0,
                Yaw = random.NextFloat(0f, 360f),
                Scale = random.NextFloat(0.92f, 1.08f),
            };

            return found;
        }

        private static Vector2 PickPoint(in PlacementContext context, ref Random random)
        {
            if (context.Cells.Count == 0)
            {
                return Vector2.zero;
            }

            var cell = context.Cells[random.NextInt(0, context.Cells.Count)];

            return new Vector2(cell.x + random.NextFloat(), cell.y + random.NextFloat());
        }

        private static float ScoreCandidate(in PlacementContext context, Vector2 point, List<PlantPlacement> placed)
        {
            if (IsInsideRegion(in context, point) == false)
            {
                return -1f;
            }

            var radius = context.Radii[(int)context.Kind];

            if (Vector2.Distance(point, context.Spawn) < SPAWN_CLEARING + radius)
            {
                return -1f;
            }

            for (var i = 0; i < context.Obstacles.Length; i++)
            {
                var shape = ObstacleShape.From(in context.Obstacles[i], Vector2.zero);

                if (Vector2.Distance(point, shape.Closest(point)) < shape.Radius + OBSTACLE_DISTANCE)
                {
                    return -1f;
                }
            }

            var margin = float.MaxValue;

            for (var i = 0; i < placed.Count; i++)
            {
                var other = placed[i];
                var needed = radius + context.Radii[(int)other.Kind] + OBJECT_GAP;
                var slack = Vector2.Distance(point, other.Position) - needed;

                if (slack < 0f)
                {
                    return -1f;
                }

                margin = Mathf.Min(margin, slack);
            }

            return margin;
        }

        private static bool IsInsideRegion(in PlacementContext context, Vector2 point)
        {
            return Matches(in context, point)
                && Matches(in context, point + new Vector2(OBJECT_INSET, 0f))
                && Matches(in context, point - new Vector2(OBJECT_INSET, 0f))
                && Matches(in context, point + new Vector2(0f, OBJECT_INSET))
                && Matches(in context, point - new Vector2(0f, OBJECT_INSET));
        }

        private static bool Matches(in PlacementContext context, Vector2 point)
            => context.Layout.At(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)) == context.Letter;

        private static void Shuffle(List<PlantPlacement> placements, uint stream)
        {
            var random = Random.CreateFromIndex(stream);

            for (var i = placements.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(0, i + 1);

                (placements[i], placements[j]) = (placements[j], placements[i]);
            }
        }

        private readonly record struct PlacementContext(
              LayoutText Layout
            , char Letter
            , PlantKind Kind
            , float[] Radii
            , Vector2 Spawn
            , ObstaclePlacement[] Obstacles
            , List<Vector2Int> Cells
        );
    }
}
