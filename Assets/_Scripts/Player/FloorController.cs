using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public class FloorController: MonoBehaviour
    {
        public Action OnFall, OnRecovery;
        public Action<bool> OnTouchingFloorChanged;
        [SerializeField] private float minDistanceToFloor;
        [SerializeField] private bool _isTouchingFloor;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private List<Transform> raycastsOrigins;
        [SerializeField] private Transform raycastCenter;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private float maxDistance;
        private GameObject _player;
        private bool _isConfigured;
        
        private void FixedUpdate()
        {
            if (!_isConfigured) return;
            var touching = false;
            foreach (var origin in raycastsOrigins)
            {
                if (Physics.Raycast(origin.position + Vector3.up, Vector3.down, out var hit, 10, layerMask))
                {
                    var isTouchingFloor = hit.distance <= minDistanceToFloor;
                    if (isTouchingFloor)
                    {
                        OnRecovery?.Invoke();
                        if (!_isTouchingFloor)
                        {
                            OnTouchingFloorChanged?.Invoke(true);
                            _isTouchingFloor = true;
                        }
                        return;
                    }
                }
            }
            
            if (_isTouchingFloor) OnTouchingFloorChanged?.Invoke(false);
            _isTouchingFloor = false;
        }

        private void Update()
        {
            
        }

        private void LateUpdate()
        {
            UpdateShadow();
        }

        private void UpdateShadow()
        {
            if (Physics.Raycast(raycastCenter.position + Vector3.up * 2, Vector3.down, out var hitToShadow, 100, layerMask))
            {
                shadow.gameObject.SetActive(true);
                float distance = Vector3.Distance(transform.position, hitToShadow.point);
                float scale = Mathf.Lerp(1.0f, 0.5f, distance / maxDistance);  // Escala de la sombra
                shadow.transform.localScale = new Vector3(scale, scale, 1f); // Z en 1 ya que es un sprite 2D
                // Ajusta la transparencia
                shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(.6f, 0.2f, distance / maxDistance));
                shadow.transform.position = new Vector3(shadow.transform.position.x, hitToShadow.point.y + 0.05f, shadow.transform.position.z);
            }
        }

        public bool IsTouchingFloor()
        {
            return _isTouchingFloor;
        }

        public void Configure(GameObject movementRigidBodyV2)
        {
            _player = movementRigidBodyV2.gameObject;
            _isConfigured = true;
        }

        private void OnDrawGizmos()
        {
            if(!_isConfigured) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(raycastCenter.position + Vector3.up * 2, Vector3.down * 10);
            
            Gizmos.color = _isTouchingFloor ? Color.green : Color.red;
            Gizmos.DrawSphere(_player.transform.position + Vector3.up, 0.5f);
            Gizmos.DrawLine(_player.transform.position, _player.transform.position + Vector3.up * 2);
        }
    }
}