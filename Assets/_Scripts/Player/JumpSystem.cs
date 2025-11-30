using System;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

namespace _Scripts.Player
{
    public class JumpSystem : MonoBehaviour, IJumpSystem
    {
        public Action OnAttack, OnMidAir, OnRelease, OnSustain, OnEndJump;

        [SerializeField, InterfaceType(typeof(IBehaviourOfJumpSystem))]
        private MonoBehaviour behaviourOfJumpSystemNormal;

        private IBehaviourOfJumpSystem BehaviourOfJumpSystemNormal =>
            behaviourOfJumpSystemNormal as IBehaviourOfJumpSystem;

        [SerializeField, InterfaceType(typeof(IBehaviourOfJumpSystem))]
        private MonoBehaviour behaviourOfJumpSystemWalls;

        private IBehaviourOfJumpSystem BehaviourOfJumpSystemWalls =>
            behaviourOfJumpSystemWalls as IBehaviourOfJumpSystem;

        private TeaTime _attack, _decay, _sustain, _release, _endJump;
        private Rigidbody _rigidbody;
        private float _deltatimeLocal;
        private RigidbodyConstraints _rigidbodyConstraints; // eliminar comentado si no se usa
        private bool _isScalableWall;
        private FloorController _floorController;
        private IMovementRigidBodyV2 _movementRigidBodyV2;
        private bool _isJump;
        // --- NUEVOS PARAMETROS PARA CONFIABILIDAD DEL SALTO ---
        [Header("Jump Reliability")]
        [Tooltip("Tiempo (s) después de dejar el suelo en el que aún se puede saltar (coyote time)")] [SerializeField]
        private float coyoteTime = 0.12f;
        [Tooltip("Tiempo (s) para almacenar un input de salto antes de tocar el suelo (jump buffer)")] [SerializeField]
        private float jumpBufferTime = 0.15f;
        [Tooltip("Evitar segundo salto mientras está en fase de TeaTime")] [SerializeField]
        private bool preventDoubleDuringSequence = true;

        [Header("Physics Jump (Optional)")]
        [Tooltip("Usar impulso físico en vez de secuencia TeaTime")] [SerializeField]
        private bool physicsJumpMode = true;
        [Tooltip("Fuerza vertical aplicada (VelocityChange) al saltar")] [SerializeField]
        private float jumpImpulseForce = 7f;
        [Tooltip("Fuerza horizontal adicional al hacer wall jump")] [SerializeField]
        private float wallJumpHorizontalForce = 4f;
        [Tooltip("Factor vertical adicional en wall jump (si se usa modo físico)")] [SerializeField]
        private float wallJumpVerticalBoost = 1.1f;

        private float _lastGroundedTime = -999f;
        private float _lastJumpPressedTime = -999f;
        private bool _bufferConsumed;

        public bool IsJumpingInScalableWall => _isJump && _isScalableWall;

        public void Configure(Rigidbody rb, IMovementRigidBodyV2 movementRigidBodyV2,
            FloorController floorController)
        {
            Debug.Log($"Configured JumpSystem: {rb.gameObject.name}");
            BehaviourOfJumpSystemWalls.Configure(rb, this);
            BehaviourOfJumpSystemNormal.Configure(rb, this);
            _rigidbody = rb;
            _rigidbodyConstraints = _rigidbody.constraints; // actualmente no usado, mantener por si se restaura
            _floorController = floorController;
            _movementRigidBodyV2 = movementRigidBodyV2;
            _lastGroundedTime = Time.time; // inicia como si estuviera en suelo
        }

        private void Update()
        {
            if (_floorController != null && _floorController.IsTouchingFloor())
            {
                if (!_isJump && physicsJumpMode && (Time.time - _lastGroundedTime) > 0.02f)
                {
                    Debug.Log("[JumpSystem] Aterrizaje detectado (physics mode)");
                }
                _lastGroundedTime = Time.time;
                if (physicsJumpMode)
                {
                    if (_isJump)
                    {
                        OnEndJump?.Invoke();
                        _isJump = false;
                    }
                }
                if (!physicsJumpMode && !_isJump && !_bufferConsumed && Time.time - _lastJumpPressedTime <= jumpBufferTime)
                {
                    ExecuteNormalJump();
                    _bufferConsumed = true;
                }
            }
        }

        public void Jump(bool isTouchingFloor, bool isTouchingScalableWall, Vector3 scalableWallDirection)
        {
            _lastJumpPressedTime = Time.time;
            _bufferConsumed = false;
            if (preventDoubleDuringSequence && _isJump)
            {
                Debug.Log("[JumpSystem] Ignorado: ya está en secuencia de salto");
                return;
            }
            bool canCoyote = Time.time - _lastGroundedTime <= coyoteTime;
            Debug.Log($"[JumpSystem] Jump request floor={isTouchingFloor} wall={isTouchingScalableWall} coyote={canCoyote} physicsMode={physicsJumpMode}");
            if (isTouchingScalableWall && !isTouchingFloor)
            {
                ExecuteWallJump(scalableWallDirection);
                return;
            }
            if (isTouchingFloor || canCoyote)
            {
                ExecuteNormalJump();
                _bufferConsumed = true;
                return;
            }
            // Buffer queda pendiente
        }

