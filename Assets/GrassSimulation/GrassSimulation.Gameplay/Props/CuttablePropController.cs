using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class CuttablePropController : MonoBehaviour
    {
        private const float CONTACT_SHAKE = 1f;
        private const float LOCKED_SHAKE = 0.35f;
        private const float MIN_DISTANCE = 1e-4f;
        private const float KICK_DECAY = 5f;
        private const int KIND_COUNT = (int)PropKind.Fruit + 1;
        private const float HALF_GAP = 0.05f;
        private const float DEBRIS_SHRINK_SECONDS = 0.3f;
        private const float FLIP_DEGREES = -90f;

        [SerializeField]
        private GrassClippingsEmitter _chips;

        [SerializeField]
        private FruitSliceEffects _sliceEffects;

        [SerializeField]
        private float _chipInterval = 0.1f;

        [SerializeField]
        private int _chipsPerHit = 3;

        [SerializeField]
        private int _burstChips = 14;

        [SerializeField]
        private int _burstLeaves = 16;

        [SerializeField]
        private float _breakKick = 1f;

        [SerializeField]
        private Vector2 _dropOutwardSpeed = new(1.2f, 2.4f);

        [SerializeField]
        private Vector2 _dropUpwardSpeed = new(3.5f, 5f);

        [SerializeField]
        private float _dropGravity = 14f;

        [SerializeField]
        private float _dropBounce = 0.35f;

        [SerializeField]
        private float _dropCollectDelay = 0.55f;

        [SerializeField]
        private float _dropFlySeconds = 0.3f;

        [SerializeField]
        private float _dropSpinDegrees = 540f;

        [SerializeField]
        private Vector2 _halfSideSpeed = new(1.8f, 2.8f);

        [SerializeField]
        private float _halfUpSpeed = 2.6f;

        [SerializeField]
        private float _halfFlipSeconds = 0.45f;

        [SerializeField]
        private float _halfLifetime = 1.4f;

        [SerializeField]
        private int _juiceDrops = 22;

        [SerializeField]
        private int _seeds = 8;

        [SerializeField]
        private Color _seedColor = new(0.16f, 0.12f, 0.1f);

        private readonly List<CuttableProp> _props = new();
        private readonly List<PropDrop> _drops = new();
        private readonly int[] _brokenByKind = new int[KIND_COUNT];

        private Transform _collector;
        private GameObject _layout;
        private Mesh _halfMesh;
        private float _chipTimer;

        public IReadOnlyList<CuttableProp> Props => _props;

        public float CameraKick { get; private set; }

        public bool IsTouchingLocked { get; private set; }

        public int LockedTier { get; private set; }

        private static bool IsInContact(CuttableProp prop, Vector3 blade, float radius)
        {
            var center = prop.transform.position;
            var offset = new Vector2(blade.x - center.x, blade.z - center.z);
            var reach = radius + prop.FootprintRadius * 0.5f;
            return offset.sqrMagnitude <= reach * reach;
        }

        private static float Range(Vector2 range)
            => Random.Range(range.x, range.y);

        public void Load(GameObject layout, Transform collector)
        {
            ResetAll();

            if (_layout.IsValid())
            {
                Destroy(_layout);
            }

            _collector = collector;
            _props.Clear();
            _layout = layout.IsValid() ? Instantiate(layout, transform) : null;

            if (_layout.IsValid())
            {
                _layout.GetComponentsInChildren(includeInactive: true, results: _props);
            }
        }

        public int Broken(PropKind kind)
            => _brokenByKind[(int)kind];

        public void ResetAll()
        {
            var propCount = _props.Count;

            for (var i = 0; i < propCount; i++)
            {
                _props[i].ResetState();
            }

            var dropCount = _drops.Count;

            for (var i = 0; i < dropCount; i++)
            {
                Destroy(_drops[i].target.gameObject);
            }

            _drops.Clear();
            System.Array.Clear(_brokenByKind, 0, _brokenByKind.Length);
            CameraKick = 0f;
            IsTouchingLocked = false;
        }

        public Vector3 PushOut(Vector3 position, float bodyRadius)
        {
            var propCount = _props.Count;

            for (var i = 0; i < propCount; i++)
            {
                var prop = _props[i];

                if (prop.IsBroken || prop.BlockRadius <= 0f)
                {
                    continue;
                }

                var center = prop.transform.position;
                var offset = new Vector2(position.x - center.x, position.z - center.z);
                var minDistance = prop.BlockRadius + bodyRadius;
                var distance = offset.magnitude;

                if (distance >= minDistance)
                {
                    continue;
                }

                var direction = distance > MIN_DISTANCE ? offset / distance : Vector2.right;
                var pushed = new Vector2(center.x, center.z) + direction * minDistance;
                position = new Vector3(pushed.x, position.y, pushed.y);
            }

            return position;
        }

        public int Cut(Vector3 blade, float radius, int tier, float cuttingPower, float deltaTime)
        {
            var xp = 0;
            var propCount = _props.Count;
            var emitChips = AdvanceChipTimer(deltaTime);

            IsTouchingLocked = false;

            for (var i = 0; i < propCount; i++)
            {
                var prop = _props[i];

                if (prop.IsBroken || IsInContact(prop, blade, radius) == false)
                {
                    continue;
                }

                if (prop.RequiredTier > tier)
                {
                    prop.Hit(LOCKED_SHAKE);
                    IsTouchingLocked = true;
                    LockedTier = prop.RequiredTier;
                    continue;
                }

                prop.Hit(CONTACT_SHAKE);
                prop.Progress += deltaTime * cuttingPower / prop.Toughness;

                if (emitChips)
                {
                    EmitChips(prop, blade, _chipsPerHit, prop.ChipColor);
                }

                if (prop.Progress >= 1f)
                {
                    xp += BreakProp(prop, blade);
                }
            }

            return xp;
        }

        private void OnDestroy()
        {
            if (_halfMesh.IsValid())
            {
                Destroy(_halfMesh);
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            CameraKick = Mathf.Max(CameraKick - KICK_DECAY * deltaTime, 0f);

            for (var i = _drops.Count - 1; i >= 0; i--)
            {
                var drop = _drops[i];

                if (UpdateDrop(ref drop, deltaTime))
                {
                    _drops[i] = drop;
                    continue;
                }

                Destroy(drop.target.gameObject);
                _drops.RemoveAt(i);
            }
        }

        private bool AdvanceChipTimer(float deltaTime)
        {
            _chipTimer -= deltaTime;

            if (_chipTimer > 0f)
            {
                return false;
            }

            _chipTimer = _chipInterval;
            return true;
        }

        private void EmitChips(CuttableProp prop, Vector3 from, int count, Color color)
        {
            if (_chips.IsInvalid())
            {
                return;
            }

            _chips.Emit(prop.EffectPosition, prop.transform.position - from, color, count);
        }

        private int BreakProp(CuttableProp prop, Vector3 blade)
        {
            prop.Break();
            _brokenByKind[(int)prop.Kind]++;
            CameraKick = Mathf.Max(CameraKick, _breakKick);

            if (prop.SplitOnBreak)
            {
                SplitFruit(prop, blade);
            }
            else
            {
                EmitChips(prop, blade, _burstChips, prop.ChipColor);
                EmitChips(prop, blade, _burstLeaves, prop.BurstColor);
            }

            SpawnDrops(prop);
            return prop.Xp;
        }

        private void SplitFruit(CuttableProp prop, Vector3 blade)
        {
            var toFruit = prop.transform.position - blade;
            toFruit.y = 0f;

            var side = Vector3.right;

            if (toFruit.sqrMagnitude > MIN_DISTANCE)
            {
                side = Vector3.Cross(Vector3.up, toFruit.normalized);
            }

            var center = prop.transform.position + Vector3.up * (prop.HalfSize.y * 0.5f);
            SpawnHalf(prop, center, side);
            SpawnHalf(prop, center, -side);

            if (_sliceEffects.IsValid())
            {
                var fruitSize = Mathf.Max(prop.HalfSize.x, prop.HalfSize.z);
                _sliceEffects.Play(center, toFruit, prop.JuiceColor, fruitSize);
                return;
            }

            EmitChips(prop, blade, _juiceDrops, prop.JuiceColor);
            EmitChips(prop, blade, _seeds, _seedColor);
        }

        private void SpawnHalf(CuttableProp prop, Vector3 center, Vector3 side)
        {
            if (_halfMesh.IsInvalid())
            {
                _halfMesh = FruitHalfMesh.Create();
            }

            var half = new GameObject("FruitHalf");
            var halfTransform = half.transform;
            var facing = Quaternion.FromToRotation(Vector3.right, side);
            halfTransform.SetPositionAndRotation(center + side * HALF_GAP, facing);
            halfTransform.localScale = prop.HalfSize;
            half.AddComponent<MeshFilter>().sharedMesh = _halfMesh;
            half.AddComponent<MeshRenderer>().sharedMaterials = new[] { prop.RindMaterial, prop.FleshMaterial };

            var flipAxis = Vector3.Cross(side, Vector3.up);

            _drops.Add(new PropDrop {
                target = halfTransform,
                velocity = side * Range(_halfSideSpeed) + Vector3.up * _halfUpSpeed,
                startScale = prop.HalfSize,
                startRotation = facing,
                endRotation = Quaternion.AngleAxis(FLIP_DEGREES, flipAxis) * facing,
                floor = prop.HalfSize.x * 0.5f,
                isDebris = true,
            });
        }

        private void SpawnDrops(CuttableProp prop)
        {
            var drops = prop.Drops;

            if (drops.Length == 0)
            {
                return;
            }

            var count = prop.DropCount;

            for (var i = 0; i < count; i++)
            {
                var prefab = drops[i % drops.Length];

                if (prefab.IsInvalid())
                {
                    continue;
                }

                var angle = (i + Random.value * 0.5f) / count * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var velocity = outward * Range(_dropOutwardSpeed) + Vector3.up * Range(_dropUpwardSpeed);
                var instance = Instantiate(prefab, prop.EffectPosition, Random.rotation);
                instance.transform.localScale *= prop.DropScale;

                _drops.Add(new PropDrop {
                    target = instance.transform,
                    velocity = velocity,
                    spin = Random.onUnitSphere * _dropSpinDegrees,
                    startScale = instance.transform.localScale,
                });
            }
        }

        private bool UpdateDrop(ref PropDrop drop, float deltaTime)
        {
            drop.age += deltaTime;

            var target = drop.target;
            if (drop.isDebris)
            {
                FallDrop(ref drop, deltaTime);

                var flip = Mathf.SmoothStep(0f, 1f, drop.age / _halfFlipSeconds);
                target.rotation = Quaternion.Slerp(drop.startRotation, drop.endRotation, flip);

                var remaining = _halfLifetime - drop.age;
                var shrink = Mathf.Clamp01(remaining / DEBRIS_SHRINK_SECONDS);
                target.localScale = drop.startScale * shrink;
                return remaining > 0f;
            }

            var isFalling = drop.age < _dropCollectDelay;

            if (isFalling || _collector.IsInvalid())
            {
                FallDrop(ref drop, deltaTime);
                return isFalling || drop.age < _dropCollectDelay + _dropFlySeconds;
            }

            var t = Mathf.Clamp01((drop.age - _dropCollectDelay) / _dropFlySeconds);
            var eased = t * t * (3f - 2f * t);
            var destination = _collector.position + Vector3.up * 0.4f;
            var arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.6f);

            target.position = Vector3.Lerp(drop.flyStart, destination, eased) + arc;
            target.localScale = drop.startScale * (1f - eased * 0.8f);
            target.Rotate(drop.spin * deltaTime, Space.World);
            return t < 1f;
        }

        private void FallDrop(ref PropDrop drop, float deltaTime)
        {
            drop.velocity += Vector3.down * (_dropGravity * deltaTime);

            var position = drop.target.position + drop.velocity * deltaTime;

            if (position.y < drop.floor)
            {
                position.y = drop.floor;
                drop.velocity.x *= 0.6f;
                drop.velocity.y *= -_dropBounce;
                drop.velocity.z *= 0.6f;
            }

            drop.target.position = position;
            drop.target.Rotate(drop.spin * deltaTime, Space.World);
            drop.flyStart = position;
        }

        private struct PropDrop
        {
            public Transform target;
            public Vector3 velocity;
            public Vector3 spin;
            public Vector3 startScale;
            public Vector3 flyStart;
            public Quaternion startRotation;
            public Quaternion endRotation;
            public float floor;
            public float age;
            public bool isDebris;
        }
    }
}
