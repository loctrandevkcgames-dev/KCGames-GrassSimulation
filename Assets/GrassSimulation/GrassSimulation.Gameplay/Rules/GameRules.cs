using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "GameRules", menuName = "Grass Simulation/Game Rules")]
    public sealed class GameRules : ScriptableObject
    {
        [SerializeField]
        private ProtectedMode _protectedMode = ProtectedMode.Warn;

        [SerializeField]
        private int _failLimit = 3;

        [SerializeField]
        private float _retrigger = 1f;

        [SerializeField]
        private bool _timerEnabled = true;

        [SerializeField]
        private float _timerMultiplier = 1f;

        [SerializeField]
        private float _star2TimeLeft = 0.2f;

        [SerializeField]
        private float _timerWarning = 15f;

        [SerializeField]
        private bool _autoSlow;

        [SerializeField]
        private float _slowHintThreshold = 0.8f;

        [SerializeField]
        private float _cleanupFinishArea = 0.01f;

        [SerializeField]
        private int _cleanupClusterMax = 4;

        [SerializeField]
        private float _idleHint = 8f;

        [SerializeField]
        private int _boosterGiftTurbo = 3;

        [SerializeField]
        private int _boosterGiftExtraTime = 3;

        public GameRulesValues Values => new(
              _protectedMode
            , _failLimit
            , _retrigger
            , _timerEnabled
            , _timerMultiplier
            , _star2TimeLeft
            , _timerWarning
            , _autoSlow
            , _slowHintThreshold
            , _cleanupFinishArea
            , _cleanupClusterMax
            , _idleHint
            , _boosterGiftTurbo
            , _boosterGiftExtraTime
        );
    }
}
