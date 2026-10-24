using System;
using UnityEngine;

namespace _Scripts.Player
{
    public interface ICharacterV2
    {
        Action OnAction { get; set; }
        Action<ICharacterV2> OnDead { get; set; }
        GameObject Model3DInstance { get; }
        void DisableControls();
        void SetPositionAndRotation(GameObject refOfPlayer);
        void EnableControls();
        Transform GetGameObject();
        void StartDeadAction();
        
        AnimationController GetAnimationController();
    }
}