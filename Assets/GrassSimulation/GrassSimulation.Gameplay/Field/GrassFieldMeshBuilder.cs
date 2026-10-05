using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Gameplay
{
    internal sealed class GrassFieldMeshBuilder
    {
        private const int BLADE_SEGMENTS = 4;
        private const float TUFT_JITTER = 0.7f;
        private const float TUFT_SPREAD = 0.5f;
        private const float CELL_COVERAGE = 1.2f;
        private const float BLADE_CURVE = 0.45f;
        private const float COMB_AMOUNT = 0.6f;
        private const float BLADE_TAPER = 0.7f;
        private const float LEAF_BASE = 0.15f;
        private const float STEM_WIDTH = 0.35f;
        private const float STEM_HEIGHT = 1.2f;
        private const float STEM_CURVE = 0.15f;
        private const float EXTRA_HEAD_CHANCE = 0.35f;
        private const int HEAD_PETALS = 6;
        private const float HEAD_INNER_RADIUS = 0.45f;
        private const float HEAD_CENTER_LIFT = 0.01f;
        private const float HEAD_CENTER_RADIUS = 0.3f;
        private const float ABSOLUTE_COLOR = 1f;
        private const int PUFF_SLICES = 8;
        private const int PUFF_RINGS = 5;
        private const float PUFF_SPREAD = 0.35f;
        private const float PUFF_SQUASH = 0.9f;
        private const float PUFF_BEND = 0.5f;
        private const float MIN_TINT = 0.88f;
        private const float TINT_RANGE = 0.3f;

        private static readonly Color s_flowerCenter = new(1f, 0.82f, 0.22f);
        private static readonly Vector3 s_combDirection = new Vector3(0.35f, 0f, 1f).normalized;

        private readonly List<Vector3> _positions = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Color> _colors = new();
        private readonly List<Vector4> _uvs = new();
        private readonly List<Vector4> _clumps = new();
        private readonly List<int> _triangles = new();

        private static float Next(System.Random random)
            => (float)random.NextDouble();

        private static float LeafWidth(float t)
            => Mathf.Sin(Mathf.PI * Mathf.Lerp(LEAF_BASE, 1f, t)) * (1f - t * BLADE_TAPER * 0.5f);

        private static Vector3 Direction(float angle)
            => new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        public bool TryBuildChunk(
              FieldGrid grid
            , RectInt chunk
            , in PlantSettings settings
            , System.Random random
            , out Mesh mesh
        )
        {
            var cellSize = grid.CellSize;

            Clear();

            for (var z = chunk.yMin; z < chunk.yMax; z++)
            {
                for (var x = chunk.xMin; x < chunk.xMax; x++)
                {
                    if (grid.GetKind(grid.IndexOf(x, z)) != settings.Kind)
                    {
                        continue;
                    }

                    var root = new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
                    var clump = new Vector4(root.x, root.y, root.z, Next(random));

                    if (settings.Shape == PlantShape.Puff)
                    {
                        AddPuffs(root, clump, cellSize, settings, random);
                    }
                    else
                    {
                        AddTuft(root, clump, cellSize, settings, random);
                    }
                }
            }

            if (_positions.Count == 0)
            {
                mesh = default;
                return false;
            }

            mesh = new Mesh { name = $"{settings.Kind} {chunk.xMin}x{chunk.yMin}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);
            mesh.SetUVs(1, _clumps);
            mesh.SetTriangles(_triangles, submesh: 0);
            mesh.RecalculateBounds();

            var bounds = mesh.bounds;
            bounds.Expand(1f);
            mesh.bounds = bounds;
            mesh.UploadMeshData(markNoLongerReadable: true);
            return true;
        }

        private void Clear()
        {
            _positions.Clear();
            _normals.Clear();
            _colors.Clear();
            _uvs.Clear();
            _clumps.Clear();
            _triangles.Clear();
        }

        private void AddVertex(Vector3 position, Vector3 normal, Color color, Vector4 uv, Vector4 clump)
        {
            _positions.Add(position);
            _normals.Add(normal);
            _colors.Add(color);
            _uvs.Add(uv);
            _clumps.Add(clump);
        }

        private void AddTuft(
              Vector3 root
            , Vector4 clump
            , float cellSize
            , in PlantSettings settings
            , System.Random random
        )
        {
            var jitter = new Vector3(Next(random) - 0.5f, 0f, Next(random) - 0.5f) * (cellSize * TUFT_JITTER);
            var center = root + jitter;
            var bladeCount = settings.BladesPerCell + random.Next(0, 2);
            var startAngle = Next(random) * Mathf.PI * 2f;

            for (var i = 0; i < bladeCount; i++)
            {
                var angle = startAngle + (i + Next(random) * 0.6f) / bladeCount * Mathf.PI * 2f;
                var outward = Direction(angle);
                var scatter = new Vector3(Next(random) - 0.5f, 0f, Next(random) - 0.5f) * (cellSize * CELL_COVERAGE);
                var basePosition = root + scatter;
                var height = Mathf.Lerp(settings.MinHeight, settings.MaxHeight, Next(random));
                var leanDirection = Vector3.Lerp(outward, s_combDirection, COMB_AMOUNT).normalized;
                var lean = leanDirection * (height * BLADE_CURVE * (0.6f + 0.4f * Next(random)));
                var tint = MIN_TINT + TINT_RANGE * Next(random);

                AddBlade(basePosition, outward, lean, height, settings.BladeWidth, tint, clump);
            }

            if (settings.HasHead == false)
            {
                return;
            }

            var headCount = Next(random) < EXTRA_HEAD_CHANCE ? 2 : 1;

            for (var i = 0; i < headCount; i++)
            {
                var outward = Direction(Next(random) * Mathf.PI * 2f);
                var basePosition = center + outward * (cellSize * TUFT_SPREAD * 0.5f * Next(random));
                var height = Mathf.Lerp(settings.MinHeight, settings.MaxHeight, Next(random)) * STEM_HEIGHT;
                var lean = outward * (height * STEM_CURVE);
                var width = settings.BladeWidth * STEM_WIDTH;

                AddBlade(basePosition, outward, lean, height, width, MIN_TINT, clump);
                AddHead(basePosition + Vector3.up * height + lean, settings, random, clump);
            }
        }

        private void AddBlade(
              Vector3 basePosition
            , Vector3 outward
            , Vector3 lean
            , float height
            , float width
            , float tint
            , Vector4 clump
        )
        {
            var first = _positions.Count;
            var side = Vector3.Cross(Vector3.up, outward) * (width * 0.5f);
            var normal = (outward + Vector3.up * 0.5f).normalized;

            for (var row = 0; row < BLADE_SEGMENTS; row++)
            {
                var t = (float)row / BLADE_SEGMENTS;
                var spine = basePosition + Vector3.up * (height * t) + lean * (t * t);
                var halfWidth = side * LeafWidth(t);
                var color = new Color(tint, tint, tint, t);

                AddVertex(spine - halfWidth, normal, color, new Vector2(0f, t), clump);
                AddVertex(spine + halfWidth, normal, color, new Vector2(1f, t), clump);
            }

            var tip = basePosition + Vector3.up * height + lean;
            AddVertex(tip, normal, new Color(tint, tint, tint, 1f), new Vector2(0.5f, 1f), clump);

            for (var row = 0; row < BLADE_SEGMENTS - 1; row++)
            {
                var left = first + row * 2;
                AddQuad(left, left + 1, left + 3, left + 2);
            }

            var lastLeft = first + (BLADE_SEGMENTS - 1) * 2;
            _triangles.Add(lastLeft);
            _triangles.Add(lastLeft + 1);
            _triangles.Add(lastLeft + 2);
        }

        private void AddHead(Vector3 center, in PlantSettings settings, System.Random random, Vector4 clump)
        {
            var headColor = settings.HeadColor;
            var startAngle = Next(random) * Mathf.PI * 2f;
            headColor.a = 1f;

            AddDisc(center, settings.HeadRadius, HEAD_INNER_RADIUS, startAngle, headColor, clump);

            var centerPosition = center + Vector3.up * HEAD_CENTER_LIFT;
            var centerRadius = settings.HeadRadius * HEAD_CENTER_RADIUS;
            AddDisc(
                  center: centerPosition
                , radius: centerRadius
                , innerRadius: 1f
                , startAngle: startAngle
                , color: s_flowerCenter
                , clump: clump
            );
        }

        private void AddDisc(
              Vector3 center
            , float radius
            , float innerRadius
            , float startAngle
            , Color color
            , Vector4 clump
        )
        {
            var first = _positions.Count;
            var pointCount = HEAD_PETALS * 2;
            var uv = new Vector4(0.5f, 0.5f, ABSOLUTE_COLOR, 0f);

            AddVertex(center, Vector3.up, color, uv, clump);

            for (var i = 0; i < pointCount; i++)
            {
                var isPetalTip = i % 2 == 0;
                var pointRadius = radius * (isPetalTip ? 1f : innerRadius);
                var direction = Direction(startAngle + (float)i / pointCount * Mathf.PI * 2f);

                AddVertex(center + direction * pointRadius, Vector3.up, color, uv, clump);
            }

            for (var i = 0; i < pointCount; i++)
            {
                _triangles.Add(first);
                _triangles.Add(first + 1 + (i + 1) % pointCount);
                _triangles.Add(first + 1 + i);
            }
        }

        private void AddPuffs(
              Vector3 root
            , Vector4 clump
            , float cellSize
            , in PlantSettings settings
            , System.Random random
        )
        {
            var puffCount = settings.BladesPerCell;

            for (var i = 0; i < puffCount; i++)
            {
                var spread = cellSize * PUFF_SPREAD * 2f;
                var offset = new Vector3(Next(random) - 0.5f, 0f, Next(random) - 0.5f) * spread;
                var radius = Mathf.Lerp(settings.MinHeight, settings.MaxHeight, Next(random)) * 0.5f;
                var center = root + offset + Vector3.up * (radius * PUFF_SQUASH * (0.5f + 0.5f * Next(random)));
                var tint = MIN_TINT + TINT_RANGE * Next(random);

                AddPuff(root, center, radius, tint, clump);
            }
        }

        private void AddPuff(Vector3 root, Vector3 center, float radius, float tint, Vector4 clump)
        {
            var first = _positions.Count;
            var columns = PUFF_SLICES + 1;
            var topHeight = center.y - root.y + radius * PUFF_SQUASH;

            for (var ring = 0; ring <= PUFF_RINGS; ring++)
            {
                var latitude = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, (float)ring / PUFF_RINGS);
                var ringRadius = Mathf.Cos(latitude);
                var ringHeight = Mathf.Sin(latitude);

                for (var slice = 0; slice < columns; slice++)
                {
                    var longitude = (float)slice / PUFF_SLICES * Mathf.PI * 2f;
                    var direction = Direction(longitude) * ringRadius + Vector3.up * ringHeight;
                    var position = center + new Vector3(direction.x, direction.y * PUFF_SQUASH, direction.z) * radius;
                    var bend = Mathf.Clamp01((position.y - root.y) / topHeight) * PUFF_BEND;
                    var uv = new Vector2((float)slice / PUFF_SLICES, (float)ring / PUFF_RINGS);

                    AddVertex(position, direction, new Color(tint, tint, tint, bend), uv, clump);
                }
            }

            for (var ring = 0; ring < PUFF_RINGS; ring++)
            {
                for (var slice = 0; slice < PUFF_SLICES; slice++)
                {
                    var bottomLeft = first + ring * columns + slice;
                    var topLeft = bottomLeft + columns;
                    AddQuad(bottomLeft, bottomLeft + 1, topLeft + 1, topLeft);
                }
            }
        }

        private void AddQuad(int a, int b, int c, int d)
        {
            _triangles.Add(a);
            _triangles.Add(c);
            _triangles.Add(b);
            _triangles.Add(a);
            _triangles.Add(d);
            _triangles.Add(c);
        }
    }
}
