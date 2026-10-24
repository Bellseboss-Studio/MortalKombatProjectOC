using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace V2
{
    public class StepsConfig : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int countOfSteps;
        private int currentStep;
        private byte[] imageBytes;
        private List<string> _upScales;
        private string _professionSaved;

        public void Configure(int countOfSteps)
        {
            this.countOfSteps = countOfSteps;
            currentStep = 0;
            SetStep(currentStep);
            // Debug.Log($"StepsConfig Configure {countOfSteps}");
        }

        public void SetStep(int step)
        {
            // Debug.Log($"StepsConfig SetStep {step} / {countOfSteps}");
            if (step < 0 || step > countOfSteps)
            {
                // Debug.LogError("Step out of range");
                return;
            }

            scrollRect.horizontalNormalizedPosition = (float)step / countOfSteps;
        }

        public void NextStep()
        {
            // Debug.Log("StepsConfig NextStep");
            currentStep++;
            SetStep(currentStep);
        }

        public void PreviousStep()
        {
            // Debug.Log("StepsConfig PreviousStep");
            currentStep--;
            SetStep(currentStep);
        }
    }
}