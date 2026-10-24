using UnityEngine;

namespace Bellseboss.Pery.Scripts.Input
{
    internal class InputMovementCustomV2 : InputCustomV2
    {
        private readonly float _force;

        public InputMovementCustomV2(float force)
        {
            _force = force;
        }

        public Vector3 CalculateMovement(Vector2 input, float speed, GameObject camera, GameObject player, bool isJumping = false)
        {
            var direction = player.transform.position - camera.transform.position;
            if(!isJumping) direction.y = 0; // Ignora la componente Y para mantener el movimiento en el plano XZ
            //direction.y = 0; // Ignora la componente Y para mantener el movimiento en el plano XZ
            direction.Normalize();
            var right = new Vector3(direction.z, 0, -direction.x);
            var result = input.x * right + input.y * direction;
            result.Normalize();
            return result * speed;
        }
    }
}