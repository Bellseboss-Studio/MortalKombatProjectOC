using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Player
{
    public class InputPlayerV2 : MonoBehaviour
    {
        [SerializeField] private float timeToReadInput;
        public Action<Vector2, INPUTS> onMoveEvent;
        public Action<bool> onTargetEvent;
        public Action onKickEvent;
        public Action onPunchEvent;
        public Action onJumpEvent;
        public Action onActionEvent;
        public Action onCameraMovementEvet;
        public Action onFatalityEvent;
        public Vector3 CurrentVector => _currentInputVector;
        private Vector3 _currentInputVector;
        private INPUTS _lastInput;
        private float _nextReadTime;
        private bool _isReadingInput;
        private bool _canReadInput = true;
        private bool _canReadInputToFatality;

        public bool CanReadInput => _canReadInput;

        public void OnMove(InputAction.CallbackContext context)
        {
            _currentInputVector = context.ReadValue<Vector2>();
            if (_canReadInput)
            {
                onMoveEvent?.Invoke(_currentInputVector, _lastInput);   
            }
            if (Time.time >= _nextReadTime && _canReadInputToFatality)
            {
                _lastInput = GetDirectionFromVector(_currentInputVector);
                _nextReadTime = Time.time + timeToReadInput;
                _isReadingInput = true;
                /*Debug.Log($"Reading input {_lastInput} at {Time.time} next read at {_nextReadTime}");*/
            }
        }

        public void OnCameraMovement(InputAction.CallbackContext context)
        {
            if (!_canReadInput) return;
            onCameraMovementEvet?.Invoke();
        }

        public void OnKick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_canReadInputToFatality)
                {
                    _lastInput = INPUTS.QUICK;
                    _isReadingInput = true;   
                }
                if (_canReadInput)
                {
                    onKickEvent?.Invoke();
                }
            }
        }
        
        public void OnPunch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_canReadInputToFatality)
                {
                    _lastInput = INPUTS.POWER;
                    _isReadingInput = true;
                }
                if (_canReadInput)
                {
                    onPunchEvent?.Invoke();
                }
            }
        }
        
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_canReadInputToFatality)
                {
                }
                if (_canReadInput)
                {
                    onJumpEvent?.Invoke();
                }
            }
        }
        
        public void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_canReadInputToFatality)
                {
                }
                if (_canReadInput)
                {
                    onActionEvent?.Invoke();
                }
            }
        }

        public void OnFatality(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_canReadInputToFatality)
                {
                }
                if (_canReadInput)
                {
                    onFatalityEvent?.Invoke();
                }
            }
        }

        private INPUTS GetDirectionFromVector(Vector2 vector)
        {
            if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
            {
                if(vector.x > 0 && vector.x > 0.01f)
                {
                    /*Debug.Log("Right");*/
                    return INPUTS.RIGHT;
                }
                if(vector.x < 0 && vector.x < -0.01f)
                {
                    /*Debug.Log("Left");*/
                    return INPUTS.LEFT;
                }
            }
            else
            {
                if (vector.y > 0 && vector.y > 0.01f)
                {
                    /*Debug.Log("Up");*/
                    return INPUTS.UP;
                }
                if(vector.y < 0 && vector.y < -0.01f)
                {
                    /*Debug.Log("Down");*/
                    return INPUTS.DOWN;
                }
            }
            return INPUTS.NONE;
        }
        public bool ReadInput(out INPUTS input)
        {
            var aux = _isReadingInput;
            input = INPUTS.NONE;
            if (aux)
            {
                input = _lastInput;
            }
            _isReadingInput = false;
            if (input == INPUTS.NONE)
            {
                aux = false;
            }
            return aux;
        }

        public void StartToReadInputs(bool b)
        {
            Debug.Log(b);
            _canReadInput = b;
            if (b)
            {
                _lastInput = INPUTS.NONE;
                _isReadingInput = false;
                _currentInputVector = Vector3.zero;
            }
        }

        public void StartToReadInputsToFatality(bool canRead)
        {
            _canReadInputToFatality = canRead;
        }
    }
}
