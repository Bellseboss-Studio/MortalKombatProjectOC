using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public abstract class InteractiveManager : MonoBehaviour
{
    [SerializeField, InterfaceType(typeof(IColliderWithLayer))]
    protected Object colliderWithLayer;
    [SerializeField] protected Activable activable;
    [SerializeField] protected PlayableDirector playableDirector;
    [SerializeField] protected CinemachineVirtualCamera cinemachineVirtualCamera;
    protected bool canChangePosition;
    protected IColliderWithLayer _collider => colliderWithLayer as IColliderWithLayer;
    protected ICharacterV2 _characterV2;
    protected double deltaTimeLocal;

    protected virtual void Start()
    {
        _collider.ColliderEnter += OnColliderEnter;
        _collider.ColliderExit += OnColliderExit;
        cinemachineVirtualCamera.gameObject.SetActive(false);
    }

    protected abstract void OnColliderExit(GameObject o, CameraCollider room);

    protected abstract void OnColliderEnter(GameObject o, CameraCollider room);

    public void SignalAction()
    {
        activable.Activate();
    }

    protected abstract void OnActionTrigger();
}