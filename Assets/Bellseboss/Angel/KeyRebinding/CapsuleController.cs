using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellseboss.Angel.KeyRebinding
{
    public class CapsuleController : MonoBehaviour
    {
        private Vector3 _moveDir;
        [SerializeField] private CharacterController characterController;
        private Vector3 _playerVelocity;
        [SerializeField] private float playerSpeed = 2f;
        [SerializeField] private float jumpHeight = 1f;

        private const float Gravity = -9.81f;
        
        private void Start()
        {
            _moveDir = Vector3.zero;
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            Vector2 newMoveDir = ctx.ReadValue<Vector2>();
            _moveDir.x = newMoveDir.x;
            _moveDir.z = newMoveDir.y;
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                _playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * Gravity);
            }
        }

        private void Update()
        {
            if (characterController.isGrounded && _playerVelocity.y < 0)
            {
                _playerVelocity.y = 0;
            }

            characterController.Move(_moveDir * playerSpeed * Time.deltaTime);
            _playerVelocity.y += Gravity * Time.deltaTime;
            characterController.Move(_playerVelocity * Time.deltaTime);
        }
    }
}