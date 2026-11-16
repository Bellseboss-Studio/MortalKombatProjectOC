using UnityEngine;
using UnityEngine.InputSystem;

namespace MK.Rebuild
{
    /// <summary>
    /// Finds the nearest <see cref="Interactable"/> in front of the player, shows a prompt and,
    /// on confirm, plants her on the prop's anchor and holds control until the gesture ends.
    ///
    /// Ownership is explicit: it acquires the body token at <see cref="BodyPriority.Cinematic"/>
    /// (nothing can steal the body mid-action) plus a <see cref="ControlLock"/> cinematic handle,
    /// and releases BOTH on every exit path — natural end, disable, or a failed acquire. A
    /// dropped release would leave the player permanently without input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("How far the prop can be, in metres.")]
        [SerializeField] private float range = 2.4f;

        [Tooltip("Minimum dot(forward, toProp) so the prompt only shows what the player faces.")]
        [SerializeField] private float minFacingDot = 0.2f;

        [Tooltip("Confirm key.")]
        [SerializeField] private Key interactKey = Key.E;

        private PlayerMotor motor;
        private AnimatorVisual visual;
        private Interactable candidate;
        private Interactable busy;
        private float timer;
        private BodyOwnership.Token bodyToken;
        private ControlLock.Handle lockHandle;

        /// <summary>Prop the player could operate right now (null when none is in reach).</summary>
        public Interactable Candidate => candidate;

        /// <summary>True while an interaction is playing.</summary>
        public bool Busy => busy != null;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            visual = GetComponentInChildren<AnimatorVisual>(true);
        }

        private void Update()
        {
            if (busy != null)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f) Finish();
                return;
            }

            candidate = FindBest();
            if (candidate == null) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[interactKey].wasPressedThisFrame) Begin(candidate);
        }

        private void OnDisable()
        {
            Finish();
        }

        private Interactable FindBest()
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            Vector3 origin = transform.position;

            // El cuerpo gira en el visual, no en la raíz: transform.forward no sirve acá.
            Vector3 facing = visual != null ? visual.Facing : transform.forward;

            var all = Interactable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Interactable prop = all[i];
                if (prop == null || !prop.CanInteract) continue;

                Vector3 toProp = prop.Anchor.position - origin;
                toProp.y = 0f;
                float distance = toProp.magnitude;
                if (distance > range) continue;
                if (distance > 0.01f && Vector3.Dot(facing, toProp / distance) < minFacingDot) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = prop;
                }
            }

            return best;
        }

        private void Begin(Interactable prop)
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (motor == null || motor.Body == null) return;

            bodyToken = motor.Body.Acquire(BodyPriority.Cinematic);
            if (bodyToken == null) return;

            lockHandle = ControlLock.Acquire(ControlReason.Cinematic);

            Transform anchor = prop.Anchor;
            motor.Teleport(anchor.position, anchor.forward);
            if (visual != null) visual.PlayInteract(0.12f);

            busy = prop;
            candidate = null;
            timer = Mathf.Max(0.05f, prop.Duration);

            prop.Interact();
        }

        private void Finish()
        {
            if (lockHandle != null)
            {
                ControlLock.Release(lockHandle);
                lockHandle = null;
            }

            if (bodyToken != null && motor != null && motor.Body != null)
            {
                motor.Body.Release(bodyToken);
                bodyToken = null;
            }

            busy = null;
            timer = 0f;
        }

        private void OnGUI()
        {
            if (busy != null || candidate == null) return;

            string label = "[" + interactKey + "] " + candidate.Prompt;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
            };

            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 96f, 360f, 32f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, label, style);
        }
    }
}
