using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public class StatePatter : MonoBehaviour
    {
        private IFatalitySystem _fatalitySystem;
        private Dictionary<STATE_FATALITY, StatePatterFatality> _statePatterFatality;
        private StatePatterFatality _currentState;
        private bool _isRunning;

        public void Configure(IFatalitySystem fatalitySystem, STATE_FATALITY firstState,
            Dictionary<STATE_FATALITY, StatePatterFatality> states)
        {
            _fatalitySystem = fatalitySystem;
            _statePatterFatality = states;
            _currentState = _statePatterFatality[firstState];
        }

        public void StartStates()
        {
            _isRunning = true;
            StartCoroutine(StatePatterCurrutine());
        }

        private IEnumerator StatePatterCurrutine()
        {
            while (_isRunning)
            {
                yield return _currentState.Starting();
                yield return new WaitForSeconds(0.1f);
                yield return _currentState.Execute();
                yield return new WaitForSeconds(0.1f);
                yield return _currentState.End();
                _currentState = _statePatterFatality[_currentState.NextState()];
            }
        }
    }

    public enum STATE_FATALITY
    {
        IDLE,
        CINEMATIC,
        INPUTS,
        VALIDATE,
        FATALITY,
        NO_FATALITY
    }
}