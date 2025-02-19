using System;
using Unity.Cinemachine;
using InputSystemCustom;
using UnityEngine;
using View;
using Object = UnityEngine.Object;

namespace Builder
{
    public class CharacterBuilder
    {
        private Character _character;
        private TypeOfInputs _input;
        private GameObject mainCamera;
        private Vector3 position;
        private CinemachineTargetGroup _targetGroup;
        private CinemachineFreeLook _secondCamera;

        public CharacterBuilder()
        {
            position = Vector3.zero;
        }

        public CharacterBuilder WithCharacter(Character character)
        {
            _character = character;
            return this;
        }

        public CharacterBuilder WithMainCamera(GameObject cameraExternal)
        {
            mainCamera = cameraExternal;
            return this;
        }

        public CharacterBuilder WithInput(TypeOfInputs inputCustom)
        {
            _input = inputCustom;
            return this;
        }

        public CharacterBuilder InPosition(Vector3 position)
        {
            this.position = position;
            return this;
        }

        public CharacterBuilder WithTargetGroup(CinemachineTargetGroup targetGroup)
        {
            _targetGroup = targetGroup;
            return this;
        }

        public CharacterBuilder WithSecondCamera(CinemachineFreeLook secondCamera)
        {
            _secondCamera = secondCamera;
            return this;
        }
        
        public Character Build()
        {
            CheckPreConditions();
            var characterInstantiate = Object.Instantiate(_character);
            characterInstantiate.transform.position = position;
            InputCustom valueOfInput = null;
            switch (_input)
            {
                case TypeOfInputs.PlayerControl:
                    valueOfInput = new MovementController(characterInstantiate, mainCamera);
                    break;
                case TypeOfInputs.EnemyIa:
                    valueOfInput = new MovementEnemies(characterInstantiate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            characterInstantiate.Configure(valueOfInput, mainCamera);
            valueOfInput.ConfigureInputWithCharacter();
            return characterInstantiate;
        }

        private void CheckPreConditions()
        {
            
        }
    }
}