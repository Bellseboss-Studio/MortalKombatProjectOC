using UnityEngine;

namespace _Scripts.Player
{
    public interface IMovementRigidBodyV2
    {
        void UpdateAnimation();
        void UpdateAnimation(bool isTouchingFloor, bool isTouchingWall);
        void ChangeToNormalJump();
        void ChangeRotation(Vector3 rotation);
        void RestoreRotation();
        void EndAttackMovement();
        void PlayerFall();
        void PlayerRecovery();
        bool IsAttacking();
        void PlayerFallV2();
        void PlayerRecoveryV2();
        bool IsJumpingInWall();
        void OnStartRunning();
        void OnStopRunning();
    }
}