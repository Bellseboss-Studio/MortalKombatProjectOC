using UnityEngine;

namespace _Scripts.Player
{
    public class ReferencesOfPlayer : MonoBehaviour
    {
        [SerializeField] private GameObject referenceOfFatalitySystem;
        [SerializeField] private Animator animator;

        public GameObject ReferenceOfFatalitySystem => referenceOfFatalitySystem;
        public Animator Animator => animator;
    }
}