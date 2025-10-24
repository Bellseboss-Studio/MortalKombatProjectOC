using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public interface ICombatSystemV2
    {
        /*void PowerAttack(float runningDistance, Vector3 runningDirection);
        void QuickAttack(float runningDistance, Vector3 runningDirection);
        void CanMove();*/
        /*Vector3 RotateToTargetAngel(Vector3 originalDirection);*/
        /*bool CanAttack();
        AttackMovementSystem GetAttackSystem();*/
        public Action<string> GetActionToAnimate();
        void PlayerTouchEnemy();
        List<GameObject> GetEnemiesInCombat();
        void SetEnemiesInCombat(List<GameObject> gameObjects);
        void RotateCharacter(Vector3 position);
        void EndAttackMovement();
        void SetCanReadInputs(bool b);
        bool GetCanReadInputs();
        Action<StunInfo> OnReceiveDamage { get; set; }
    }
}