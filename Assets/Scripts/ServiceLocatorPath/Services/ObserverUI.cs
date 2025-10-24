using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using ServiceLocatorPath;
using UnityEngine;
using View;
using View.Characters;
using View.Installers;
using View.UI;

namespace ServiceLocatorPath
{
    public class ObserverUI : IObserverUI
    {
        private readonly IUIController _ui;
        private float totalLife;

        public ObserverUI(IUIController ui)
        {
            _ui = ui;
        }

        public void Observer(ICharacterUi character, ICharacterV2 characterV2)
        {
            character.OnEnterDamageEvent += CharacterOnEnterDamageEvent;
            character.OnAddingEnergy += AddingEnergy;
            characterV2.OnDead += CharacterV2OnDead;
            totalLife = character.GetLife();
            _ui.DefaultValue();
        }

        private void CharacterV2OnDead(ICharacterV2 obj)
        {
            _ui.SetSliderValue(0);
            _ui.ShowGameOver();
        }

        private void AddingEnergy(float energy)
        {
            _ui.SetEnergyValue(energy);
        }

        private void CharacterOnEnterDamageEvent(float damage)
        {
            var percentageDamage = damage / totalLife;
            var valueSlider = _ui.GetSliderValue();
            //var totalLifePercent = Mathf.Clamp(percentageDamage - valueSlider, 0, 1);
            var totalLifePercent = percentageDamage - valueSlider;
            _ui.SetSliderValue(totalLifePercent);
        }
    }
}
