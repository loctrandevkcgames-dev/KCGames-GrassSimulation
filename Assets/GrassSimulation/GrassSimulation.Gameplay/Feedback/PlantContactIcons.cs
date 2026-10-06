using System;
using System.Collections.Generic;
using EncosyTower.Pooling;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class PlantContactIcons : MonoBehaviour
    {
        [SerializeField]
        private Sprite _lockSprite;

        [SerializeField]
        private Sprite _slowSprite;

        [SerializeField]
        private float _lifetime = 0.9f;

        [SerializeField]
        private float _rise = 0.6f;

        [SerializeField]
        private float _cooldown = 0.7f;

        [SerializeField]
        private float _height = 0.9f;

        [SerializeField]
        private float _size = 0.9f;

        [SerializeField]
        private int _sortingOrder = 100;

        private readonly List<Icon> _icons = new();
        private readonly float[] _lockedUntil = new float[PlantKindExtensions.Length];
        private readonly float[] _slowUntil = new float[PlantKindExtensions.Length];

        private GameObject _template;
        private GameObjectPool _pool;

        public void ShowLocked(PlantKind kind, Vector3 position)
        {
            Show(_lockSprite, _lockedUntil, kind, position);
        }

        public void ShowSlow(PlantKind kind, Vector3 position)
        {
            Show(_slowSprite, _slowUntil, kind, position);
        }

        public void Clear()
        {
            var count = _icons.Count;

            for (var i = 0; i < count; i++)
            {
                _pool.Return(_icons[i].target.gameObject, ReturningStrategy.Default);
            }

            _icons.Clear();
            Array.Clear(_lockedUntil, 0, _lockedUntil.Length);
            Array.Clear(_slowUntil, 0, _slowUntil.Length);
        }

        public void Tick(float deltaTime, Quaternion cameraRotation)
        {
            for (var i = _icons.Count - 1; i >= 0; i--)
            {
                var icon = _icons[i];

                icon.age += deltaTime;

                if (icon.age >= _lifetime)
                {
                    _pool.Return(icon.target.gameObject, ReturningStrategy.Default);
                    _icons.RemoveAt(i);
                    continue;
                }

                var t = icon.age / _lifetime;
                var fade = 1f - t * t;

                icon.target.SetPositionAndRotation(icon.start + Vector3.up * (_rise * t), cameraRotation);
                icon.renderer.color = new Color(1f, 1f, 1f, fade);
                _icons[i] = icon;
            }
        }

        private void OnDestroy()
        {
            if (_pool != null)
            {
                _pool.Dispose();
                _pool = null;
            }

            if (_template.IsValid())
            {
                Destroy(_template);
            }
        }

        private void Show(Sprite sprite, float[] cooldowns, PlantKind kind, Vector3 position)
        {
            var now = Time.time;

            if (sprite.IsInvalid() || now < cooldowns[(int)kind])
            {
                return;
            }

            cooldowns[(int)kind] = now + _cooldown;

            var instance = GetPool().RentGameObject(RentingStrategy.Default);
            var spriteRenderer = instance.GetComponent<SpriteRenderer>();
            var start = position + Vector3.up * _height;

            spriteRenderer.sprite = sprite;
            instance.transform.localScale = Vector3.one * _size;
            instance.transform.position = start;

            _icons.Add(new Icon { target = instance.transform, renderer = spriteRenderer, start = start });
        }

        private GameObjectPool GetPool()
        {
            if (_pool != null)
            {
                return _pool;
            }

            _template = new GameObject("PlantContactIconTemplate");
            _template.transform.SetParent(parent: transform, worldPositionStays: false);
            _template.SetActive(false);

            var spriteRenderer = _template.AddComponent<SpriteRenderer>();

            spriteRenderer.sortingOrder = _sortingOrder;

            var root = new GameObject("Pool PlantContactIcon").transform;

            root.SetParent(parent: transform, worldPositionStays: false);

            _pool = new GameObjectPool {
                Prefab = new GameObjectPrefab { Source = _template, Parent = root },
                RentingStrategy = RentingStrategy.Activate,
                ReturningStrategy = ReturningStrategy.Deactivate,
                TrimCloneSuffix = true,
            };

            return _pool;
        }

        private struct Icon
        {
            public Transform target;
            public SpriteRenderer renderer;
            public Vector3 start;
            public float age;
        }
    }
}
