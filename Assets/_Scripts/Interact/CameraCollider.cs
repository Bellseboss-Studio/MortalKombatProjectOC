using System;
using UnityEngine;

namespace _Scripts.Interact
{
    public class CameraCollider : MonoBehaviour, IColliderWithLayer
    {
        [SerializeField] private LayerMask layer;
        public Action<GameObject, CameraCollider> ColliderEnter { get; set; }
        public Action<GameObject, CameraCollider> ColliderExit { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            var o = other.gameObject;
            //Debug.Log($"CameraCollider:OnTriggerEnter: {o.name} - {o.layer} - {layer}");
            //validate if the layer is the same
            if ((layer.value & 1 << o.layer) > 0)
            {
                ColliderEnter?.Invoke(o, this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var o = other.gameObject;
            //Debug.Log($"CameraCollider:OnTriggerExit: {o.name} - {o.layer} - {layer}");
            //validate if the layer is the same
            if ((layer.value & 1 << o.layer) > 0)
            {
                ColliderExit?.Invoke(o, this);
            }
        }
    }
}