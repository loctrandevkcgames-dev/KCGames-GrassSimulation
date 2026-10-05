using System;
using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassSandbox : MonoBehaviour
    {
        private const float MIN_SEGMENT = 1e-4f;
        private const float ZONE_FLASH_DECAY = 2f;
        private const float ZONE_MARGIN = 0.3f;
        private const float HUD_LINES_PER_SCREEN = 36f;
        private const float HUD_WIDTH_IN_LINES = 26f;
        private const float HUD_HEIGHT_IN_LINES = 16f;

        [SerializeField]
        private GrassFieldRenderer _field;

        [SerializeField]
        private LawnMowerController _mower;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Renderer _protectedZone;

        [SerializeField]
        private GrassClippingsEmitter _clippings;

        [SerializeField]
        private CuttablePropController _props;

        [SerializeField]
        private MachineConfig _machine;

        [SerializeField]
        private LevelCatalog _catalog;

        [SerializeField]
        private int _startLevel;

        [SerializeField]
        private float _cameraShake = 0.12f;

        [SerializeField]
        private int _clippingsPerCell = 3;

        [SerializeField]
        private int _petalsPerFlower = 2;

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

        private readonly Color[] _clippingColorByKind = new Color[(int)PlantKind.ProtectedFlower + 1];
        private readonly List<int> _harvestedCells = new();

        private LevelDefinition _level;
        private FieldGrid _grid;
        private FieldFeedback _feedback;
        private GrassCutter _cutter;
        private LevelSession _session;
        private MaterialPropertyBlock _zoneProperties;
        private RectInt _protectedBed;
        private bool _hasProtectedBed;
        private Vector3 _previousBlade;
        private Vector3 _cameraFocus;
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
        private GUIStyle _hudStyle;

        private static bool IsNewPress(bool isHeld, ref bool wasHeld)
        {
            var isNewPress = isHeld && wasHeld == false;
            wasHeld = isHeld;
            return isNewPress;
        }

        private void Start()
        {
            _zoneProperties = new MaterialPropertyBlock();

            IndexClippingColors();
            LoadLevel(_startLevel);
        }

        private void LoadLevel(int index)
        {
            _levelIndex = _catalog.ClampIndex(index);
            _level = _catalog.Get(_levelIndex);
            _grid = _level.CreateGrid();
            _feedback = new FieldFeedback(_grid.Count);
            _cutter = new GrassCutter(_grid, _feedback, _plants);
            _hasProtectedBed = _level.TryGetProtectedBed(out _protectedBed);

            ClearPropCells();
            PlaceProtectedZone();

            _session = new LevelSession(_level, _machine, _cutter.CountCuttableCells());
            _field.Build(_grid, _plants, _level.Seed);
            _mower.Bounds = _grid.Bounds;

            ResetRun();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            HandleKeys();

            if (_session.IsSimulating)
            {
                var stats = _session.Growth.Stats;

                _mower.MaxSpeed = stats.Speed;
                _mower.Step(deltaTime, _camera);
                PushMowerOutOfProps();

                var radiusDelta = _machine.CutRadiusTweenSpeed * deltaTime;
                _cutRadius = Mathf.MoveTowards(_cutRadius, stats.CutRadius, radiusDelta);

                var blade = _mower.transform.position;
                Cut(_previousBlade, blade, stats.CuttingPower, deltaTime);
                CutProps(blade, stats.CuttingPower, deltaTime);
                _previousBlade = blade;
            }

            _session.EndTick(deltaTime);
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
            PlaceCamera();
        }

        private void OnGUI()
        {
            var lineHeight = Screen.height / HUD_LINES_PER_SCREEN;
            var margin = lineHeight * 0.5f;
            var size = new Vector2(HUD_WIDTH_IN_LINES, HUD_HEIGHT_IN_LINES) * lineHeight;

            _hudStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
            _hudStyle.fontSize = Mathf.RoundToInt(lineHeight * 0.7f);

            var growth = _session.Growth;
            var stats = growth.Stats;

            GUILayout.BeginArea(new Rect(new Vector2(margin, margin), size), GUI.skin.box);
            HudLine($"Level {_levelIndex + 1} / {_catalog.Count}   {_level.Id}");
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

            HudLine(HintText());
            GUILayout.EndArea();
        }

        private string StateText()
        {
            var state = _session.IsPaused ? "Paused" : _session.State.ToString();
            var result = _session.Result;

            return result.Outcome == LevelOutcome.None
                ? $"<b>{state}</b>"
                : $"<b>{state}</b> ({result.Outcome}, {result.Stars} stars)";
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
            _session.Reset();
            _zoneFlash = 0f;
            _cutRadius = _session.Growth.Stats.CutRadius;

            var spawn = _grid.ToWorld(_level.Spawn);
            _mower.ResetTo(spawn);
            _previousBlade = spawn;
            _cameraFocus = spawn;

            PlaceCamera();
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
            var position = _cameraFocus + shake - rotation * Vector3.forward * _cameraDistance;
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

            if (resetPressed)
            {
                ResetRun();
                return;
            }

            if (nextLevelPressed)
            {
                TryAdvanceLevel();
                return;
            }

            if (previousLevelPressed || skipLevelPressed)
            {
                LoadLevel(_levelIndex + (skipLevelPressed ? 1 : -1));
                return;
            }

            if (pausePressed)
            {
                TogglePause();
            }

            if (confirmPressed)
            {
                Confirm();
            }

            if (firstPressed)
            {
                _session.TryChooseUpgrade(0);
            }
            else if (secondPressed)
            {
                _session.TryChooseUpgrade(1);
            }
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

        private void TogglePause()
        {
            if (_session.IsPaused)
            {
                _session.Resume();
            }
            else
            {
                _session.Pause();
            }
        }

        private void Confirm()
        {
            switch (_session.State)
            {
                case LevelState.Preview:
                {
                    _session.TryBegin();
                    break;
                }

                case LevelState.Success:
                {
                    _session.TryEnterCleanup();
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
                _clippings.Emit(position, away, plant.HeadColor, _petalsPerFlower);
            }
        }

        private void DecayFeedback(float deltaTime)
        {
            _feedback.Decay(deltaTime);
            _zoneFlash = Mathf.Max(_zoneFlash - ZONE_FLASH_DECAY * deltaTime, 0f);
        }

        private void WriteCellStates()
        {
            _feedback.Write(_grid, _field.CellStates);
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
    }
}
