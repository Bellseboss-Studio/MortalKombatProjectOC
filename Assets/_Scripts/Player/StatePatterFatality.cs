using System.Collections;

namespace _Scripts.Player
{
    public abstract class StatePatterFatality
    {
        protected IFatalitySystem _fatalitySystem;

        protected StatePatterFatality(IFatalitySystem fatalitySystem)
        {
            _fatalitySystem = fatalitySystem;
        }

        internal IEnumerator Starting()
        {
            yield return null;
            StartState();
        }

        internal abstract IEnumerator Execute();

        internal IEnumerator End()
        {
            yield return null;
            EndState();
        }

        protected virtual void EndState()
        {
        }

        protected virtual void StartState()
        {
            /*Debug.Log($"StartState {GetType().Name}");*/
        }

        public abstract STATE_FATALITY NextState();
    }
}