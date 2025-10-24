using System.Collections;

namespace _Scripts.Player
{
    public class CinematicState : StatePatterFatality
    {
        public CinematicState(IFatalitySystem fatalitySystem) : base(fatalitySystem)
        {
        }

        internal override IEnumerator Execute()
        {
            while (!_fatalitySystem.FinishedCinematic())
            {
                yield return null;
            }
        }

        public override STATE_FATALITY NextState()
        {
            return STATE_FATALITY.INPUTS;
        }

        protected override void StartState()
        {
            base.StartState();
            _fatalitySystem.StartCinematic();
        }

        protected override void EndState()
        {
            base.EndState();
            _fatalitySystem.HidePanelTitle();
        }
    }
}