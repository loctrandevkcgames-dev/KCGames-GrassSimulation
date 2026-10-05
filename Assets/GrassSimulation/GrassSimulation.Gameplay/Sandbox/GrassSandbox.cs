using System;
using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassSandbox : MonoBehaviour
    {
        private const int CELLS_X = 72;
        private const int CELLS_Z = 56;
        private const float CELL_SIZE = 0.25f;
        private const float MIN_SEGMENT = 1e-4f;
        private const float ZONE_FLASH_DECAY = 2f;
        private const float ZONE_MARGIN = 0.3f;
        private const float HUD_LINES_PER_SCREEN = 36f;
        private const float HUD_WIDTH_IN_LINES = 26f;
        private const float HUD_HEIGHT_IN_LINES = 10f;
        private const float MOWER_BODY_RADIUS = 0.3f;

        private static readonly RectInt s_flowerZone = new(50, 6, 16, 20);
        private static readonly RectInt s_thickGrassPath = new(8, 32, 56, 5);
        private static readonly RectInt s_lowBushZone = new(24, 42, 20, 10);
        private static readonly RectInt s_hardBushZone = new(4, 44, 12, 9);
        private static readonly RectInt s_protectedBorder = new(50, 37, 16, 16);
        private static readonly RectInt s_protectedBed = new(52, 39, 12, 12);

        [SerializeField]
        private GrassFieldRenderer _field;

        [SerializeField]
        private GrassMowerController _mower;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Renderer _protectedZone;

        [SerializeField]
        private GrassClippingsEmitter _clippings;

        [SerializeField]
        private CuttablePropController _props;

        [SerializeField]
        private float _cameraShake = 0.12f;

        [SerializeField]
        private int _clippingsPerCell = 3;

        [SerializeField]
        private int _petalsPerFlower = 2;

        [SerializeField]
        private PlantSettings[] _plants = Array.Empty<PlantSettings>();

        [SerializeField]
        private Vector2 _spawn = new(2f, 2f);

        [SerializeField]
        private float _spawnClearing = 1f;

        [SerializeField]
        private int _seed = 4;

        [SerializeField]
        private int[] _xpThresholds = { 100, 260, 480 };

        [SerializeField]
        private float _baseCutRadius = 0.65f;

        [SerializeField]
        private float _cutRadiusStep = 0.15f;

        [SerializeField]
        private float _maxCutRadius = 1.1f;

        [SerializeField]
        private float _radiusTweenSeconds = 0.25f;

        [SerializeField]
        private bool _pauseOnLevelUp;

        [SerializeField]
        private float _baseCuttingPower = 1f;

        [SerializeField]
        private float _cuttingPowerStep = 0.3f;

        [SerializeField]
        private float _baseSpeed = 4f;

        [SerializeField]
        private float _speedStep = 0.25f;

        [SerializeField]
        private float _maxSpeed = 4.75f;

        [SerializeField]
        private float _warningDistance = 0.5f;

        [SerializeField]
        private float _protectedCooldown = 1f;

        [SerializeField]
        private float _cameraPitch = 60f;

        [SerializeField]
        private float _cameraDistance = 25f;

        [SerializeField]
        private float _cameraLookAhead = 1.2f;

        [SerializeField]
        private float _cameraSmoothing = 6f;

        private readonly int[] _harvestedByKind = new int[(int)PlantKind.ProtectedFlower + 1];
        private readonly Color[] _clippingColorByKind = new Color[(int)PlantKind.ProtectedFlower + 1];
        private readonly List<int> _harvestedCells = new();

        private FieldGrid _grid;
        private FieldFeedback _feedback;
        private GrassCutter _cutter;
        private MaterialPropertyBlock _zoneProperties;
        private Vector3 _previousBlade;
        private Vector3 _cameraFocus;
        private float _cutRadius;
        private float _zoneFlash;
        private float _lastProtectedTouch;
        private int _tier;
        private int _xp;
        private int _wideBladeUpgrades;
        private int _engineUpgrades;
        private int _pendingUpgrades;
        private int _protectedHits;
        private bool _wasResetHeld;
        private bool _wasWideBladeHeld;
        private bool _wasEngineHeld;
        private GUIStyle _hudStyle;

        private float TargetCutRadius => Mathf.Min(_baseCutRadius + _wideBladeUpgrades * _cutRadiusStep, _maxCutRadius);

        private float CuttingPower => _baseCuttingPower + _engineUpgrades * _cuttingPowerStep;

        private float Speed => Mathf.Min(_baseSpeed + _engineUpgrades * _speedStep, _maxSpeed);

        private static bool IsNewPress(bool isHeld, ref bool wasHeld)
        {
            var isNewPress = isHeld && wasHeld == false;
            wasHeld = isHeld;
            return isNewPress;
        }

        private void Start()
        {
            _grid = new FieldGrid(CELLS_X, CELLS_Z, CELL_SIZE);
            _feedback = new FieldFeedback(_grid.Count);
            _cutter = new GrassCutter(_grid, _feedback, _plants);
            _zoneProperties = new MaterialPropertyBlock();

            IndexClippingColors();
            CreateLayout();
            ClearPropCells();
            PlaceProtectedZone();

            _field.Build(_grid, _plants, _seed);
            _mower.Bounds = _grid.Bounds;

            ResetRun();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            HandleKeys();

            var isChoosingUpgrade = _pauseOnLevelUp && _pendingUpgrades > 0;

            if (isChoosingUpgrade == false)
            {
                _mower.MaxSpeed = Speed;
                _mower.Step(deltaTime, _camera);
                PushMowerOutOfProps();

                var radiusDelta = _cutRadiusStep / _radiusTweenSeconds * deltaTime;
                _cutRadius = Mathf.MoveTowards(_cutRadius, TargetCutRadius, radiusDelta);

                var blade = _mower.transform.position;
                Cut(_previousBlade, blade, deltaTime);
                CutProps(blade, deltaTime);
                _previousBlade = blade;
            }

            DecayFeedback(deltaTime);
            WriteCellStates();
            UpdateBlade();
            UpdateProtectedZone();
        }

        private void LateUpdate()
        {
            if (_camera.IsInvalid())
            {
                return;
            }

            var mowerPosition = _mower.transform.position;
            var lookAhead = _mower.Velocity / Mathf.Max(Speed, MIN_SEGMENT) * _cameraLookAhead;
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

            GUILayout.BeginArea(new Rect(new Vector2(margin, margin), size), GUI.skin.box);
            HudLine($"Tier {_tier}   XP {_xp}{NextThresholdText()}");
            HudLine($"Cut radius {TargetCutRadius:0.00} m   Power {CuttingPower:0.00}");
            HudLine($"Speed {Speed:0.00} m/s   Protected hits {_protectedHits}");
            HudLine($"Grass {Harvested(PlantKind.Grass)}   Flowers {Harvested(PlantKind.HarvestFlower)}");
            HudLine($"Thick grass {Harvested(PlantKind.ThickGrass)}   Low bush {Harvested(PlantKind.LowBush)}");
            HudLine($"Hard bush {Harvested(PlantKind.HardBush)}");
            HudLine(PropsText());

            if (_props.IsValid() && _props.IsTouchingLocked)
            {
                HudLine($"<color=#ff8080>Needs tier {_props.LockedTier}</color>");
            }

            if (_pendingUpgrades > 0)
            {
                HudLine($"<color=yellow>LEVEL UP x{_pendingUpgrades}: 1 = Wide blade, 2 = Strong engine</color>");
            }

            HudLine("Move: WASD / arrows / drag / gamepad   R: reset");
            GUILayout.EndArea();
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

            _props.Initialize(_mower.transform);

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
            mowerTransform.position = _props.PushOut(mowerTransform.position, MOWER_BODY_RADIUS);
        }

        private void CutProps(Vector3 blade, float deltaTime)
        {
            if (_props.IsInvalid())
            {
                return;
            }

            var xp = _props.Cut(blade, _cutRadius, _tier, CuttingPower, deltaTime);

            if (xp > 0)
            {
                AddXp(xp);
            }
        }

        private void HudLine(string text)
        {
            GUILayout.Label(text, _hudStyle);
        }

        private string NextThresholdText()
        {
            var thresholdIndex = _tier - 1;
            return thresholdIndex < _xpThresholds.Length ? $" / {_xpThresholds[thresholdIndex]}" : " (max tier)";
        }

        private int Harvested(PlantKind kind)
            => _harvestedByKind[(int)kind];

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

        private void CreateLayout()
        {
            _grid.Fill(new RectInt(0, 0, CELLS_X, CELLS_Z), PlantKind.Grass);
            _grid.Fill(s_flowerZone, PlantKind.HarvestFlower);
            _grid.Fill(s_thickGrassPath, PlantKind.ThickGrass);
            _grid.Fill(s_lowBushZone, PlantKind.LowBush);
            _grid.Fill(s_hardBushZone, PlantKind.HardBush);
            _grid.Fill(s_protectedBorder, PlantKind.None);
            _grid.Fill(s_protectedBed, PlantKind.ProtectedFlower);
            _grid.ClearCircle(_spawn, _spawnClearing);
        }

        private void PlaceProtectedZone()
        {
            if (_protectedZone.IsInvalid())
            {
                return;
            }

            var min = _grid.Origin + new Vector2(s_protectedBed.xMin, s_protectedBed.yMin) * CELL_SIZE;
            var size = new Vector2(s_protectedBed.width, s_protectedBed.height) * CELL_SIZE;
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
            Array.Clear(_harvestedByKind, 0, _harvestedByKind.Length);

            _tier = 1;
            _xp = 0;
            _wideBladeUpgrades = 0;
            _engineUpgrades = 0;
            _pendingUpgrades = 0;
            _protectedHits = 0;
            _zoneFlash = 0f;
            _lastProtectedTouch = float.NegativeInfinity;
            _cutRadius = TargetCutRadius;

            var spawn = _grid.ToWorld(_spawn);
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

            var isWideBladeHeld = keyboard.digit1Key.isPressed || keyboard.numpad1Key.isPressed;
            var isEngineHeld = keyboard.digit2Key.isPressed || keyboard.numpad2Key.isPressed;
            var resetPressed = IsNewPress(keyboard.rKey.isPressed, ref _wasResetHeld);
            var widePressed = IsNewPress(isWideBladeHeld, ref _wasWideBladeHeld);
            var enginePressed = IsNewPress(isEngineHeld, ref _wasEngineHeld);

            if (resetPressed)
            {
                ResetRun();
                return;
            }

            if (_pendingUpgrades == 0)
            {
                return;
            }

            if (widePressed)
            {
                _wideBladeUpgrades++;
                _pendingUpgrades--;
            }
            else if (enginePressed)
            {
                _engineUpgrades++;
                _pendingUpgrades--;
            }
        }

        private void Cut(Vector3 from, Vector3 to, float deltaTime)
        {
            var stroke = new CutStroke(from, to, _cutRadius, _tier, CuttingPower, deltaTime);
            var touchedProtected = _cutter.Cut(stroke, _harvestedCells);
            var harvestedCount = _harvestedCells.Count;

            for (var i = 0; i < harvestedCount; i++)
            {
                var index = _harvestedCells[i];
                ref readonly var plant = ref _cutter.GetPlant(_grid.GetKind(index));
                Harvest(plant.Kind, plant.Xp);
                EmitClippings(index, plant);
            }

            _harvestedCells.Clear();

            if (touchedProtected)
            {
                RegisterProtectedTouch();
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

        private void Harvest(PlantKind kind, int xp)
        {
            _harvestedByKind[(int)kind]++;
            AddXp(xp);
        }

        private void AddXp(int xp)
        {
            _xp += xp;

            while (_tier - 1 < _xpThresholds.Length && _xp >= _xpThresholds[_tier - 1])
            {
                _tier++;
                _pendingUpgrades++;
            }
        }

        private void RegisterProtectedTouch()
        {
            var now = Time.time;

            if (now - _lastProtectedTouch > _protectedCooldown)
            {
                _protectedHits++;
                _zoneFlash = 1f;
            }

            _lastProtectedTouch = now;
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
            var bladeXZ = _grid.ToLocal(blade);
            var bedMin = new Vector2(s_protectedBed.xMin, s_protectedBed.yMin) * CELL_SIZE;
            var bedMax = new Vector2(s_protectedBed.xMax, s_protectedBed.yMax) * CELL_SIZE;
            var nearest = Vector2.Max(bedMin, Vector2.Min(bladeXZ, bedMax));
            var gap = Vector2.Distance(bladeXZ, nearest) - _cutRadius;
            var warning = 1f - Mathf.Clamp01(gap / _warningDistance);

            _field.SetBlade(blade, _cutRadius, warning);
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
