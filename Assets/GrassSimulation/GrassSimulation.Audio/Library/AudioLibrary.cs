using UnityEngine;

namespace GrassSimulation.Audio
{
    [CreateAssetMenu(fileName = "GrassAudioLibrary", menuName = "GrassSimulation/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField]
        private SoundCue[] _cues;

        [SerializeField]
        private AudioClip _cutLoopLight;

        [SerializeField]
        private AudioClip _cutLoopMedium;

        [SerializeField]
        private AudioClip _cutLoopDense;

        [SerializeField]
        private CutLayerThresholds _cutThresholds = new(light: 6f, medium: 30f, dense: 90f);

        [SerializeField]
        private Vector2 _cutPitch = new(0.95f, 1.05f);

        [SerializeField]
        private float _cutDensitySmoothing = 0.1f;

        [SerializeField]
        private float _cutSilenceTimeout = 0.2f;

        [SerializeField]
        private int _snipClusterCells = 12;

        [SerializeField]
        private MowerHumSettings _mowerHum;

        [SerializeField]
        private AudioClip _gameplayMusic;

        [SerializeField]
        private AudioClip _homeMusic;

        [SerializeField]
        private AudioClip _zenMusic;

        [SerializeField]
        private AmbienceLayer[] _ambience;

        [SerializeField]
        private float _musicFadeSeconds = 0.4f;

        [SerializeField]
        private float _duckDecibels = -6f;

        [SerializeField]
        private float _duckSeconds = 1.5f;

        [SerializeField]
        private float _duckAttackSeconds = 0.05f;

        [SerializeField]
        private float _duckReleaseSeconds = 0.5f;

        [SerializeField]
        private float _timerAccentSeconds = 5f;

        [SerializeField]
        private float _sequenceStepSeconds = 0.25f;

        [SerializeField]
        private float _sequenceAfterJingleSeconds = 0.6f;

        private SoundCue[] _byId;

        public AudioClip CutLoopLight => _cutLoopLight;

        public AudioClip CutLoopMedium => _cutLoopMedium;

        public AudioClip CutLoopDense => _cutLoopDense;

        public CutLayerThresholds CutThresholds => _cutThresholds;

        public Vector2 CutPitch => _cutPitch;

        public float CutDensitySmoothing => _cutDensitySmoothing;

        public float CutSilenceTimeout => _cutSilenceTimeout;

        public int SnipClusterCells => _snipClusterCells;

        public MowerHumSettings MowerHum => _mowerHum;

        public AudioClip GameplayMusic => _gameplayMusic;

        public AudioClip HomeMusic => _homeMusic;

        public AudioClip ZenMusic => _zenMusic;

        public AmbienceLayer[] Ambience => _ambience;

        public float MusicFadeSeconds => _musicFadeSeconds;

        public float DuckDecibels => _duckDecibels;

        public float DuckSeconds => _duckSeconds;

        public float DuckAttackSeconds => _duckAttackSeconds;

        public float DuckReleaseSeconds => _duckReleaseSeconds;

        public float TimerAccentSeconds => _timerAccentSeconds;

        public float SequenceStepSeconds => _sequenceStepSeconds;

        public float SequenceAfterJingleSeconds => _sequenceAfterJingleSeconds;

        public bool TryGetCue(SoundId id, out SoundCue cue)
        {
            if (_byId == null)
            {
                Rebuild();
            }

            cue = _byId[(int)id];
            return cue.HasClips;
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            _byId = null;
        }

        private void Rebuild()
        {
            _byId = new SoundCue[SoundIdExtensions.Length];

            var count = _cues == null ? 0 : _cues.Length;

            for (var i = 0; i < count; i++)
            {
                var index = (int)_cues[i].Id;

                if (index >= 0 && index < _byId.Length)
                {
                    _byId[index] = _cues[i];
                }
            }

            for (var i = 0; i < _byId.Length; i++)
            {
                if (_byId[i].HasClips == false)
                {
                    ThrowHelper.LogWarning($"The audio library has no clips for the cue '{(SoundId)i}'.");
                }
            }
        }
    }
}
