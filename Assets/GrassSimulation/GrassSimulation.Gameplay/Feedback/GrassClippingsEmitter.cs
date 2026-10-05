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

        private ParticleSystem _particles;
        private Mesh _leafMesh;
        private int _budgetFrame;
        private int _budgetUsed;

        private static Mesh CreateLeafMesh()
        {
            var mesh = new Mesh { name = "GrassClippingLeaf" };
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

        private static float Range(Vector2 range)
            => Random.Range(range.x, range.y);

        public void Emit(Vector3 position, Vector3 away, Color color, int count)
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
                EmitOne(position, direction, color);
            }
        }

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _leafMesh = CreateLeafMesh();

            var particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = _leafMesh;
        }

        private void OnDestroy()
        {
            if (_leafMesh.IsValid())
            {
                Destroy(_leafMesh);
            }
        }

        private void EmitOne(Vector3 position, Vector3 direction, Color color)
        {
            var spread = Quaternion.Euler(0f, Random.Range(-_spreadDegrees, _spreadDegrees), 0f) * direction;
            var jitterX = Random.Range(minInclusive: -1f, maxInclusive: 1f);
            var jitterZ = Random.Range(minInclusive: -1f, maxInclusive: 1f);
            var jitter = new Vector3(jitterX, 0f, jitterZ) * _spawnJitter;
            var shade = 1f + Random.Range(-_shadeJitter, _shadeJitter);
            var tint = new Color(color.r * shade, color.g * shade, color.b * shade, 1f);

            var emitParams = new ParticleSystem.EmitParams {
                position = position + jitter + Vector3.up * _spawnHeight,
                velocity = spread * Range(_outwardSpeed) + Vector3.up * Range(_upwardSpeed),
                startColor = tint,
                startSize = Range(_size),
                startLifetime = Range(_lifetime),
                rotation3D = Random.insideUnitSphere * 180f,
                angularVelocity3D = Random.insideUnitSphere * _spinDegrees,
            };

            _particles.Emit(emitParams: emitParams, count: 1);
        }
    }
}
