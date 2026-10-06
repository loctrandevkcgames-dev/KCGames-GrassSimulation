using System.Collections.Generic;
using EncosyTower.PubSub;
using EncosyTower.UnityExtensions;
using GrassSimulation.Gameplay;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class GameAudio : MonoBehaviour
    {
        private const int VOICE_COUNT = 12;
        private const float AMBIENCE_FADE_SECONDS = 1f;

        [SerializeField]
        private AudioLibrary _library;

        [SerializeField]
        private AudioMixer _mixer;

        [SerializeField]
        private AudioMixerGroup _musicGroup;

        [SerializeField]
        private AudioMixerGroup _ambienceGroup;

        [SerializeField]
        private AudioMixerGroup _loopsGroup;

        [SerializeField]
        private AudioMixerGroup _eventsGroup;

        [SerializeField]
        private AudioMixerGroup _uiGroup;

        private readonly List<ISubscription> _subscriptions = new();
        private readonly CueSequenceScheduler _sequences = new();

        private SfxVoicePool _voices;
        private MowerHumPlayer _hum;
        private MusicPlayer _music;
        private AmbiencePlayer _ambience;
        private MixerVolumes _volumes;
        private CueGate _gate;
        private LevelState _state = LevelState.Preview;
        private bool _isPaused;
        private bool _isHome = true;
        private int _levelStartedFrame = -1;
        private float _previousRemaining;
        private float _jingleTime;
        private SoundId _jingle;
        private bool _hasJingle;

        public float MowerSpeed01 { get; set; }

        public float RemainingTime { get; set; }

        private bool IsRunning
            => _isHome == false
            && _isPaused == false
            && (_state == LevelState.Playing || _state == LevelState.UpgradeChoice || _state == LevelState.Cleanup);

        public void Bind(
              MessageSubscriber.Subscriber<GameplayScope> gameplay
            , MessageSubscriber.Subscriber<AudioScope> audio
        )
        {
            if (_library.IsInvalid())
            {
                ThrowHelper.LogWarning("GameAudio has no audio library, so the game stays silent.");
                return;
            }

            Build();

            _subscriptions.Unsubscribe();
            _subscriptions.Add(LevelStartedMsg.Subscribe(in gameplay, OnLevelStarted));
            _subscriptions.Add(LevelFinishedMsg.Subscribe(in gameplay, OnLevelFinished));
            _subscriptions.Add(LevelStateChangedMsg.Subscribe(in gameplay, OnLevelStateChanged));
            _subscriptions.Add(HomeChangedMsg.Subscribe(in gameplay, OnHomeChanged));
            _subscriptions.Add(PropBrokenMsg.Subscribe(in gameplay, OnPropBroken));
            _subscriptions.Add(QuotaCompletedMsg.Subscribe(in gameplay, OnQuotaCompleted));
            _subscriptions.Add(TierUpMsg.Subscribe(in gameplay, OnTierUp));
            _subscriptions.Add(UpgradeChosenMsg.Subscribe(in gameplay, OnUpgradeChosen));
            _subscriptions.Add(ProtectedHitMsg.Subscribe(in gameplay, OnProtectedHit));
            _subscriptions.Add(PlayerOptionsChangedMsg.Subscribe(in gameplay, OnPlayerOptionsChanged));
            _subscriptions.Add(UiSoundRequestedMsg.Subscribe(in audio, OnUiSoundRequested));
        }

        private void Update()
        {
            if (_voices == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            var deltaTime = Time.unscaledDeltaTime;

            _hum.Step(IsRunning, MowerSpeed01, deltaTime);
            _music.Step(now, deltaTime);
            _ambience.Step(deltaTime);
            StepSequences(now);
            StepJingle(now);
            StepTimer(now);
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
        }

        private void Build()
        {
            if (_voices != null)
            {
                return;
            }

            _voices = new SfxVoicePool(transform, VOICE_COUNT);
            _hum = new MowerHumPlayer(transform, _library.MowerHum, _loopsGroup);
            _music = new MusicPlayer(transform, _library, _musicGroup);
            _ambience = new AmbiencePlayer(transform, _library.Ambience, _ambienceGroup);
            _gate = new CueGate(seed: (uint)System.Environment.TickCount | 1u);

            if (_mixer.IsValid())
            {
                _volumes = new MixerVolumes(_mixer);
                _volumes.Apply();
            }
            else
            {
                ThrowHelper.LogWarning("GameAudio has no audio mixer, so the volume options are ignored.");
            }

            _ambience.Start(AMBIENCE_FADE_SECONDS);
        }

        private void StepTimer(float now)
        {
            var isTicking = _state == LevelState.Playing && _isPaused == false && _isHome == false;
            var previous = _previousRemaining;

            _previousRemaining = RemainingTime;

            if (isTicking == false)
            {
                return;
            }

            var tick = TimerTicks.Evaluate(
                  previous
                , RemainingTime
                , LevelSession.TIMER_WARNING_SECONDS
                , _library.TimerAccentSeconds
            );

            if (tick == TimerTick.Tick)
            {
                PlayCue(SoundId.TimerTick, now);
            }
            else if (tick == TimerTick.Accent)
            {
                PlayCue(SoundId.TimerTickAccent, now);
            }
        }

        private void StepSequences(float now)
        {
            while (_sequences.TryTakeDue(now, out var id, out var index))
            {
                PlayCueIndexed(id, index, now);
            }
        }

        private void StepJingle(float now)
        {
            if (_hasJingle && now >= _jingleTime)
            {
                _hasJingle = false;
                PlayCue(_jingle, now);
            }
        }

        private void RefreshMusic()
        {
            if (_isHome)
            {
                _music.Play(_library.HomeMusic, _library.MusicFadeSeconds);
                return;
            }

            if (_state == LevelState.Success || _state == LevelState.Failure)
            {
                return;
            }

            _music.Play(_library.GameplayMusic, _library.MusicFadeSeconds);
        }

        private void ScheduleSequence(SoundId id, int count, float now)
        {
            _sequences.Schedule(id, count, now, _library.SequenceStepSeconds);
        }

        private void ResetSequences()
        {
            _sequences.Clear();
            _hasJingle = false;
        }

        private void PlayCue(SoundId id, float now)
        {
            if (_library.TryGetCue(id, out var cue) == false)
            {
                return;
            }

            if (_gate.TryPick(id, cue.Clips.Length, cue.Order, cue.Pitch, cue.Cooldown, now, out var pick))
            {
                Emit(in cue, in pick, now);
            }
        }

        private void PlayCueIndexed(SoundId id, int index, float now)
        {
            if (_library.TryGetCue(id, out var cue) == false)
            {
                return;
            }

            if (_gate.TryPick(id, cue.Clips.Length, index, cue.Pitch, cue.Cooldown, now, out var pick))
            {
                Emit(in cue, in pick, now);
            }
        }

        private void Emit(in SoundCue cue, in CuePick pick, float now)
        {
            _voices.Play(cue.Clips[pick.Variant], GetGroup(cue.Bus), cue.Volume, pick.Pitch, now);
        }

        private AudioMixerGroup GetGroup(AudioBus bus)
        {
            return bus switch {
                AudioBus.Music => _musicGroup,
                AudioBus.Ambience => _ambienceGroup,
                AudioBus.SfxLoops => _loopsGroup,
                AudioBus.Ui => _uiGroup,
                _ => _eventsGroup,
            };
        }

        private void OnLevelStarted(LevelStartedMsg message)
        {
            _levelStartedFrame = Time.frameCount;
            ResetSequences();
            PlayCue(SoundId.LevelStart, Time.unscaledTime);
            RefreshMusic();
        }

        private void OnLevelFinished(LevelFinishedMsg message)
        {
            var now = Time.unscaledTime;

            _music.FadeOut(_library.MusicFadeSeconds);
            _jingle = message.Result.Outcome.IsSuccess ? SoundId.JingleWin : SoundId.JingleLose;
            _jingleTime = now + _library.MusicFadeSeconds;
            _hasJingle = true;
            _sequences.BlockUntil(_jingleTime + _library.SequenceAfterJingleSeconds);
        }

        private void OnLevelStateChanged(LevelStateChangedMsg message)
        {
            _state = message.State;
            _isPaused = message.IsPaused;

            if (_state == LevelState.Preview)
            {
                ResetSequences();
            }

            RefreshMusic();
        }

        private void OnHomeChanged(HomeChangedMsg message)
        {
            _isHome = message.IsHome;
            ResetSequences();
            RefreshMusic();
        }

        private void OnPropBroken(PropBrokenMsg message)
        {
            PlayCue(message.IsSplit ? SoundId.FruitPop : SoundId.BushTrim, Time.unscaledTime);
        }

        private void OnQuotaCompleted(QuotaCompletedMsg message)
        {
            if (Time.frameCount == _levelStartedFrame)
            {
                return;
            }

            PlayCue(SoundId.QuotaComplete, Time.unscaledTime);
        }

        private void OnTierUp(TierUpMsg message)
        {
            var now = Time.unscaledTime;

            _music.Duck(now);
            PlayCue(SoundId.TierUp, now);
        }

        private void OnUpgradeChosen(UpgradeChosenMsg message)
        {
            PlayCue(SoundId.UpgradeChosen, Time.unscaledTime);
        }

        private void OnProtectedHit(ProtectedHitMsg message)
        {
            PlayCue(SoundId.ProtectedHit, Time.unscaledTime);
        }

        private void OnPlayerOptionsChanged(PlayerOptionsChangedMsg message)
        {
            if (_volumes != null)
            {
                _volumes.Apply();
            }
        }

        private void OnUiSoundRequested(UiSoundRequestedMsg message)
        {
            var now = Time.unscaledTime;

            switch (message.Sound)
            {
                case UiSound.Tap:
                {
                    PlayCue(SoundId.Tap, now);
                    break;
                }

                case UiSound.PopupOpen:
                {
                    PlayCue(SoundId.PopupOpen, now);
                    break;
                }

                case UiSound.PopupClose:
                {
                    PlayCue(SoundId.PopupClose, now);
                    break;
                }

                case UiSound.ToggleOn:
                {
                    PlayCue(SoundId.ToggleOn, now);
                    break;
                }

                case UiSound.ToggleOff:
                {
                    PlayCue(SoundId.ToggleOff, now);
                    break;
                }

                case UiSound.Stars:
                {
                    ScheduleSequence(SoundId.Star, message.Count, now);
                    break;
                }

                case UiSound.Coins:
                {
                    ScheduleSequence(SoundId.Coin, message.Count, now);
                    break;
                }
            }
        }
    }
}
