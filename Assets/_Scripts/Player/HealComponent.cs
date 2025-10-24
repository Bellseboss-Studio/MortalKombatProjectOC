using UnityEngine;

namespace _Scripts.Player
{
    public class HealComponent : MonoBehaviour
    {
        private ICharacterV2 _character;

        public void Configure(ICharacterV2 character)
        {
            _character = character;
            character.OnDead += OnDead;
        }

        private void OnDead(ICharacterV2 obj)
        {
            Debug.Log("Dead!");
            _character.StartDeadAction();
        }
    }
}