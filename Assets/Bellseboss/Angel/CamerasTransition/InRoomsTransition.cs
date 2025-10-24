using System;
using _Scripts.Interact;
using DG.Tweening;
using UnityEngine;

namespace Bellseboss.Angel
{
    [Serializable]
    public class InRoomsTransition
    {
        public CameraCollider cameraCollider;
        public Transform cameraTransform;
        public float transitionTime;
        public Ease easeType;
        public Vector3 CameraPosition => cameraTransform.position;
    }
}