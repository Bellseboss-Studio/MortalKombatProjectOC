using System;
using UnityEngine;

namespace _Scripts.Player
{
    public class BehaviourOfJumpSystemWalls : MonoBehaviour, IBehaviourOfJumpSystem
    {
        public Action OnAttack { get; set; }
        public Action OnMidAir { get; set; }
        public Action OnSustain { get; set; }
        public Action OnRelease { get; set; }
        public Action OnEndJump { get; set; }
        
        private TeaTime _attack, _decresing, _sustain, _release, _endJump, _delayToJump;
        private float _deltatimeLocal, _deltatimeLocalToJump;
        [SerializeField] private float timeToAttack, timeToDecreasing, timeToSustain, timeToRelease, timeToWaitToJump;
        [SerializeField] private float maxHeighJump, heightDecreasing;
        [SerializeField] private float forceToAttack, forceToDecreasing;
        [SerializeField] private FloorController floorController;
        [SerializeField] private float lenghtOfArrow;
        [SerializeField] private Vector3 _direction;
        private bool canJump;
        private bool isJumping;
        private bool isColliding;


        public void Configure(Rigidbody _rigidbody, IJumpSystem jumpSystem)
        {
            var gameObjectToPlayer = _rigidbody.gameObject;
            _attack = this.tt().Pause().Add(() =>
            {
                _rigidbody.useGravity = false;
                _deltatimeLocal = 0;
                isJumping = true;
                isColliding = false;
            }).Add(()=>
            {
                jumpSystem.ChangeRotation(_direction);
            }).Add(() => { OnAttack?.Invoke(); }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToAttack || isColliding)
                {
                    loop.Break();
                }
                float t = _deltatimeLocal / timeToAttack;
                float heightMultiplier = (Mathf.Cos(t * Mathf.PI * 0.5f) + 1) / 2;
                
                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position + ((_direction * .8f) + Vector3.up) * (maxHeighJump * heightMultiplier),
                    forceToAttack * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }).Add(() =>
            {
            }).Add(() =>
            {
                if (isColliding) return;
                _decresing.Play();
            });
            _decresing = this.tt().Pause().Add(() =>
            {
                OnMidAir?.Invoke();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToAttack + timeToDecreasing || isColliding || floorController.IsTouchingFloor())
                {
                    loop.Break();
                }

                float t = (_deltatimeLocal - timeToAttack) / timeToDecreasing;
                float heightMultiplier = Mathf.Log(1 + Mathf.Abs(t) * 4);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position - Vector3.up + (_direction / 3) * (heightDecreasing * heightMultiplier),
                    forceToDecreasing * loop.deltaTime);
                if (!double.IsNaN(position.x) && !double.IsNaN(position.y) && !double.IsNaN(position.z))
                {
                    gameObjectToPlayer.transform.position = position;
                }
            }).Add(() =>
            {
                if (isColliding) return;
                _sustain.Play();
            });
            _sustain = this.tt().Pause().Add(() =>
            {
                OnSustain?.Invoke();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToAttack + timeToDecreasing + timeToSustain || isColliding || floorController.IsTouchingFloor())
                {
                    loop.Break();
                }
            }).Add(() =>
            {
                if (isColliding) return;
                _release.Play();
            });
            
            _release = this.tt().Pause().Add(() =>
            {
                OnRelease?.Invoke();
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (floorController.IsTouchingFloor() || isColliding)
                {
                    loop.Break();
                }

                var t = _deltatimeLocal / timeToAttack;
                var heightMultiplier = Mathf.Log(1 + t * forceToDecreasing);

                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position - Vector3.up + (_direction / 6) * (heightDecreasing * heightMultiplier),
                    forceToDecreasing * loop.deltaTime);
                //validate is not NaN
                if (!double.IsNaN(position.x) && !double.IsNaN(position.y) && !double.IsNaN(position.z))
                {
                    gameObjectToPlayer.transform.position = position;
                }
            }).Add(() => { _endJump.Play(); });
            
            _endJump = this.tt().Pause().Add(() =>
            {
                _rigidbody.useGravity = true;
                _deltatimeLocal = 0;
                /*jumpSystem.ChangeNormalWall();*/
                isJumping = false;
                jumpSystem.RestoreRotation();
                OnEndJump?.Invoke();
            });
            _delayToJump = this.tt().Add(() =>
            {
                _deltatimeLocal = 0;
                _rigidbody.useGravity = false;
                canJump = true;
                isColliding = true;
                isJumping = false;
            }).Loop(loop =>
            {
                _deltatimeLocal += loop.deltaTime;
                if (_deltatimeLocal >= timeToWaitToJump || isJumping)
                {
                    loop.Break();
                }
            }).Add(() =>
            {
                canJump = false;
                if(!isJumping)
                {
                    _rigidbody.useGravity = true;
                }
            });
            
        }

        public TeaTime GetAttack()
        {
            return _attack;
        }

        public TeaTime GetDecay()
        {
            return _decresing;
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
            _decresing.Stop();
            _sustain.Stop();
            _release.Stop();
            _endJump.Stop();
        }

        public void ConfigureWall(Vector3 direction)
        {
            _direction = direction;
            _delayToJump.Play();
        }
        
        
        private void OnDrawGizmos()
        {
            var direc = _direction;
            if (direc == Vector3.zero) return;
            Gizmos.color = Color.red;
            var target = transform.position + direc * lenghtOfArrow;
            Gizmos.DrawLine(transform.position, target);
        
            Vector3 rightArrowPoint = Quaternion.LookRotation(direc) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
            Vector3 leftArrowPoint = Quaternion.LookRotation(direc) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);

            Gizmos.DrawLine(target, target + rightArrowPoint * (lenghtOfArrow/5));
            Gizmos.DrawLine(target, target + leftArrowPoint * (lenghtOfArrow/5));

        }
    }
}