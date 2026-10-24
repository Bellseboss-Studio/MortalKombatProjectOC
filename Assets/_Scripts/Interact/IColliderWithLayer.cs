using System;
using UnityEngine;

namespace _Scripts.Interact
{
    public interface IColliderWithLayer
    {
        Action<GameObject, CameraCollider> ColliderEnter { get; set; }
        Action<GameObject, CameraCollider> ColliderExit { get; set; }
    }
}