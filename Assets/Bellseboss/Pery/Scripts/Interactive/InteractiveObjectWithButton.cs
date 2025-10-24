using System;
using _Scripts.Interact;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

public class InteractiveObjectWithButton : InteractiveManager
{
    [SerializeField] protected string animationTrigger;
    [SerializeField] protected Animator animatorInteractiveObject;
    [SerializeField] protected GameObject refOfPlayer;
    [SerializeField] protected TooltipToInteractable tooltipToInteractable;
    public Action OnActionEnter;
    public Action OnActionExit;

    protected override void Start()
    {
        base.Start();
        tooltipToInteractable.Configurate(this);
    }

    protected override void OnColliderExit(GameObject o, CameraCollider room)
    {
        if (o.TryGetComponent(out ICharacterV2 character))
        {
            _characterV2.OnAction -= OnActionTrigger;
            _characterV2 = null;
            OnActionExit?.Invoke();
        }
    }

    protected override void OnColliderEnter(GameObject o, CameraCollider room)
    {
        if (o.TryGetComponent(out ICharacterV2 character))
        {
            _characterV2 = character;
            _characterV2.OnAction += OnActionTrigger;
            OnActionEnter?.Invoke();
        }
    }

    protected override void OnActionTrigger()
    {
        playableDirector.Play();
        animatorInteractiveObject.SetTrigger("activate");
        _characterV2.DisableControls();
        _characterV2.ActivateAnimationTrigger(animationTrigger);
        canChangePosition = true;
        deltaTimeLocal = 0;
        cinemachineVirtualCamera.LookAt = refOfPlayer.transform;
    }

    private void Update()
    {
        if (!canChangePosition || _characterV2 == null) return;
        _characterV2.SetPositionAndRotation(refOfPlayer);
        deltaTimeLocal += Time.deltaTime;
        if (deltaTimeLocal >= playableDirector.duration)
        {
            canChangePosition = false;
            _characterV2.EnableControls();
            Debug.Log($"InteractiveManager: OnAction {deltaTimeLocal}");
        }
    }
}