        private void ExecuteNormalJump()
        {
            if (physicsJumpMode)
            {
                // Impulso físico directo
                Vector3 vel = _rigidbody.linearVelocity; // usa linearVelocity consistente con MovementRigidbodyV2
                vel.y = 0f; // reset vertical para consistencia
                _rigidbody.linearVelocity = vel;
                _rigidbody.AddForce(Vector3.up * jumpImpulseForce, ForceMode.VelocityChange);
                _isJump = true;
                OnAttack?.Invoke(); // usar evento ataque como inicio
                return; // no usar TeaTime
            }
            _isScalableWall = false;
            _attack?.Stop();
            _decay?.Stop();
            _sustain?.Stop();
            _release?.Stop();
            _attack = BehaviourOfJumpSystemNormal.GetAttack();
            _decay = BehaviourOfJumpSystemNormal.GetDecay();
            _sustain = BehaviourOfJumpSystemNormal.GetSustain();
            _release = BehaviourOfJumpSystemNormal.GetRelease();
            _endJump = BehaviourOfJumpSystemNormal.GetEndJump();
            BehaviourOfJumpSystemNormal.OnAttack = () => { OnAttack?.Invoke(); _isJump = true; };
            BehaviourOfJumpSystemNormal.OnMidAir = () => { OnMidAir?.Invoke(); };
            BehaviourOfJumpSystemNormal.OnSustain = () => { OnSustain?.Invoke(); };
            BehaviourOfJumpSystemNormal.OnRelease = () => { OnRelease?.Invoke(); };
            BehaviourOfJumpSystemNormal.OnEndJump = () => { OnEndJump?.Invoke(); _isJump = false; };
            _attack.Play();
        }

        private void ExecuteWallJump(Vector3 scalableWallDirection)
        {
            if (physicsJumpMode)
            {
                Vector3 vel = _rigidbody.linearVelocity;
                vel.y = 0f;
                _rigidbody.linearVelocity = vel;
                // Impulso combinado (alejarse de la pared dirección recibida + vertical)
                Vector3 horizontalDir = scalableWallDirection.normalized;
                Vector3 force = (horizontalDir * wallJumpHorizontalForce) + (Vector3.up * jumpImpulseForce * wallJumpVerticalBoost);
                _rigidbody.AddForce(force, ForceMode.VelocityChange);
                _isJump = true;
                _isScalableWall = true;
                OnAttack?.Invoke();
                return;
            }
            _isScalableWall = true;
            _attack?.Stop();
            _decay?.Stop();
            _sustain?.Stop();
            _release?.Stop();
            _attack = BehaviourOfJumpSystemWalls.GetAttack();
            _decay = BehaviourOfJumpSystemWalls.GetDecay();
            _sustain = BehaviourOfJumpSystemWalls.GetSustain();
            _release = BehaviourOfJumpSystemWalls.GetRelease();
            _endJump = BehaviourOfJumpSystemWalls.GetEndJump();
            BehaviourOfJumpSystemWalls.OnAttack = () => { OnAttack?.Invoke(); _isJump = true; };
            BehaviourOfJumpSystemWalls.OnMidAir = () => { OnMidAir?.Invoke(); };
            BehaviourOfJumpSystemWalls.OnSustain = () => { OnSustain?.Invoke(); };
            BehaviourOfJumpSystemWalls.OnRelease = () => { OnRelease?.Invoke(); };
            BehaviourOfJumpSystemWalls.OnEndJump = () => { OnEndJump?.Invoke(); _isJump = false; };
            var behaviourOfJumpSystemWallsMono = BehaviourOfJumpSystemWalls as BehaviourOfJumpSystemWalls;
            System.Diagnostics.Debug.Assert(behaviourOfJumpSystemWallsMono != null,
                nameof(behaviourOfJumpSystemWallsMono) + " != null");
            behaviourOfJumpSystemWallsMono.ConfigureWall(scalableWallDirection);
            _attack.Play();
        }


        public void ChangeNormalWall()
        {
            _movementRigidBodyV2.ChangeToNormalJump();
        }

        public void ChangeRotation(Vector3 rotation)
        {
            _movementRigidBodyV2.ChangeRotation(rotation);
        }

        public void RestoreRotation()
        {
            _movementRigidBodyV2.RestoreRotation();
        }

        public void ExitToWall()
        {
            //TODO: doing something went exit to wall
        }

        public bool IsJump()
        {
            return _isJump;
        }
    }

    public interface IJumpSystem
    {
        void ChangeNormalWall();
        void ChangeRotation(Vector3 rotation);
        void RestoreRotation();
    }
}