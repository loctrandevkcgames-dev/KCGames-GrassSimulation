using System;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class CuttableProp : MonoBehaviour
    {
        private const float SHAKE_DECAY = 6f;
        private const float SHAKE_DEGREES = 9f;
        private const float SHAKE_FREQUENCY = 38f;
        private const float SQUASH_AMOUNT = 0.12f;
        private const float BREAK_SECONDS = 0.28f;
        private const float BREAK_POP = 0.18f;

        [SerializeField]
        private PropKind _kind;

        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private int _requiredTier = 1;

        [SerializeField]
        private float _toughness = 1f;

        [SerializeField]
        private int _xp = 5;

        [SerializeField]
        private float _footprintRadius = 0.5f;

        [SerializeField]
        private float _blockRadius = 0.25f;

        [SerializeField]
        private float _effectHeight = 0.5f;

        [SerializeField]
        private Color _chipColor = new(0.69f, 0.47f, 0.29f);

        [SerializeField]
        private Color _burstColor = new(0.36f, 0.65f, 0.24f);

        [SerializeField]
        private GameObject[] _drops = Array.Empty<GameObject>();

        [SerializeField]
        private int _dropCount = 3;

        [SerializeField]
        private float _dropScale = 1f;

        [SerializeField]
        private bool _splitOnBreak;

        [SerializeField]
        private Material _rindMaterial;

        [SerializeField]
        private Material _fleshMaterial;

        [SerializeField]
        private Vector3 _halfSize = new(0.4f, 0.4f, 0.4f);

        [SerializeField]
        private Color _juiceColor = new(0.95f, 0.3f, 0.3f);

        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private float _shake;
        private float _breakTimer;
        private float _phase;

        public PropKind Kind => _kind;

        public int RequiredTier => _requiredTier;

        public float Toughness => _toughness;

        public int Xp => _xp;

        public float FootprintRadius => _footprintRadius;

        public float BlockRadius => _blockRadius;

        public Color ChipColor => _chipColor;

        public Color BurstColor => _burstColor;

        public GameObject[] Drops => _drops;

        public int DropCount => _dropCount;

        public float DropScale => _dropScale;

        public bool SplitOnBreak => _splitOnBreak;

        public Material RindMaterial => _rindMaterial;

        public Material FleshMaterial => _fleshMaterial;

        public Vector3 HalfSize => _halfSize;

        public Color JuiceColor => _juiceColor;

        public Vector3 EffectPosition => transform.position + Vector3.up * _effectHeight;

        public float Progress { get; set; }

        public bool IsBroken { get; private set; }

        public void Hit(float strength)
        {
            _shake = Mathf.Max(_shake, strength);
        }

        public void Break()
        {
            IsBroken = true;
            _breakTimer = BREAK_SECONDS;

            if (_splitOnBreak)
            {
                gameObject.SetActive(false);
            }
        }

        public void ResetState()
        {
            Progress = 0f;
            IsBroken = false;
            _shake = 0f;
            _breakTimer = 0f;
            gameObject.SetActive(true);
            _visual.localScale = _baseScale;
            _visual.localRotation = _baseRotation;
        }

        private void Awake()
        {
            if (_visual.IsInvalid())
            {
                _visual = transform;
            }

            _baseScale = _visual.localScale;
            _baseRotation = _visual.localRotation;
            _phase = UnityEngine.Random.value * 10f;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            if (IsBroken)
            {
                AnimateBreak(deltaTime);
                return;
            }

            _shake = Mathf.Max(_shake - SHAKE_DECAY * deltaTime, 0f);
            _phase += deltaTime * SHAKE_FREQUENCY;

            var wobble = _shake * SHAKE_DEGREES;
            var tilt = Quaternion.Euler(Mathf.Sin(_phase) * wobble, 0f, Mathf.Cos(_phase * 1.17f) * wobble);
            var squash = 1f - Progress * SQUASH_AMOUNT;

            _visual.localRotation = _baseRotation * tilt;
            _visual.localScale = Vector3.Scale(_baseScale, new Vector3(1f / squash, squash, 1f / squash));
        }

        private void AnimateBreak(float deltaTime)
        {
            _breakTimer -= deltaTime;

            if (_breakTimer <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            var t = 1f - _breakTimer / BREAK_SECONDS;
            var pop = 1f + Mathf.Sin(Mathf.Min(t * 2f, 1f) * Mathf.PI) * BREAK_POP;
            var shrink = 1f - Mathf.SmoothStep(0f, 1f, t);
            _visual.localScale = _baseScale * (pop * shrink);
        }
    }
}
