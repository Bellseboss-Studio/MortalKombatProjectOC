using System;
using System.Collections.Generic;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

public class AttackMovementSystem : MonoBehaviour, IFocusTarget
{
    public Action OnAttack, OnMidAir, OnRelease, OnSustain, OnEndAttack;
    [SerializeField] private AttackMovementData attackMovementDataQuick;
    [SerializeField] private AttackMovementData attackMovementDataPower;
    [SerializeField] private int maxNumberOfCombosQuick;
    [SerializeField] private int maxNumberOfCombosPower;
    private TargetFocus targetFocus; 
    private bool canAttackAgain = true;
    private bool attacking;
    private float _deltatimeLocal;
    private Vector3 _distance;
    private int _numberOfCombosQuick;
    private int _numberOfCombosPower;
    private TeaTime _attack, _decresing, _sustain, _release;
    private Rigidbody _rigidbody;
    private RigidbodyConstraints _rigidbodyConstraints;
    private AttackMovementData _attackMovementData;
    private bool _isQuickAttack;
    private List<EnemyV2> _enemies = new List<EnemyV2>();
    private StatisticsOfCharacter _statisticsOfCharacter;


    public void Configure(Rigidbody rigidbody, StatisticsOfCharacter statisticsOfCharacter,
        IMovementRigidBodyV2 movementRigidBodyV2)
    {
        _statisticsOfCharacter = statisticsOfCharacter;
        targetFocus.Configure(this);
        _rigidbody = rigidbody;
        var gameObjectToPlayer = rigidbody.gameObject;
        _rigidbodyConstraints = _rigidbody.constraints;
        _attack = this.tt().Pause().Add(() =>
        {
            _deltatimeLocal = 0;
            //_rigidbody.useGravity = false;
            /*_rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ |
                                     RigidbodyConstraints.FreezeRotationX;*/
            if (attacking)
            {
                if (_isQuickAttack)
                {
                    if (_numberOfCombosQuick <= maxNumberOfCombosQuick)
                    {
                        _numberOfCombosQuick++;
                    }
                }
                else
                {
                    if (_numberOfCombosPower <= maxNumberOfCombosPower)
                    {
                        _numberOfCombosPower++;
                    }
                }
            }
        }).Add(() =>
        {
            OnAttack?.Invoke();
            targetFocus.EnableCollider();
        }).Loop(loop =>
        {
            _deltatimeLocal += loop.deltaTime;
            if (_deltatimeLocal >= _attackMovementData.timeToAttack)
            {
                loop.Break();
            }

            float t = _deltatimeLocal / _attackMovementData.timeToAttack;
            float heightMultiplier = Mathf.Cos(t * Mathf.PI * 0.5f);

            var position = gameObjectToPlayer.transform.position;
            position = Vector3.Lerp(position,
                position + transform.forward * (_attackMovementData.maxDistance * heightMultiplier),
                _attackMovementData.forceToAttack * loop.deltaTime);
            gameObjectToPlayer.transform.position = position;
        }).Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Attack End");
            _decresing.Play();
            foreach (var enemy in targetFocus.GetEnemies<PJV2>())
            {
                /*enemy.ReceiveDamage(_statisticsOfCharacter.damage, gameObject.transform.forward);*/
                /*enemy.SetAnimationToHit(_isQuickAttack, _isQuickAttack ? _numberOfCombosQuick : _numberOfCombosPower);*/
            }
            targetFocus.CleanEnemies();
            targetFocus.DisableCollider();
        });

        _decresing = this.tt().Pause().Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Decresing Start");
            canAttackAgain = true;
            OnMidAir?.Invoke();
        }).Loop(loop =>
        {
            _deltatimeLocal += loop.deltaTime;
            if (_deltatimeLocal >= _attackMovementData.timeToAttack + _attackMovementData.timeToDecreasing)
            {
                canAttackAgain = false;
                loop.Break();
            }

            float t = (_deltatimeLocal - _attackMovementData.timeToAttack) / _attackMovementData.timeToDecreasing;
            float heightMultiplier = Mathf.Log(1 + t * 4);

            var position = gameObjectToPlayer.transform.position;
            position = Vector3.Lerp(position,
                position - transform.forward * (_attackMovementData.distanceToDecresing * heightMultiplier),
                _attackMovementData.forceToDecreasing * loop.deltaTime);
            if (!double.IsNaN(position.x) && !double.IsNaN(position.y) && !double.IsNaN(position.z))
            {
                gameObjectToPlayer.transform.position = position;
            }
        }).Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Decresing End");
            _sustain.Play();   
        });

        _sustain = this.tt().Pause().Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Sustain Start");
            OnSustain?.Invoke();
        }).Loop(loop =>
        {
            _deltatimeLocal += loop.deltaTime;
            if (_deltatimeLocal >= _attackMovementData.timeToAttack + _attackMovementData.timeToDecreasing +
                _attackMovementData.timeToSustain)
            {
                loop.Break();
            }
        }).Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Sustain End");
            _release.Play();
        });
        
        _release = this.tt().Pause().Add(() =>
        {
            //Debug.Log("AttackMovementSystem: Release Start");
            OnRelease?.Invoke();
        }).Loop(loop=>
        {
            _deltatimeLocal += loop.deltaTime;
            if (_deltatimeLocal >= _attackMovementData.timeToAttack + _attackMovementData.timeToDecreasing + _attackMovementData.timeToSustain + _attackMovementData.timeToRelease)
            {
                loop.Break();
            }

            var t = _deltatimeLocal / _attackMovementData.timeToAttack;
            var heightMultiplier = Mathf.Log(1 + t * _attackMovementData.forceToDecreasing);

            var position = gameObjectToPlayer.transform.position;
            position = Vector3.Lerp(position, position - transform.forward * (_attackMovementData.maxDistance * heightMultiplier), _attackMovementData.forceToDecreasing * loop.deltaTime);
            gameObjectToPlayer.transform.position = position;
        }).Add(() =>
        {
            canAttackAgain = true;
            _numberOfCombosQuick = 0;
            _numberOfCombosPower = 0;
            attacking = false;
            _rigidbody.linearVelocity = Vector3.zero;
            //_rigidbody.useGravity = true;
            /*_rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;*/
            //Debug.Log("AttackMovementSystem: End Attack");
            OnEndAttack?.Invoke();
            movementRigidBodyV2.EndAttackMovement();
        });
    }

    public void Attack(Vector3 distance, TypeOfAttack typeOfAttack)
    {
        canAttackAgain = false;
        attacking = true;
        _distance = distance;
        if(typeOfAttack == TypeOfAttack.Quick)
        {
            _isQuickAttack = true;
            _attackMovementData = attackMovementDataQuick;
        }
        else
        {
            _isQuickAttack = false;
            _attackMovementData = attackMovementDataPower;
        }
        
        _decresing.Stop();
        _attack.Stop().Play();
    }
    
    public enum TypeOfAttack
    {
        Quick,
        Power
    }
    public bool CanAttackAgain()
    {
        return canAttackAgain;
    }

    public bool FullCombo()
    {
        return _numberOfCombosQuick >= maxNumberOfCombosQuick || _numberOfCombosPower >= maxNumberOfCombosPower;
    }
}