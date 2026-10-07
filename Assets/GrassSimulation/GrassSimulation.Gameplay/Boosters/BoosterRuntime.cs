using System;
using EncosyTower.Common;

namespace GrassSimulation.Gameplay
{
    public sealed class BoosterRuntime
    {
        private readonly BoosterValues _values;
        private readonly bool[] _isEquipped = new bool[BoosterKindExtensions.Length];
        private readonly bool[] _isUsed = new bool[BoosterKindExtensions.Length];
        private readonly bool[] _isRunning = new bool[BoosterKindExtensions.Length];
        private readonly float[] _remaining = new float[BoosterKindExtensions.Length];
        private readonly BoosterKind[] _ended = new BoosterKind[BoosterKindExtensions.Length];

        public BoosterRuntime(in BoosterValues values)
        {
            _values = values;
        }

        public float SpeedMultiplier
            => _isRunning[(int)BoosterKind.Turbo] ? _values.TurboSpeedMultiplier : 1f;

        public float PowerMultiplier
            => _isRunning[(int)BoosterKind.Turbo] ? _values.TurboPowerMultiplier : 1f;

        public float ExtraTimeSeconds => _values.ExtraTimeSeconds;

        public bool IsEquipped(BoosterKind kind)
            => _isEquipped[(int)kind];

        public bool IsRunning(BoosterKind kind)
            => _isRunning[(int)kind];

        public bool IsUsed(BoosterKind kind)
            => _isUsed[(int)kind];

        public float GetRemaining(BoosterKind kind)
            => _remaining[(int)kind];

        public float GetDuration(BoosterKind kind)
            => kind == BoosterKind.Turbo ? _values.TurboSeconds : 0f;

        public BoosterKind GetEnded(int index)
            => _ended[index];

        public void SetEquipped(BoosterKind kind, bool isEquipped)
        {
            _isEquipped[(int)kind] = isEquipped;
        }

        public BoosterSlotState GetState(BoosterKind kind)
        {
            var index = (int)kind;

            if (_isEquipped[index] == false)
            {
                return BoosterSlotState.NotEquipped;
            }

            if (_isRunning[index])
            {
                return BoosterSlotState.Running;
            }

            if (_isUsed[index])
            {
                return BoosterSlotState.Used;
            }

            return TryFindBlocker(kind, out _) ? BoosterSlotState.Blocked : BoosterSlotState.Ready;
        }

        public Success<BoosterError> TryActivate(BoosterKind kind)
        {
            var index = (int)kind;

            if (_isEquipped[index] == false)
            {
                return Success.No<BoosterError>(new BoosterError.NotEquipped(kind));
            }

            if (_isUsed[index])
            {
                return Success.No<BoosterError>(new BoosterError.AlreadyUsed(kind));
            }

            if (TryFindBlocker(kind, out var blocker))
            {
                return Success.No<BoosterError>(new BoosterError.Blocked(kind, blocker));
            }

            var duration = GetDuration(kind);

            _isUsed[index] = true;
            _isRunning[index] = duration > 0f;
            _remaining[index] = duration;
            return Success.Yes;
        }

        /// <summary>Advances running boosters and returns how many ended; read them with GetEnded.</summary>
        public int Tick(float deltaTime)
        {
            var endedCount = 0;

            for (var i = 0; i < _isRunning.Length; i++)
            {
                if (_isRunning[i] == false)
                {
                    continue;
                }

                _remaining[i] = Math.Max(_remaining[i] - deltaTime, 0f);

                if (_remaining[i] <= 0f)
                {
                    _isRunning[i] = false;
                    _ended[endedCount++] = (BoosterKind)i;
                }
            }

            return endedCount;
        }

        /// <summary>Stops running boosters and returns how many stopped; read them with GetEnded.</summary>
        public int ClearEffects()
        {
            var endedCount = 0;

            for (var i = 0; i < _isRunning.Length; i++)
            {
                if (_isRunning[i])
                {
                    _isRunning[i] = false;
                    _remaining[i] = 0f;
                    _ended[endedCount++] = (BoosterKind)i;
                }
            }

            return endedCount;
        }

        public void Reset()
        {
            ClearEffects();
            Array.Clear(_isEquipped, 0, _isEquipped.Length);
            Array.Clear(_isUsed, 0, _isUsed.Length);
            Array.Clear(_remaining, 0, _remaining.Length);
        }

        private bool TryFindBlocker(BoosterKind kind, out BoosterKind blocker)
        {
            for (var i = 0; i < _isRunning.Length; i++)
            {
                var other = (BoosterKind)i;

                if (_isRunning[i] && BoosterRules.Conflicts(kind, other))
                {
                    blocker = other;
                    return true;
                }
            }

            blocker = default;
            return false;
        }
    }
}
