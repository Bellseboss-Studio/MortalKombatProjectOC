using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.Player
{
    public class CharacterV2 : PJV2, ICharacterV2, IMovementRigidBodyV2, IAnimationController, IRotationCharacterV2,
        ICombatSystem, IFocusTarget, ICombatSystemV2, IFatality, ICharacterUi, IStunSystem, IPlayer
    {
        public string Id => id;
        public Action OnAction { get; set; }
        public Action<ICharacterV2> OnDead { get; set; }

        public GameObject Model3DInstance
        {
            get => _model3DInstance;
        }

        public Action<StunInfo> OnReceiveDamage { get; set; }

        public AnimationController GetAnimationController()
        {
            return animationController;
        }

        [SerializeField] private string id;
        [SerializeField] private InputPlayerV2 inputPlayerV2;
        [SerializeField] private MovementRigidbodyV2 movementRigidbodyV2;
        [SerializeField] private CinemachineVirtualCameraBase cameraMain;
        [SerializeField] private Rigidbody rigidbody;
        [Range(0, 10)] [SerializeField] private float speedWalk;
        [Range(0, 20)] [SerializeField] private float speedRun;
        [SerializeField] private AnimationController animationController;
        [SerializeField] private GameObject model3D;
        private GameObject _model3DInstance;
        [SerializeField] private RotationCharacterV2 rotationCharacterV2;
        [SerializeField] private float forceRotation;
        [SerializeField] private TargetFocus targetFocus;
        [SerializeField] private StatisticsOfCharacter statisticsOfCharacter;
        [SerializeField] private CombatSystemAngel combatSystemAngel;
        [SerializeField] private StunSystem stunSystem;
        [SerializeField] private MovementADSR movementADSR;

        [SerializeField, InterfaceType(typeof(IFatalitySystem))]
        private MonoBehaviour FatalitySystem;

        private IFatalitySystem fatalitySystem => FatalitySystem as IFatalitySystem;
        private StatisticsOfCharacter _statisticsOfCharacter;
        private bool IsDead;
        private bool _canUseButtons = true;
        private bool isAnimationWasRun, isAnimationRecovered;
        [SerializeField] private List<GameObject> _enemiesInCombat;

        [SerializeField] private HealComponent healComponent;


        public event Action<float> OnEnterDamageEvent;
        public event Action<float> OnAddingEnergy;

        public bool CanReadInputs
        {
            get => inputPlayerV2.CanReadInput;
            set => inputPlayerV2.StartToReadInputs(value);
        }

        public float GetLife()
        {
            return _statisticsOfCharacter.life;
        }

        void Start()
        {
            Configure();
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance.UnregisterService<IPlayer>();
        }

        public void Configure()
        {
            inputPlayerV2.onMoveEvent += OnMove;
            inputPlayerV2.onTargetEvent += OnTargetEvent;
            inputPlayerV2.onPunchEvent += OnPunchEvent;
            inputPlayerV2.onKickEvent += OnKickEvent;
            inputPlayerV2.onJumpEvent += OnJumpEvent;
            inputPlayerV2.onActionEvent += OnActionEvent;
            inputPlayerV2.onFatalityEvent += OnFatalityEvent;

            _model3DInstance = Instantiate(model3D, transform);
            animationController.Configure(_model3DInstance.GetComponent<ReferencesOfPlayer>().Animator, transform);
            targetFocus.Configure(this);
            targetFocus.EnableCollider();

            movementRigidbodyV2.GetJumpSystem().OnAttack += JumpOnAttack;
            movementRigidbodyV2.GetJumpSystem().OnMidAir += JumpOnMidAir;
            movementRigidbodyV2.GetJumpSystem().OnRelease += JumpOnRelease;
            movementRigidbodyV2.GetJumpSystem().OnEndJump += JumpOnEndJump;

            rotationCharacterV2.CanRotate(true);

            _statisticsOfCharacter = Instantiate(statisticsOfCharacter);

            movementADSR.Configure(rigidbody, _statisticsOfCharacter, this);

            fatalitySystem.Configure(this, this);

            // ServiceLocator.Instance.GetService<IObserverUI>().Observer(this, this);

            // ServiceLocator.Instance.GetService<IPauseMainMenu>().onPause += OnPausaMenu; 

            ConfigCamera(cameraMain);


            CanReadInputs = true;

            ServiceLocator.Instance.RegisterService<IPlayer>(this);

            healComponent.Configure(this);
        }

        private void OnPausaMenu(bool ispause)
        {
            if (ispause)
            {
                DisableControls();
            }
            else
            {
                EnableControls();
            }
        }

        private void OnFatalityEvent()
        {
            if (_statisticsOfCharacter.energy >= 100 && targetFocus.IsEnemyTouched())
            {
                fatalitySystem.Fatality();
            }
        }

        public void SetPositionAndRotation(GameObject refOfPlayer)
        {
            transform.position = Vector3.Lerp(transform.position, refOfPlayer.transform.position, 0.5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, refOfPlayer.transform.rotation, 0.5f);
        }

        private void JumpOnEndJump()
        {
            animationController.PlayJumpLand();
            isAnimationWasRun = false;
            isAnimationRecovered = false;
        }

        private void JumpOnRelease()
        {
            animationController.PlayJumpFall();
        }

        private void JumpOnMidAir()
        {
            animationController.PlayJumpApex();
        }

        private void JumpOnAttack()
        {
            animationController.PlayJumpStart();
        }

        void OnActionEvent()
        {
            OnAction?.Invoke();
        }

        private void OnJumpEvent()
        {
            if (!CanReadInputs || IsAttacking()) return;
            movementRigidbodyV2.Jump();
        }

        private void OnKickEvent()
        {
            if (!CanReadInputs) return;
            combatSystemAngel.ExecuteMovement(TypeOfAttack.Power);
        }

        private void OnPunchEvent()
        {
            if (!CanReadInputs) return;
            combatSystemAngel.ExecuteMovement(TypeOfAttack.Quick);
        }

        private void OnTargetEvent(bool isTarget)
        {
            movementRigidbodyV2.IsTarget(isTarget);
        }

        private void OnMove(Vector2 vector2, INPUTS inputs)
        {
            if (combatSystemAngel.Attacking || !CanReadInputs)
            {
                combatSystemAngel.oneTimeOnEndAttack += () =>
                {
                    if (rotationCharacterV2.CanRotate() && !movementRigidbodyV2.IsJump)
                    {
                        rotationCharacterV2.Direction(vector2);
                    }
                };
            }
            else
            {
                if (rotationCharacterV2.CanRotate() && !movementRigidbodyV2.IsJump)
                {
                    rotationCharacterV2.Direction(vector2);
                }
            }

            movementRigidbodyV2.Direction(vector2);
        }


        public override void DisableControls()
        {
            rotationCharacterV2.CanRotate(false);
            movementRigidbodyV2.CanMove(false);
            _canUseButtons = false;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.freezeRotation = true;
            CanReadInputs = true;
            inputPlayerV2.StartToReadInputs(_canUseButtons);
            animationController.UpdateMovementAnimation(movementRigidbodyV2.GetXZVelocity());
        }

        public void EnableControls()
        {
            movementRigidbodyV2.CanMove(true);
            rotationCharacterV2.CanRotate(true);
            _canUseButtons = true;
            CanReadInputs = true;
            inputPlayerV2.StartToReadInputs(_canUseButtons);
        }

        public Transform GetGameObject()
        {
            return transform;
        }

        public void LoadRageTo(int percentage)
        {
            _statisticsOfCharacter.energy = statisticsOfCharacter.energy * percentage / 100;
            OnAddingEnergy?.Invoke(_statisticsOfCharacter.energy);
        }

        public void LoadLifeTo(int percentage)
        {
            _statisticsOfCharacter.life = statisticsOfCharacter.life * percentage / 100;
            OnEnterDamageEvent?.Invoke(_statisticsOfCharacter.life);
        }

        public void StartDeadAction()
        {
            DisableControls();
            animationController.PlayDeath();
        }

        public void PlayerTouchEnemy()
        {
            _statisticsOfCharacter.energy += _statisticsOfCharacter.energyToAdd;
            OnAddingEnergy?.Invoke(_statisticsOfCharacter.energy);
        }

        public List<GameObject> GetEnemiesInCombat()
        {
            return _enemiesInCombat;
        }

        public void SetEnemiesInCombat(List<GameObject> gameObjects)
        {
            _enemiesInCombat = gameObjects;
        }

        public void RotateCharacter(Vector3 position)
        {
            rotationCharacterV2.RotateToDirection(position);
        }

        public IMovementRigidBodyV2 GetMovementRigidBody()
        {
            return this;
        }


        public Vector3 RotateToTargetAngel(Vector3 originalDirection)
        {
            return targetFocus.RotateToTarget(originalDirection);
        }

        public Vector3 RotateToTarget(Vector3 originalDirection)
        {
            return targetFocus.RotateToTarget(originalDirection);
        }

        public void SetCamera(CinemachineVirtualCameraBase currentCamera)
        {
            ConfigCamera(currentCamera);
        }

        private void ConfigCamera(CinemachineVirtualCameraBase currentCamera)
        {
            movementRigidbodyV2.Configure(rigidbody, speedWalk, speedRun, currentCamera.gameObject, this,
                _statisticsOfCharacter);
            combatSystemAngel.Configure(rigidbody, _statisticsOfCharacter, this, this);
            stunSystem.Configure(rigidbody, _statisticsOfCharacter, this, this, this);
            rotationCharacterV2.Configure(currentCamera.gameObject, gameObject, this, forceRotation);
        }

        public void UpdateAnimation()
        {
            animationController.UpdateMovementAnimation(movementRigidbodyV2.GetXZVelocity());
        }

        public void UpdateAnimation(bool isTouchingFloor, bool isTouchingWall)
        {
            animationController.UpdateMovementAnimation(movementRigidbodyV2.GetXZVelocity());
            //TODO Revisar el switcheo entre los tipos de salto
            // animationController.JumpingWalls(isTouchingFloor, isTouchingWall, movementRigidbodyV2.GetJumpSystem().IsJump());
        }

        public void ChangeToNormalJump()
        {
            movementRigidbodyV2.ChangeToNormalJump();
        }

        public void ChangeRotation(Vector3 rotation)
        {
            rotationCharacterV2.ChangeDirection(rotation);
        }

        public void RestoreRotation()
        {
            rotationCharacterV2.RestoreRotation();
        }

        public void EndAttackMovement()
        {
            rotationCharacterV2.Direction(Vector3.zero);
            rotationCharacterV2.CanRotateWhileAttack(false);
        }

        public void SetCanReadInputs(bool b)
        {
            CanReadInputs = b;
        }

        public bool GetCanReadInputs()
        {
            return CanReadInputs;
        }


        public void PlayerFall()
        {
        }

        public void PlayerRecovery()
        {
        }

        public void PlayerFallV2()
        {
            animationController.PlayJumpFall();
        }

        public void PlayerRecoveryV2()
        {
            animationController.PlayJumpLand();
        }

        public bool IsJumpingInWall()
        {
            return movementRigidbodyV2.GetJumpSystem().IsJumpingInScalableWall;
        }

        public void OnStartRunning()
        {
            
        }

        public void OnStopRunning()
        {
            
        }

        public bool IsAttacking()
        {
            return combatSystemAngel.Attacking;
        }

        public void TouchedScallableWall(bool leave, float forceToGravitate, Vector3 direction)
        {
            movementRigidbodyV2.IsScalableWall(leave, forceToGravitate, direction);
        }

        public void ExitScalableWall()
        {
            movementRigidbodyV2.ExitToWall();
        }

        public override void ReceiveDamage(int damage, GameObject transformForward, StunInfo currentAttackStunTime)
        {
            if (IsDead) return;
            _statisticsOfCharacter.life -= damage;
            Debug.Log($"_statisticsOfCharacter.life {_statisticsOfCharacter.life}");
            OnEnterDamageEvent?.Invoke(_statisticsOfCharacter.life);
            if (_statisticsOfCharacter.life <= 0)
            {
                IsDead = true;
                OnDead?.Invoke(this);
                Debug.Log("CharacterV2: Dead");
            }

            if (movementADSR.CanAttackAgain() && !IsDead)
            {
                movementADSR.Attack(transformForward.transform.forward);
            }

            rotationCharacterV2.RotateToDirection(transformForward.transform.forward);
            this.OnReceiveDamage?.Invoke(currentAttackStunTime);
        }

        public override void SetAnimationToHit(string animationParameterName)
        {
            if (IsDead) return;
            //TODO: Usar animationParameterName para distintos tipos de hit
            animationController.PlayHit();
        }

        public override void Stun(bool isStun)
        {
            movementRigidbodyV2.CanMove(!isStun);
            rotationCharacterV2.CanRotate(!isStun);
        }

        public GameObject GetEnemyToKillWithFatality()
        {
            return targetFocus.GetClosestEnemy();
        }

        public bool ReadInput(out INPUTS input)
        {
            return inputPlayerV2.ReadInput(out input);
        }

        public void StartToReadInputs(bool b)
        {
            CanReadInputs = b;
        }

        public void StartAnimationFatality()
        {
            animationController.PlayFatality();
        }

        public void StartToReadInputsToFatality(bool canRead)
        {
            inputPlayerV2.StartToReadInputsToFatality(canRead);
        }

        public void GetIntoEnemyZone(GameObject enemy, bool isNear)
        {
            if (isNear)
                _enemiesInCombat.Add(enemy);
            else
                _enemiesInCombat.Remove(enemy);
        }

        public void GetIntoEnemyZone(List<GameObject> enemies)
        {
            _enemiesInCombat = enemies;
        }

        public void GetOutOfEnemyZone()
        {
            _enemiesInCombat.Clear();
        }

        GameObject IPlayer.GetGameObject()
        {
            return gameObject;
        }
    }
}