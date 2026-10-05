using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class FruitSliceEffects : MonoBehaviour
    {
        private const float MIN_DIRECTION = 1e-4f;
        private const float SPLAT_HEIGHT = 0.02f;
        private const float SLASH_GROW = 0.3f;
        private const float SLASH_START = 0.4f;
        private const int SLASH_POINTS = 8;

        [SerializeField]
        private ParticleSystem _droplets;

        [SerializeField]
        private ParticleSystem _splats;

        [SerializeField]
        private Material _slashMaterial;

        [SerializeField]
        private int _dropletCount = 26;

        [SerializeField]
        private int _mistCount = 14;

        [SerializeField]
        private Vector2 _dropletSize = new(0.08f, 0.2f);

        [SerializeField]
        private Vector2 _mistSize = new(0.03f, 0.06f);

        [SerializeField]
        private Vector2 _dropletSpeed = new(1.4f, 4.2f);

        [SerializeField]
        private Vector2 _dropletUpSpeed = new(1.5f, 4f);

        [SerializeField]
        private Vector2 _dropletLifetime = new(0.45f, 0.85f);

        [SerializeField]
        private int _splatCount = 2;

        [SerializeField]
        private Vector2 _splatScale = new(1.6f, 2.6f);

        [SerializeField]
        private float _splatLifetime = 4f;

        [SerializeField]
        private float _slashSeconds = 0.26f;

        [SerializeField]
        private float _slashLength = 2.2f;

        [SerializeField]
        private float _slashWidth = 0.2f;

        [SerializeField]
        private float _slashHeight = 0.35f;

        private readonly List<Slash> _slashes = new();

        private static float Range(Vector2 range)
            => Random.Range(range.x, range.y);

        public void Play(Vector3 center, Vector3 cutDirection, Color juice, float fruitSize)
        {
            cutDirection.y = 0f;

            var direction = Vector3.forward;

            if (cutDirection.sqrMagnitude > MIN_DIRECTION)
            {
                direction = cutDirection.normalized;
            }

            EmitDroplets(center, juice, _dropletCount, _dropletSize);
            EmitDroplets(center, juice, _mistCount, _mistSize);
            EmitSplats(center, juice, fruitSize);
            SpawnSlash(center, direction, fruitSize);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            for (var i = _slashes.Count - 1; i >= 0; i--)
            {
                var slash = _slashes[i];
                slash.age += deltaTime;

                if (slash.age >= _slashSeconds || slash.line.IsInvalid())
                {
                    if (slash.line.IsValid())
                    {
                        Destroy(slash.line.gameObject);
                    }

                    _slashes.RemoveAt(i);
                    continue;
                }

                AnimateSlash(slash);
                _slashes[i] = slash;
            }
        }

        private void EmitDroplets(Vector3 center, Color juice, int count, Vector2 size)
        {
            if (_droplets.IsInvalid())
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var outward = Random.insideUnitCircle.normalized;
                var velocity = new Vector3(outward.x, 0f, outward.y) * Range(_dropletSpeed);
                var shade = Random.Range(minInclusive: 0.85f, maxInclusive: 1.1f);

                var emitParams = new ParticleSystem.EmitParams {
                    position = center + Random.insideUnitSphere * 0.08f,
                    velocity = velocity + Vector3.up * Range(_dropletUpSpeed),
                    startColor = new Color(juice.r * shade, juice.g * shade, juice.b * shade, 1f),
                    startSize = Range(size),
                    startLifetime = Range(_dropletLifetime),
                };

                _droplets.Emit(emitParams: emitParams, count: 1);
            }
        }

        private void EmitSplats(Vector3 center, Color juice, float fruitSize)
        {
            if (_splats.IsInvalid())
            {
                return;
            }

            for (var i = 0; i < _splatCount; i++)
            {
                var offset = Random.insideUnitCircle * (fruitSize * 0.35f);

                var emitParams = new ParticleSystem.EmitParams {
                    position = new Vector3(center.x + offset.x, SPLAT_HEIGHT + i * 0.001f, center.z + offset.y),
                    velocity = Vector3.zero,
                    startColor = new Color(juice.r, juice.g, juice.b, 0.9f),
                    startSize = fruitSize * Range(_splatScale) * (i == 0 ? 1f : 0.55f),
                    rotation = Random.Range(minInclusive: 0f, maxInclusive: 360f),
                    startLifetime = _splatLifetime,
                };

                _splats.Emit(emitParams: emitParams, count: 1);
            }
        }

        private void SpawnSlash(Vector3 center, Vector3 direction, float fruitSize)
        {
            if (_slashMaterial.IsInvalid())
            {
                return;
            }

            var tilt = Quaternion.AngleAxis(Random.Range(minInclusive: -25f, maxInclusive: 25f), Vector3.up);
            var along = tilt * direction * (fruitSize * _slashLength * 0.5f);
            var middle = center + Vector3.up * _slashHeight;

            var go = new GameObject("FruitSlash");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _slashMaterial;
            line.positionCount = SLASH_POINTS;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

            var slash = new Slash { line = line, start = middle - along, end = middle + along };
            AnimateSlash(slash);
            _slashes.Add(slash);
        }

        private void AnimateSlash(Slash slash)
        {
            var t = slash.age / _slashSeconds;
            var grow = Mathf.Clamp01(SLASH_START + t / SLASH_GROW);
            var fade = 1f - Mathf.Clamp01((t - SLASH_GROW) / (1f - SLASH_GROW));
            var head = Vector3.Lerp(slash.start, slash.end, grow);
            var tail = Vector3.Lerp(slash.start, slash.end, Mathf.Clamp01(t - SLASH_GROW));

            for (var i = 0; i < SLASH_POINTS; i++)
            {
                slash.line.SetPosition(i, Vector3.Lerp(tail, head, (float)i / (SLASH_POINTS - 1)));
            }

            slash.line.widthMultiplier = _slashWidth * (0.4f + 0.6f * fade);
            slash.line.startColor = new Color(1f, 1f, 1f, fade);
            slash.line.endColor = new Color(1f, 1f, 1f, fade);
        }

        private struct Slash
        {
            public LineRenderer line;
            public Vector3 start;
            public Vector3 end;
            public float age;
        }
    }
}
