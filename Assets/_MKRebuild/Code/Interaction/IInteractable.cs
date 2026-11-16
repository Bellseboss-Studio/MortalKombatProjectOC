using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Anything the player can walk up to and operate: a lever, a valve, a crank, a chest.
    ///
    /// The interactable OWNS the framing: <see cref="Anchor"/> decides where the player
    /// stands and which way she faces while the action plays. That keeps the pose aligned
    /// with the prop without the player prefab knowing what a lever is.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Where the player is planted, and which way she faces (anchor.forward).</summary>
        Transform Anchor { get; }

        /// <summary>Verb shown in the prompt, e.g. "Accionar palanca".</summary>
        string Prompt { get; }

        /// <summary>False while the object cannot be operated (already used, locked).</summary>
        bool CanInteract { get; }

        /// <summary>How long the player stays planted, in seconds.</summary>
        float Duration { get; }

        /// <summary>Runs once, at the moment the player commits to the action.</summary>
        void Interact();
    }
}
