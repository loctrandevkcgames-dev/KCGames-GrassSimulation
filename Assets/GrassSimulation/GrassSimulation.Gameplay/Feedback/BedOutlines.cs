using System;
using System.Collections.Generic;
using EncosyTower.UnityExtensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrassSimulation.Gameplay
{
    public sealed class BedOutlines
    {
        private const float FLASH_DECAY = 2f;
        private const float MARGIN = 0.3f;
        private const float HEIGHT = 0.02f;

        private readonly Renderer _template;
        private readonly List<Renderer> _renderers = new();
        private readonly MaterialPropertyBlock _properties = new();

        private float[] _flashes = Array.Empty<float>();

        public BedOutlines(Renderer template)
        {
            _template = template;
        }

        public void Place(FieldGrid grid, ReadOnlySpan<RectInt> beds)
        {
            if (_template.IsInvalid())
            {
                return;
            }

            var bedCount = beds.Length;

            _flashes = new float[bedCount];

            for (var i = _renderers.Count; i < bedCount; i++)
            {
                var outline = i == 0 ? _template : Object.Instantiate(_template, _template.transform.parent);

                _renderers.Add(outline);
            }

            var cellSize = grid.CellSize;

            for (var i = 0; i < _renderers.Count; i++)
            {
                var isUsed = i < bedCount;

                _renderers[i].gameObject.SetActive(isUsed);

                if (isUsed == false)
                {
                    continue;
                }

                var bed = beds[i];
                var min = grid.Origin + new Vector2(bed.xMin, bed.yMin) * cellSize;
                var size = new Vector2(bed.width, bed.height) * cellSize;
                var center = min + size * 0.5f;
                var outline = _renderers[i].transform;

                outline.SetPositionAndRotation(new Vector3(center.x, HEIGHT, center.y), Quaternion.Euler(90f, 0f, 0f));
                outline.localScale = new Vector3(size.x + MARGIN, size.y + MARGIN, 1f);
            }
        }

        public void Flash(int bed)
        {
            if ((uint)bed < (uint)_flashes.Length)
            {
                _flashes[bed] = 1f;
            }
        }

        public void Clear()
        {
            Array.Clear(_flashes, 0, _flashes.Length);
        }

        public void Step(float deltaTime)
        {
            var bedCount = _flashes.Length;

            for (var i = 0; i < bedCount && i < _renderers.Count; i++)
            {
                _flashes[i] = Mathf.Max(_flashes[i] - FLASH_DECAY * deltaTime, 0f);

                var outline = _renderers[i];

                outline.GetPropertyBlock(_properties);
                _properties.SetFloat(GrassFieldShaderIds.Flash, _flashes[i]);
                outline.SetPropertyBlock(_properties);
            }
        }
    }
}
