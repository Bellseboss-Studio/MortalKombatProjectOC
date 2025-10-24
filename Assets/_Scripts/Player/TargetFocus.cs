using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public class TargetFocus : MonoBehaviour
    {
        public Action<GameObject> CollisionEnter;
        public Action<GameObject> CollisionExit;
        public LayerMask layerMask;
        public string tagToCompare;
        private IFocusTarget _focusTarget;
        private List<GameObject> _enemies = new List<GameObject>();
        private Collider _collider;

        public void Configure(IFocusTarget focusTarget)
        {
            _focusTarget = focusTarget;
            _collider = GetComponent<Collider>();
            DisableCollider();
        }
        public Vector3 RotateToTarget(Vector3 originalDirection)
        {
            var result = originalDirection;
            if (_enemies.Count > 0)
            {
                var closestEnemy = GetClosestEnemy();
                //Get the direction to the target
                result = closestEnemy.transform.position - transform.position;
                result.y = 0;
                result.Normalize();
            }
            return result;
        }

        public GameObject GetClosestEnemy()
        {
            GameObject closestEnemy = null;
            var minDistance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                var distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
            return closestEnemy;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((layerMask.value & (1 << other.gameObject.layer)) > 0 && other.gameObject.CompareTag(tagToCompare))
            {
                //Debug.Log($"TargetFocus: OnTriggerEnter: other: {other.gameObject.name}");
                _enemies.Add(other.gameObject);
                CollisionEnter?.Invoke(other.gameObject);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            //compare layer
            if ((layerMask.value & (1 << other.gameObject.layer)) > 0 && other.gameObject.CompareTag(tagToCompare))
            {
                //Debug.Log($"TargetFocus: OnTriggerExit: other: {other.gameObject.name}");
                _enemies.Remove(other.gameObject);
                CollisionExit?.Invoke(other.gameObject);
            }
        }

        public Vector3 GetTarget()
        {
            if(_enemies.Count > 0)
            {
                var closestEnemy = GetClosestEnemy();
                return closestEnemy.transform.position;
            }
            return Vector3.zero;
        }

        public void EnableCollider()
        {
            _collider.enabled = true;
        }

        public void DisableCollider()
        {
            _collider.enabled = false;
        }

        public List<T> GetEnemies<T>()
        {
            var list = new List<T>();
            //Debug.Log($"TargetFocus: GetEnemies: _enemies: {_enemies.Count}");
            foreach (var o in _enemies)
            {
                if (o.TryGetComponent(out T t))
                {
                    list.Add(t);
                }
            }
            return list;
        }

        public void CleanEnemies()
        {
            _enemies = new List<GameObject>();
        }

        public bool IsEnemyTouched()
        {
            return _enemies.Count > 0;
        }
    }
}


namespace Bellseboss.Pery.Scripts.Input
{
}