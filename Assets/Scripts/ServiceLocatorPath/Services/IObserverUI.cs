using _Scripts.Player;
using Bellseboss.Pery.Scripts.Input;
using View.Characters;

namespace View.Installers
{
    internal interface IObserverUI
    {
        void Observer(ICharacterUi character, ICharacterV2 characterV2);
    }
}