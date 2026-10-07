using System.Collections.Generic;
using EncosyTower.PubSub;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [RequireComponent(typeof(LawnMowerController))]
    public sealed class LawnMowerAnimator : MonoBehaviour
    {
        private const float MAX_DELTA_TIME = 1f / 30f;
        private const float EPSILON = 1e-5f;
        private const float TELEPORT_DISTANCE = 1.5f;
        private const float FULL_TURN = 360f;
        private const float SPIN_RETURN_SPEED = 8f;
        private const float SHAKE_SEED_X = 11.3f;
        private const float SHAKE_SEED_Y = 47.9f;
        private const float SHAKE_SEED_Z = 83.1f;
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private Transform _body;

        [SerializeField]
        private Transform _bag;

        [Header("Running")]
        [SerializeField]
        private float _idleShake = 0.004f;

        [SerializeField]
        private float _driveShake = 0.01f;

        [SerializeField]
        private float _cutShake = 0.02f;

        [SerializeField]
        private float _shakeFrequency = 30f;

        [SerializeField]
        private float _shakeDegrees = 1.5f;

        [SerializeField]
        private float _bobHeight = 0.025f;

        [SerializeField]
        private float _bobFrequency = 10f;

        [SerializeField]
        private float _pitchPerAcceleration = 0.6f;

        [SerializeField]
        private float _rollPerAcceleration = 0.5f;

        [SerializeField]
        private float _maxTiltDegrees = 9f;

        [SerializeField]
        private float _tiltStiffness = 140f;

        [SerializeField]
        private float _tiltDamping = 12f;

        [Header("Cutting")]
        [SerializeField]
        private float _cutPerCell = 0.25f;

        [SerializeField]
        private float _cutDecay = 2.5f;

        [SerializeField]
        private float _bagInflate = 0.18f;

        [SerializeField]
        private float _bagFillPerCell = 0.003f;

        [SerializeField]
        private float _bagFillInflate = 0.25f;

        [Header("Engine start")]
        [SerializeField]
        private float _startShake = 0.035f;

        [SerializeField]
        private float _startDecay = 2.5f;

        [SerializeField]
        private float _startPopImpulse = 2.5f;

        [Header("Tier up")]
        [SerializeField]
        private float _tierPopImpulse = 7f;

        [SerializeField]
        private float _scaleStiffness = 220f;

        [SerializeField]
        private float _scaleDamping = 9f;

        [Header("Protected hit")]
        [SerializeField]
        private float _hitWobbleImpulse = 420f;

        [SerializeField]
        private float _wobbleStiffness = 260f;

        [SerializeField]
        private float _wobbleDamping = 7f;

        [Header("Turbo")]
        [SerializeField]
        private Color _boostTint = new(1f, 0.55f, 0.2f);

        [SerializeField]
        private float _boostTintShare = 0.55f;

        [SerializeField]
        private float _boostShake = 0.012f;

        [Header("Celebrating")]
        [SerializeField]
        private float _hopHeight = 0.35f;

        [SerializeField]
        private float _hopPeriod = 0.9f;

        [Header("Stalled")]
        [SerializeField]
        private float _sputterDuration = 1.2f;

        [SerializeField]
        private float _sputterShake = 0.04f;

        [SerializeField]
        private float _sputterFrequency = 9f;

        [SerializeField]
        private float _slumpPitch = 9f;

        [SerializeField]
        private float _slumpRoll = 4f;

        private readonly List<ISubscription> _subscriptions = new();
        private MaterialPropertyBlock _tintBlock;

        private Color _baseTint = Color.white;
        private bool _isBoosting;

        private LawnMowerController _controller;
        private Vector3 _bodyPosition;
        private Quaternion _bodyRotation;
        private Vector3 _bodyScale;
        private Vector3 _bagScale;
        private Vector3 _previousPosition;
        private Vector3 _previousVelocity;
        private MowerAnimationState _state;
        private Spring _pitch;
        private Spring _roll;
        private Spring _scale;
        private Spring _wobble;
        private float _stateTime;
        private float _clock;
        private float _cutLevel;
        private float _bagFill;
        private float _startKick;
        private float _spin;
        private float _wobbleSign = 1f;
        private bool _hasPreviousPosition;

        public Vector3 ChutePosition => _bag.IsValid() ? _bag.position : transform.position;

        public Vector3 ChuteDirection => _bag.IsValid() ? _bag.position - transform.position : -transform.forward;

        public MowerAnimationState State
        {
            get => _state;
            set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                _stateTime = 0f;
            }
        }

        private static float Shake(float time, float seed)
            => (Mathf.PerlinNoise(time, seed) - 0.5f) * 2f;

        public void Bind(MessageSubscriber.Subscriber<GameplayScope> subscriber)
        {
            _subscriptions.Unsubscribe();
            _subscriptions.Add(LevelStartedMsg.Subscribe(in subscriber, OnLevelStarted));
            _subscriptions.Add(TierUpMsg.Subscribe(in subscriber, OnTierUp));
            _subscriptions.Add(ProtectedHitMsg.Subscribe(in subscriber, OnProtectedHit));
            _subscriptions.Add(BoosterActivatedMsg.Subscribe(in subscriber, OnBoosterActivated));
            _subscriptions.Add(BoosterEndedMsg.Subscribe(in subscriber, OnBoosterEnded));
        }

        public void NotifyCut(int cells)
        {
            _cutLevel = Mathf.Min(a: _cutLevel + cells * _cutPerCell, b: 1f);
            _bagFill = Mathf.Min(a: _bagFill + cells * _bagFillPerCell, b: 1f);
        }

        public void SetTint(Color tint)
        {
            _baseTint = tint;
            ApplyTint(_isBoosting ? Color.Lerp(tint, _boostTint, _boostTintShare) : tint);
        }

        public void SetBoost(bool isBoosting)
        {
            if (_isBoosting == isBoosting)
            {
                return;
            }

            _isBoosting = isBoosting;
            SetTint(_baseTint);
        }

        private void ApplyTint(Color tint)
        {
            if (_body.IsInvalid())
            {
                return;
            }

            _tintBlock ??= new MaterialPropertyBlock();

            var renderers = _body.GetComponentsInChildren<Renderer>(includeInactive: true);

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                renderer.GetPropertyBlock(_tintBlock);
                _tintBlock.SetColor(s_baseColorId, tint);
                renderer.SetPropertyBlock(_tintBlock);
            }
        }

        public void ResetPose()
        {
            _state = MowerAnimationState.Parked;
            _stateTime = 0f;
            _cutLevel = 0f;
            _bagFill = 0f;
            _startKick = 0f;
            _spin = 0f;
            _pitch = default;
            _roll = default;
            _scale = default;
            _wobble = default;
            _previousVelocity = Vector3.zero;
            _hasPreviousPosition = false;

            ApplyPose(Vector3.zero, Quaternion.identity, scaleOffset: 0f);
        }

        private void Awake()
        {
            _controller = GetComponent<LawnMowerController>();

            if (_body.IsValid())
            {
                _bodyPosition = _body.localPosition;
                _bodyRotation = _body.localRotation;
                _bodyScale = _body.localScale;
            }

            if (_bag.IsValid())
            {
                _bagScale = _bag.localScale;
            }
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
        }

        private void LateUpdate()
        {
            if (_body.IsInvalid())
            {
                return;
            }

            var deltaTime = Mathf.Min(Time.deltaTime, MAX_DELTA_TIME);

            if (deltaTime < EPSILON)
            {
                return;
            }

            _clock += deltaTime;
            _stateTime += deltaTime;
            _cutLevel = Mathf.MoveTowards(current: _cutLevel, target: 0f, maxDelta: _cutDecay * deltaTime);
            _startKick = Mathf.MoveTowards(current: _startKick, target: 0f, maxDelta: _startDecay * deltaTime);

            var velocity = SampleVelocity(deltaTime);
            var pose = _state switch {
                MowerAnimationState.Running => RunningPose(velocity, deltaTime),
                MowerAnimationState.Celebrating => CelebratingPose(deltaTime),
                MowerAnimationState.Stalled => StalledPose(deltaTime),
                _ => ParkedPose(deltaTime),
            };

            _scale.Step(target: 0f, _scaleStiffness, _scaleDamping, deltaTime);
            _wobble.Step(target: 0f, _wobbleStiffness, _wobbleDamping, deltaTime);

            var rotation = Quaternion.Euler(pose.Pitch, pose.Yaw, pose.Roll + _wobble.Value);
            ApplyPose(pose.Offset, rotation, _scale.Value);
            ApplyBag();
        }

        private Vector3 SampleVelocity(float deltaTime)
        {
            var position = transform.position;
            var delta = position - _previousPosition;
            var isTeleport = _hasPreviousPosition == false || delta.magnitude > TELEPORT_DISTANCE;

            _previousPosition = position;
            _hasPreviousPosition = true;

            return isTeleport ? Vector3.zero : delta / deltaTime;
        }

        private MowerPose RunningPose(Vector3 velocity, float deltaTime)
        {
            var facing = _body.parent.IsValid() ? _body.parent.rotation : Quaternion.identity;
            var acceleration = Quaternion.Inverse(facing) * ((velocity - _previousVelocity) / deltaTime);
            var speed01 = Mathf.Clamp01(velocity.magnitude / Mathf.Max(_controller.MaxSpeed, EPSILON));

            _previousVelocity = velocity;

            var pitchTarget = Mathf.Clamp(-acceleration.z * _pitchPerAcceleration, -_maxTiltDegrees, _maxTiltDegrees);
            var rollTarget = Mathf.Clamp(acceleration.x * _rollPerAcceleration, -_maxTiltDegrees, _maxTiltDegrees);

            _pitch.Step(pitchTarget, _tiltStiffness, _tiltDamping, deltaTime);
            _roll.Step(rollTarget, _tiltStiffness, _tiltDamping, deltaTime);
            ReturnSpin(deltaTime);

            var driveShake = Mathf.Lerp(_idleShake, _driveShake, speed01);
            var boostShake = _isBoosting ? _boostShake : 0f;
            var engine = driveShake + _cutShake * _cutLevel + _startShake * _startKick + boostShake;
            var bob = Mathf.Abs(Mathf.Sin(_clock * _bobFrequency)) * _bobHeight * speed01;
            var shake = EngineShake(engine, _shakeFrequency);
            var shakeDegrees = _shakeDegrees * engine / Mathf.Max(_driveShake, EPSILON);

            return new MowerPose(
                  shake + Vector3.up * bob
                , _pitch.Value + Shake(_clock * _shakeFrequency, SHAKE_SEED_Y) * shakeDegrees
                , _spin
                , _roll.Value + Shake(_clock * _shakeFrequency, SHAKE_SEED_Z) * shakeDegrees
            );
        }

        private MowerPose CelebratingPose(float deltaTime)
        {
            SettleTilt(deltaTime);

            var phase = _stateTime / Mathf.Max(_hopPeriod, EPSILON);
            var hopPhase = phase - Mathf.Floor(phase);
            var hop = Mathf.Sin(hopPhase * Mathf.PI) * _hopHeight;

            _spin = Mathf.SmoothStep(from: 0f, to: FULL_TURN, hopPhase);

            return new MowerPose(Vector3.up * hop, _pitch.Value, _spin, _roll.Value);
        }

        private MowerPose StalledPose(float deltaTime)
        {
            var progress = Mathf.Clamp01(_stateTime / Mathf.Max(_sputterDuration, EPSILON));
            var slump = Mathf.SmoothStep(from: 0f, to: 1f, progress);

            _pitch.Step(_slumpPitch * slump, _tiltStiffness, _tiltDamping, deltaTime);
            _roll.Step(_slumpRoll * slump, _tiltStiffness, _tiltDamping, deltaTime);
            ReturnSpin(deltaTime);

            var sputter = (1f - progress) * Mathf.Abs(Mathf.Sin(_stateTime * _sputterFrequency));
            var shake = EngineShake(_sputterShake * sputter, _shakeFrequency * 0.5f);

            return new MowerPose(shake, _pitch.Value, _spin, _roll.Value);
        }

        private MowerPose ParkedPose(float deltaTime)
        {
            SettleTilt(deltaTime);
            ReturnSpin(deltaTime);

            return new MowerPose(Vector3.zero, _pitch.Value, _spin, _roll.Value);
        }

        private void SettleTilt(float deltaTime)
        {
            _previousVelocity = Vector3.zero;
            _pitch.Step(target: 0f, _tiltStiffness, _tiltDamping, deltaTime);
            _roll.Step(target: 0f, _tiltStiffness, _tiltDamping, deltaTime);
        }

        private void ReturnSpin(float deltaTime)
        {
            var blend = 1f - Mathf.Exp(-SPIN_RETURN_SPEED * deltaTime);
            _spin = Mathf.LerpAngle(a: _spin, b: 0f, t: blend);
        }

        private Vector3 EngineShake(float amplitude, float frequency)
        {
            var time = _clock * frequency;
            var x = Shake(time, SHAKE_SEED_X);
            var y = Shake(time, SHAKE_SEED_Y) * 0.5f;
            var z = Shake(time, SHAKE_SEED_Z);
            return new Vector3(x, y, z) * amplitude;
        }

        private void ApplyPose(Vector3 offset, Quaternion rotation, float scaleOffset)
        {
            if (_body.IsInvalid())
            {
                return;
            }

            var stretch = new Vector3(1f - scaleOffset * 0.5f, 1f + scaleOffset, 1f - scaleOffset * 0.5f);

            _body.localPosition = _bodyPosition + offset;
            _body.localRotation = _bodyRotation * rotation;
            _body.localScale = Vector3.Scale(_bodyScale, stretch);
        }

        private void ApplyBag()
        {
            if (_bag.IsInvalid())
            {
                return;
            }

            var puff = _cutLevel * _bagInflate * (1f + 0.3f * Mathf.Sin(_clock * _bobFrequency * 2f));
            _bag.localScale = _bagScale * (1f + _bagFill * _bagFillInflate + puff);
        }

        private void OnLevelStarted(LevelStartedMsg message)
        {
            _startKick = 1f;
            _scale.Velocity += _startPopImpulse;
        }

        private void OnTierUp(TierUpMsg message)
        {
            _scale.Velocity += _tierPopImpulse;
        }

        private void OnProtectedHit(ProtectedHitMsg message)
        {
            _wobble.Velocity += _hitWobbleImpulse * _wobbleSign;
            _wobbleSign = -_wobbleSign;
        }

        private void OnBoosterActivated(BoosterActivatedMsg message)
        {
            if (message.Kind == BoosterKind.Turbo)
            {
                SetBoost(isBoosting: true);
                _scale.Velocity += _startPopImpulse;
            }
        }

        private void OnBoosterEnded(BoosterEndedMsg message)
        {
            if (message.Kind == BoosterKind.Turbo)
            {
                SetBoost(isBoosting: false);
            }
        }

        private readonly record struct MowerPose(Vector3 Offset, float Pitch, float Yaw, float Roll);

        private struct Spring
        {
            public float Value;
            public float Velocity;

            public void Step(float target, float stiffness, float damping, float deltaTime)
            {
                Velocity += ((target - Value) * stiffness - Velocity * damping) * deltaTime;
                Value += Velocity * deltaTime;
            }
        }
    }
}
