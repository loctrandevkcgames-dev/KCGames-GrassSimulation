using EncosyTower.UnityExtensions;
using TMPro;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class PlantObjectView : MonoBehaviour
    {
        private const float SHAKE_DECAY = 6f;
        private const float SHAKE_DEGREES = 9f;
        private const float SHAKE_FREQUENCY = 38f;
        private const float SQUASH_AMOUNT = 0.18f;
        private const float SHRINK_AMOUNT = 0.12f;
        private const float POP_AMOUNT = 0.18f;
        private const float MIN_DIRECTION = 1e-4f;
        private const float LABEL_FADE_SECONDS = 0.6f;

        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private GameObject _stump;

        [SerializeField]
        private TMP_Text _countLabel;

        [SerializeField]
        private bool _falls;

        [SerializeField]
        private float _effectHeight = 0.5f;

        [SerializeField]
        private float _effectSize = 0.5f;

        [SerializeField]
        private float _harvestSeconds = 0.3f;

        [SerializeField]
        private float _fallDegrees = 80f;

        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Vector3 _awayDirection;
        private float _lastProgress;
        private float _shake;
        private float _phase;
        private float _harvestAge;
        private bool _isHarvested;
        private bool _isInitialized;

        public bool IsHarvested => _isHarvested;

        public bool IsCutting { get; private set; }

        public float EffectSize => _effectSize;

        public Vector3 EffectPosition => transform.position + Vector3.up * _effectHeight;

        public void Setup(int fruitCount)
        {
            Initialize();
            ResetState();

            if (_countLabel.IsValid())
            {
                var hasLabel = fruitCount > 0;

                _countLabel.gameObject.SetActive(hasLabel);
                _countLabel.text = $"x{fruitCount}";
            }
        }

        public void SetProgress(float progress)
        {
            IsCutting = progress > _lastProgress;
            _lastProgress = progress;

            if (IsCutting)
            {
                _shake = 1f;
            }
        }

        public void Harvest(Vector3 blade)
        {
            _isHarvested = true;
            _harvestAge = 0f;

            var away = transform.position - blade;

            away.y = 0f;
            _awayDirection = away.sqrMagnitude > MIN_DIRECTION ? away.normalized : Vector3.forward;

            if (_stump.IsValid())
            {
                _stump.SetActive(true);
            }
        }

        public void Face(Quaternion rotation)
        {
            if (_countLabel.IsValid() && _countLabel.gameObject.activeSelf)
            {
                _countLabel.transform.rotation = rotation;
            }
        }

        public void ResetState()
        {
            _isHarvested = false;
            IsCutting = false;
            _lastProgress = 0f;
            _shake = 0f;
            _harvestAge = 0f;

            if (_stump.IsValid())
            {
                _stump.SetActive(false);
            }

            if (_countLabel.IsValid())
            {
                _countLabel.alpha = 1f;
                _countLabel.transform.localScale = Vector3.one;
            }

            _visual.gameObject.SetActive(true);
            _visual.localScale = _baseScale;
            _visual.localRotation = _baseRotation;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            if (_isHarvested)
            {
                AnimateHarvest(deltaTime);
                return;
            }

            _shake = Mathf.Max(_shake - SHAKE_DECAY * deltaTime, 0f);
            _phase += deltaTime * SHAKE_FREQUENCY;

            var wobble = _shake * SHAKE_DEGREES;
            var tilt = Quaternion.Euler(Mathf.Sin(_phase) * wobble, 0f, Mathf.Cos(_phase * 1.17f) * wobble);
            var squash = 1f - _lastProgress * SQUASH_AMOUNT;
            var shrink = 1f - _lastProgress * SHRINK_AMOUNT;
            var scale = new Vector3(shrink / squash, squash, shrink / squash);

            _visual.localRotation = _baseRotation * tilt;
            _visual.localScale = Vector3.Scale(_baseScale, scale);
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (_visual.IsInvalid())
            {
                _visual = transform;
            }

            _baseScale = _visual.localScale;
            _baseRotation = _visual.localRotation;
            _phase = Random.value * 10f;
        }

        private void AnimateHarvest(float deltaTime)
        {
            _harvestAge += deltaTime;

            var t = Mathf.Clamp01(_harvestAge / _harvestSeconds);

            if (_falls)
            {
                AnimateFall(t);
            }
            else
            {
                var pop = 1f + Mathf.Sin(Mathf.Min(t * 2f, 1f) * Mathf.PI) * POP_AMOUNT;
                var shrink = 1f - Mathf.SmoothStep(0f, 1f, t);

                _visual.localScale = _baseScale * (pop * shrink);
            }

            if (_countLabel.IsValid() && _countLabel.gameObject.activeSelf)
            {
                AnimateLabel();
            }

            if (t >= 1f)
            {
                _visual.gameObject.SetActive(false);
            }
        }

        private void AnimateFall(float t)
        {
            var eased = t * t;
            var axis = Vector3.Cross(Vector3.up, _awayDirection);
            var fall = Quaternion.AngleAxis(_fallDegrees * eased, axis);
            var shrink = 1f - Mathf.SmoothStep(0.6f, 1f, t);

            _visual.rotation = fall * transform.rotation * _baseRotation;
            _visual.localScale = _baseScale * shrink;
        }

        private void AnimateLabel()
        {
            var t = Mathf.Clamp01(_harvestAge / LABEL_FADE_SECONDS);
            var label = _countLabel.transform;

            label.localScale = Vector3.one * (1f + t * 0.4f);
            _countLabel.alpha = 1f - t;

            if (t >= 1f)
            {
                _countLabel.alpha = 1f;
                label.localScale = Vector3.one;
                _countLabel.gameObject.SetActive(false);
            }
        }
    }
}
