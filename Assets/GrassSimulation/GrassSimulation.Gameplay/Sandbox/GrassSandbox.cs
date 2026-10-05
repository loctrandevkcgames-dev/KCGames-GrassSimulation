using System;
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
        private const float SHAKE_DECAY = 4f;
        private const float LOCK_FLASH_DECAY = 3f;
        private const float PROTECTED_FLASH_DECAY = 2f;
        private const float ZONE_MARGIN = 0.3f;
        private const byte FULL = 255;
        private const float PARTIAL_SCALE = 254f;
        private const float HUD_LINES_PER_SCREEN = 36f;
        private const float HUD_WIDTH_IN_LINES = 26f;
        private const float HUD_HEIGHT_IN_LINES = 9f;

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

        private readonly PlantSettings[] _plantByKind = new PlantSettings[(int)PlantKind.ProtectedFlower + 1];
        private readonly int[] _harvestedByKind = new int[(int)PlantKind.ProtectedFlower + 1];
        private readonly Color[] _clippingColorByKind = new Color[(int)PlantKind.ProtectedFlower + 1];
        private readonly PlantKind[] _kinds = new PlantKind[CELLS_X * CELLS_Z];
        private readonly float[] _progress = new float[CELLS_X * CELLS_Z];
        private readonly float[] _shake = new float[CELLS_X * CELLS_Z];
        private readonly float[] _lockFlash = new float[CELLS_X * CELLS_Z];
        private readonly float[] _protectedFlash = new float[CELLS_X * CELLS_Z];

        private MaterialPropertyBlock _zoneProperties;
        private Vector2 _origin;
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

        private static void Fill(PlantKind[] kinds, RectInt rect, PlantKind kind)
        {
            for (var z = rect.yMin; z < rect.yMax; z++)
            {
                for (var x = rect.xMin; x < rect.xMax; x++)
                {
                    kinds[z * CELLS_X + x] = kind;
                }
            }
        }

        private static Vector2 CellCenter(int x, int z)
            => new((x + 0.5f) * CELL_SIZE, (z + 0.5f) * CELL_SIZE);

        private static float ContactCoverage(Vector2 from, Vector2 to, Vector2 point, float radius)
        {
            var segment = to - from;
            var length = segment.magnitude;

            if (length < MIN_SEGMENT)
            {
                return (point - to).sqrMagnitude <= radius * radius ? 1f : 0f;
            }

            var direction = segment / length;
            var offset = point - from;
            var along = Vector2.Dot(offset, direction);
            var across = Mathf.Abs(direction.x * offset.y - direction.y * offset.x);

            if (across > radius)
            {
                return 0f;
            }

            var halfChord = Mathf.Sqrt(radius * radius - across * across);
            var enter = Mathf.Max(along - halfChord, 0f);
            var exit = Mathf.Min(along + halfChord, length);
            return Mathf.Max(exit - enter, 0f) / length;
        }

        private static bool IsNewPress(bool isHeld, ref bool wasHeld)
        {
            var isNewPress = isHeld && wasHeld == false;
            wasHeld = isHeld;
            return isNewPress;
        }

        private static byte ToByte(float value)
            => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * FULL);

        private static byte ToClearanceByte(float progress)
            => progress >= 1f ? FULL : (byte)(Mathf.Clamp01(progress) * PARTIAL_SCALE);

        private void Start()
        {
            _origin = new Vector2(-CELLS_X * CELL_SIZE * 0.5f, -CELLS_Z * CELL_SIZE * 0.5f);
            _zoneProperties = new MaterialPropertyBlock();

            IndexPlants();
            CreateLayout();
            PlaceProtectedZone();

            _field.Build(_origin, CELLS_X, CELLS_Z, CELL_SIZE, _kinds, _plants, _seed);
            _mower.Bounds = new Rect(_origin, new Vector2(CELLS_X * CELL_SIZE, CELLS_Z * CELL_SIZE));

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

                var radiusDelta = _cutRadiusStep / _radiusTweenSeconds * deltaTime;
                _cutRadius = Mathf.MoveTowards(_cutRadius, TargetCutRadius, radiusDelta);

                var blade = _mower.transform.position;
                Cut(_previousBlade, blade, deltaTime);
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

            if (_pendingUpgrades > 0)
            {
                HudLine($"<color=yellow>LEVEL UP x{_pendingUpgrades}: 1 = Wide blade, 2 = Strong engine</color>");
            }

            HudLine("Move: WASD / arrows / drag / gamepad   R: reset");
            GUILayout.EndArea();
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

        private void IndexPlants()
        {
            var plantCount = _plants.Length;

            for (var i = 0; i < plantCount; i++)
            {
                var plant = _plants[i];
                var kindIndex = (int)plant.Kind;
                _plantByKind[kindIndex] = plant;

                if (plant.Material.IsValid())
                {
                    _clippingColorByKind[kindIndex] = plant.Material.GetColor(GrassFieldShaderIds.TipColor);
                }
            }
        }

        private void CreateLayout()
        {
            Fill(_kinds, new RectInt(0, 0, CELLS_X, CELLS_Z), PlantKind.Grass);
            Fill(_kinds, s_flowerZone, PlantKind.HarvestFlower);
            Fill(_kinds, s_thickGrassPath, PlantKind.ThickGrass);
            Fill(_kinds, s_lowBushZone, PlantKind.LowBush);
            Fill(_kinds, s_hardBushZone, PlantKind.HardBush);
            Fill(_kinds, s_protectedBorder, PlantKind.None);
            Fill(_kinds, s_protectedBed, PlantKind.ProtectedFlower);

            for (var z = 0; z < CELLS_Z; z++)
            {
                for (var x = 0; x < CELLS_X; x++)
                {
                    if (Vector2.Distance(CellCenter(x, z), _spawn) <= _spawnClearing)
                    {
                        _kinds[z * CELLS_X + x] = PlantKind.None;
                    }
                }
            }
        }

        private void PlaceProtectedZone()
        {
            if (_protectedZone.IsInvalid())
            {
                return;
            }

            var min = _origin + new Vector2(s_protectedBed.xMin, s_protectedBed.yMin) * CELL_SIZE;
            var size = new Vector2(s_protectedBed.width, s_protectedBed.height) * CELL_SIZE;
            var center = min + size * 0.5f;
            var zone = _protectedZone.transform;

            zone.SetPositionAndRotation(new Vector3(center.x, 0.02f, center.y), Quaternion.Euler(90f, 0f, 0f));
            zone.localScale = new Vector3(size.x + ZONE_MARGIN, size.y + ZONE_MARGIN, 1f);
        }

        private void ResetRun()
        {
            Array.Clear(_progress, 0, _progress.Length);
            Array.Clear(_shake, 0, _shake.Length);
            Array.Clear(_lockFlash, 0, _lockFlash.Length);
            Array.Clear(_protectedFlash, 0, _protectedFlash.Length);
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

            var spawn = new Vector3(_origin.x + _spawn.x, 0f, _origin.y + _spawn.y);
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
            var position = _cameraFocus - rotation * Vector3.forward * _cameraDistance;
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
            var radius = _cutRadius;
            var start = new Vector2(from.x, from.z) - _origin;
            var end = new Vector2(to.x, to.z) - _origin;
            var min = Vector2.Min(start, end) - Vector2.one * radius;
            var max = Vector2.Max(start, end) + Vector2.one * radius;
            var xMin = Mathf.Max(0, Mathf.FloorToInt(min.x / CELL_SIZE));
            var zMin = Mathf.Max(0, Mathf.FloorToInt(min.y / CELL_SIZE));
            var xMax = Mathf.Min(CELLS_X - 1, Mathf.FloorToInt(max.x / CELL_SIZE));
            var zMax = Mathf.Min(CELLS_Z - 1, Mathf.FloorToInt(max.y / CELL_SIZE));
            var touchedProtected = false;

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var index = z * CELLS_X + x;
                    var kind = _kinds[index];

                    if (kind == PlantKind.None)
                    {
                        continue;
                    }

                    var coverage = ContactCoverage(start, end, CellCenter(x, z), radius);

                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    touchedProtected |= CutCell(index, kind, coverage * deltaTime);
                }
            }

            if (touchedProtected)
            {
                RegisterProtectedTouch();
            }
        }

        private bool CutCell(int index, PlantKind kind, float contactTime)
        {
            ref readonly var plant = ref _plantByKind[(int)kind];

            if (plant.IsProtected)
            {
                _protectedFlash[index] = 1f;
                return true;
            }

            if (_progress[index] >= 1f)
            {
                return false;
            }

            if (plant.RequiredTier > _tier)
            {
                _lockFlash[index] = 1f;
                return false;
            }

            _shake[index] = 1f;
            _progress[index] += contactTime * CuttingPower / plant.Toughness;

            if (_progress[index] >= 1f)
            {
                _progress[index] = 1f;
                Harvest(kind, plant.Xp);
                EmitClippings(index, plant);
            }

            return false;
        }

        private void EmitClippings(int index, in PlantSettings plant)
        {
            if (_clippings.IsInvalid())
            {
                return;
            }

            var center = _origin + CellCenter(index % CELLS_X, index / CELLS_X);
            var position = new Vector3(center.x, 0f, center.y);
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
            var count = _kinds.Length;

            for (var i = 0; i < count; i++)
            {
                _shake[i] = Mathf.Max(_shake[i] - SHAKE_DECAY * deltaTime, 0f);
                _lockFlash[i] = Mathf.Max(_lockFlash[i] - LOCK_FLASH_DECAY * deltaTime, 0f);
                _protectedFlash[i] = Mathf.Max(_protectedFlash[i] - PROTECTED_FLASH_DECAY * deltaTime, 0f);
            }

            _zoneFlash = Mathf.Max(_zoneFlash - PROTECTED_FLASH_DECAY * deltaTime, 0f);
        }

        private void WriteCellStates()
        {
            var states = _field.CellStates;
            var count = _kinds.Length;

            for (var i = 0; i < count; i++)
            {
                var clearance = _kinds[i] == PlantKind.None ? FULL : ToClearanceByte(_progress[i]);
                var shake = ToByte(_shake[i]);
                var lockFlash = ToByte(_lockFlash[i]);
                var protectedFlash = ToByte(_protectedFlash[i]);
                states[i] = new Color32(clearance, shake, lockFlash, protectedFlash);
            }

            _field.MarkCellStatesDirty();
        }

        private void UpdateBlade()
        {
            var blade = _mower.transform.position;
            var bladeXZ = new Vector2(blade.x, blade.z) - _origin;
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
