using System;
using TMPro;
using UnityEngine;

namespace _Scripts.Player
{
    public class InputPressed : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _inputText;

        public void ConvertEnumToString(INPUTS input)
        {
            //convert enum to string
            switch (input)
            {
                case INPUTS.UP:
                    _inputText.text = "UP";
                    break;
                case INPUTS.DOWN:
                    _inputText.text = "DOWN";
                    break;
                case INPUTS.LEFT:
                    _inputText.text = "LEFT";
                    break;
                case INPUTS.RIGHT:
                    _inputText.text = "RIGHT";
                    break;
                case INPUTS.POWER:
                    _inputText.text = "POWER";
                    break;
                case INPUTS.QUICK:
                    _inputText.text = "QUICK";
                    break;
                case INPUTS.NONE:
                    // Debug.Log("NONE");
                    _inputText.text = "";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }
    }
}