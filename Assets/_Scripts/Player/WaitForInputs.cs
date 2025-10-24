using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace _Scripts.Player
{
    public class WaitForInputs : StatePatterFatality
    {
        private List<InputRead> _inputsRead = new List<InputRead>();
        private int _countOfInputToRead;

        public WaitForInputs(IFatalitySystem fatalitySystem, int countOfInputToRead) : base(fatalitySystem)
        {
            _countOfInputToRead = countOfInputToRead;
            _inputsRead.Add(new InputRead(INPUTS.UP));
            _inputsRead.Add(new InputRead(INPUTS.DOWN));
            _inputsRead.Add(new InputRead(INPUTS.LEFT));
            _inputsRead.Add(new InputRead(INPUTS.RIGHT));
            _inputsRead.Add(new InputRead(INPUTS.QUICK));
            _inputsRead.Add(new InputRead(INPUTS.POWER));
        }

        internal override IEnumerator Execute()
        {
            while (!_fatalitySystem.FinishedCinematic())
            {
                yield return null;
                if (_fatalitySystem.GetInputs().Count >= _countOfInputToRead) continue;
                if (_fatalitySystem.ReadInput(out var input))
                {
                    foreach (var inputRead in _inputsRead.Where(inputRead => inputRead.input == input))
                    {
                        _fatalitySystem.GetInputs().Add(inputRead);
                        break;
                    }
                }
            }
        }

        protected override void StartState()
        {
            base.StartState();
            _fatalitySystem.ContinueCinematic();
            _fatalitySystem.ShowPanelInputs();
            _fatalitySystem.CanReadInputsToFatality(true);
        }

        protected override void EndState()
        {
            base.EndState();
            _fatalitySystem.HidePanelInputs();
            _fatalitySystem.CanReadInputs(false);
            _fatalitySystem.CanReadInputsToFatality(true);
        }

        public override STATE_FATALITY NextState()
        {
            return STATE_FATALITY.VALIDATE;
        }
    }

    [Serializable]
    public class InputRead
    {
        public INPUTS input;

        public InputRead(INPUTS input)
        {
            this.input = input;
        }
    }

    [Serializable]
    public class GroupInputs
    {
        public List<InputRead> inputs;
    }

    public enum INPUTS
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        QUICK,
        POWER,
        NONE
    }
}