using System;
using System.Collections.Generic;
using System.Linq;
using Bellseboss.Angel.CombatSystem;
using TargetingSystemPath;
using UnityEngine;

namespace _Scripts.Player
{
    public class CombatSystemAngel : MonoBehaviour, IFocusTarget
    {
        [SerializeField] private List<CombatMovement> combatMovements;
        [SerializeField] private List<TypeOfAttack> currentComboSequence;
        
        private ICombatSystemV2 _combatSystemAngel;


        public Action OnAttack, OnMidAir, OnRelease, OnSustain, OnEndAttack, oneTimeOnEndAttack, OnStunt, OnEndStunt;
        private TeaTime _attack, _decresing, _sustain, _release;
        private Rigidbody _rigidbody;
        private RigidbodyConstraints _rigidbodyConstraints;
        private bool _isQuickAttack;
        private List<EnemyV2> _enemies = new List<EnemyV2>();
        private StatisticsOfCharacter _statisticsOfCharacter;
        private CombatMovement _currentAttack;
        private List<CombatMovement> _movementsQueue;
        private Action<string> _actionToAnimate;
        private MoveAttackingSystem _moveAttackingSystem;
        private TargetingSystem _targetingSystem;
        [SerializeField] private float angleAttack;
        [SerializeField] private float autoTargetDistance;
        [SerializeField] private bool canAttackAgain = true;
        [SerializeField] private bool attacking;
        [SerializeField] private float _deltatimeLocal;
        [SerializeField] private TargetFocus targetFocus;


        private List<GameObject> _enemiesInCombat
        {
            get => _combatSystemAngel.GetEnemiesInCombat();
            set => _combatSystemAngel.SetEnemiesInCombat(value);
        }

        public bool Attacking => attacking;

        private void Start()
        {
            
        }

        public void ExecuteMovement(TypeOfAttack typeOfAttack)
        {
            if (!_combatSystemAngel.GetCanReadInputs()) return;
            currentComboSequence.Add(typeOfAttack);
            bool found = false;
            CombatMovement combatMovement1 = null;
            foreach (var combatMovement in combatMovements.Where(combatMovement => combatMovement.comboSequence.SequenceEqual(currentComboSequence)))
            {
                combatMovement1 = combatMovement;
                found = true;
            }

            if (!found)
            {
                currentComboSequence.Remove(currentComboSequence[currentComboSequence.Count - 1]);
                return;
            }

            if (canAttackAgain)
            {
                _currentAttack = combatMovement1;
                _actionToAnimate.Invoke(_currentAttack.transitionParameterName);
                Attack(_currentAttack);
            }
            else
            {
                _movementsQueue.Add(combatMovement1);
            }
        }

        public void Configure(Rigidbody rigidbody, StatisticsOfCharacter statisticsOfCharacter,
            IMovementRigidBodyV2 movementRigidBodyV2, ICombatSystemV2 characterV2, bool isPlayer = true)
        {
            _targetingSystem = new TargetingSystem();
            var gameObjectToPlayer = rigidbody.gameObject;
            _moveAttackingSystem = new MoveAttackingSystem(gameObjectToPlayer, transform);
            _combatSystemAngel = characterV2;
            _combatSystemAngel.OnReceiveDamage += GotAttacked;
            _movementsQueue = new List<CombatMovement>();
            _statisticsOfCharacter = statisticsOfCharacter;
            targetFocus.Configure(this);
            _rigidbody = rigidbody;
            _rigidbodyConstraints = _rigidbody.constraints;
            _actionToAnimate = _combatSystemAngel.GetActionToAnimate();
            _attack = this.tt().Pause().Add(() =>
            {
                if (isPlayer)
                {
                    _enemiesInCombat =
                        new List<GameObject>(_targetingSystem.SetEnemiesOrder(_enemiesInCombat, transform.position));
                    if (_enemiesInCombat.Count > 0)
                        _targetingSystem.SetAutomaticTarget(autoTargetDistance, _enemiesInCombat, gameObject,
                            angleAttack, _combatSystemAngel);
                }
                _deltatimeLocal = 0;
            }).Add(() =>
            {
                OnAttack?.Invoke();
                targetFocus.EnableCollider();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentAttack.timeToAttack)
                {
                    loop.Break();
                }

                float t = _deltatimeLocal / _currentAttack.timeToAttack;
                float heightMultiplier = Mathf.Cos(t * Mathf.PI * 0.5f);

                _moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop, _currentAttack.forceToAttack, _currentAttack.maxDistance);
            }).Add(() =>
            {
                _decresing.Play();
                foreach (var enemy in targetFocus.GetEnemies<PJV2>())
                {
                    Debug.Log("has da;o");
                    enemy.ReceiveDamage(_statisticsOfCharacter.damage, gameObject, _currentAttack.stuntInfo);
                    enemy.SetAnimationToHit(_currentAttack.stuntInfo.parameterName);
                }

                if (targetFocus.GetEnemies<EnemyV2>().Count > 0)
                {
                    _combatSystemAngel.PlayerTouchEnemy();
                }
                targetFocus.CleanEnemies();
                targetFocus.DisableCollider();
            });

