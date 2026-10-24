using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Volume that asks the flow to move to a state when the player walks through it
    /// (the mid-demo reveal, and any future scripted transition).
    ///
    /// It only REQUESTS: <see cref="GameFlowController.Enter"/> still validates the
    /// transition table, so a trigger placed on the wrong side of the beat is rejected
    /// with a log instead of reshuffling the demo. One trigger owns one target; the
    /// encounter zone keeps its own <see cref="BossEncounterTrigger"/> because it also
    /// spawns the boss.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FlowTrigger : MonoBehaviour
    {
        [Tooltip("State requested when the player enters the volume.")]
        [SerializeField] private FlowState target = FlowState.Beat;

        [Tooltip("Fire only once per scene load. Keep ON for story beats.")]
        [SerializeField] private bool once = true;

        private bool fired;

        /// <summary>Editor convenience: a flow volume is always a trigger.</summary>
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (once && fired) return;
            if (other == null) return;
            if (other.GetComponentInParent<PlayerDamage>() == null) return;

            GameFlowController flow = GameFlowController.Instance;
            if (flow == null) return;

            flow.Enter(target);
            fired = true;
        }
    }
}
