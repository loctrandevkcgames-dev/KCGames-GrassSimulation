using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class GrassClippingsEmitter : MonoBehaviour
    {
        private const float LEAF_HALF_LENGTH = 0.5f;
        private const float LEAF_HALF_WIDTH = 0.28f;
        private const float LEAF_FOLD = 0.14f;
        private const int CHIP_MESH_INDEX = 0;
        private const int BLADE_VARIANTS = 3;
        private const int BLADE_SEGMENTS = 3;
        private const int BLADE_RING_VERTICES = 4;
        private const float BLADE_HALF_WIDTH = 0.2f;
        private const float BLADE_HALF_THICKNESS = 0.07f;
        private const float BLADE_BASE_SHADE = 0.6f;
        private const float BLADE_TIP_SHADE = 0.85f;
        private const float BLADE_UNDERSIDE_SHADE = 0.75f;
        private const float BLADE_CUT_SHADE = 1.25f;
        private const float MIN_DIRECTION = 1e-4f;

        [SerializeField]
        private int _maxParticlesPerFrame = 60;

        [SerializeField]
        private float _spawnHeight = 0.35f;

        [SerializeField]
        private float _spawnJitter = 0.1f;

        [SerializeField]
        private float _spreadDegrees = 55f;

        [SerializeField]
        private Vector2 _outwardSpeed = new(1.4f, 3.2f);

        [SerializeField]
        private Vector2 _upwardSpeed = new(2.6f, 4.4f);

        [SerializeField]
        private Vector2 _size = new(0.22f, 0.36f);

        [SerializeField]
        private Vector2 _lifetime = new(0.8f, 1.3f);

        [SerializeField]
        private float _spinDegrees = 900f;

        [SerializeField]
        private float _shadeJitter = 0.15f;

        [SerializeField]
        private float _hueJitter = 0.03f;

        [SerializeField]
        private float _saturationJitter = 0.12f;

        [SerializeField]
        private float _bladeSpinDegrees = 1100f;

        [SerializeField]
        private float _bladeTumbleDegrees = 120f;

        private ParticleSystem _particles;
        private Mesh[] _meshes;
        private int _budgetFrame;
        private int _budgetUsed;

        private static Mesh CreateChipMesh()
        {
            var mesh = new Mesh { name = "GrassClippingChip" };
            var back = new Vector3(0f, 0f, -LEAF_HALF_LENGTH);
            var front = new Vector3(0f, 0f, LEAF_HALF_LENGTH);
            var left = new Vector3(-LEAF_HALF_WIDTH, LEAF_FOLD, 0f);
            var right = new Vector3(LEAF_HALF_WIDTH, LEAF_FOLD, 0f);

            mesh.SetVertices(new[] { back, front, left, back, right, front });
            mesh.SetTriangles(new[] { 0, 1, 2, 3, 4, 5 }, submesh: 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBladeMesh(int variant)
        {
            var mesh = new Mesh { name = "GrassClippingBlade" + variant };
            var rings = new Vector3[(BLADE_SEGMENTS + 1) * BLADE_RING_VERTICES];
            var shades = new float[BLADE_SEGMENTS + 1];
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var bend = Random.Range(0.06f, 0.16f) * (variant % 2 == 0 ? 1f : -1f);
            var sway = Random.Range(-0.12f, 0.12f);
            var width = BLADE_HALF_WIDTH * Random.Range(0.85f, 1.15f);

            // Each ring is a diamond cross-section (left, top, right, bottom) so the chunk has real thickness.
            for (var i = 0; i <= BLADE_SEGMENTS; i++)
            {
                var t = i / (float)BLADE_SEGMENTS;
                var z = (t - 0.5f) * 2f * LEAF_HALF_LENGTH;
                var half = width * (1f - Mathf.Pow(t, 1.6f));
                var thick = BLADE_HALF_THICKNESS * half / width;
                var center = new Vector3(sway * t * t, bend * t * t, z);
                var ring = i * BLADE_RING_VERTICES;

                rings[ring] = center + new Vector3(-half, 0f, 0f);
                rings[ring + 1] = center + new Vector3(0f, thick, 0f);
                rings[ring + 2] = center + new Vector3(half, 0f, 0f);
                rings[ring + 3] = center + new Vector3(0f, -thick, 0f);
                shades[i] = Mathf.Lerp(BLADE_BASE_SHADE, BLADE_TIP_SHADE, t);
            }

            for (var i = 0; i < BLADE_SEGMENTS; i++)
            {
                var near = i * BLADE_RING_VERTICES;
                var far = near + BLADE_RING_VERTICES;

                for (var side = 0; side < BLADE_RING_VERTICES; side++)
                {
                    var next = (side + 1) % BLADE_RING_VERTICES;
                    var underside = side >= 2 ? BLADE_UNDERSIDE_SHADE : 1f;
                    var nearColor = BladeColor(shades[i] * underside);
                    var farColor = BladeColor(shades[i + 1] * underside);

                    AddFacet(vertices, colors, rings[near + side], rings[far + side], rings[near + next], nearColor, farColor, nearColor);
                    AddFacet(vertices, colors, rings[near + next], rings[far + side], rings[far + next], nearColor, farColor, farColor);
                }
            }

            // The cut end shows the paler inside of the leaf.
            var cut = BladeColor(BLADE_BASE_SHADE * BLADE_CUT_SHADE);
            AddFacet(vertices, colors, rings[0], rings[1], rings[2], cut, cut, cut);
            AddFacet(vertices, colors, rings[0], rings[3], rings[2], cut, cut, cut);

            var triangles = new int[vertices.Count];

            for (var i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, submesh: 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Color BladeColor(float shade)
            => new(shade, shade, shade * 0.88f, 1f);

        private static void AddFacet(
              List<Vector3> vertices
            , List<Color> colors
            , Vector3 a
            , Vector3 b
            , Vector3 c
            , Color colorA
            , Color colorB
            , Color colorC
        )
        {
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            colors.Add(colorA);
            colors.Add(colorB);
            colors.Add(colorC);
        }

        private static float Range(Vector2 range)
            => Random.Range(range.x, range.y);

        public void Emit(
              Vector3 position
            , Vector3 away
            , Color color
            , int count
            , ClippingShape shape = ClippingShape.Blade
        )
        {
            var frame = Time.frameCount;

            if (_budgetFrame != frame)
            {
                _budgetFrame = frame;
                _budgetUsed = 0;
            }

            var allowed = Mathf.Min(count, _maxParticlesPerFrame - _budgetUsed);

            if (allowed <= 0 || _particles.IsInvalid())
            {
                return;
            }

            _budgetUsed += allowed;
            away.y = 0f;

            var direction = away.sqrMagnitude > MIN_DIRECTION ? away.normalized : Vector3.forward;

            for (var i = 0; i < allowed; i++)
            {
                EmitOne(position, direction, color, shape);
            }
        }

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _meshes = new Mesh[1 + BLADE_VARIANTS];
            _meshes[CHIP_MESH_INDEX] = CreateChipMesh();

            for (var i = 0; i < BLADE_VARIANTS; i++)
            {
                _meshes[CHIP_MESH_INDEX + 1 + i] = CreateBladeMesh(i);
            }

            var particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.SetMeshes(_meshes);
        }

        private void OnDestroy()
        {
            if (_meshes == null)
            {
                return;
            }

            foreach (var mesh in _meshes)
            {
                if (mesh.IsValid())
                {
                    Destroy(mesh);
                }
            }
        }

        private void EmitOne(Vector3 position, Vector3 direction, Color color, ClippingShape shape)
        {
            var spread = Quaternion.Euler(0f, Random.Range(-_spreadDegrees, _spreadDegrees), 0f) * direction;
            var jitterX = Random.Range(minInclusive: -1f, maxInclusive: 1f);
            var jitterZ = Random.Range(minInclusive: -1f, maxInclusive: 1f);
            var jitter = new Vector3(jitterX, 0f, jitterZ) * _spawnJitter;
            var isBlade = shape == ClippingShape.Blade;

            var emitParams = new ParticleSystem.EmitParams {
                position = position + jitter + Vector3.up * _spawnHeight,
                velocity = spread * Range(_outwardSpeed) + Vector3.up * Range(_upwardSpeed),
                startColor = Tint(color, isBlade),
                startSize = Range(_size),
                startLifetime = Range(_lifetime),
            };

            if (isBlade)
            {
                var spin = Random.value < 0.5f ? -_bladeSpinDegrees : _bladeSpinDegrees;

                emitParams.meshIndex = CHIP_MESH_INDEX + 1 + Random.Range(0, BLADE_VARIANTS);
                emitParams.rotation3D = new Vector3(
                      Random.Range(-60f, 60f)
                    , Random.Range(0f, 360f)
                    , Random.Range(0f, 360f)
                );
                emitParams.angularVelocity3D = new Vector3(
                      Random.Range(-_bladeTumbleDegrees, _bladeTumbleDegrees)
                    , Random.Range(-_bladeTumbleDegrees, _bladeTumbleDegrees)
                    , spin * Random.Range(0.6f, 1f)
                );
            }
            else
            {
                emitParams.meshIndex = CHIP_MESH_INDEX;
                emitParams.rotation3D = Random.insideUnitSphere * 180f;
                emitParams.angularVelocity3D = Random.insideUnitSphere * _spinDegrees;
            }

            _particles.Emit(emitParams: emitParams, count: 1);
        }

        private Color Tint(Color color, bool jitterHue)
        {
            var shade = 1f + Random.Range(-_shadeJitter, _shadeJitter);

            if (!jitterHue)
            {
                return new Color(color.r * shade, color.g * shade, color.b * shade, 1f);
            }

            Color.RGBToHSV(color, out var h, out var s, out var v);

            h = Mathf.Repeat(h + Random.Range(-_hueJitter, _hueJitter), 1f);
            s = Mathf.Clamp01(s + Random.Range(-_saturationJitter, _saturationJitter));
            v = Mathf.Clamp01(v * shade);

            var tint = Color.HSVToRGB(h, s, v);
            tint.a = 1f;
            return tint;
        }
    }
}
