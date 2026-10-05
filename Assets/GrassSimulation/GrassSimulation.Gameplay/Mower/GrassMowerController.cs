using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassMowerController : MonoBehaviour
    {
        private const float DEAD_ZONE = 0.1f;
        private const float MIN_TURN_SPEED = 0.05f;

        [SerializeField]
        private Transform _model;

        [SerializeField]
        private float _acceleration = 12f;

        [SerializeField]
        private float _deceleration = 18f;

        [SerializeField]
        private float _turnDegreesPerSecond = 540f;

        [SerializeField]
        private float _joystickRadiusPixels = 90f;

        private Vector3 _velocity;
        private Vector2 _joystickOrigin;
        private bool _isDragging;

        public Vector3 Velocity => _velocity;

        public float MaxSpeed { get; set; } = 4f;

        public Rect Bounds { get; set; }

        private static Vector2 ReadKeyboard()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var input = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            return input;
        }

        private static Vector2 ReadGamepad()
        {
            var gamepad = Gamepad.current;
            return gamepad == null ? Vector2.zero : gamepad.leftStick.ReadValue();
        }

        public void ResetTo(Vector3 position)
        {
            transform.position = position;
            _velocity = Vector3.zero;
            _isDragging = false;
        }

        public void Step(float deltaTime, Camera view)
        {
            var input = ReadInput();
            var yaw = view.IsValid() ? view.transform.eulerAngles.y : 0f;
            var targetVelocity = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y) * MaxSpeed;
            var isSpeedingUp = targetVelocity.sqrMagnitude > _velocity.sqrMagnitude;
            var rate = isSpeedingUp ? _acceleration : _deceleration;

            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, rate * deltaTime);
            transform.position = Slide(transform.position + _velocity * deltaTime);

            TurnModel(deltaTime);
        }

        private Vector3 Slide(Vector3 position)
        {
            var bounds = Bounds;

            if (position.x < bounds.xMin || position.x > bounds.xMax)
            {
                position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
                _velocity.x = 0f;
            }

            if (position.z < bounds.yMin || position.z > bounds.yMax)
            {
                position.z = Mathf.Clamp(position.z, bounds.yMin, bounds.yMax);
                _velocity.z = 0f;
            }

            return position;
        }

        private void TurnModel(float deltaTime)
        {
            if (_model.IsInvalid() || _velocity.magnitude < MIN_TURN_SPEED)
            {
                return;
            }

            var target = Quaternion.LookRotation(_velocity, Vector3.up);
            var maxDegrees = _turnDegreesPerSecond * deltaTime;
            _model.rotation = Quaternion.RotateTowards(_model.rotation, target, maxDegrees);
        }

        private Vector2 ReadInput()
        {
            var input = ReadKeyboard();
            var stick = ReadGamepad();
            var drag = ReadPointerDrag();

            if (stick.sqrMagnitude > input.sqrMagnitude)
            {
                input = stick;
            }

            if (drag.sqrMagnitude > input.sqrMagnitude)
            {
                input = drag;
            }

            return input.magnitude < DEAD_ZONE ? Vector2.zero : Vector2.ClampMagnitude(input, 1f);
        }

        private Vector2 ReadPointerDrag()
        {
            var pointer = Pointer.current;

            if (pointer == null || pointer.press.isPressed == false)
            {
                _isDragging = false;
                return Vector2.zero;
            }

            var position = pointer.position.ReadValue();

            if (_isDragging == false)
            {
                _isDragging = true;
                _joystickOrigin = position;
            }

            return (position - _joystickOrigin) / _joystickRadiusPixels;
        }
    }
}
