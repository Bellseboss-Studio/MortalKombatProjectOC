using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

public class InteractiveObjectWithCollision : InteractiveManager
{
    protected override void OnColliderExit(GameObject o, CameraCollider room)
    {
        
    }

    protected override void OnColliderEnter(GameObject o, CameraCollider room)
    {
        playableDirector.Play();
        o.GetComponent<ICharacterV2>().DisableControls();
    }

    protected override void OnActionTrigger()
    {
        SignalAction();
    }
}