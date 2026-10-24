using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Player;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

public abstract class EnemyV2 : PJV2, IAnimationController, IEnemyV2, IMovementRigidBodyV2, ICombatSystemV2, IStunSystem
{
    public event Action OnArriveToTarget;
    public event Action<bool> OnPlayerDetected;
    public event Action<bool> OnPlayerInNearZone;
    public event Action<EnemyV2> OnDead;

    public Action<StunInfo> OnReceiveDamage { get; set; }
    public AnimationController GetAnimationController()
    {
        return animationController;
    }

    [SerializeField] private string id;
    [SerializeField] private StatisticsOfCharacter statisticsOfCharacter;
    [SerializeField] private MovementADSR movementADSR;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private GameObject model;

    [SerializeField, InterfaceType(typeof(IAiController))]
    private Object ai;

    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private float minDistanceToArriveToTarget;
    [SerializeField] private float minDistanceToArriveToEnemy;
    [SerializeField] private AttackMovementSystem attackMovementSystem;
    private GameObject _model;
    private StatisticsOfCharacter _statisticsOfCharacter;
    [SerializeField] private CharacterV2 _characterV2;
    private bool _inFarZone;
    private bool _inNearZone;
    private List<GameObject> _paths;
    private GameObject _target;
    private bool _canMove, _canRotate, _canRotateToTarget;
    private GameObject centerOfTheZone;
    [SerializeField] private StatesOfEnemy _state;
    [SerializeField] private TargetFocus colliderToDamage;
    [SerializeField] private CombatSystemAngel combatSystemAngel;
    [SerializeField] private StunSystem stunSystem;
    [SerializeField] private List<Collider> collidersToDisable;
    private bool isInstarotating;
    private IAiController _aiController => ai as IAiController;

    public string Id => id;
    public bool IsDead { get; private set; }
    public bool IsDeadWithFatality { get; private set; }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public GameObject GetCenterOfTheZone()
    {
        return centerOfTheZone;
    }

    public void SetInstaRotation(bool instaRotation)
    {
        isInstarotating = instaRotation;
    }

    public AnimationController GetAnimatorController()
    {
        return animationController;
    }

    private bool canAttack = true;

    private void Start()
    {
        Configure();
    }

    private void Configure()
    {
        _statisticsOfCharacter = Instantiate(statisticsOfCharacter);
        /*movementADSR.Configure(GetComponent<Rigidbody>(), _statisticsOfCharacter, this);*/
        _model = Instantiate(model, transform);
        animationController.Configure(_model.GetComponent<Animator>(), transform);
        _aiController.Configure(this, ref stunSystem.OnEndStunt);
        _state = StatesOfEnemy.NORMAL;

        animationController.OnFinishAnimation += () =>
        {
            // Debug.Log("EnemyV2: Finish Animation Damage");
            _canMove = true;
            _canRotate = true;
        };
        combatSystemAngel.Configure(rigidbody, _statisticsOfCharacter, this, this, false);
        stunSystem.Configure(rigidbody, _statisticsOfCharacter, this, this, this, false);
        combatSystemAngel.OnEndAttack += () => { _aiController.StartAi(); };
    }

