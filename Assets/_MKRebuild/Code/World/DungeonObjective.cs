using System;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Counts the dungeon's levers and announces completion. The objective is the only thing
    /// that knows how many are needed: levers just report, and the door only listens.
    ///
    /// State lives in the scene component on purpose — the greybox has no save system, and
    /// resetting the scene is the intended way to retry the objective.
    /// </summary>
    public sealed class DungeonObjective : MonoBehaviour
    {
        [Tooltip("Levers needed to complete the objective.")]
        [SerializeField] private int required = 3;

        private int activated;

        /// <summary>How many levers have been thrown.</summary>
        public int Activated => activated;

        /// <summary>How many the objective needs.</summary>
        public int Required => required;

        /// <summary>True once the requirement is met.</summary>
        public bool IsComplete => activated >= required;

        /// <summary>Fired with (activated, required) on every lever.</summary>
        public event Action<int, int> Progress;

        /// <summary>Fired exactly once, when the last lever is thrown.</summary>
        public event Action Completed;

        /// <summary>Called by <see cref="LeverInteractable"/> when it is thrown.</summary>
        public void NotifyActivated(LeverInteractable lever)
        {
            if (IsComplete) return;

            activated++;
            Progress?.Invoke(activated, required);

            if (IsComplete)
            {
                Completed?.Invoke();
                Debug.Log("[Objective] Completado: " + activated + "/" + required + ". La gran puerta se abre.");
            }
            else
            {
                Debug.Log("[Objective] Progreso: " + activated + "/" + required + ".");
            }
        }

        /// <summary>Debug seam: throws every lever count at once.</summary>
        public void ForceComplete()
        {
            while (!IsComplete) NotifyActivated(null);
        }
    }
}
