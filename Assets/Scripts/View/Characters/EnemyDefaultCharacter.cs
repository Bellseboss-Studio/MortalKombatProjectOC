using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ServiceLocatorPath;
using StatesOfEnemies;
using UnityEngine;
using View.Characters.Enemy;
using View.UI;
using View.Zone;

namespace View.Characters
{
    public class EnemyDefaultCharacter : Character, IEnemyCharacter
    {
        protected EnemyBehavior behaviorEnemy;
        private List<Vector3> _points;
        [SerializeField]private float toleranceToArrivedPoint;
        [SerializeField] private RedZoneComponent _redZoneComponent;
        private AreaZoneController yellowZone;
        private AreaZoneController greenZone;
        [SerializeField] private AreaZoneController rangeOfAttack;
        [SerializeField] private TypesEnemy type;
        [SerializeField] private UiController _uiController;
        private GameObject target;
        private ObserverUI _observerUI;
        [SerializeField] private LookAtCameraForever cameraLook;
        private Vector3 _toPoint;
        public TypesEnemy TypeEnemy => type;
        public delegate void OnPlayerTrigger(GameObject player);

        public delegate void OnDeath(GameObject gameObject);

        public OnDeath OnDeathDelegate;
        private bool _canMove;

        protected override void UpdateLegacy()
        {
            /*if (target != null)
            {
                instantiate.transform.rotation = RotatingLocal(target.transform.position - transform.position);
            }*/
            if (rb.linearVelocity == Vector3.zero) return;
            instantiate.transform.rotation = RotatingLocal(rb.linearVelocity);
        }

        protected override void ConfigureExplicit()
        {
            _points = new List<Vector3>();
        }

        public override float GetDamageForKick()
        {
            return power * 2;
        }

        public override float GetDamageForPunch()
        {
            return power * 1;
        }

        protected override void Muerte()
        {
            OnDeathDelegate?.Invoke(gameObject);
            behaviorEnemy.SetIAmDeath(true);
            animator.SetTrigger("Muerte");
        }

        public override Vector3 GetDirectionWithObjective()
        {
            return transform.forward;
        }

        public void SetBehavior(AreaZoneController yellowZone, AreaZoneController greenZone)
        {
            this.yellowZone = yellowZone;
            this.greenZone = greenZone;
            
            this.yellowZone.OnPlayerEnter += YellowZoneOnOnPlayerEnter;
            
            behaviorEnemy = gameObject.AddComponent<EnemyBehavior>();
            var enemyState = behaviorEnemy.Configuration(this);
            StartCoroutine(behaviorEnemy.StartState(enemyState));
        }

        private void YellowZoneOnOnPlayerEnter(GameObject player)
        {
            Debug.Log("Player has entered");
        }

        public void MoveToPoint(Vector3 toPoint)
        {
            _canMove = true;
            _toPoint = toPoint;
        }

        public List<Vector3> GetPoints()
        {
            return _points;
        }

        public bool IsEnemyArrived(Vector3 concurrentPoint)
        {
            var position = transform.position;
            return (concurrentPoint - position).sqrMagnitude < toleranceToArrivedPoint;
        }

        public void SubscribeOnPlayerEnterTrigger(OnPlayerTrigger action)
        {
            _redZoneComponent.OnPlayerEnter += action;
        }
        
        public void SubscribeOnPlayerExitTrigger(OnPlayerTrigger action)
        {
            _redZoneComponent.OnPlayerExit += action;
        }

        public bool IsPlayerInYellowZone()
        {
            return yellowZone.IsPlayerInThisZone();
        }

        public bool IsPlayerInGreenZone()
        {
            return greenZone.IsPlayerInThisZone();
        }

        public void LookTarget(Vector3 target)
        {
            movementPlayer = Vector3.zero;
            instantiate.transform.LookAt(target);
        }

        public bool IsPlayerInRangeOfAttack()
        {
            return /*rangeOfAttack.IsPlayerInThisZone() && */behaviorEnemy.IsEnemyArrived(GetPointAccordingPlayer(gameObject));
        }

        public void StopMovement()
        {
            _canMove = false;
        }

        public float GetVelocity()
        {
            return velocityOfAttack;
        }

        public void Attack(Character characterToAttack, float damage)
        {
            characterToAttack.ApplyDamage(damage);
        }

        public Vector3 GetPointAccordingPlayer(GameObject transformPosition)
        {
            return yellowZone.GetPointAccordingPlayer(transformPosition);
        }

        public void SetPoints(List<GameObject> pointsList)
        {
            foreach (var point in pointsList)
            {
                _points.Add(point.transform.position);
            }
        }

        public void SetRootCamera(GameObject cameraRoot)
        {
            cameraLook.SetCameraRoot(cameraRoot);
        }

        public Vector3 GetToPoint()
        {
            return _toPoint;
        }

        public bool CanMove()
        {
            return _canMove;
        }
    }
}

public enum TypesEnemy
{
    Default
}