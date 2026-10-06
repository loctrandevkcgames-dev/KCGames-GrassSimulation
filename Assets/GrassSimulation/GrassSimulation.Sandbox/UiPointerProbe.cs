using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GrassSimulation.Sandbox
{
    public sealed class UiPointerProbe
    {
        private readonly List<RaycastResult> _results = new();

        private PointerEventData _eventData;

        public bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return false;
            }

            _eventData ??= new PointerEventData(eventSystem);
            _eventData.position = screenPosition;
            _results.Clear();
            eventSystem.RaycastAll(_eventData, _results);

            return _results.Count > 0;
        }
    }
}
