using System;
using Bellseboss.Angel.CombatSystem;
using UnityEngine;

namespace _Scripts.Player
{
    public class StunSystem : MonoBehaviour
    {
        private TeaTime _attack, _decreasing, _sustain, _release;
        public Action OnAttack, OnMidAir, OnRelease, OnSustain, OnEndAttack, oneTimeOnEndAttack, OnStunt, OnEndStunt;
        [SerializeField] private float _deltatimeLocal;
        private Rigidbody _rigidbody;
        private RigidbodyConstraints _rigidbodyConstraints;
        private MoveAttackingSystem _moveAttackingSystem;
        private StunInfo _currentStun;
        private ICombatSystemV2 _combatSystemAngel;

        public void Configure(Rigidbody rigidbody, StatisticsOfCharacter statisticsOfCharacter,
            IMovementRigidBodyV2 movementRigidBodyV2, IStunSystem characterV2, ICombatSystemV2 combatSystemAngel, bool isPlayer = true)
        {
            var gameObjectToPlayer = rigidbody.gameObject;
            _rigidbody = rigidbody;
            _rigidbodyConstraints = _rigidbody.constraints;
            combatSystemAngel.OnReceiveDamage += GotAttacked;
            _combatSystemAngel = combatSystemAngel;
            _moveAttackingSystem = new MoveAttackingSystem(gameObjectToPlayer, transform);
            
            _attack = this.tt().Pause().Add(() =>
            {
                if (isPlayer)
                {
                    
                }

                _deltatimeLocal = 0;
            }).Add(() => { OnAttack?.Invoke(); }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentStun.timeToAttack)
                {
                    loop.Break();
                }

                float t = 1 - _deltatimeLocal / _currentStun.timeToAttack;
                float heightMultiplier = Mathf.Log(1 + t * 4);

                _moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop, _currentStun.forceToAttack,
                    _currentStun.maxDistance, true);
            }).Add(() => { _decreasing.Play(); });

            _decreasing = this.tt().Pause().Add(() => { OnMidAir?.Invoke(); }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentStun.timeToAttack + _currentStun.timeToDecreasing)
                {
                    loop.Break();
                }

                float t = 1 - ((_deltatimeLocal - _currentStun.timeToAttack) / _currentStun.timeToDecreasing);
                float heightMultiplier = Mathf.Log(1 + t * 4);
                Debug.Log(t);
                /*_moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop,
                    _currentStun.forceToDecreasing, _currentStun.distanceToDecreasing, true);*/

            }).Add(() => { _sustain.Play(); });

            _sustain = this.tt().Pause().Add(() => { OnSustain?.Invoke(); }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentStun.timeToAttack + _currentStun.timeToDecreasing +
                    _currentStun.timeToSustain)
                {
                    loop.Break();
                }
            }).Add(() => { _release.Play(); });

            _release = this.tt().Pause().Add(() => { OnRelease?.Invoke(); }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentStun.timeToAttack + _currentStun.timeToDecreasing +
                    _currentStun.timeToSustain + _currentStun.timeToRelease)
                {
                    loop.Break();
                }

                var t = _deltatimeLocal / _currentStun.timeToAttack;
                var heightMultiplier = Mathf.Log(1 + t * _currentStun.forceToDecreasing);

                /*_moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop,
                    _currentStun.forceToDecreasing, 0, true);*/

            }).Add(EndStunt);
        }

        private void GotAttacked(StunInfo currentStun)
        {
            _combatSystemAngel.SetCanReadInputs(false);
            _currentStun = currentStun;
            _decreasing.Stop();
            _sustain.Stop();
            _release.Stop();
            _attack.Stop().Play();
        }

        private void EndStunt()
        {
            _combatSystemAngel.SetCanReadInputs(true);
            OnEndStunt?.Invoke();
        }
    }
}