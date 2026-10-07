using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 slice: camera-relative walk + Mario-style jump on a Rigidbody.
    /// All numbers come from <see cref="PlayerParams"/> — NO hardcoded constants.
    /// NO wall grab, NO combat.
    /// Jump: variable height (cut on release) + asymmetric gravity + coyote.
    /// Input in Update, physics in FixedUpdate, own ground-check via RaycastAll.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Single source of gameplay numbers. Required.")]
        [SerializeField] private PlayerParams paramsAsset;

        [Tooltip("Stick snap distance below feet that still counts as grounded.")]
        [SerializeField] private float groundSnap = 0.15f;

        [Tooltip("Downward stick force while grounded (keeps contact on Floor).")]
        [SerializeField] private float stickForce = -1f;

        private Rigidbody rb;
        private CapsuleCollider body;
        private ICharacterVisual visual;
        private PlayerInput input;

        private Vector2 moveInput;
        private Vector3 smoothVel;
        private float verticalVel;
        private float bufferTimer;
        private float coyoteTimer;
        private Transform cameraTransform;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            body = GetComponent<CapsuleCollider>();
            visual = GetComponent<ProxyVisualAdapter>();
            var adapter = visual as ProxyVisualAdapter;
            if (adapter != null) adapter.Bind(paramsAsset);

            // M0 physical contract (also set in scene): interpolated, no torque.
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true;
            rb.useGravity = false;

            input = new PlayerInput();
            RefreshCamera();
        }

        private void EnsureInput()
        {
            // OnEnable can run without Awake after a domain reload
            // (exiting play mode): non-serialized fields are null there.
            if (input == null) input = new PlayerInput();
        }

        private void OnEnable()
        {
            EnsureInput();
            input.Player.Enable();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();
        }

        private void OnDestroy()
        {
            if (input != null) input.Dispose();
        }

        private void RefreshCamera()
        {
            var cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : null;
        }

        private void Update()
        {
            if (paramsAsset == null || input == null) return;
            moveInput = input.Player.Move.ReadValue<Vector2>();
            if (input.Player.Jump.WasPressedThisFrame())
                bufferTimer = paramsAsset.jumpBuffer;
            // Mario variable height: release while ascending cuts upward velocity.
            if (input.Player.Jump.WasReleasedThisFrame() && verticalVel > 0f)
                verticalVel *= paramsAsset.jumpCutMultiplier;
        }

        private void FixedUpdate()
        {
            if (paramsAsset == null) return;
            if (cameraTransform == null) RefreshCamera();

            float dt = Time.fixedDeltaTime;
            bufferTimer = Mathf.Max(0f, bufferTimer - dt);

            bool grounded = CheckGrounded();

            // Coyote: refresh grace window while grounded, decay while airborne.
            if (grounded)
                coyoteTimer = paramsAsset.coyoteTime;
            else
                coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);

            // Jump: buffered press + grounded OR inside coyote window.
            if (bufferTimer > 0f && (grounded || coyoteTimer > 0f))
            {
                verticalVel = paramsAsset.jumpVelocity;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
            }

            if (grounded)
            {
                if (verticalVel < 0f) verticalVel = stickForce;
            }
            else
            {
                // Mario asymmetric gravity: float up, snap down.
                float g = paramsAsset.gravity;
                if (verticalVel <= 0f) g *= paramsAsset.fallGravityMultiplier;
                verticalVel += g * dt;
                verticalVel = Mathf.Max(verticalVel, paramsAsset.maxFallSpeed);
            }

            // Camera-relative wish direction on XZ.
            Vector3 wishDir = CameraRelative(moveInput);
            Vector3 target = wishDir * paramsAsset.walkSpeed;

            float smoothTime = wishDir.sqrMagnitude > 0.0001f
                ? paramsAsset.accelTime
                : paramsAsset.decelTime;
            if (!grounded) smoothTime /= Mathf.Max(0.01f, paramsAsset.airControlFactor);

            Vector3 horizontal = Vector3.SmoothDamp(
                new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z),
                target, ref smoothVel, Mathf.Max(0.0001f, smoothTime), Mathf.Infinity, dt);

            rb.linearVelocity = new Vector3(horizontal.x, verticalVel, horizontal.z);

            if (visual != null)
            {
                if (wishDir.sqrMagnitude > 0.0001f) visual.SetFacing(wishDir);
                visual.SetGrounded(grounded);
            }
        }

        private Vector3 CameraRelative(Vector2 stick)
        {
            if (stick.sqrMagnitude < 0.000001f) return Vector3.zero;
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 dir = forward * stick.y + right * stick.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private bool CheckGrounded()
        {
            Vector3 origin = body.bounds.center;
            float maxDist = body.height * 0.5f + groundSnap;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider != body)
                    return true;
            }
            return false;
        }
    }
}
