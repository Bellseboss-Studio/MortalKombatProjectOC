using UnityEngine;

namespace _Scripts.Player
{
    internal class RotationCharacterV2 : MonoBehaviour
    {
        private GameObject _player;
        private GameObject _camera;
        private bool _isConfigured;
        private IRotationCharacterV2 _rotationCharacterV2;

        [Header("Rotación")] [SerializeField] private float baseRotationSpeed = 8f;
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float accelerationTime = 0.3f; // tiempo en segundos para alcanzar velocidad completa

        private float _rotationVelocity; // valor 0–1 que aumenta cuando hay input
        private Vector2 _vector2;
        private Vector3 _lastDirection;
        private bool _canChangeDirection;
        private bool _canRotate;
        private bool _canRotateWhileAttack;
        private bool _isChangingDirection;

        private float _currentRotationSpeed;
        private float _forceRotation;

        public void Configure(GameObject camera, GameObject player, IRotationCharacterV2 rotationCharacterV2,
            float forceRotation)
        {
            _camera = camera;
            _player = player;
            _rotationCharacterV2 = rotationCharacterV2;
            _forceRotation = forceRotation;
            _isConfigured = true;
            _canRotate = true;
        }

        public void Direction(Vector2 vector2) => _vector2 = vector2;
        public void Direction(Vector3 vector3) => _lastDirection = vector3;

        private void Update()
        {
            if (!_isConfigured || !_canRotate) return;

            // Si está rotando hacia un target (ataque, lock-on, etc)
            if (_canRotateWhileAttack)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_lastDirection);
                _player.transform.rotation = Quaternion.Slerp(
                    _player.transform.rotation,
                    targetRotation,
                    _forceRotation * Time.deltaTime
                );
                return;
            }

            // --- Rotación normal según input ---
            var direction = _player.transform.position - _camera.transform.position;
            direction.y = 0;
            direction.Normalize();

            var right = new Vector3(direction.z, 0, -direction.x);
            var desiredDir = _vector2.x * right + _vector2.y * direction;

            bool hasInput = desiredDir.sqrMagnitude > 0.01f;
            if (hasInput)
            {
                desiredDir.Normalize();
                _lastDirection = desiredDir;
                // Acelera progresivamente al rotar
                _rotationVelocity += Time.deltaTime / accelerationTime;
            }
            else
            {
                // Frena rotación progresivamente al soltar el input
                _rotationVelocity -= Time.deltaTime / accelerationTime;
            }

            _rotationVelocity = Mathf.Clamp01(_rotationVelocity);
            float curveValue = rotationCurve.Evaluate(_rotationVelocity);
            _currentRotationSpeed = baseRotationSpeed * curveValue;

            if (_lastDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_lastDirection);
                _player.transform.rotation = Quaternion.RotateTowards(
                    _player.transform.rotation,
                    targetRotation,
                    _currentRotationSpeed * Time.deltaTime * 100f
                );
            }
        }

        public void CanRotate(bool canRotate)
        {
            _vector2 = Vector2.zero;
            _canRotate = canRotate;
        }

        public bool CanRotate()
        {
            return _canRotate;
        }

        public void RotateToDirection(Vector3 direction)
        {
            //invert direction
            direction = -direction;
            //rotate to direction without lerp
            direction = new Vector3(direction.x, 0, direction.z);
            _player.transform.rotation = Quaternion.LookRotation(direction);
            _lastDirection = direction;
        }

        public void ChangeDirection(Vector3 rotation)
        {
            /*Debug.Log("Change Direction: " + rotation);*/
            _canChangeDirection = true;
            _lastDirection = rotation;
        }

        public void RestoreRotation()
        {
            _canChangeDirection = false;
        }

        public void CanRotateWhileAttack(bool canRotateWhileAttack)
        {
            _canRotateWhileAttack = canRotateWhileAttack;
        }

        public void RotateToLookTheTarget(Vector3 getTarget)
        {
            if (getTarget != Vector3.zero)
            {
                _lastDirection = getTarget - _player.transform.position;
                _lastDirection.y = 0;
            }
        }

        public void RotateInstant(Vector3 direction)
        {
            direction.y = 0;
            if (direction == Vector3.zero) return;

            _player.transform.rotation = Quaternion.LookRotation(direction);
            _lastDirection = direction;
        }
    }
}