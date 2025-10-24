using System.Collections;

namespace _Scripts.Player
{
    public class NoFatalityState : StatePatterFatality
    {
        public NoFatalityState(IFatalitySystem fatalitySystem) : base(fatalitySystem)
        {
        }

        protected override void StartState()
        {
            base.StartState();
            _fatalitySystem.StartCinematic();
            _fatalitySystem.ShowPanelTitle("FATALITY FAILED!");
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