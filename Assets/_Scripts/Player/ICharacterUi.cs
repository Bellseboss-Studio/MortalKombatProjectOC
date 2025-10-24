using System;

namespace _Scripts.Player
{
    public interface ICharacterUi
    {
        event Action<float> OnEnterDamageEvent;
        event Action<float> OnAddingEnergy;
        float GetLife();
    }
}