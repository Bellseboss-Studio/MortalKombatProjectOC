using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Player
{
    public class UiFatalityInputs : MonoBehaviour
    {
        [SerializeField] private List<InputPressed> _inputs;
        private FatalitySystem _fatalitySystem;
        private int _currentInput;

        public void DefaultValue()
        {
            foreach (var input in _inputs)
            {
                input.ConvertEnumToString(INPUTS.NONE);
            }

            _currentInput = 0;
        }

        private void OnInputPressed(INPUTS obj)
        {
            if (_currentInput < _inputs.Count)
            {
                _inputs[_currentInput].ConvertEnumToString(obj);
                _currentInput++;
            }
        }

        public void Configure(FatalitySystem fatalitySystem)
        {
            _fatalitySystem = fatalitySystem;
            _fatalitySystem.OnInputPressed += OnInputPressed;
            DefaultValue();
        }
    }
}