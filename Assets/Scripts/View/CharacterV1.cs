using System;
using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using UnityEngine;

namespace View
{
    public abstract class CharacterV1 : MonoBehaviour, ICharacterUi
    {
        public event Action<float> OnEnterDamageEvent;
        public event Action<float> OnAddingEnergy;
        public float GetLife()
        {
            return 0;
        }
    }
}