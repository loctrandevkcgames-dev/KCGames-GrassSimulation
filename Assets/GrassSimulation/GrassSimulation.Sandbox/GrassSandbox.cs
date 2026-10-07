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
        private const int DEBUG_XP = 100;
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
        private PlantObjectPresenter _plantObjects;

        [SerializeField]
        private ObstacleView _obstacleView;

        [SerializeField]
        private PlantContactIcons _contactIcons;

        [SerializeField]
        private MachineCatalog _machines;

        [SerializeField]
        private LevelCatalog _catalog;

        [SerializeField]
        private GameRules _rules;

        [SerializeField]
        private string _progressFolder = "Progress";

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
        private float _chutePuffPerCell = 0.35f;

        [SerializeField]
        private Color _lawnColor = new(0.42f, 0.7f, 0.27f);

        [SerializeField]
        private float _lawnAmount = 0.9f;

        [SerializeField]
        private PlantCatalog _plantCatalog;

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
        private readonly List<PlantHarvest> _harvestedObjects = new();
        private readonly PlantContactReport _contacts = new();
        private readonly List<ISubscription> _subscriptions = new();
        private readonly List<ProcessRegistry> _registries = new();
        private readonly List<int> _touchedBeds = new();
        private readonly UiPointerProbe _uiProbe = new();

        private LevelDefinition _level;
        private FieldGrid _grid;
        private FieldFeedback _feedback;
        private GrassCutter _cutter;
        private ObstacleField _obstacles;
        private PlantObjectField _plantField;
        private BedOutlines _bedOutlines;
        private LevelSession _session;
        private ProgressionService _progression;
        private LevelSettlementHandler _settlementHandler;
        private LevelCommandRouter _commandRouter;
        private Func<MachineId, bool> _isMachineOwned;
        private GameHaptics _haptics;
        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private MessagePublisher.Publisher<GameplayScope> _gameplayEvents;
        private Result<LevelSettlement, SettleError> _lastSettlement;
        private bool _hasLastSettlement;
        private Vector3 _previousBlade;
        private Vector3 _cameraFocus;
        private CameraBlend _previewBlend;
        private Rect _previewViewport;
        private float _followOrthoSize;
        private float _chuteBudget;
        private Color _chuteColor;
        private float _cutRadius;
        private float _speedLimit = float.PositiveInfinity;
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
        private bool _wasDebugXpHeld;
        private bool _showDebugHud;
        private bool _isHome;
        private bool _isStarting;
        private bool _keepMachine;
        private bool _wasSimulating;
        private FlowRequest _pendingFlow;
        private GUIStyle _hudStyle;

        public LevelSession Session => _session;

        private GameRulesValues RulesValues => _rules.IsValid() ? _rules.Values : GameRulesValues.Default;

        private bool IsTouchingLocked => _contacts.Locked.Count > 0;

        private int CleanupTier => _catalog.GetIntroducedTier(_levelIndex);

        private int CutTier
        {
            get
            {
                var tier = _session.Growth.Tier;

                return _session.State == LevelState.Cleanup ? Mathf.Max(tier, CleanupTier) : tier;
            }
        }

        public void Begin()
        {
            if (_isHome || _session == null)
            {
                return;
            }

            var state = _session.State;

            if (state == LevelState.Preview && _keepMachine == false && ShouldShowLoadout())
            {
                _session.TryOpenLoadout();
                return;
            }

            if (state == LevelState.Preview || state == LevelState.Loadout)
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

        public void SelectMachine(MachineId machine)
        {
            if (_session == null || _session.State != LevelState.Loadout)
            {
                return;
            }

            var selected = _progression.SelectMachine(machine);

            if (selected.TryGetFailure(out var failure))
            {
                ThrowHelper.LogError_SelectMachineFailed(failure);
                return;
            }

            if (_mowerAnimator.IsValid())
            {
                _mowerAnimator.SetTint(ResolveSelectedMachine().Tint);
            }
        }

        public void BackToPreview()
        {
            _session?.BackToPreview();
        }

        public void ChangeMachine()
        {
            _pendingFlow = FlowRequest.ChangeMachine;
        }

        private static bool IsNewPress(bool isHeld, ref bool wasHeld)
        {
            var isNewPress = isHeld && wasHeld == false;
            wasHeld = isHeld;
            return isNewPress;
        }

        private void Start()
        {
            _bedOutlines = new BedOutlines(_protectedZone);
            _followOrthoSize = _camera.IsValid() ? _camera.orthographicSize : FALLBACK_ORTHO_SIZE;
            _previewViewport = _previewFallbackViewport;

            IndexClippingColors();

            var directory = Path.Combine(Application.persistentDataPath, _progressFolder);

            _progression = new ProgressionService(new FileProgressStore(directory));
            _progression.Initialize();
            _isMachineOwned = _progression.IsMachineOwned;

            _settlementHandler = new LevelSettlementHandler(
                  _progression
                , _catalog
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
                    _keepMachine = _session.State != LevelState.Preview && _session.State != LevelState.Loadout;
                    ResetRun();
                    break;
                }

                case FlowRequest.ChangeMachine:
                {
                    _keepMachine = false;
                    ResetRun();

                    if (ShouldShowLoadout())
                    {
                        _session.TryOpenLoadout();
                    }

                    break;
                }

                case FlowRequest.GoHome:
                {
                    _keepMachine = false;
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
            GetLoadoutRequest.Register(in gameplayHub, ProvideLoadout);
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

            return LevelPreview.From(_catalog.Get(index), index, RulesValues);
        }

        private LevelSnapshot ProvideLevelSnapshot(GetLevelSnapshotRequest request)
        {
            var unlockedKinds = PlantKindMask.FromTier(_plantCatalog.Plants, _session.Growth.UpgradeTier);

            var cleanupTier = _session.State == LevelState.Cleanup ? CleanupTier : 0;

            return LevelSnapshot.From(
                  _session
                , _level
                , _levelIndex
                , _catalog.Count
                , unlockedKinds
                , cleanupTier
            );
        }

        private LoadoutSnapshot ProvideLoadout(GetLoadoutRequest request)
        {
            return LoadoutSnapshot.From(_machines, _isMachineOwned, _progression.SelectedMachine);
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

                var machine = ResolveSelectedMachine();

                if (_session.TryBegin(machine))
                {
                    ApplyMachine(machine);
                }
            }
        }

        private bool ShouldShowLoadout()
        {
            return LoadoutRules.ShouldShow(_progression.OwnedMachineCount, hasBoosterStock: false);
        }

        private MachineConfig ResolveSelectedMachine()
        {
            return _machines.TryFind(_progression.SelectedMachine, out var machine) ? machine : _machines.Standard;
        }

        private void ApplyMachine(MachineConfig machine)
        {
            _mower.BodyRadius = machine.BodyRadius;
            _cutRadius = machine.BaseCutRadius;

            if (_mowerAnimator.IsValid())
            {
                _mowerAnimator.SetTint(machine.Tint);
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
            _obstacles = _level.CreateObstacles(_grid);
            _plantField = _level.CreatePlantObjects(_grid, _plantCatalog.Plants);
            _feedback = new FieldFeedback(_grid.Count);
            SetLitterColors();
            _cutter = new GrassCutter(_grid, _feedback, _plantCatalog.Plants, _level.Beds, _obstacles);

            BuildObjects();
            _bedOutlines.Place(_grid, _level.Beds);

            _keepMachine = false;

            var machine = ResolveSelectedMachine();

            _session = new LevelSession(
                  _level
                , machine
                , _cutter.CountCuttableCells() + _plantField.Count
                , GlobalMessenger.Publisher.Scope<GameplayScope>()
                , RulesValues
            );
            _field.Build(_grid, _plantCatalog.Plants, _level.Seed);
            _mower.Bounds = _grid.Bounds;
            _mower.Obstacles = _obstacles;
            ApplyMachine(machine);

            ResetRun();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            RunPendingFlow();
            TryFinishStart();
            HandleKeys();

            var isSimulating = _session.IsSimulating;

            if (isSimulating && _wasSimulating == false)
            {
                _mower.ResetInput();
            }

            _wasSimulating = isSimulating;

            if (isSimulating)
            {
                var stats = _session.Growth.Stats;

                _mower.MaxSpeed = Mathf.Min(stats.Speed, _speedLimit);
                _mower.Step(deltaTime, _camera);
                StepTireTracks(deltaTime);

                var radiusDelta = _session.Machine.CutRadiusTweenSpeed * deltaTime;
                _cutRadius = Mathf.MoveTowards(_cutRadius, stats.CutRadius, radiusDelta);

                var blade = _mower.transform.position;

                _contacts.Clear();
                Cut(_previousBlade, blade, stats.CuttingPower, deltaTime);
                CutPlantObjects(_previousBlade, blade, stats.CuttingPower, deltaTime);
                ApplyContacts();
                _previousBlade = blade;
            }
            else
            {
                _contacts.Clear();
                _speedLimit = float.PositiveInfinity;
            }

            _session.EndTick(deltaTime);
            AnimateMower();
            PushAudioFrame();
            TickObjects(deltaTime);
            _feedback.Decay(deltaTime);
            _bedOutlines.Step(deltaTime);
            WriteCellStates();
            UpdateBlade();
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

            var isOverview = _session.State == LevelState.Preview || _session.State == LevelState.Loadout;
            var wantsPreview = _isHome == false && isOverview && _isStarting == false;
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
            HudLine($"{StateText()}   {TimeText()}   Cleared {_session.ClearedFraction:P0}");
            HudLine($"Tier {growth.Tier}   XP {growth.Xp}{NextThresholdText()}   {ProtectedHitsText()}");
            HudLine($"Radius {stats.CutRadius:0.00} m   Power {stats.CuttingPower:0.00}   Speed {stats.Speed:0.00}");
            HudQuotas();
            HudLine(ObjectsText());

            if (IsTouchingLocked)
            {
                HudLine($"<color=#ff8080>Needs tier {_contacts.Locked[0].RequiredTier}</color>");
            }

            if (_contacts.Slow.Count > 0)
            {
                HudLine("<color=yellow>Slow down</color>");
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
                var stars = _progression.GetStars(level);

                HudLine($"Best {StarRules.Count(stars)} stars ({stars})   Attempts {_progression.GetAttempts(level)}");
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
                var firstClear = settlement.IsFirstCompletion ? "   first clear" : string.Empty;

                HudLine($"Earned {settlement.Earned}   new {settlement.New}{firstClear}");
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

        private string TimeText()
        {
            return _session.Rules.IsTimed ? $"Time {_session.RemainingTime:0.0} s" : "Untimed";
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
                LevelState.Loadout => "Enter: start",
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

        private string ObjectsText()
        {
            var plants = _plantCatalog.Plants;
            var text = string.Empty;

            for (var i = 0; i < plants.Length; i++)
            {
                var kind = plants[i].Kind;
                var total = CountObjects(kind);

                if (total > 0)
                {
                    text += $"{kind} {_plantField.CountHarvested(kind)}/{total}  ";
                }
            }

            return text;
        }

        private int CountObjects(PlantKind kind)
        {
            var total = 0;
            var count = _plantField.Count;

            for (var i = 0; i < count; i++)
            {
                if (_plantField.Get(i).Kind == kind)
                {
                    total++;
                }
            }

            return total;
        }

        private void BuildObjects()
        {
            if (_obstacleView.IsValid())
            {
                _obstacleView.Build(_level.Obstacles, _grid.Origin);
            }

            if (_plantObjects.IsValid())
            {
                _plantObjects.Load(_plantField, _level.Plants, _grid.Origin);
            }
        }

        private void TickObjects(float deltaTime)
        {
            var rotation = _camera.IsValid() ? _camera.transform.rotation : Quaternion.identity;

            if (_plantObjects.IsValid())
            {
                _plantObjects.Tick(deltaTime, _mower.transform.position, rotation);
            }

            if (_contactIcons.IsValid())
            {
                _contactIcons.Tick(deltaTime, rotation);
            }
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
            _audio.IsTimed = _session.Rules.IsTimed;
            _audio.TimerWarningSeconds = _session.Rules.TimerWarning;
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
                PuffChute(cells);
            }
        }

        private void PuffChute(int cells)
        {
            if (_clippings.IsInvalid() || PlayerOptions.GetReduceEffects())
            {
                return;
            }

            _chuteBudget += cells * _chutePuffPerCell;

            var count = Mathf.FloorToInt(_chuteBudget);

            if (count <= 0)
            {
                return;
            }

            _chuteBudget -= count;
            _clippings.Emit(_mowerAnimator.ChutePosition, _mowerAnimator.ChuteDirection, _chuteColor, count);
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

        private void CutPlantObjects(Vector3 from, Vector3 to, float cuttingPower, float deltaTime)
        {
            var stroke = new CutStroke(
                  from
                , to
                , _cutRadius
                , CutTier
                , cuttingPower
                , deltaTime
                , RulesValues.SlowHintThreshold
            );

            _plantField.Cut(stroke, _obstacles, _harvestedObjects, _contacts);

            var harvestCount = _harvestedObjects.Count;

            for (var i = 0; i < harvestCount; i++)
            {
                var harvest = _harvestedObjects[i];

                _session.RecordHarvest(harvest.Kind, harvest.Xp, harvest.Units);
                PublishPlantHarvested(in harvest);
                PlayObjectHarvest(in harvest, to);
            }

            _harvestedObjects.Clear();
        }

        private void PublishPlantHarvested(in PlantHarvest harvest)
        {
            var isFruit = _plantField.GetPlant(harvest.Kind).IsFruit;

            PlantHarvestedMsg.Publish(
                  in _gameplayEvents
                , new PlantHarvestedMsg(harvest.Kind, harvest.Xp, harvest.FruitCount, isFruit)
            );
        }

        private void PlayObjectHarvest(in PlantHarvest harvest, Vector3 blade)
        {
            NotifyMowerCut(harvest.Units);

            if (_plantObjects.IsValid())
            {
                _plantObjects.PlayHarvest(in harvest, blade);
            }
        }

        private void ApplyContacts()
        {
            var lockedCount = _contacts.Locked.Count;

            for (var i = 0; i < lockedCount; i++)
            {
                var contact = _contacts.Locked[i];

                _session.RecordLockedTouch(contact.Kind, contact.RequiredTier);

                if (_contactIcons.IsValid())
                {
                    _contactIcons.ShowLocked(contact.Kind, contact.Position);
                }
            }

            var slowCount = _contacts.Slow.Count;

            for (var i = 0; i < slowCount; i++)
            {
                var contact = _contacts.Slow[i];

                _session.RecordSlowHint(contact.Kind);

                if (_contactIcons.IsValid())
                {
                    _contactIcons.ShowSlow(contact.Kind, contact.Position);
                }
            }

            _speedLimit = RulesValues.AutoSlow ? _contacts.SpeedLimit : float.PositiveInfinity;
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
            var plants = _plantCatalog.Plants;
            var plantCount = plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = plants[i];

                if (plant.Material.IsValid())
                {
                    _clippingColorByKind[(int)plant.Kind] = plant.Material.GetColor(GrassFieldShaderIds.TipColor);
                }
            }
        }

        private void SetLitterColors()
        {
            var plants = _plantCatalog.Plants;
            var plantCount = plants.Length;

            _feedback.SetLitterColors(PlantKind.None, _lawnColor, _lawnColor, accentShare: 0f);
            _feedback.SetLawnAmount(_lawnAmount);

            for (var i = 0; i < plantCount; i++)
            {
                var plant = plants[i];

                if (plant.Representation == PlantRepresentation.Object)
                {
                    continue;
                }

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

        private void ResetRun()
        {
            if (_plantObjects.IsValid())
            {
                _plantObjects.ResetAll();
            }

            if (_contactIcons.IsValid())
            {
                _contactIcons.Clear();
            }

            _grid.ResetProgress();
            _plantField.ResetProgress();
            _feedback.Clear();
            _bedOutlines.Clear();
            _contacts.Clear();
            _speedLimit = float.PositiveInfinity;
            _hasLastSettlement = false;
            _cutRadius = _session.Machine.BaseCutRadius;
            _mower.BodyRadius = _session.Machine.BodyRadius;

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
            var kick = _plantObjects.IsValid() ? _plantObjects.CameraKick : 0f;
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
            var debugXpPressed = IsNewPress(keyboard.f7Key.isPressed, ref _wasDebugXpHeld);

            if (hudTogglePressed)
            {
                _showDebugHud = !_showDebugHud;
            }

            if (debugXpPressed)
            {
                _session.RecordXp(DEBUG_XP);
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
                case LevelState.Loadout:
                {
                    StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
                    break;
                }

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
            var stroke = new CutStroke(
                  from
                , to
                , _cutRadius
                , CutTier
                , cuttingPower
                , deltaTime
                , RulesValues.SlowHintThreshold
            );

            _cutter.Cut(stroke, _harvestedCells, _touchedBeds, _contacts);

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

            var touchedCount = _touchedBeds.Count;

            for (var i = 0; i < touchedCount; i++)
            {
                var bed = _touchedBeds[i];

                if (_session.RecordProtectedTouch(bed))
                {
                    _bedOutlines.Flash(bed);
                }
            }

            _touchedBeds.Clear();
        }

        private void EmitClippings(int index, in PlantDefinition plant)
        {
            if (_clippings.IsInvalid())
            {
                return;
            }

            var position = _grid.ToWorld(_grid.CellCenter(index));
            var away = position - _mower.transform.position;
            var leafColor = _clippingColorByKind[(int)plant.Kind];
            var leafShape = plant.Shape == PlantShape.Puff ? ClippingShape.Chip : ClippingShape.Blade;

            _chuteColor = leafColor;
            _clippings.Emit(position, away, leafColor, _clippingsPerCell, leafShape);

            if (plant.HasHead)
            {
                _clippings.Emit(position, away, plant.HeadColor, _petalsPerFlower, ClippingShape.Chip);
            }
        }

        private void WriteCellStates()
        {
            _feedback.Write(_grid, _field.CellStates, _field.LitterStates, _field.LitterAccentStates, _field.CutStates);
            _field.MarkCellStatesDirty();
        }

        private void UpdateBlade()
        {
            var blade = _mower.transform.position;
            var warning = ProtectedWarning(blade);

            _field.SetBlade(blade, _cutRadius, warning);
        }

        private float ProtectedWarning(Vector3 blade)
        {
            var cellSize = _grid.CellSize;
            var bladeXZ = _grid.ToLocal(blade);
            var beds = _level.Beds;
            var warning = 0f;

            for (var i = 0; i < beds.Length; i++)
            {
                var bed = beds[i];
                var bedMin = new Vector2(bed.xMin, bed.yMin) * cellSize;
                var bedMax = new Vector2(bed.xMax, bed.yMax) * cellSize;
                var nearest = Vector2.Max(bedMin, Vector2.Min(bladeXZ, bedMax));
                var gap = Vector2.Distance(bladeXZ, nearest) - _cutRadius;

                warning = Mathf.Max(warning, 1f - Mathf.Clamp01(gap / _warningDistance));
            }

            return warning;
        }

        private enum FlowRequest
        {
            None,
            Retry,
            LoadNext,
            GoHome,
            Play,
            FinishCleanup,
            ChangeMachine,
        }
    }
}
