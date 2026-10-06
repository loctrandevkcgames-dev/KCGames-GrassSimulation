using System;
using System.Collections.Generic;
using System.IO;
using EncosyTower.Common;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using EncosyTower.UnityExtensions;
using GrassSimulation.Audio;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassSimulation.Sandbox
{
    public sealed class GrassSandbox : MonoBehaviour, ILevelFlowHost
    {
        private const float MIN_SEGMENT = 1e-4f;
        private const float ZONE_FLASH_DECAY = 2f;
        private const float ZONE_MARGIN = 0.3f;
        private const float HUD_LINES_PER_SCREEN = 36f;
        private const float HUD_WIDTH_IN_LINES = 26f;
        private const float HUD_HEIGHT_IN_LINES = 19f;
        private const float MAX_BLEND_STEP = 1f / 20f;
        private const float FALLBACK_ORTHO_SIZE = 5.5f;

        [SerializeField]
        private GrassFieldRenderer _field;

        [SerializeField]
        private LawnMowerController _mower;

        [SerializeField]
        private LawnMowerAnimator _mowerAnimator;

        [SerializeField]
        private GameAudio _audio;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Renderer _protectedZone;

        [SerializeField]
        private GrassClippingsEmitter _clippings;

        [SerializeField]
        private TireTrackRenderer _tireTracks;

        [SerializeField]
        private CuttablePropController _props;

        [SerializeField]
        private MachineConfig _machine;

        [SerializeField]
        private LevelCatalog _catalog;

        [SerializeField]
        private float _cameraShake = 0.12f;

        [SerializeField]
        private int _clippingsPerCell = 3;

        [SerializeField]
        private int _petalsPerFlower = 2;

        [SerializeField]
        private float _petalLitterShare = 0.3f;

        [SerializeField]
        private float _leafLitterShare = 0.6f;

        [SerializeField]
        private float _leafLitterShade = 0.8f;

        [SerializeField]
        private PlantSettings[] _plants = Array.Empty<PlantSettings>();

        [SerializeField]
        private float _warningDistance = 0.5f;

        [SerializeField]
        private float _cameraPitch = 60f;

        [SerializeField]
        private float _cameraDistance = 25f;

        [SerializeField]
        private float _cameraLookAhead = 1.2f;

        [SerializeField]
        private float _cameraSmoothing = 6f;

        [SerializeField]
        private float _previewEnterSeconds = 0.6f;

        [SerializeField]
        private float _previewExitSeconds = 0.5f;

        [SerializeField]
        private float _previewMargin = 0.5f;

        [SerializeField]
        private float _previewHeightAllowance = 2f;

        [SerializeField]
        private Rect _previewFallbackViewport = new(x: 0.05f, y: 0.52f, width: 0.9f, height: 0.29f);

        private readonly Color[] _clippingColorByKind = new Color[PlantKindExtensions.Length];
        private readonly List<int> _harvestedCells = new();
        private readonly List<ISubscription> _subscriptions = new();
        private readonly List<ProcessRegistry> _registries = new();
        private readonly UiPointerProbe _uiProbe = new();

        private LevelDefinition _level;
        private FieldGrid _grid;
        private FieldFeedback _feedback;
        private GrassCutter _cutter;
        private LevelSession _session;
        private ProgressionService _progression;
        private LevelSettlementHandler _settlementHandler;
        private LevelCommandRouter _commandRouter;
        private GameHaptics _haptics;
        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private MessagePublisher.Publisher<GameplayScope> _gameplayEvents;
        private Result<LevelSettlement, SettleError> _lastSettlement;
        private bool _hasLastSettlement;
        private MaterialPropertyBlock _zoneProperties;
        private RectInt _protectedBed;
        private bool _hasProtectedBed;
        private Vector3 _previousBlade;
        private Vector3 _cameraFocus;
        private CameraBlend _previewBlend;
        private Rect _previewViewport;
        private float _followOrthoSize;
        private float _cutRadius;
        private float _zoneFlash;
        private int _levelIndex;
        private bool _wasResetHeld;
        private bool _wasNextLevelHeld;
        private bool _wasPreviousLevelHeld;
        private bool _wasSkipLevelHeld;
        private bool _wasConfirmHeld;
        private bool _wasPauseHeld;
        private bool _wasFirstUpgradeHeld;
        private bool _wasSecondUpgradeHeld;
        private bool _wasReloadHeld;
        private bool _wasRetryHeld;
        private bool _wasWipeHeld;
        private bool _wasHudToggleHeld;
        private bool _showDebugHud;
        private bool _isHome;
        private bool _isStarting;
        private FlowRequest _pendingFlow;
        private GUIStyle _hudStyle;

        public LevelSession Session => _session;

        public void Begin()
        {
            if (_isHome == false && _session != null && _session.State == LevelState.Preview)
            {
                _isStarting = true;
            }
        }

        public void Retry()
        {
            _pendingFlow = FlowRequest.Retry;
        }

        public void LoadNext()
        {
            _pendingFlow = FlowRequest.LoadNext;
        }

        public void GoHome()
        {
            _pendingFlow = FlowRequest.GoHome;
        }

        public void Play()
        {
            _pendingFlow = FlowRequest.Play;
        }

        public void FinishCleanup()
        {
            _pendingFlow = FlowRequest.FinishCleanup;
        }

        public void RetrySave()
        {
            _settlementHandler.RetryPending();
        }

        private static bool IsNewPress(bool isHeld, ref bool wasHeld)
        {
            var isNewPress = isHeld && wasHeld == false;
            wasHeld = isHeld;
            return isNewPress;
        }

        private void Start()
        {
            _zoneProperties = new MaterialPropertyBlock();
            _followOrthoSize = _camera.IsValid() ? _camera.orthographicSize : FALLBACK_ORTHO_SIZE;
            _previewViewport = _previewFallbackViewport;

            IndexClippingColors();

            var directory = Path.Combine(Application.persistentDataPath, "Progress");

            _progression = new ProgressionService(new FileProgressStore(directory));
            _progression.Initialize();

            _settlementHandler = new LevelSettlementHandler(
                  _progression
                , GlobalMessenger.Subscriber.Scope<GameplayScope>()
                , GlobalMessenger.Publisher.Scope<ProgressionScope>()
            );

            _mower.IsPointerBlocked = _uiProbe.IsOverUi;

            BindMowerAnimator();
            BindAudio();

            _haptics = new GameHaptics(GlobalMessenger.Subscriber.Scope<GameplayScope>());

            var settledSubscriber = GlobalMessenger.Subscriber.Scope<ProgressionScope>();
            _subscriptions.Add(LevelSettledMsg.Subscribe(in settledSubscriber, OnLevelSettled));

            var cameraSubscriber = GlobalMessenger.Subscriber.Scope<CameraScope>();
            _subscriptions.Add(PreviewViewportChangedMsg.Subscribe(in cameraSubscriber, OnPreviewViewportChanged));

            LoadLevel(_progression.FindFirstIncomplete(_catalog));

            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _gameplayEvents = GlobalMessenger.Publisher.Scope<GameplayScope>();
            _commandRouter = new LevelCommandRouter(GlobalMessenger.Subscriber.Scope<LevelCommandScope>(), this);
            RegisterQueries();
            SetHome(isHome: true);
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
            _registries.Unregister();
            _mower.IsPointerBlocked = null;
            _commandRouter?.Dispose();
            _settlementHandler?.Dispose();
            _haptics?.Dispose();
        }

        private void RunPendingFlow()
        {
            var request = _pendingFlow;

            _pendingFlow = FlowRequest.None;

            switch (request)
            {
                case FlowRequest.Retry:
                {
                    ResetRun();
                    break;
                }

                case FlowRequest.GoHome:
                {
                    SetHome(isHome: true);
                    ResetRun();
                    _previewBlend.Snap(target: 0f);
                    break;
                }

                case FlowRequest.LoadNext:
                {
                    TryAdvanceLevel();
                    break;
                }

                case FlowRequest.Play:
                {
                    var state = _session.State;
                    var canPlay = state != LevelState.Playing && state != LevelState.UpgradeChoice;

                    if (canPlay)
                    {
                        LoadLevel(_progression.FindFirstIncomplete(_catalog));
                        SetHome(isHome: false);
                    }

                    break;
                }

                case FlowRequest.FinishCleanup:
                {
                    if (_session.State != LevelState.Cleanup)
                    {
                        break;
                    }

                    if (_levelIndex + 1 < _catalog.Count)
                    {
                        LoadLevel(_levelIndex + 1);
                    }
                    else
                    {
                        SetHome(isHome: true);
                        ResetRun();
                    }

                    break;
                }
            }
        }

        private void RegisterQueries()
        {
            var gameplayHub = GlobalProcessor.Instance.Scope<GameplayScope>().WithRegistries(_registries);
            var progressionHub = GlobalProcessor.Instance.Scope<ProgressionScope>().WithRegistries(_registries);

            GetLevelSnapshotRequest.Register(in gameplayHub, ProvideLevelSnapshot);
            GetLevelPreviewRequest.Register(in gameplayHub, ProvideLevelPreview);
            GetJoystickStateRequest.Register(in gameplayHub, ProvideJoystickState);
            GetProgressSnapshotRequest.Register(in progressionHub, ProvideProgressSnapshot);
        }

        private JoystickState ProvideJoystickState(GetJoystickStateRequest request)
        {
            return _session != null && _session.IsSimulating ? _mower.GetJoystickState() : default;
        }

        private LevelPreview ProvideLevelPreview(GetLevelPreviewRequest request)
        {
            var index = _catalog.ClampIndex(request.LevelIndex);

            return LevelPreview.From(_catalog.Get(index), index);
        }

        private LevelSnapshot ProvideLevelSnapshot(GetLevelSnapshotRequest request)
        {
            var unlockedKinds = PlantKindMask.FromTier(_plants, _session.Growth.UpgradeTier);

            return LevelSnapshot.From(_session, _level, _levelIndex, _catalog.Count, unlockedKinds);
        }

        private ProgressSnapshot ProvideProgressSnapshot(GetProgressSnapshotRequest request)
        {
            return ProgressSnapshot.From(_progression, _catalog, _settlementHandler.HasPending);
        }

        private void OnLevelSettled(LevelSettledMsg message)
        {
            _lastSettlement = message.Outcome;
            _hasLastSettlement = true;
        }

        private void OnPreviewViewportChanged(PreviewViewportChangedMsg message)
        {
            _previewViewport = message.Viewport;
        }

        private void TryFinishStart()
        {
            if (_isStarting && _previewBlend.Weight <= 0f)
            {
                _isStarting = false;
                _session.TryBegin();
            }
        }

        private void SetHome(bool isHome)
        {
            _isHome = isHome;
            HomeChangedMsg.Publish(in _gameplayEvents, new HomeChangedMsg(isHome));
        }

        private void LoadLevel(int index)
        {
            _levelIndex = _catalog.ClampIndex(index);
            _level = _catalog.Get(_levelIndex);
            _grid = _level.CreateGrid();
            _feedback = new FieldFeedback(_grid.Count);
            SetLitterColors();
            _cutter = new GrassCutter(_grid, _feedback, _plants);
            _hasProtectedBed = _level.TryGetProtectedBed(out _protectedBed);

            ClearPropCells();
            PlaceProtectedZone();

            _session = new LevelSession(
                  _level
                , _machine
                , _cutter.CountCuttableCells()
                , GlobalMessenger.Publisher.Scope<GameplayScope>()
            );
            _field.Build(_grid, _plants, _level.Seed);
            _mower.Bounds = _grid.Bounds;

            ResetRun();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            RunPendingFlow();
            TryFinishStart();
            HandleKeys();

            if (_session.IsSimulating)
            {
                var stats = _session.Growth.Stats;

                _mower.MaxSpeed = stats.Speed;
                _mower.Step(deltaTime, _camera);
                PushMowerOutOfProps();
                StepTireTracks(deltaTime);

                var radiusDelta = _machine.CutRadiusTweenSpeed * deltaTime;
                _cutRadius = Mathf.MoveTowards(_cutRadius, stats.CutRadius, radiusDelta);

                var blade = _mower.transform.position;
                Cut(_previousBlade, blade, stats.CuttingPower, deltaTime);
                CutProps(blade, stats.CuttingPower, deltaTime);
                _previousBlade = blade;
            }

            _session.EndTick(deltaTime);
            AnimateMower();
            PushAudioFrame();
            DecayFeedback(deltaTime);
            WriteCellStates();
            UpdateBlade();
            UpdateProtectedZone();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && _session != null)
            {
                _session.Pause();
            }
        }

        private void LateUpdate()
        {
            if (_camera.IsInvalid())
            {
                return;
            }

            var mowerPosition = _mower.transform.position;
            var lookAhead = _mower.Velocity / Mathf.Max(_session.Growth.Stats.Speed, MIN_SEGMENT) * _cameraLookAhead;
            var blend = 1f - Mathf.Exp(-_cameraSmoothing * Time.deltaTime);

            _cameraFocus = Vector3.Lerp(_cameraFocus, mowerPosition + lookAhead, blend);

            var wantsPreview = _isHome == false && _session.State == LevelState.Preview && _isStarting == false;
            var blendSeconds = wantsPreview ? _previewEnterSeconds : _previewExitSeconds;

            _previewBlend.Step(
                  target: wantsPreview ? 1f : 0f
                , seconds: blendSeconds
                , deltaTime: Mathf.Min(Time.unscaledDeltaTime, MAX_BLEND_STEP)
            );

            PlaceCamera();
        }

        private void OnGUI()
        {
            if (_showDebugHud == false)
            {
                return;
            }

            var lineHeight = Screen.height / HUD_LINES_PER_SCREEN;
            var margin = lineHeight * 0.5f;
            var size = new Vector2(HUD_WIDTH_IN_LINES, HUD_HEIGHT_IN_LINES) * lineHeight;

            _hudStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
            _hudStyle.fontSize = Mathf.RoundToInt(lineHeight * 0.7f);

            var growth = _session.Growth;
            var stats = growth.Stats;

            GUILayout.BeginArea(new Rect(new Vector2(margin, margin), size), GUI.skin.box);
            HudLine($"Level {_levelIndex + 1} / {_catalog.Count}   {_level.Id.Value}");
            HudLine($"{StateText()}   Time {_session.RemainingTime:0.0} s   Cleared {_session.ClearedFraction:P0}");
            HudLine($"Tier {growth.Tier}   XP {growth.Xp}{NextThresholdText()}   {ProtectedHitsText()}");
            HudLine($"Radius {stats.CutRadius:0.00} m   Power {stats.CuttingPower:0.00}   Speed {stats.Speed:0.00}");
            HudQuotas();
            HudLine(PropsText());

            if (_props.IsValid() && _props.IsTouchingLocked)
            {
                HudLine($"<color=#ff8080>Needs tier {_props.LockedTier}</color>");
            }

            if (_session.State == LevelState.UpgradeChoice)
            {
                HudLine($"<color=yellow>LEVEL UP x{growth.PendingUpgrades}: {UpgradeChoicesText()}</color>");
            }

            HudProgression();
            HudLine(HintText());
            HudLine("F5: reload progress   F6: retry save   Shift+Del: wipe");
            GUILayout.EndArea();
        }

        private void HudProgression()
        {
            var state = _session.State;

            if (state != LevelState.Playing && state != LevelState.UpgradeChoice)
            {
                var level = _level.Id;
                HudLine($"Coins {_progression.Coins}   Best {_progression.GetBestStars(level)} stars");
            }

            if (_progression.IsReadOnly)
            {
                HudLine($"<color=#ff8080>Progress is read-only: {_progression.LoadFailure.ToMessage()}</color>");
            }

            if (_settlementHandler.HasPending)
            {
                var detail = _hasLastSettlement && _lastSettlement.TryGetError(out var error)
                    ? $"{error.ToMessage()} "
                    : string.Empty;

                HudLine($"<color=#ff8080>Save failed: {detail}(F6 retry)</color>");
                return;
            }

            if (state != LevelState.Playing && state != LevelState.UpgradeChoice
                && _hasLastSettlement && _lastSettlement.TryGetValue(out var settlement)
            )
            {
                var best = settlement.IsNewBest ? "   new best" : string.Empty;
                HudLine($"+{settlement.CoinsGranted} coins{best}");
            }
        }

        private string StateText()
        {
            var state = _session.IsPaused ? "Paused" : _session.State.ToStringFast();
            var result = _session.Result;

            return result.IsFinished
                ? $"<b>{state}</b> ({result.Outcome.ToLabel()})"
                : $"<b>{state}</b>";
        }

        private string ProtectedHitsText()
        {
            var hits = _session.Protection.Hits;

            return _session.CountsProtectedHits
                ? $"Protected hits {hits} / {_session.ProtectedHitLimit}"
                : $"Protected hits {hits}";
        }

        private string HintText()
        {
            return _session.State switch {
                LevelState.Preview => "Enter: start   R: reset",
                LevelState.Success => "Enter: cleanup   N: next level   R: retry",
                LevelState.Failure => "R: retry",
                LevelState.Cleanup => "N: next level   P: pause   R: retry",
                _ => "Move: WASD / arrows / drag / gamepad   P: pause   R: reset",
            };
        }

        private void HudQuotas()
        {
            var objectives = _session.Objectives;
            var count = objectives.QuotaCount;

            for (var i = 0; i < count; i++)
            {
                ref readonly var quota = ref objectives.GetQuota(i);
                var label = quota.IsBonus ? "Bonus" : "Quota";
                var mark = objectives.IsQuotaMet(i) ? " <color=#80ff80>done</color>" : string.Empty;
                HudLine($"{label} {quota.Kind} {objectives.GetQuotaProgress(i)} / {quota.Amount}{mark}");
            }
        }

        private string UpgradeChoicesText()
        {
            var growth = _session.Growth;
            var count = growth.UpgradeOptionCount;
            var text = string.Empty;

            for (var i = 0; i < count; i++)
            {
                var separator = i == 0 ? string.Empty : ", ";
                text += $"{separator}{i + 1} = {growth.GetUpgrade(i).Id}";
            }

            return text;
        }

        private string PropsText()
        {
            if (_props.IsInvalid())
            {
                return string.Empty;
            }

            var berries = _props.Broken(PropKind.BerryBush);
            var logs = _props.Broken(PropKind.Log);
            var fruitTrees = _props.Broken(PropKind.FruitTree);
            var trees = _props.Broken(PropKind.Tree);
            var fruits = _props.Broken(PropKind.Fruit);
            return $"Berry {berries}  Log {logs}  Apple tree {fruitTrees}  Tree {trees}  Fruit {fruits}";
        }

        private void ClearPropCells()
        {
            if (_props.IsInvalid())
            {
                return;
            }

            _props.Load(_level.PropLayout, _mower.transform);

            var props = _props.Props;
            var propCount = props.Count;

            for (var i = 0; i < propCount; i++)
            {
                var prop = props[i];
                _grid.ClearCircle(_grid.ToLocal(prop.transform.position), prop.FootprintRadius);
            }
        }

        private void PushMowerOutOfProps()
        {
            if (_props.IsInvalid())
            {
                return;
            }

            var mowerTransform = _mower.transform;
            mowerTransform.position = _props.PushOut(mowerTransform.position, _machine.BodyRadius);
        }

        private void BindMowerAnimator()
        {
            if (_mowerAnimator.IsValid())
            {
                _mowerAnimator.Bind(GlobalMessenger.Subscriber.Scope<GameplayScope>());
            }

            _field.Bind(GlobalMessenger.Subscriber.Scope<GameplayScope>());
        }

        private void BindAudio()
        {
            if (_props.IsValid())
            {
                _props.Bind(GlobalMessenger.Publisher.Scope<GameplayScope>());
            }

            if (_audio.IsValid())
            {
                _audio.Bind(
                      GlobalMessenger.Subscriber.Scope<GameplayScope>()
                    , GlobalMessenger.Subscriber.Scope<AudioScope>()
                );
            }
        }

        private void PushAudioFrame()
        {
            if (_audio.IsInvalid())
            {
                return;
            }

            var speed = _session.IsSimulating ? _mower.Velocity.magnitude : 0f;

            _audio.MowerSpeed01 = speed / Mathf.Max(_session.Growth.Stats.Speed, MIN_SEGMENT);
            _audio.RemainingTime = _session.RemainingTime;
        }

        private void AnimateMower()
        {
            if (_mowerAnimator.IsValid())
            {
                _mowerAnimator.State = ToMowerAnimationState();
            }
        }

        private MowerAnimationState ToMowerAnimationState()
        {
            if (_session.IsPaused)
            {
                return MowerAnimationState.Parked;
            }

            return _session.State switch {
                LevelState.Playing or LevelState.UpgradeChoice or LevelState.Cleanup => MowerAnimationState.Running,
                LevelState.Success => MowerAnimationState.Celebrating,
                LevelState.Failure => MowerAnimationState.Stalled,
                _ => MowerAnimationState.Parked,
            };
        }

        private void NotifyMowerCut(int cells)
        {
            if (cells > 0 && _mowerAnimator.IsValid())
            {
                _mowerAnimator.NotifyCut(cells);
            }
        }

        private void ResetMowerAnimator()
        {
            if (_mowerAnimator.IsValid())
            {
                _mowerAnimator.ResetPose();
            }
        }

        private void StepTireTracks(float deltaTime)
        {
            if (_tireTracks.IsValid())
            {
                _tireTracks.Step(deltaTime);
            }
        }

        private void ClearTireTracks()
        {
            if (_tireTracks.IsValid())
            {
                _tireTracks.Clear();
            }
        }

        private void CutProps(Vector3 blade, float cuttingPower, float deltaTime)
        {
            if (_props.IsInvalid())
            {
                return;
            }

            var xp = _props.Cut(blade, _cutRadius, _session.Growth.Tier, cuttingPower, deltaTime);

            if (xp > 0)
            {
                _session.RecordXp(xp);
            }
        }

        private void HudLine(string text)
        {
            GUILayout.Label(text, _hudStyle);
        }

        private string NextThresholdText()
        {
            return _session.Growth.TryGetNextThreshold(out var threshold) ? $" / {threshold}" : " (max tier)";
        }

        private void IndexClippingColors()
        {
            var plantCount = _plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = _plants[i];

                if (plant.Material.IsValid())
                {
                    _clippingColorByKind[(int)plant.Kind] = plant.Material.GetColor(GrassFieldShaderIds.TipColor);
                }
            }
        }

        private void SetLitterColors()
        {
            var plantCount = _plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = _plants[i];
                var leafColor = _clippingColorByKind[(int)plant.Kind];

                if (plant.HasHead)
                {
                    _feedback.SetLitterColors(plant.Kind, leafColor, plant.HeadColor, _petalLitterShare);
                }
                else if (plant.Shape == PlantShape.Puff)
                {
                    var leafChip = leafColor * _leafLitterShade;
                    _feedback.SetLitterColors(plant.Kind, leafColor, leafChip, _leafLitterShare);
                }
                else
                {
                    _feedback.SetLitterColors(plant.Kind, leafColor, leafColor, accentShare: 0f);
                }
            }
        }

        private void PlaceProtectedZone()
        {
            if (_protectedZone.IsInvalid())
            {
                return;
            }

            _protectedZone.gameObject.SetActive(_hasProtectedBed);

            if (_hasProtectedBed == false)
            {
                return;
            }

            var cellSize = _grid.CellSize;
            var min = _grid.Origin + new Vector2(_protectedBed.xMin, _protectedBed.yMin) * cellSize;
            var size = new Vector2(_protectedBed.width, _protectedBed.height) * cellSize;
            var center = min + size * 0.5f;
            var zone = _protectedZone.transform;

            zone.SetPositionAndRotation(new Vector3(center.x, 0.02f, center.y), Quaternion.Euler(90f, 0f, 0f));
            zone.localScale = new Vector3(size.x + ZONE_MARGIN, size.y + ZONE_MARGIN, 1f);
        }

        private void ResetRun()
        {
            if (_props.IsValid())
            {
                _props.ResetAll();
            }

            _grid.ResetProgress();
            _feedback.Clear();
            _hasLastSettlement = false;
            _zoneFlash = 0f;
            _cutRadius = Mathf.Min(_machine.BaseCutRadius, _machine.MaxCutRadius);

            var spawn = _grid.ToWorld(_level.Spawn);
            _mower.ResetTo(spawn);
            ClearTireTracks();
            ResetMowerAnimator();
            _previousBlade = spawn;
            _cameraFocus = spawn;
            _isStarting = false;

            PlaceCamera();
            _session.Reset();
        }

        private bool TryFitPreview(out CameraPose pose)
        {
            return OrthoFraming.TryFit(
                  _grid.Bounds
                , _cameraPitch
                , _camera.aspect
                , _previewViewport
                , _previewMargin
                , _previewHeightAllowance
                , out pose
            );
        }

        private void PlaceCamera()
        {
            if (_camera.IsInvalid())
            {
                return;
            }

            var rotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            var kick = _props.IsValid() ? _props.CameraKick : 0f;
            var shake = UnityEngine.Random.insideUnitSphere * (kick * _cameraShake);
            var pose = new CameraPose(_cameraFocus + shake, _followOrthoSize);
            var eased = _previewBlend.Eased;

            if (_camera.orthographic)
            {
                if (eased > 0f && TryFitPreview(out var preview))
                {
                    pose = CameraPose.Lerp(in pose, in preview, eased);
                }

                _camera.orthographicSize = pose.OrthoSize;
            }

            var position = pose.Focus - rotation * Vector3.forward * _cameraDistance;
            _camera.transform.SetPositionAndRotation(position, rotation);
        }

        private void HandleKeys()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            var isConfirmHeld = keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed;
            var isPauseHeld = keyboard.pKey.isPressed || keyboard.escapeKey.isPressed;
            var isFirstUpgradeHeld = keyboard.digit1Key.isPressed || keyboard.numpad1Key.isPressed;
            var isSecondUpgradeHeld = keyboard.digit2Key.isPressed || keyboard.numpad2Key.isPressed;
            var resetPressed = IsNewPress(keyboard.rKey.isPressed, ref _wasResetHeld);
            var nextLevelPressed = IsNewPress(keyboard.nKey.isPressed, ref _wasNextLevelHeld);
            var previousLevelPressed = IsNewPress(keyboard.pageUpKey.isPressed, ref _wasPreviousLevelHeld);
            var skipLevelPressed = IsNewPress(keyboard.pageDownKey.isPressed, ref _wasSkipLevelHeld);
            var confirmPressed = IsNewPress(isConfirmHeld, ref _wasConfirmHeld);
            var pausePressed = IsNewPress(isPauseHeld, ref _wasPauseHeld);
            var firstPressed = IsNewPress(isFirstUpgradeHeld, ref _wasFirstUpgradeHeld);
            var secondPressed = IsNewPress(isSecondUpgradeHeld, ref _wasSecondUpgradeHeld);
            var reloadPressed = IsNewPress(keyboard.f5Key.isPressed, ref _wasReloadHeld);
            var retryPressed = IsNewPress(keyboard.f6Key.isPressed, ref _wasRetryHeld);
            var isShiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            var wipePressed = IsNewPress(isShiftHeld && keyboard.deleteKey.isPressed, ref _wasWipeHeld);
            var hudTogglePressed = IsNewPress(keyboard.f1Key.isPressed, ref _wasHudToggleHeld);

            if (hudTogglePressed)
            {
                _showDebugHud = !_showDebugHud;
            }

            if (wipePressed)
            {
                WipeProgress();
                return;
            }

            if (reloadPressed)
            {
                _progression.Reload();
                _hasLastSettlement = false;
                return;
            }

            if (retryPressed)
            {
                RetrySaveRequestedMsg.Publish(in _commands, new RetrySaveRequestedMsg());
                return;
            }

            if (resetPressed)
            {
                RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());
                return;
            }

            if (nextLevelPressed)
            {
                NextLevelRequestedMsg.Publish(in _commands, new NextLevelRequestedMsg());
                return;
            }

            if (previousLevelPressed || skipLevelPressed)
            {
                LoadLevel(_levelIndex + (skipLevelPressed ? 1 : -1));
                return;
            }

            if (pausePressed)
            {
                PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(_session.IsPaused == false));
            }

            if (confirmPressed)
            {
                Confirm();
            }

            if (firstPressed)
            {
                UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: 0));
            }
            else if (secondPressed)
            {
                UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: 1));
            }
        }

        private void WipeProgress()
        {
            var wiped = _progression.Wipe();

            if (wiped.TryGetFailure(out var failure))
            {
                ThrowHelper.LogError_WipeFailed(failure);
                return;
            }

            _settlementHandler.ClearPending();
            LoadLevel(_progression.FindFirstIncomplete(_catalog));
        }

        private void TryAdvanceLevel()
        {
            var hasWon = _session.State == LevelState.Success || _session.State == LevelState.Cleanup;
            var hasNextLevel = _levelIndex + 1 < _catalog.Count;

            if (hasWon && hasNextLevel)
            {
                LoadLevel(_levelIndex + 1);
            }
        }

        private void Confirm()
        {
            switch (_session.State)
            {
                case LevelState.Preview:
                {
                    if (_isHome)
                    {
                        PlayRequestedMsg.Publish(in _commands, new PlayRequestedMsg());
                    }
                    else
                    {
                        StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
                    }

                    break;
                }

                case LevelState.Success:
                {
                    CleanupRequestedMsg.Publish(in _commands, new CleanupRequestedMsg());
                    break;
                }
            }
        }

        private void Cut(Vector3 from, Vector3 to, float cuttingPower, float deltaTime)
        {
            var stroke = new CutStroke(from, to, _cutRadius, _session.Growth.Tier, cuttingPower, deltaTime);
            var touchedProtected = _cutter.Cut(stroke, _harvestedCells);
            var harvestedCount = _harvestedCells.Count;

            for (var i = 0; i < harvestedCount; i++)
            {
                var index = _harvestedCells[i];
                ref readonly var plant = ref _cutter.GetPlant(_grid.GetKind(index));
                _session.RecordHarvest(plant.Kind, plant.Xp);
                EmitClippings(index, plant);
            }

            NotifyMowerCut(harvestedCount);

            _harvestedCells.Clear();

            if (touchedProtected && _session.RecordProtectedTouch())
            {
                _zoneFlash = 1f;
            }
        }

        private void EmitClippings(int index, in PlantSettings plant)
        {
            if (_clippings.IsInvalid())
            {
                return;
            }

            var position = _grid.ToWorld(_grid.CellCenter(index));
            var away = position - _mower.transform.position;
            var leafColor = _clippingColorByKind[(int)plant.Kind];

            _clippings.Emit(position, away, leafColor, _clippingsPerCell);

            if (plant.HasHead)
            {
                _clippings.Emit(position, away, plant.HeadColor, _petalsPerFlower, ClippingShape.Chip);
            }
        }

        private void DecayFeedback(float deltaTime)
        {
            _feedback.Decay(deltaTime);
            _zoneFlash = Mathf.Max(_zoneFlash - ZONE_FLASH_DECAY * deltaTime, 0f);
        }

        private void WriteCellStates()
        {
            _feedback.Write(_grid, _field.CellStates, _field.LitterStates, _field.LitterAccentStates, _field.CutStates);
            _field.MarkCellStatesDirty();
        }

        private void UpdateBlade()
        {
            var blade = _mower.transform.position;
            var warning = _hasProtectedBed ? ProtectedWarning(blade) : 0f;

            _field.SetBlade(blade, _cutRadius, warning);
        }

        private float ProtectedWarning(Vector3 blade)
        {
            var cellSize = _grid.CellSize;
            var bladeXZ = _grid.ToLocal(blade);
            var bedMin = new Vector2(_protectedBed.xMin, _protectedBed.yMin) * cellSize;
            var bedMax = new Vector2(_protectedBed.xMax, _protectedBed.yMax) * cellSize;
            var nearest = Vector2.Max(bedMin, Vector2.Min(bladeXZ, bedMax));
            var gap = Vector2.Distance(bladeXZ, nearest) - _cutRadius;
            return 1f - Mathf.Clamp01(gap / _warningDistance);
        }

        private void UpdateProtectedZone()
        {
            if (_protectedZone.IsInvalid())
            {
                return;
            }

            _protectedZone.GetPropertyBlock(_zoneProperties);
            _zoneProperties.SetFloat(GrassFieldShaderIds.Flash, _zoneFlash);
            _protectedZone.SetPropertyBlock(_zoneProperties);
        }

        private enum FlowRequest
        {
            None,
            Retry,
            LoadNext,
            GoHome,
            Play,
            FinishCleanup,
        }
    }
}
