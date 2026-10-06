using UnityEngine;

namespace GrassSimulation.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField]
        private float _minTopMargin;

        private RectTransform _rectTransform;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedResolution;
        private Vector2 _appliedParentSize;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            var parentSize = GetParentSize();

            if (Screen.safeArea != _appliedSafeArea
                || Screen.width != _appliedResolution.x
                || Screen.height != _appliedResolution.y
                || parentSize != _appliedParentSize
            )
            {
                Apply();
            }
        }

        private void Apply()
        {
            _rectTransform = _rectTransform != null ? _rectTransform : (RectTransform)transform;

            var safeArea = Screen.safeArea;
            var width = Screen.width;
            var height = Screen.height;

            _appliedSafeArea = safeArea;
            _appliedResolution = new Vector2Int(width, height);
            _appliedParentSize = GetParentSize();

            if (width <= 0 || height <= 0)
            {
                return;
            }

            var anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            var anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            var parentHeight = _appliedParentSize.y;

            if (_minTopMargin > 0f && parentHeight > 0f)
            {
                anchorMax.y = Mathf.Min(anchorMax.y, 1f - _minTopMargin / parentHeight);
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        private Vector2 GetParentSize()
        {
            var rect = _rectTransform != null ? _rectTransform : (RectTransform)transform;

            return rect.parent is RectTransform parent ? parent.rect.size : Vector2.zero;
        }
    }
}
