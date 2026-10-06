using System;
using System.Collections.Generic;
using EncosyTower.Pooling;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class PlantObjectPresenter : MonoBehaviour
    {
        private const float KICK_DECAY = 5f;
        private const float LEAF_SHADE = 0.2f;

        [SerializeField]
        private GrassClippingsEmitter _chips;

        [SerializeField]
        private FruitSliceEffects _sliceEffects;

        [SerializeField]
        private float _chipInterval = 0.1f;

        [SerializeField]
        private int _chipsPerHit = 3;

        [SerializeField]
        private int _burstChips = 14;

        [SerializeField]
        private int _burstLeaves = 16;

        [SerializeField]
        private float _harvestKick = 1f;

        private readonly List<Slot> _slots = new();
        private readonly Dictionary<GameObject, GameObjectPool> _pools = new();

        private PlantObjectField _field;
        private float _chipTimer;

        public float CameraKick { get; private set; }

        public void Load(PlantObjectField field, ReadOnlySpan<PlantPlacement> placements, Vector2 origin)
        {
            ReleaseAll();

            _field = field;

            var count = placements.Length;

            for (var i = 0; i < count; i++)
            {
                SpawnSlot(i, in placements[i], origin);
            }

            ResetAll();
        }

        public void ResetAll()
        {
            var count = _slots.Count;

            for (var i = 0; i < count; i++)
            {
                var view = _slots[i].view;

                if (view.IsValid())
                {
                    view.Setup(_field.Get(i).FruitCount);
                }
            }

            CameraKick = 0f;
        }

        public void PlayHarvest(in PlantHarvest harvest, Vector3 blade)
        {
            var view = _slots[harvest.Index].view;

            if (view.IsInvalid())
            {
                return;
            }

            ref readonly var plant = ref _field.GetPlant(harvest.Kind);

            view.Harvest(blade);
            CameraKick = Mathf.Max(CameraKick, _harvestKick);

            if (plant.IsFruit && _sliceEffects.IsValid() && plant.FruitCount <= 0)
            {
                var cutDirection = view.transform.position - blade;

                _sliceEffects.Play(view.EffectPosition, cutDirection, plant.EffectColor, view.EffectSize);
                return;
            }

            EmitChips(view, blade, _burstChips, plant.EffectColor);
            EmitChips(view, blade, _burstLeaves, Color.Lerp(plant.EffectColor, Color.black, LEAF_SHADE));
        }

        public void Tick(float deltaTime, Vector3 blade, Quaternion cameraRotation)
        {
            CameraKick = Mathf.Max(CameraKick - KICK_DECAY * deltaTime, 0f);

            var emitChips = AdvanceChipTimer(deltaTime);
            var count = _slots.Count;

            for (var i = 0; i < count; i++)
            {
                var view = _slots[i].view;
                var state = _field.Get(i);

                if (view.IsInvalid())
                {
                    continue;
                }

                view.Face(cameraRotation);

                if (state.Harvested)
                {
                    continue;
                }

                view.SetProgress(state.Progress);

                if (emitChips && view.IsCutting)
                {
                    EmitChips(view, blade, _chipsPerHit, _field.GetPlant(state.Kind).EffectColor);
                }
            }
        }

        private void OnDestroy()
        {
            ReleaseAll();
            _pools.Clear();
        }

        private void SpawnSlot(int index, in PlantPlacement placement, Vector2 origin)
        {
            ref readonly var plant = ref _field.GetPlant(placement.Kind);
            var prefabs = plant.ObjectPrefabs;

            if (prefabs == null || prefabs.Length == 0)
            {
                ThrowHelper.LogError_MissingObjectPrefab(placement.Kind);
                _slots.Add(default);
                return;
            }

            var prefab = prefabs[index % prefabs.Length];
            var pool = GetPool(prefab);
            var instance = pool.RentGameObject(RentingStrategy.Default);
            var scale = placement.Scale > 0f ? placement.Scale : 1f;
            var position = placement.Position + origin;
            var rotation = Quaternion.Euler(0f, placement.Yaw, 0f);

            instance.transform.SetPositionAndRotation(new Vector3(position.x, 0f, position.y), rotation);
            instance.transform.localScale = Vector3.one * scale;

            _slots.Add(new Slot {
                pool = pool,
                gameObject = instance,
                view = instance.GetComponent<PlantObjectView>(),
            });
        }

        private void ReleaseAll()
        {
            var count = _slots.Count;

            for (var i = 0; i < count; i++)
            {
                var slot = _slots[i];

                if (slot.gameObject.IsValid())
                {
                    slot.pool.Return(slot.gameObject, ReturningStrategy.Default);
                }
            }

            _slots.Clear();
        }

        private GameObjectPool GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            var root = new GameObject($"Pool {prefab.name}").transform;

            root.SetParent(parent: transform, worldPositionStays: false);

            pool = new GameObjectPool {
                Prefab = new GameObjectPrefab { Source = prefab, Parent = root },
                RentingStrategy = RentingStrategy.Activate,
                ReturningStrategy = ReturningStrategy.Deactivate,
                TrimCloneSuffix = true,
            };

            _pools.Add(prefab, pool);
            return pool;
        }

        private bool AdvanceChipTimer(float deltaTime)
        {
            _chipTimer -= deltaTime;

            if (_chipTimer > 0f)
            {
                return false;
            }

            _chipTimer = _chipInterval;
            return true;
        }

        private void EmitChips(PlantObjectView view, Vector3 blade, int count, Color color)
        {
            if (_chips.IsInvalid())
            {
                return;
            }

            _chips.Emit(view.EffectPosition, view.transform.position - blade, color, count, ClippingShape.Chip);
        }

        private struct Slot
        {
            public GameObjectPool pool;
            public GameObject gameObject;
            public PlantObjectView view;
        }
    }
}
