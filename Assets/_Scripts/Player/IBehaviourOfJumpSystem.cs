using System;
using UnityEngine;

namespace _Scripts.Player
{
    public interface IBehaviourOfJumpSystem
    {
        public Action OnAttack { get; set; }
        public Action OnMidAir { get; set; }
        public Action OnSustain { get; set; }
        public Action OnRelease { get; set; }
        public Action OnEndJump { get; set; }
        public void Configure(Rigidbody _rigidbody, IJumpSystem jumpSystem);
        public TeaTime GetAttack();
        public TeaTime GetDecay();
        public TeaTime GetSustain();
        public TeaTime GetRelease();
        TeaTime GetEndJump();
        void StopAll();
    }
}