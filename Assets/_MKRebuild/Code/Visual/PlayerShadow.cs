using UnityEngine;
using UnityEngine.Rendering;

namespace MK.Rebuild
{
    /// <summary>
    /// m3-build-feedback contact/blob shadow (2026-10-19): a runtime quad with a
    /// radial alpha mask and a transparent URP/Unlit material. No SpriteRenderer,
    /// no custom shader, no physics: the quad is collider-free and this component
    /// only raycasts downward. Build-safe because <c>Universal Render Pipeline/Unlit</c>
    /// is in Always Included Shaders (m3 VFX fix).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlayerShadow : MonoBehaviour
    {
        private const string ShaderName = "Universal Render Pipeline/Unlit";
        private const int MaskSize = 64;

        [Tooltip("Maximum downward raycast distance (metres).")]
        [SerializeField] private float maxDistance = 50f;

        [Tooltip("Layers treated as ground. Default is everything.")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("How far the shadow sits above the ground along the surface normal (metres).")]
        [SerializeField] private float groundOffset = 0.02f;

        [Tooltip("Shadow diameter when the player is grounded (metres).")]
        [SerializeField] private float baseDiameter = 1.2f;

        [Tooltip("Shadow diameter at maxHeight (metres).")]
        [SerializeField] private float minDiameter = 0.35f;

        [Tooltip("Height at which the shadow reaches minDiameter and minAlpha (metres).")]
        [SerializeField] private float maxHeight = 8f;

        [Tooltip("Shadow opacity when the player is grounded (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float baseAlpha = 0.55f;

        [Tooltip("Shadow opacity at maxHeight (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.12f;

        [Tooltip("Raycast origin height above the player root (metres).")]
        [SerializeField] private float rayStartOffset = 0.3f;

        private Rigidbody _rb;
        private Collider _body;
        private GameObject _shadow;
        private MeshRenderer _renderer;
        private Material _material;
        private Texture2D _mask;
        private bool _disabled;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _body = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            BuildShadow();
        }

        private void OnDisable()
        {
            CleanupShadow();
        }

        private void LateUpdate()
        {
            if (_disabled || _shadow == null) return;

            // EDITOR PREVIEW: the prefab has no ground, so show a rest-pose disc at
            // the player's feet. This is what lets the visual be aligned in the prefab
            // stage. The generated objects are DontSave, so they never dirty the prefab.
            if (!Application.isPlaying)
            {
                ShowRestPose();
                return;
            }

            Vector3 origin = transform.position + Vector3.up * rayStartOffset;
            RaycastHit[] hits = Physics.RaycastAll(
                origin, Vector3.down, maxDistance, groundMask, QueryTriggerInteraction.Ignore);

            bool found = false;
            RaycastHit closest = default;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;

                // Ignore the player: any child (including the shadow quad's own
                // collider, if it still exists this frame) and the player's body.
                if (col.transform.IsChildOf(transform)) continue;
                if (_rb != null && col.attachedRigidbody == _rb) continue;

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closest = hits[i];
                    found = true;
                }
            }

            if (!found)
            {
                // No ground within range (e.g. over a pit): hide entirely.
                if (_shadow.activeSelf) _shadow.SetActive(false);
                return;
            }

            float playerBottom = _body != null ? _body.bounds.min.y : transform.position.y;
            float height = Mathf.Max(0f, playerBottom - closest.point.y);
            float t = Mathf.Clamp01(height / Mathf.Max(0.01f, maxHeight));

            float diameter = Mathf.Lerp(baseDiameter, minDiameter, t);
            float alpha = Mathf.Lerp(baseAlpha, minAlpha, t);

            Transform shadowTransform = _shadow.transform;
            shadowTransform.position = closest.point + closest.normal * groundOffset;
            // Unity's Quad visible face points -Z, so map -Z to the ground normal.
            shadowTransform.rotation = Quaternion.FromToRotation(Vector3.back, closest.normal);
            shadowTransform.localScale = Vector3.one * diameter;

            if (_material != null)
            {
                _material.SetColor("_BaseColor", new Color(0f, 0f, 0f, alpha));
            }

            if (!_shadow.activeSelf) _shadow.SetActive(true);
        }

        private void OnDestroy()
        {
            CleanupShadow();
        }

        private void CleanupShadow()
        {
            if (_shadow != null) DestroyGenerated(_shadow);
            _shadow = null;
            _renderer = null;

            DestroyGenerated(_material);
            DestroyGenerated(_mask);
            _material = null;
            _mask = null;
        }

        /// <summary>
        /// Prefab-stage preview: a flat disc at the player's feet, using the same
        /// generated material as runtime. No raycast: there is no ground in the
        /// prefab preview scene.
        /// </summary>
        private void ShowRestPose()
        {
            if (_shadow == null || _material == null) return;

            float feetY = _body != null ? _body.bounds.min.y : transform.position.y;
            Transform shadowTransform = _shadow.transform;
            shadowTransform.position =
                new Vector3(transform.position.x, feetY + groundOffset, transform.position.z);
            shadowTransform.rotation = Quaternion.FromToRotation(Vector3.back, Vector3.up);
            shadowTransform.localScale = Vector3.one * baseDiameter;
            _material.SetColor("_BaseColor", new Color(0f, 0f, 0f, baseAlpha));
            if (!_shadow.activeSelf) _shadow.SetActive(true);
        }

        private void BuildShadow()
        {
            if (_shadow != null) return;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_body == null) _body = GetComponent<Collider>();

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                // Never build a null-backed Material (magenta in some builds).
                Debug.LogWarning(
                    "[PlayerShadow] Shader '" + ShaderName + "' not found; contact shadow disabled. " +
                    "Add it to Project Settings > Graphics > Always Included Shaders.", this);
                _disabled = true;
                return;
            }

            _mask = CreateRadialMask();
            _mask.hideFlags = HideFlags.DontSave;

            _material = new Material(shader);
            _material.name = "LeenaShadowMaterial";
            _material.hideFlags = HideFlags.DontSave;
            _material.SetTexture("_BaseMap", _mask);
            _material.SetColor("_BaseColor", new Color(0f, 0f, 0f, baseAlpha));

            // Transparent setup identical to GreyBoxFx. The quad is rotated by its
            // visible face (-Z), so default back-face culling hides it from below.
            _material.SetFloat("_Surface", 1f); // 0 = opaque, 1 = transparent
            _material.SetFloat("_Blend", 0f);    // alpha blend
            _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _material.SetFloat("_ZWrite", 0f);
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.renderQueue = (int)RenderQueue.Transparent;

            _shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _shadow.name = "LeenaShadow";
            _shadow.hideFlags = HideFlags.DontSave; // editor preview, never saved in the prefab
            _shadow.transform.SetParent(transform, false);
            _shadow.transform.localPosition = Vector3.zero;
            _shadow.transform.localRotation = Quaternion.identity;
            _shadow.transform.localScale = Vector3.one; // player root scale is 1

            // The quad must never collide; it is a visual-only decal.
            Collider autoCollider = _shadow.GetComponent<Collider>();
            if (autoCollider != null) DestroyGenerated(autoCollider);

            _renderer = _shadow.GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                DestroyGenerated(_material);
                DestroyGenerated(_mask);
                _material = null;
                _mask = null;
                DestroyGenerated(_shadow);
                _shadow = null;
                _disabled = true;
                return;
            }

            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.sharedMaterial = _material;

            // Runtime hides it until the first ground raycast; edit mode shows the
            // rest-pose disc immediately so the prefab is usable for alignment.
            if (Application.isPlaying) _shadow.SetActive(false);
            else ShowRestPose();
        }

        private static Texture2D CreateRadialMask()
        {
            var texture = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false);
            texture.name = "LeenaShadowMask";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[MaskSize * MaskSize];
            float half = (MaskSize - 1) * 0.5f;

            for (int y = 0; y < MaskSize; y++)
            {
                for (int x = 0; x < MaskSize; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float distance = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    // 1 at the centre, smoothly 0 at the edge.
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, distance);
                    pixels[y * MaskSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static void DestroyGenerated(Object generated)
        {
            if (generated == null) return;
            if (Application.isPlaying) Destroy(generated);
            else DestroyImmediate(generated);
        }
    }
}
