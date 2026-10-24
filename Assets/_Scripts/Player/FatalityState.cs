using System.Collections;

namespace _Scripts.Player
{
    public class FatalityState : StatePatterFatality
    {
        public FatalityState(IFatalitySystem fatalitySystem) : base(fatalitySystem)
        {
        }

        protected override void StartState()
        {
            base.StartState();
            _fatalitySystem.StartCinematic();
            _fatalitySystem.ShowPanelTitle("FATALITY");
            _fatalitySystem.StartAudioFatality();
            _fatalitySystem.FatalityPlayer();
            _fatalitySystem.FatalityEnemy();
            _fatalitySystem.FatalityComposite();
        }

        internal override IEnumerator Execute()
        {
            while (!_fatalitySystem.FinishedCinematic())
            {
                yield return null;
            }
        }

        protected override void EndState()
        {
            base.EndState();
            _fatalitySystem.EndOfFatality();
            _fatalitySystem.HidePanelTitle();
        }

        public override STATE_FATALITY NextState()
        {
            return STATE_FATALITY.IDLE;
        }
    }
}