    private void Update()
    {
        if (IsDead) return;
        if (_canRotate)
        {
            var position = _target.transform.position;
            RotateToTarget(position, isInstarotating);
        }

        if (_canMove)
        {
            var direction = _target.transform.position - transform.position;
            direction.Normalize();
            direction *= GetSpeedToMove();
            rigidbody.linearVelocity = direction * Time.deltaTime;
            if (Vector3.Distance(transform.position, _target.transform.position) <
                (GetPlayer() != null ? minDistanceToArriveToEnemy : minDistanceToArriveToTarget))
            {
                _canMove = false;
                OnArriveToTarget?.Invoke();
            }
        }
        else
        {
            rigidbody.ResetInertiaTensor();
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        animationController.UpdateMovementAnimation(rigidbody.linearVelocity.magnitude / 10);
    }

    public override void Stun(bool isStun)
    {
        _canMove = !isStun;
        _canRotate = !isStun;
    }

    private float GetSpeedToMove()
    {
        return _state switch
        {
            StatesOfEnemy.ANGRY => _statisticsOfCharacter.speedToMoveAngry,
            StatesOfEnemy.NORMAL => _statisticsOfCharacter.speedToMoveNormal,
            StatesOfEnemy.SCARED => _statisticsOfCharacter.speedToMoveScared,
            _ => 0
        };
    }

    public void RotateToTargetIdle(GameObject transformForward, bool b)
    {
        _canRotateToTarget = false;
        _target = transformForward;
    }

    public CharacterV2 GetPlayer()
    {
        return _characterV2;
    }

    public void CanMove(bool b)
    {
        _canMove = b;
    }

    public void RotateToTarget(Vector3 transformForward, bool instant = false)
    {
        var targetRotation = Quaternion.LookRotation(transformForward - transform.position);
        targetRotation.x = 0;
        targetRotation.z = 0;
        if (instant)
        {
            transform.rotation = targetRotation;
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
    }

    public override void ReceiveDamage(int damage, GameObject direction, StunInfo currentAttackStunTime)
    {
        if (IsDead) return;
        _statisticsOfCharacter.life -= damage;
        // Debug.Log(_statisticsOfCharacter.life);
        if (_statisticsOfCharacter.life <= 0)
        {
            IsDead = true;
            Died();
        }

        /*if (movementADSR.CanAttackAgain() && !IsDead)
        {
            movementADSR.Attack(direction);
        }*/

        _target = direction;

        OnReceiveDamage?.Invoke(currentAttackStunTime);
    }

    public void Died()
    {
        // Debug.Log("EnemyV2: Die");
        OnDead?.Invoke(this);
        if (IsDeadWithFatality) return;
        animationController.PlayDeath();
    }

    public float GetTimeToAttack()
    {
        return _statisticsOfCharacter.timeToAttack;
    }

    public string GetAttackAnimationName()
    {
        return _statisticsOfCharacter.attackAnimationName;
    }

    public float GetTimeBetweenAttacks()
    {
        return _statisticsOfCharacter.timeBetweenAttacks;
    }

    public bool CanActivateCollider(float delta)
    {
        _statisticsOfCharacter.timeToActivateCollider -= delta;
        if (_statisticsOfCharacter.timeToActivateCollider <= 0)
        {
            _statisticsOfCharacter.timeToActivateCollider = _statisticsOfCharacter.timeToEnableCollider;
            return true;
        }

        return false;
    }

    public void ColliderToAttack(bool enableCollider)
    {
        if (enableCollider)
        {
            colliderToDamage.EnableCollider();
        }
        else
        {
            colliderToDamage.DisableCollider();
        }
    }

    /*public void SendDamage()
    {
        _characterV2.ReceiveDamage(_statisticsOfCharacter.damage, transform.forward);
    }*/

    public void AttackPlayer()
    {
        if (!canAttack) return;
        combatSystemAngel.ExecuteMovement(TypeOfAttack.Power);
    }

    public void SetState(StatesOfEnemy state)
    {
        _state = state;
    }

    public void CanRotate(bool b)
    {
        _canRotate = b;
    }

    public override void SetAnimationToHit(string animationParameterName)
    {
        // Debug.Log($"EnemyV2: IsDead {IsDead}");
        if (IsDead) return;
        //TODO set animation to hit
        // Debug.Log($"EnemyV2: Set Animation To Hit {animationParameterName}");
        _aiController.TakeDamage();
        animationController.PlayHit();
    }

    public void SetPlayer(CharacterV2 characterV2)
    {
        _characterV2 = characterV2;
        if (_characterV2 != null)
        {
            _aiController.SetPlayer(characterV2);
        }
    }

    public void IntoToFarZone(bool b)
    {
        _inFarZone = b;
        OnPlayerDetected?.Invoke(b);
    }

    public void IntoToNearZone(bool b)
    {
        _inNearZone = b;
        OnPlayerInNearZone?.Invoke(b);
    }

    public List<GameObject> Paths()
    {
        //need reorganize the paths with random positions
        Random rng = new Random();
        List<GameObject> randomizedList = _paths.OrderBy(x => rng.Next()).ToList();
        return randomizedList;
    }

    public void MoveTo(GameObject target)
    {
        _target = target;
        _canMove = true;
        _canRotate = true;
        _canRotateToTarget = true;
    }

    public void Configure(GameObject[] pathToFollow, GameObject center)
    {
        _paths = new List<GameObject>(pathToFollow);
        centerOfTheZone = center;
    }

    public Action OnAction { get; set; }

    public override void DisableControls()
    {
        CanMove(false);
        CanRotate(false);
        rigidbody.linearVelocity = Vector3.zero;
    }

    public void UpdateAnimation()
    {
    }

    public void UpdateAnimation(bool isTouchingFloor, bool isJumping)
    {
    }

    public void ChangeToNormalJump()
    {
    }

    public void ChangeRotation(Vector3 rotation)
    {
    }

    public void RestoreRotation()
    {
    }

    public void EndAttackMovement()
    {
    }

    public void SetCanReadInputs(bool b)
    {
        canAttack = b;
    }

    public bool GetCanReadInputs()
    {
        return canAttack;
    }


    public void PlayerFall()
    {
    }

    public void PlayerRecovery()
    {
    }

    public bool IsAttacking()
    {
        return combatSystemAngel.Attacking;
    }

    public void PlayerFallV2()
    {
        throw new NotImplementedException();
    }

    public void PlayerRecoveryV2()
    {
        throw new NotImplementedException();
    }

    public bool IsJumpingInWall()
    {
        throw new NotImplementedException();
    }

    public void OnStartRunning()
    {
        throw new NotImplementedException();
    }

    public void OnStopRunning()
    {
        throw new NotImplementedException();
    }

    public void PlayerTouchEnemy()
    {
    }

    public List<GameObject> GetEnemiesInCombat()
    {
        throw new NotImplementedException();
    }

    public void SetEnemiesInCombat(List<GameObject> gameObjects)
    {
        throw new NotImplementedException();
    }

    public void RotateCharacter(Vector3 position)
    {
        throw new NotImplementedException();
    }

    public IMovementRigidBodyV2 GetMovementRigidBody()
    {
        return this;
    }

    public void Mareado()
    {
        animationController.PlayStunt();
        _aiController.StopAI();
    }

    public void GetFatalities()
    {
        animationController.PlayGetFatality();
    }

    public void SetPositionAndRotation(GameObject refOfPlayer)
    {
        transform.position = Vector3.Lerp(transform.position, refOfPlayer.transform.position, 0.5f);
        transform.rotation = Quaternion.Lerp(transform.rotation, refOfPlayer.transform.rotation, 0.5f);
    }

    public void StartAnimationFatality()
    {
        animationController.PlayGetFatality(() =>
        {
            IsDead = true;
            IsDeadWithFatality = true;
            Died();
        });
    }

    public void DisableColliders()
    {
        rigidbody.useGravity = false;
        foreach (var collider1 in collidersToDisable)
        {
            collider1.enabled = false;
        }
    }

    public void EnableColliders()
    {
        foreach (var collider1 in collidersToDisable)
        {
            collider1.enabled = true;
        }

        rigidbody.useGravity = true;
    }

    public void SetLifeTo(int percentage)
    {
        _statisticsOfCharacter.life = statisticsOfCharacter.life * percentage / 100;
    }

    public void Clean()
    {
        _aiController.StopAI();
        _aiController.Clean();
    }
}

public interface IEnemyV2
{
    Action<StunInfo> OnReceiveDamage { get; set; }
    List<GameObject> Paths();
    void MoveTo(GameObject target);
    event Action OnArriveToTarget;
    event Action<bool> OnPlayerDetected;
    event Action<bool> OnPlayerInNearZone;
    event Action<EnemyV2> OnDead;
    void RotateToTargetIdle(GameObject transformForward, bool b);
    CharacterV2 GetPlayer();
    void CanMove(bool b);
    void Died();
    float GetTimeToAttack();
    string GetAttackAnimationName();
    float GetTimeBetweenAttacks();
    bool CanActivateCollider(float delta);

    void ColliderToAttack(bool enableCollider);

    /*void SendDamage();*/
    void AttackPlayer();
    void SetState(StatesOfEnemy state);
    void CanRotate(bool b);
    public bool IsDead { get; }
    GameObject GetGameObject();
    GameObject GetCenterOfTheZone();
    void SetInstaRotation(bool instaRotation);
    AnimationController GetAnimatorController();
}

public class EnemiesV2Factory
{
    private readonly EnemiesV2Configuration EnemiesConfiguration;

    public EnemiesV2Factory(EnemiesV2Configuration enemiesConfiguration)
    {
        EnemiesConfiguration = Object.Instantiate(enemiesConfiguration);
    }

    public EnemyV2 Create(string id)
    {
        var prefab = EnemiesConfiguration.GetEnemyV2PrefabById(id);

        return Object.Instantiate(prefab);
    }
}