using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace _Scripts.Player
{
    public class ValidateInputs : StatePatterFatality
    {
        private List<InputRead> _inputsRead;
        private List<List<InputRead>> _inputsToValidate;
        private bool _canUseFatality;

        public ValidateInputs(IFatalitySystem fatalitySystem, List<GroupInputs> inputsToValidate) : base(fatalitySystem)
        {
            _inputsToValidate = inputsToValidate.Select(g => g.inputs).ToList();
        }

        protected override void StartState()
        {
            base.StartState();
            _inputsRead = _fatalitySystem.GetInputs();
            //validate if the inputs readed are the same as the inputs to validate
            _canUseFatality = _inputsToValidate
                .Where(inputToValidate => inputToValidate.Count == _inputsRead.Count)
                .Any(inputToValidate => inputToValidate.Select((t, i) => t.input == _inputsRead[i].input
                ).All(isMatch => isMatch));
        }

        internal override IEnumerator Execute()
        {
            yield return null;
        }

        public override STATE_FATALITY NextState()
        {
            return _canUseFatality ? STATE_FATALITY.FATALITY : STATE_FATALITY.NO_FATALITY;
        }
    }
}