            _decresing = this.tt().Pause().Add(() =>
            {
                canAttackAgain = true;
                OnMidAir?.Invoke();
                if (_movementsQueue.Count > 0)
                {
                    _currentAttack = _movementsQueue[0];
                    _movementsQueue.RemoveAt(0);
                    _actionToAnimate.Invoke(_currentAttack.transitionParameterName);
                    Attack(_currentAttack);
                }
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentAttack.timeToAttack + _currentAttack.timeToDecreasing)
                {
                    loop.Break();
                }

                float t = (_deltatimeLocal - _currentAttack.timeToAttack) / _currentAttack.timeToDecreasing;
                float heightMultiplier = Mathf.Log(1 + t * 4);
                
                _moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop, _currentAttack.forceToDecreasing, _currentAttack.distanceToDecresing, true);

            }).Add(() =>
            {
                _sustain.Play();
            });

            _sustain = this.tt().Pause().Add(() =>
            {
                OnSustain?.Invoke();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentAttack.timeToAttack + _currentAttack.timeToDecreasing +
                    _currentAttack.timeToSustain)
                {
                    loop.Break();
                }
            }).Add(() =>
            {
                _release.Play();
            });

            _release = this.tt().Pause().Add(() =>
            {
                OnRelease?.Invoke();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= _currentAttack.timeToAttack + _currentAttack.timeToDecreasing +
                    _currentAttack.timeToSustain + _currentAttack.timeToRelease)
                {
                    loop.Break();
                }

                var t = _deltatimeLocal / _currentAttack.timeToAttack;
                var heightMultiplier = Mathf.Log(1 + t * _currentAttack.forceToDecreasing);
                
                _moveAttackingSystem.MovePlayer(gameObjectToPlayer, heightMultiplier, loop, _currentAttack.forceToDecreasing, _currentAttack.maxDistance, true);

            }).Add(EndCombo);
            
        }

        private void EndCombo()
        {
            currentComboSequence = new List<TypeOfAttack>();
            canAttackAgain = true;
            attacking = false;
            _rigidbody.linearVelocity = Vector3.zero;
            OnEndAttack?.Invoke();
            oneTimeOnEndAttack?.Invoke();
            oneTimeOnEndAttack = null;
            _combatSystemAngel.EndAttackMovement();
        }

        private void GotAttacked(StunInfo stunInfo)
        {
            EndCombo();
            _decresing.Stop();
            _sustain.Stop();
            _release.Stop();
            _attack.Stop();
        }

        private void Attack(CombatMovement currentAttack)
        {
            _decresing.Stop();
            _sustain.Stop();
            _release.Stop();
            _attack.Stop().Play();
            attacking = true;
            canAttackAgain = false;
        }
    }
}