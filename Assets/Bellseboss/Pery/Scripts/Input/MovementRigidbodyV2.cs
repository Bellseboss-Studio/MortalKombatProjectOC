using System;
using Bellseboss.Angel.CombatSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bellseboss.Pery.Scripts.Input
{
    public class MovementRigidbodyV2 : MonoBehaviour
    {
        [SerializeField] private float force;
        [SerializeField] private FloorController floorController;

        [SerializeField] private bool isFall, isUp;

        //[SerializeField] private AttackMovementSystem attackMovementSystem;
        [SerializeField] private JumpSystem jumpSystem;
        [Range(0, 1)] [SerializeField] private float inputMin;
        [Range(0, 2)] [SerializeField] private float inputMax;
        [Range(0, 1f)] [SerializeField] private float minSpeed;
        [Range(0.5f, 1)] [SerializeField] private float maxSpeed;
        [SerializeField] private bool isScalableWall;
        [SerializeField] private float forceToGravitate;
        private Rigidbody _rigidbody;
        private float _speedRun, _speedWalk;
        private InputMovementCustomV2 _inputMovementCustom;
        private Vector2 _lastDirection;
        private bool _isConfigured;
        [SerializeField] private bool _canMove;
        private GameObject _camera;
        private bool _isTarget;
        private IMovementRigidBodyV2 _movementRigidBodyV2;
        private bool _jump;
        public bool IsJump => _jump;
        private float _velocityOfAnimation;
        private Vector3 _scalableWallFordWard;

        public void Configure(Rigidbody rigidBody, float speedWalk, float speedRun, GameObject camera,
            IMovementRigidBodyV2 movementRigidBodyV2, StatisticsOfCharacter statisticsOfCharacter)
        {
            _rigidbody = rigidBody;
            _speedWalk = speedWalk;
            _speedRun = speedRun;
            _inputMovementCustom = new InputMovementCustomV2(force);
            _isConfigured = true;
            _camera = camera;
            _movementRigidBodyV2 = movementRigidBodyV2;
            _canMove = true;
            floorController.Configure(this.gameObject);
            jumpSystem.Configure(rigidBody, movementRigidBodyV2, floorController);
            //attackMovementSystem.Configure(rigidBody, statisticsOfCharacter, movementRigidBodyV2);
            floorController.OnFall = Fall;
            floorController.OnRecovery = Recovery;
            floorController.OnTouchingFloorChanged = TouchingFloorChanged;
        }

        private void Recovery()
        {
            _movementRigidBodyV2.PlayerRecovery();
        }

        private void Fall()
        {
            _movementRigidBodyV2.PlayerFall();
        }

        private void TouchingFloorChanged(bool isTouching)
        {
            if (_movementRigidBodyV2.IsAttacking() || jumpSystem.IsJump()) return;
            if (isTouching)
            {
                _movementRigidBodyV2.PlayerRecoveryV2();
            }
            else
            {
                _movementRigidBodyV2.PlayerFallV2();
            }
        }

        public void IsScalableWall(bool isScalableWall, float forceToGravitate, Vector3 direction)
        {
            /*Debug.Log(isScalableWall);*/
            this.isScalableWall = true;
            _scalableWallFordWard = direction;
            /*this.forceToGravitate = forceToGravitate;
            this.isScalableWall = isScalableWall;
            jumpSystem.IsScalableWall(isScalableWall, floorController, direction);
            _jump = false;*/
        }

        private float CalculateDirection(float axis, bool isTarget)
        {
            var axisAbs = Mathf.Abs(axis);

            if (axisAbs < inputMin) return 0;

            if (isTarget)
            {
                return axis >= 0 ? minSpeed : -minSpeed;
            }

            if (axisAbs < inputMax)
            {
                return axis >= 0 ? minSpeed : -minSpeed;
            }

            return axis >= 0 ? maxSpeed : -maxSpeed;
        }

        private void Move()
        {
            var result = Vector2.zero;
            result.y = CalculateDirection(_lastDirection.y, _isTarget);
            result.x = CalculateDirection(_lastDirection.x, _isTarget);
            var absX = Mathf.Abs(result.x);
            var absY = Mathf.Abs(result.y);
            var _choiceMax = absX >= maxSpeed || absY >= maxSpeed;
            if (_lastDirection.x <= inputMin && _lastDirection.y <= inputMin)
            {
                _velocityOfAnimation = 0;
            }
            else if (_choiceMax)
            {
                _velocityOfAnimation = 1;
            }
            else
            {
                _velocityOfAnimation = 0.4f;
            }

            var resultMovement = _inputMovementCustom.CalculateMovement(result, _choiceMax ? _speedRun : _speedWalk,
                _camera, _rigidbody.gameObject);

            /*if (_jump)
            {
                if (floorController.IsTouchingFloor() || isScalableWall)
                {
                    jumpSystem.Jump();
                    _jump = false;
                }
            }*/
            var velocity = new Vector3(resultMovement.x, _rigidbody.linearVelocity.y, resultMovement.z);
            if (!_movementRigidBodyV2.IsJumpingInWall())
            {
                if (floorController.IsTouchingFloor())
                {
                    _rigidbody.linearVelocity = velocity;
                }
                else
                {
                    _rigidbody.linearVelocity = new Vector3(velocity.x / 1.5f, velocity.y, velocity.z / 1.5f);
                }
            }

            
            if (_rigidbody.linearVelocity.y > 0)
            {
                isUp = true;
                isFall = false;
            }
            else if (_rigidbody.linearVelocity.y < 0 && !floorController.IsTouchingFloor())
            {
                isFall = true;
                isUp = false;
            }
            else
            {
                isFall = false;
                isUp = false;
            }
        }

        public void Direction(Vector2 vector2)
        {
            _lastDirection = vector2;
        }

        private void Update()
        {
            if (!_isConfigured || !_canMove || _movementRigidBodyV2.IsAttacking()) return;
            Move();
            _movementRigidBodyV2.UpdateAnimation(floorController.IsTouchingFloor(), isScalableWall);
            if (!floorController.IsTouchingFloor() && jumpSystem.IsJump() && isScalableWall)
            {
                jumpSystem.ChangeRotation(_scalableWallFordWard);
            }
        }

        public void IsTarget(bool isTarget)
        {
            _isTarget = isTarget;
        }

        public float GetVelocity()
        {
            return _rigidbody.linearVelocity.magnitude / 10;
        }

        public void AddForce(Vector3 runningDirection, float runningDistance,
            AttackMovementSystem.TypeOfAttack typeOfAttack)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            Vector3 globalDirection = transform.TransformDirection(runningDirection.normalized);
            //Debug.Log($"MovementRigidbodyV2: AddForce: {globalDirection} - {runningDistance}");
            //_rigidbody.AddForce(globalDirection * runningDistance, ForceMode.Impulse);
            _canMove = false;
            //attackMovementSystem.Attack(globalDirection * runningDistance, typeOfAttack);
        }

        public void CanMove(bool canMove)
        {
            _canMove = canMove;
            if (!canMove)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _lastDirection = Vector2.zero;
            }
        }

        public void Jump()
        {
            jumpSystem.Jump(floorController.IsTouchingFloor(), isScalableWall, _scalableWallFordWard);
        }

        public JumpSystem GetJumpSystem()
        {
            return jumpSystem;
        }

        public float GetVelocityFloat()
        {
            return _velocityOfAnimation;
        }

        /*public AttackMovementSystem GetAttackSystem()
        {
            return attackMovementSystem;
        }*/

        public void ChangeToNormalJump()
        {
            /*IsScalableWall(false, 0, Vector3.zero);*/
        }

        public void ExitToWall()
        {
            isScalableWall = false;
            /*jumpSystem.ExitToWall();*/
        }

        public bool IsJumpingFromADRS()
        {
            return jumpSystem.IsJump();
        }

        public float GetXZVelocity()
        {
            Vector3 velocidadXZ = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

            return velocidadXZ.magnitude / 10;
        }
    }
}