using System;
using _Scripts.Player;
using Unity.Cinemachine;
using UnityEngine;
using Bellseboss.Pery.Scripts.Input;
public class MovementRigidbodyV3 : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private FloorController floorController;
    [SerializeField] private bool isFall, isUp;
    [SerializeField] private JumpSystem jumpSystem;
    [Range(0, 1)]
    [SerializeField] private float inputMin;
    [Range(0, 2)]
    [SerializeField] private float inputMax;
    [Range(0, 1f)]
    [SerializeField] private float minSpeed;
    [Range(0.5f, 1)]
    [SerializeField] private float maxSpeed;
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

    public void Configure(Rigidbody rigidBody, float speedWalk, float speedRun, GameObject camera, IMovementRigidBodyV2 movementRigidBodyV2, StatisticsOfCharacter statisticsOfCharacter)
    {
        _rigidbody = rigidBody;
        _speedWalk = speedWalk;
        _speedRun = speedRun;
        _inputMovementCustom = new InputMovementCustomV2(force);
        _isConfigured = true;
        _camera = camera;
        _movementRigidBodyV2 = movementRigidBodyV2;
        _canMove = true;
        //floorController.Configure(gameobject);
        jumpSystem.Configure(rigidBody, movementRigidBodyV2, floorController);
        //attackMovementSystem.Configure(rigidBody, statisticsOfCharacter, movementRigidBodyV2);
        floorController.OnFall = Fall;
        floorController.OnRecovery = Recovery;
    }

    private void Recovery()
    {
        _movementRigidBodyV2.PlayerRecovery();
    }

    private void Fall()
    {
        _movementRigidBodyV2.PlayerFall();
    }

    public void IsScalableWall(bool isScalableWall, float forceToGravitate, Vector3 direction)
    {
        this.forceToGravitate = forceToGravitate;
        this.isScalableWall = isScalableWall;
        /*jumpSystem.IsScalableWall(isScalableWall, floorController, direction);*/
        _jump = false;
    }

    private float CalculateDirection(float axis, bool isTarget)
    {
        var axisAbs = Mathf.Abs(axis);
        if (isTarget && axisAbs >= inputMin)
        {
            if (axisAbs <= inputMin) return 0;
            return axis >= 0 ? minSpeed : -minSpeed;
        }

        if (axisAbs >= inputMin)
        {
            if (axisAbs >= inputMin && axisAbs < inputMax)
            {
                if (axisAbs <= inputMin) return 0;
                return axis >= 0 ? minSpeed : -minSpeed;
            }
            else if (axisAbs >= inputMax)
            {
                if (axisAbs <= inputMin) return 0;
                return axis >= 0 ? maxSpeed : -maxSpeed;
            }
        }
        return 0;
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
            _camera, _rigidbody.gameObject, _jump);

        if (_jump)
        {
            if (floorController.IsTouchingFloor() || isScalableWall)
            {
                /*jumpSystem.Jump();*/
                _jump = false;
            }
        }
        if (floorController.IsTouchingFloor())
        {
            _rigidbody.linearVelocity = new Vector3(resultMovement.x, _rigidbody.linearVelocity.y, resultMovement.z);
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

    private void FixedUpdate()
    {
    }

    private void Update()
    {
        if (!_isConfigured || !_canMove) return;
        Move();
        _movementRigidBodyV2.UpdateAnimation();
    }

    public void IsTarget(bool isTarget)
    {
        _isTarget = isTarget;
    }

    public float GetVelocity()
    {
        return _rigidbody.linearVelocity.magnitude / 10;
    }

    public void AddForce(Vector3 runningDirection, float runningDistance, AttackMovementSystem.TypeOfAttack typeOfAttack)
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
    }

    public Vector3 GetVelocityV3()
    {
        return _rigidbody.linearVelocity;
    }

    public void Jump()
    {
        _jump = true;
    }

    public JumpSystem GetJumpSystem()
    {
        return jumpSystem;
    }

    public float GetVelocityFloat()
    {
        return _velocityOfAnimation;
    }

    public void ChangeToNormalJump()
    {
        IsScalableWall(false, 0, Vector3.zero);
    }

    public void ExitToWall()
    {
        jumpSystem.ExitToWall();
    }

    public bool IsJumpingFromADRS()
    {
        return jumpSystem.IsJump();
    }
}