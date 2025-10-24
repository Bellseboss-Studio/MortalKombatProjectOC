using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class MovementADSR : MonoBehaviour
    {
        public Action OnAttack, OnMidAir, OnRelease, OnSustain, OnEndAttack;
        [SerializeField] private AttackMovementData movementData;
        [SerializeField] private bool canAttackAgain = true;
        [SerializeField] private bool attacking;
        [SerializeField] private float _deltatimeLocal;
        private TeaTime _attack, _decresing, _sustain, _release;
        private Rigidbody _rigidbody;
        private RigidbodyConstraints _rigidbodyConstraints;
        private AttackMovementData _attackMovementData;
        private Vector3 _direction;
        private StatisticsOfCharacter _statisticsOfCharacter;
        private PJV2 _pJV2;

        public void Configure(Rigidbody rigidbody, StatisticsOfCharacter statisticsOfCharacter, PJV2 pj)
        {
            _statisticsOfCharacter = statisticsOfCharacter;
            _rigidbody = rigidbody;
            var gameObjectToPlayer = rigidbody.gameObject;
            _rigidbodyConstraints = _rigidbody.constraints;
            _pJV2 = pj;
            _attack = this.tt().Pause().Add(() =>
            {
                _deltatimeLocal = 0;
                //_rigidbody.useGravity = false;
                /*_rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ |
                                         RigidbodyConstraints.FreezeRotationX ;*/
                if (attacking)
                {
                }

                _pJV2.Stun(true);
            }).Add(() =>
            {
                OnAttack?.Invoke();
                //Debug.Log("AttackMovementSystem: Start Attack");
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
                    position + _direction * (_attackMovementData.maxDistance * heightMultiplier),
                    _attackMovementData.forceToAttack * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }).Add(() =>
            {
                //Debug.Log("AttackMovementSystem: Attack End");
                _decresing.Play();
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
                    position - _direction * (_attackMovementData.maxDistance * heightMultiplier),
                    _attackMovementData.forceToDecreasing * loop.deltaTime);
                if (!double.IsNaN(position.x) && !double.IsNaN(position.y) && !double.IsNaN(position.z) &&
                    !double.IsInfinity(position.x) && !double.IsInfinity(position.y) && !double.IsInfinity(position.z))
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
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _attackMovementData.timeToAttack + _attackMovementData.timeToDecreasing +
                    _attackMovementData.timeToSustain + _attackMovementData.timeToRelease)
                {
                    loop.Break();
                }

                var t = _deltatimeLocal / _attackMovementData.timeToAttack;
                var heightMultiplier = Mathf.Log(1 + t * _attackMovementData.forceToDecreasing);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position - _direction * (_attackMovementData.maxDistance * heightMultiplier),
                    _attackMovementData.forceToDecreasing * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }).Add(() =>
            {
                canAttackAgain = true;
                attacking = false;
                _rigidbody.linearVelocity = Vector3.zero;
                //_rigidbody.useGravity = true;
                /*_rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;*/
                //Debug.Log("AttackMovementSystem: End Attack");
                OnEndAttack?.Invoke();
                _pJV2.Stun(false);
            });
        }

        public void Attack(Vector3 direction)
        {
            //Debug.Log("AttackMovementSystem: Attack");
            canAttackAgain = false;
            attacking = true;
            _attackMovementData = movementData;
            _direction = direction;
            _decresing.Stop();
            _attack.Stop().Play();
        }

        public bool CanAttackAgain()
        {
            return canAttackAgain;
        }
    }
}