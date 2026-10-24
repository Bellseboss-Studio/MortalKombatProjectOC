using UnityEngine;

namespace _Scripts.Player
{
    public abstract class PJV2 : MonoBehaviour
    {
        /*public Action<StunBehaviour> OnReceiveDamage;*/
        public abstract void ReceiveDamage(int damage, GameObject transformForward, StunInfo currentAttackStunTime);
        public abstract void SetAnimationToHit(string animationParameterName);
        public abstract void Stun(bool isStun);
        public abstract void DisableControls();
    }
}