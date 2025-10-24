using UnityEngine;

namespace _Scripts.Player
{
    internal class RotationCharacterV2 : MonoBehaviour
    {
        private GameObject _player;
        private GameObject _camera;
        private bool _isConfigured;
        private IRotationCharacterV2 _rotationCharacterV2;
        [SerializeField] private Vector2 _vector2;
        [SerializeField] private Vector3 rotationWithTarget;
        private float _forceRotation;
        [SerializeField] private bool _canRotate;
        private bool _canRotateWhileAttack;
        private Vector3 _lastDirection;
        private bool _canChangeDirection;

        public void Configure(GameObject camera, GameObject player, IRotationCharacterV2 rotationCharacterV2,
            float forceRotation)
        {
            _camera = camera;
            _player = player;
            _isConfigured = true;
            _rotationCharacterV2 = rotationCharacterV2;
            _forceRotation = forceRotation;
            _canRotate = true;
        }

        public void Direction(Vector2 vector2)
        {
            _vector2 = vector2;
        }
        
        public void Direction(Vector3 vector3)
        {
            rotationWithTarget = vector3;
        }

        private void Update()
        {
            if (!_isConfigured || !_canRotate) return;
            /*if (!_canChangeDirection) return;*/
            if (_canRotateWhileAttack)
            {
                _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, Quaternion.LookRotation(rotationWithTarget), _forceRotation * Time.deltaTime);
            }
            else
            {
                var direction = _player.transform.position - _camera.transform.position;
                direction.y = 0;
                direction.Normalize();
                var right = new Vector3(direction.z, 0, -direction.x);
                var result = _vector2.x * right + _vector2.y * direction;
                if (result != Vector3.zero && !_canChangeDirection)
                {
                    _lastDirection = result;
                    _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, Quaternion.LookRotation(result), _forceRotation * Time.deltaTime);
                }
                else if (_lastDirection != Vector3.zero)
                {
                    _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation,
                        Quaternion.LookRotation(_lastDirection), _forceRotation * Time.deltaTime);
                }
            }
        }

        public void CanRotate(bool canRotate)
        {
            //Debug.Log("Can Rotate: " + canRotate);
            _vector2 = Vector2.zero;
            _canRotate = canRotate;
        }
        
        public void CanRotateWhileAttack(bool canRotate)
        {
            _canRotateWhileAttack = canRotate;
        }

        public bool CanRotate()
        {
            return _canRotate;
        }

        public void RotateToLookTheTarget(Vector3 getTarget)
        {
            if(getTarget != Vector3.zero)
            {
                _lastDirection = getTarget - _player.transform.position;
                _lastDirection.y = 0;
            }
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

        public void RotateToDirection(Vector3 direction)
        {
            //invert direction
            direction = -direction;
            //rotate to direction without lerp
            direction = new Vector3(direction.x, 0, direction.z);
            _player.transform.rotation = Quaternion.LookRotation(direction);
            _lastDirection = direction;
        }
    }
}