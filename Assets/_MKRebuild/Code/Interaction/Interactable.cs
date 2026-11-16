using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Base for every operable prop. Keeps a static registry so <see cref="PlayerInteractor"/>
    /// never needs a layer, a tag or a physics query: the interactor asks the registry for the
    /// best candidate in range. Concrete props only implement <see cref="OnInteract"/>.
    /// </summary>
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        private static readonly List<Interactable> Active = new List<Interactable>();

        [Tooltip("Where the player stands and looks while operating. Null = this transform.")]
        [SerializeField] private Transform anchor;

        [Tooltip("Verb shown in the prompt.")]
        [SerializeField] private string prompt = "Accionar";

        [Tooltip("Seconds the player stays planted. Match it to the gesture length.")]
        [SerializeField] private float duration = 0.7f;

        /// <summary>Every interactable currently enabled in the scene.</summary>
        public static IReadOnlyList<Interactable> All => Active;

        public Transform Anchor => anchor != null ? anchor : transform;

        public string Prompt => prompt;

        public float Duration => duration;

        public virtual bool CanInteract => true;

        protected virtual void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        public void Interact()
        {
            if (!CanInteract) return;
            OnInteract();
        }

        /// <summary>Runs once, when the player commits. Do the state change here.</summary>
        protected abstract void OnInteract();

        private void OnDrawGizmosSelected()
        {
            Transform target = Anchor;
            Gizmos.color = new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawWireSphere(target.position, 0.25f);
            Gizmos.DrawRay(target.position, target.forward * 1.2f);
        }
    }
}
