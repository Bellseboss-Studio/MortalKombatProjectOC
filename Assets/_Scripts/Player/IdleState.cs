using System.Collections;

namespace _Scripts.Player
{
    public class IdleState : StatePatterFatality
    {
        public IdleState(IFatalitySystem fatalitySystem) : base(fatalitySystem)
        {
        }

        protected override void StartState()
        {
            base.StartState();
            _fatalitySystem.RestartAllElements();
        }

        internal override IEnumerator Execute()
        {
            while (!_fatalitySystem.IsStartFatality())
            {
                yield return null;
            }
        }

        public override STATE_FATALITY NextState()
        {
            return STATE_FATALITY.CINEMATIC;
        }
    }
}