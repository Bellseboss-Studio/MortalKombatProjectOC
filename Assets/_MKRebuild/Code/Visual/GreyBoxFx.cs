using UnityEngine;
using UnityEngine.Rendering;

namespace MK.Rebuild
{
    /// <summary>
    /// m1-abilities grey-box VFX helper (2026-10-15): NO custom shader, NO prefab.
    /// Builds throwaway runtime primitives + materials with the project's URP
    /// built-in shaders (<c>Universal Render Pipeline/Unlit</c> and
    /// <c>Universal Render Pipeline/Particles/Unlit</c>) for the H1 projectile
    /// impact burst and the H2 area-radius ring. Materials are cached statics so a
    /// runtime burst does not leak one material per hit.
    /// </summary>
    public static class GreyBoxFx
    {
        private static Material s_projectile;
        private static Material s_ring;
        private static Material s_burst;
        private static bool s_projectileFailed;
        private static bool s_ringFailed;
        private static bool s_burstFailed;
        private static bool s_shaderMissingLogged;

        /// <summary>
        /// Opaque cyan material for the H1 projectile primitive. Returns null when
        /// the shader is unavailable in this build (never a null-backed Material).
        /// </summary>
        public static Material ProjectileMaterial()
        {
            if (s_projectile != null) return s_projectile;
            if (s_projectileFailed) return null;
            s_projectile = Build("Universal Render Pipeline/Unlit", false);
            if (s_projectile == null) { s_projectileFailed = true; return null; }
            s_projectile.SetColor("_BaseColor", new Color(0.25f, 0.95f, 1f));
            return s_projectile;
        }

        /// <summary>
        /// Transparent amber material for the H2 area-radius ring. Returns null when
        /// the shader is unavailable in this build.
        /// </summary>
        public static Material RingMaterial()
        {
            if (s_ring != null) return s_ring;
            if (s_ringFailed) return null;
            s_ring = Build("Universal Render Pipeline/Unlit", true);
            if (s_ring == null) { s_ringFailed = true; return null; }
            s_ring.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f, 0.3f));
            return s_ring;
        }

        /// <summary>
        /// Transparent material for the impact particle burst. Returns null when the
        /// shader is unavailable in this build.
        /// </summary>
        public static Material BurstMaterial()
        {
            if (s_burst != null) return s_burst;
            if (s_burstFailed) return null;
            s_burst = Build("Universal Render Pipeline/Particles/Unlit", true);
            if (s_burst == null) { s_burstFailed = true; return null; }
            s_burst.SetColor("_BaseColor", new Color(1f, 0.8f, 0.35f, 1f));
            return s_burst;
        }

        /// <summary>
        /// One-shot particle burst at <paramref name="position"/> (self-destructing).
        /// <paramref name="scale"/> grows the speed/size so the H2 area burst reads
        /// bigger than the H1 impact pop.
        /// </summary>
        public static void SpawnBurst(Vector3 position, float scale)
        {
            float s = Mathf.Max(0.4f, scale);
            var go = new GameObject("Fx_AbilityBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            // A fresh ParticleSystem auto-plays; stop it BEFORE touching duration
            // (Unity warns if duration changes while playing).
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.4f;
            main.startSpeed = 3f * s;
            main.startSize = 0.14f * s;
            main.startColor = new Color(1f, 0.8f, 0.35f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)24) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var material = BurstMaterial();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            ps.Play();
            Object.Destroy(go, 1.5f); // safety net if stopAction is missed
        }

        /// <summary>
        /// Flat transparent disc (a primitive Cylinder scaled thin) parented to
        /// <paramref name="parent"/> showing an ability's area radius. Returns the GO
        /// so the caller can enable/disable/resize it.
        /// </summary>
        public static GameObject CreateRing(Transform parent, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Fx_AbilityRing";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Build-safe: without the URP shader the primitive would keep Unity's
            // default (magenta) material. Destroy it instead of leaking it.
            var material = RingMaterial();
            if (material == null)
            {
                Object.Destroy(go);
                return null;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            go.transform.SetParent(parent, false);
            ApplyRingSize(go.transform, radius);
            return go;
        }

        /// <summary>Resizes the flat disc to the given radius (metres).</summary>
        public static void ApplyRingSize(Transform ring, float radius)
        {
            if (ring == null) return;
            float d = Mathf.Max(0.1f, radius * 2f);
            ring.localScale = new Vector3(d, 0.02f, d);
            ring.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.localRotation = Quaternion.identity;
        }

        private static Material Build(string shaderName, bool transparent)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // Player builds strip shaders that no material/scene references:
                // GraphicsSettings > Always Included Shaders must list them.
                if (!s_shaderMissingLogged)
                {
                    s_shaderMissingLogged = true;
                    Debug.LogError(
                        "[GreyBoxFx] URP shader '" + shaderName + "' not found in this build. " +
                        "Add Universal Render Pipeline/Unlit and Particles/Unlit to " +
                        "Project Settings > Graphics > Always Included Shaders.");
                }
                return null;
            }
            var material = new Material(shader);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);   // 0 = opaque, 1 = transparent
                material.SetFloat("_Blend", 0f);      // alpha blend
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            return material;
        }
    }
}
