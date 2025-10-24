using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Player
{
    internal class AnimationController : MonoBehaviour
    {
        public Action OnFinishAnimationDamage;
        [SerializeField] private string velocityName, horizontalName, verticalName, targetName, punchName, kickName;
        private Animator _animator;
        private IAnimationController _animationController;
        private bool _isFinishAnimation;

        public void Configure(Animator animator, IAnimationController animationController)
        {
            _animator = animator;
            _animationController = animationController;
        }

        public void Movement(float velocity, float speed)
        {
            _animator.SetFloat(velocityName, velocity);
        }

        public void IsTarget(bool isTarget)
        {
            _animator.SetBool(targetName, isTarget);
        }

        public void Punch()
        {
            _animator.SetTrigger(punchName);
        }

        public void Kick()
        {
            _animator.SetTrigger(kickName);
        }

        public void JumpJump()
        {
            _animator.ResetTrigger("j_mid_air");
            _animator.SetTrigger("j");
        }

        public void JumpMidAir()
        {
            _animator.SetTrigger("j_mid_air");
        }

        public void JumpFall()
        {
            _animator.ResetTrigger("j_recovery");
            _animator.SetTrigger("j_fall");
        }

        public void JumpRecovery()
        {
            _animator.ResetTrigger("j_mid_air");
            _animator.ResetTrigger("j_fall");
            _animator.SetTrigger("j_recovery");
        }

        public void Fall()
        {
            _animator.SetTrigger("j_fall");
        }

        public void ActivateTrigger(string animationTrigger)
        {
            _animator.SetTrigger(animationTrigger);
        }

        public void TakeDamage(string animationParameterName)
        {
            /*nameOfAnimation += numberOfCombos;*/
            /*Debug.Log(nameOfAnimation);*/
            StartCoroutine(FinishTimeAnimation(animationParameterName));
            _animator.Play(animationParameterName);
        }

        private IEnumerator FinishTimeAnimation(string nameOfAnimation)
        {
            var animationClip = _animator.runtimeAnimatorController.animationClips;
            foreach (var clip in animationClip)
            {
                if (clip.name == nameOfAnimation)
                {
                    yield return new WaitForSeconds(clip.length);
                    OnFinishAnimationDamage?.Invoke();
                    break;
                }
            }
        }

        public void Die(string animationTrigger)
        {
            _animator.SetTrigger(animationTrigger);
        }

        public void SetTrigger(string paramName)
        {
            _animator.SetTrigger(paramName);
        }

        public void JumpingWalls(bool isTouchingFloor, bool isTouchingWall, bool isJumping)
        {
            _animator.SetBool("isTouchingFloor", isTouchingFloor);
            _animator.SetBool("isTouchingWall", isTouchingWall);
            _animator.SetBool("isJumping", isJumping);
        }

        public void IsJumping()
        {
            _animator.SetTrigger("j");
        }

        public void Dead()
        {
            _animator.SetTrigger("dead");
        }
    }
}