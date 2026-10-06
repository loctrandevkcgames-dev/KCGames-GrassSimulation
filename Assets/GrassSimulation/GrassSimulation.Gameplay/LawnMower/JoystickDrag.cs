using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public struct JoystickDrag
    {
        private Vector2 _origin;
        private Vector2 _input;
        private float _radius;
        private int _pressPointerId;
        private bool _isPressed;
        private bool _isDragging;

        public readonly bool IsDragging => _isDragging;

        public readonly Vector2 Origin => _origin;

        public readonly Vector2 Input => _input;

        public readonly float Radius => _radius;

        public void Reset()
        {
            _input = Vector2.zero;
            _isPressed = false;
            _isDragging = false;
        }

        public Vector2 Update(
              bool isPressed
            , int pointerId
            , Vector2 position
            , Vector2 screenSize
            , Rect region
            , Func<Vector2, bool> isBlocked
            , float radius
        )
        {
            if (isPressed == false)
            {
                Reset();
                return Vector2.zero;
            }

            if (_isPressed && pointerId != _pressPointerId)
            {
                _input = Vector2.zero;
                _isDragging = false;
                _pressPointerId = pointerId;
                return Vector2.zero;
            }

            if (_isPressed == false)
            {
                _isPressed = true;
                _pressPointerId = pointerId;

                if (JoystickMath.CanStartDrag(position, screenSize, region, isBlocked))
                {
                    _isDragging = true;
                    _origin = position;
                    _radius = radius;
                }
            }

            if (_isDragging == false)
            {
                return Vector2.zero;
            }

            _input = JoystickMath.GetInput(_origin, position, _radius);
            return _input;
        }
    }
}
