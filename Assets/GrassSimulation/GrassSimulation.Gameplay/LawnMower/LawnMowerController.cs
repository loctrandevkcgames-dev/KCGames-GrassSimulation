using System;
using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassSimulation.Gameplay
{
    public sealed class LawnMowerController : MonoBehaviour
    {
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
        private float _joystickRadiusFraction = JoystickMath.RADIUS_WIDTH_FRACTION;

        [SerializeField]
        private float _joystickMinRadiusPixels = JoystickMath.MIN_RADIUS;

        [SerializeField]
        private Rect _joystickRegion = new Rect(x: 0f, y: 0f, width: 1f, height: 0.5f);

        private Vector3 _velocity;
        private JoystickDrag _drag;

        public Vector3 Velocity => _velocity;

        public bool IsDragging => _drag.IsDragging;

        public bool IsMoving => _velocity.sqrMagnitude > MIN_TURN_SPEED * MIN_TURN_SPEED;

        public Func<Vector2, bool> IsPointerBlocked { get; set; }

        public float MaxSpeed { get; set; } = 4f;

        public Rect Bounds { get; set; }

        public float BodyRadius { get; set; }

        public ObstacleField Obstacles { get; set; }

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
            _drag.Reset();
        }

        public void ResetInput()
        {
            _velocity = Vector3.zero;
            _drag.Reset();
        }

        public JoystickState GetJoystickState()
        {
            return new JoystickState(_drag.IsDragging, _drag.Origin, _drag.Input, _drag.Radius, IsMoving);
        }

        public void Step(float deltaTime, Camera view)
        {
            var input = ReadInput();
            var yaw = view.IsValid() ? view.transform.eulerAngles.y : 0f;
            var targetVelocity = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y) * MaxSpeed;
            var isSpeedingUp = targetVelocity.sqrMagnitude > _velocity.sqrMagnitude;
            var rate = isSpeedingUp ? _acceleration : _deceleration;

            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, rate * deltaTime);
            transform.position = SlideAlongObstacles(Slide(transform.position + _velocity * deltaTime));

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

        private Vector3 SlideAlongObstacles(Vector3 position)
        {
            if (Obstacles == null || Obstacles.Count == 0)
            {
                return position;
            }

            var velocity = new Vector2(_velocity.x, _velocity.z);
            var resolved = Obstacles.Resolve(new Vector2(position.x, position.z), BodyRadius, ref velocity);

            _velocity = new Vector3(velocity.x, _velocity.y, velocity.y);
            return Slide(new Vector3(resolved.x, position.y, resolved.y));
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

            return JoystickMath.ApplyDeadZone(input);
        }

        private Vector2 ReadPointerDrag()
        {
            var pointer = Pointer.current;

            if (pointer == null)
            {
                _drag.Reset();
                return Vector2.zero;
            }

            var screenSize = new Vector2(Screen.width, Screen.height);
            var radius = JoystickMath.GetRadius(
                  referenceSize: Mathf.Min(screenSize.x, screenSize.y)
                , sizeFraction: _joystickRadiusFraction
                , minRadius: _joystickMinRadiusPixels
            );

            return _drag.Update(
                  isPressed: pointer.press.isPressed
                , pointerId: pointer.deviceId
                , position: pointer.position.ReadValue()
                , screenSize: screenSize
                , region: _joystickRegion
                , isBlocked: IsPointerBlocked
                , radius: radius
            );
        }
    }
}
