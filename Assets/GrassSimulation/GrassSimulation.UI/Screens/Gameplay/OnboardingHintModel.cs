using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public sealed class OnboardingHintModel
    {
        public const int ONBOARDING_LEVEL_INDEX = 0;
        public const float IDLE_SECONDS = 8f;

        private float _idleSeconds;
        private bool _hasMoved;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public void Reset()
        {
            _idleSeconds = 0f;
            _hasMoved = false;
            _isVisible = false;
        }

        public bool Update(float deltaTime, int levelIndex, LevelState state, bool isPaused, bool isActive)
        {
            if (state == LevelState.Preview)
            {
                Reset();
                return _isVisible;
            }

            var isEligible = levelIndex == ONBOARDING_LEVEL_INDEX && state == LevelState.Playing && isPaused == false;

            if (isEligible == false)
            {
                _isVisible = false;
                return _isVisible;
            }

            if (isActive)
            {
                _hasMoved = true;
                _idleSeconds = 0f;
                _isVisible = false;
                return _isVisible;
            }

            _idleSeconds += deltaTime;
            _isVisible = _hasMoved == false || _idleSeconds >= IDLE_SECONDS;

            return _isVisible;
        }
    }
}
