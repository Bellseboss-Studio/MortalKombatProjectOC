using System;
using UnityEngine;

namespace Bellseboss.Angel.CombatSystem
{
    public class MoveAttackingSystem
    {
        private readonly GameObject _gameObjectToPlayer;
        private readonly Transform _transform;

        public MoveAttackingSystem(GameObject gameObjectToPlayer, Transform transform)
        {
            _gameObjectToPlayer = gameObjectToPlayer;
            _transform = transform;
        }
        
        public void MovePlayer(GameObject gameObjectToPlayer, float heightMultiplier, TeaHandler loop, float force,
            float distance, bool isNegative = false)
        {
            if (isNegative)
            {
                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position - _transform.forward * (distance * heightMultiplier),
                    force * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }
            else
            {
                var position = gameObjectToPlayer.transform.position;
                position = Vector3.Lerp(position,
                    position + _transform.forward * (distance * heightMultiplier),
                    force * loop.deltaTime);
                gameObjectToPlayer.transform.position = position;
            }
        }
    }
}