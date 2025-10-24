using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class BehaviourOfJumpSystemNormal : MonoBehaviour, IBehaviourOfJumpSystem
    {
        public Action OnAttack { get; set; }
        public Action OnMidAir { get; set; }
        public Action OnSustain { get; set; }
        public Action OnRelease { get; set; }
        public Action OnEndJump { get; set; }
        
        private TeaTime _attack, _decay, _sustain, _release, _endJump;
        private float _deltatimeLocal;
        [SerializeField] private float timeToAttack, timeToDecreasing, timeToSustain, timeToRelease;
        [SerializeField] private float maxHeighJump, heightDecreasing;
        [SerializeField] private float forceToAttack, forceToDecreasing;
        [SerializeField] private FloorController floorController;


        public void Configure(Rigidbody _rigidbody, IJumpSystem jumpSystem)
        {
            Debug.Log($"Configured BehaviourOfJumpSystemNormal: {_rigidbody.gameObject.name}");
            var gameObjectToPlayer = _rigidbody.gameObject;
            _attack = this.tt().Pause().Add(() =>
            {
                _deltatimeLocal = 0;
                _rigidbody.useGravity = false;
                /*_rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationZ |
                                         RigidbodyConstraints.FreezeRotationX;*/
                //Debug.Log("JumpSystem: Attack");
            }).Add(() =>
            {
                OnAttack?.Invoke(); 
                //TODO IVAN: Play sound went jump
            }).Loop(loop =>
            {
                //Debug.Log("JumpSystem: Attack Loop");
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToAttack)
                {
                    loop.Break();
                }

                float t = _deltatimeLocal / timeToAttack;
                float heightMultiplier = Mathf.Cos(t * Mathf.PI * 0.5f);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position, position + Vector3.up * (maxHeighJump * heightMultiplier),
                    forceToAttack * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }).Add(() => { _decay.Play(); });
            _decay = this.tt().Pause().Add(() =>
            {
                OnMidAir?.Invoke();
                //TODO IVAN: Play sound went jump is in mid air
            }).Loop(loop =>
            {
                //Debug.Log("JumpSystem: Decreasing Loop");
                _deltatimeLocal += loop.deltaTime;

                float t = (_deltatimeLocal - timeToAttack) / timeToDecreasing;
                float heightMultiplier = Mathf.Log(1 + t * 4);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position, position - Vector3.up * (heightDecreasing * heightMultiplier),
                    forceToDecreasing * loop.deltaTime);
                //Validate NaN value
                if (!double.IsNaN(position.x) && !double.IsNaN(position.y) && !double.IsNaN(position.z))
                {
                    gameObjectToPlayer.transform.position = position;
                }

                if (_deltatimeLocal >= timeToAttack + timeToDecreasing || floorController.IsTouchingFloor())
                {
                    loop.Break();
                }
            }).Add(() => { _sustain.Play(); });
            _sustain = this.tt().Pause().Add(() =>
            {
                OnSustain?.Invoke();
                //TODO IVAN: Play sound went jump is in sustain
            }).Loop(loop =>
            {
                //Debug.Log("JumpSystem: Sustain Loop");
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToAttack + timeToDecreasing + timeToSustain)
                {
                    loop.Break();
                }
            }).Add(() => { _release.Play(); });
            
            _release = this.tt().Pause().Add(() =>
            {
                OnRelease?.Invoke();
                //TODO IVAN: Play sound went jump is in release use the gravity
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (floorController.IsTouchingFloor())
                {
                    loop.Break();
                }

                var t = _deltatimeLocal / timeToAttack;
                var heightMultiplier = Mathf.Log(1 + t * forceToDecreasing);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position, position - Vector3.up * (maxHeighJump * heightMultiplier),
                    forceToDecreasing * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }).Add(() => { 
                _endJump.Play();
            });
            
            _endJump = this.tt().Pause().Add(() =>
            {
                _rigidbody.useGravity = true;
                /*_rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;*/
                _deltatimeLocal = 0;
                //Debug.Log("JumpSystem: Attack End");
                OnEndJump?.Invoke();
                jumpSystem.RestoreRotation();
                //TODO IVAN: Play sound went jump is in end went touch the floor
            });
        }

        public TeaTime GetAttack()
        {
            return _attack;
        }

        public TeaTime GetDecay()
        {
            return _decay;
        }

        public TeaTime GetSustain()
        {
            return _sustain;
        }

        public TeaTime GetRelease()
        {
            return _release;
        }

        public TeaTime GetEndJump()
        {
            return _endJump;
        }

        public void StopAll()
        {
            _attack.Stop();
            _decay.Stop();
            _sustain.Stop();
            _release.Stop();
            _endJump.Stop();
        }
    }
}