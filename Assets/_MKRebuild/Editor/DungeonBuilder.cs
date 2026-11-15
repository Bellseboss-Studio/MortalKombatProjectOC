using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MK.Rebuild.EditorTools
{
    /// <summary>
    /// Generador del greybox de la mazmorra. Todo el nivel sale de primitivas con BoxCollider,
    /// igual que el resto del rebuild: sin FBX, sin MeshCollider, sin colliders a mano.
    ///
    /// MenuItem: MK/Level/Build Dungeon. Regenera el root "Dungeon" completo, así que mover
    /// una sala es cambiar un número y volver a correrlo.
    ///
    /// Recorrido (eje +Z, piso en y = 0):
    ///   Atrio -> Corredor de las Lanzas -> Pozo de los Estranguladores -> Patio de Armas
    ///   -> Galería -> Cámara del Guardián -> Descenso -> Cámara del Orochi
    /// </summary>
    public static class DungeonBuilder
    {
        private const float WallH = 7f;
        private const float DoorW = 4f;
        private const float DoorH = 3.2f;
        private const float T = 0.5f;
        private const float PitDepth = 2f;

        private static Material wallMat;
        private static Material floorMat;
        private static Transform root;

        [MenuItem("MK/Level/Build Dungeon")]
        public static void Build()
        {
            GameObject existing = GameObject.Find("Dungeon");
            if (existing != null) Object.DestroyImmediate(existing);

            root = new GameObject("Dungeon").transform;
            LoadMaterials();

            BuildAtrium();
            BuildSpearCorridor();
            BuildPitRoom();
            BuildArmory();
            BuildGallery();
            BuildGuardianArena();
            BuildDescent();
            BuildOrochiArena();

            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Dungeon] greybox generado: " + root.childCount + " piezas.");
        }

        // ------------------------------------------------------------------ materiales

        private static void LoadMaterials()
        {
            wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MKRebuild/Materials/WallGrey.mat");
            floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_MKRebuild/Materials/DungeonFloor.mat");
            if (floorMat != null) return;

            floorMat = new Material(wallMat) { name = "DungeonFloor" };
            floorMat.SetColor("_BaseColor", new Color(0.26f, 0.25f, 0.24f, 1f));
            AssetDatabase.CreateAsset(floorMat, "Assets/_MKRebuild/Materials/DungeonFloor.mat");
        }

        // ------------------------------------------------------------------ piezas

        private static GameObject Box(string name, Vector3 centre, Vector3 size, Material mat, bool solid = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!solid) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        /// <summary>Piso sólido de 0.5 m de espesor con la cara superior en y = top.</summary>
        private static void Slab(string name, float cx, float cz, float w, float d, float top = 0f)
        {
            Box(name, new Vector3(cx, top - 0.25f, cz), new Vector3(w, 0.5f, d), floorMat);
        }

        /// <summary>Pared sobre el eje X (corre en X, a la altura fija z). gaps = centros de puerta.</summary>
        private static void WallX(string name, float z, float x0, float x1, float h, params float[] gaps)
        {
            foreach ((float a, float b) in Split(x0, x1, gaps))
            {
                Box(name + "_s", new Vector3((a + b) * 0.5f, h * 0.5f, z), new Vector3(b - a, h, T), wallMat);
            }

            foreach (float g in gaps)
            {
                Box(name + "_lintel", new Vector3(g, DoorH + (h - DoorH) * 0.5f, z),
                    new Vector3(DoorW, h - DoorH, T), wallMat);
            }
        }

        /// <summary>Pared sobre el eje Z (corre en Z, a la altura fija x).</summary>
        private static void WallZ(string name, float x, float z0, float z1, float h, params float[] gaps)
        {
            foreach ((float a, float b) in Split(z0, z1, gaps))
            {
                Box(name + "_s", new Vector3(x, h * 0.5f, (a + b) * 0.5f), new Vector3(T, h, b - a), wallMat);
            }

            foreach (float g in gaps)
            {
                Box(name + "_lintel", new Vector3(x, DoorH + (h - DoorH) * 0.5f, g),
                    new Vector3(T, h - DoorH, DoorW), wallMat);
            }
        }

        private static IEnumerable<(float, float)> Split(float from, float to, float[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
            {
                yield return (from, to);
                yield break;
            }

            var sorted = new List<float>(gaps);
            sorted.Sort();

            float cursor = from;
            foreach (float g in sorted)
            {
                float a = g - DoorW * 0.5f;
                float b = g + DoorW * 0.5f;
                if (a > cursor) yield return (cursor, a);
                cursor = Mathf.Max(cursor, b);
            }

            if (cursor < to) yield return (cursor, to);
        }

        /// <summary>Foso con piso bajo y un escalón para poder salir.</summary>
        private static void Pit(string name, float cx, float cz, float w, float d)
        {
            Box(name + "_floor", new Vector3(cx, -PitDepth - 0.25f, cz), new Vector3(w, 0.5f, d), floorMat);
            Box(name + "_step", new Vector3(cx, -PitDepth * 0.5f - 0.4f, cz + d * 0.5f - 0.5f),
                new Vector3(Mathf.Min(w * 0.5f, 2.5f), PitDepth * 0.5f - 0.4f, 0.8f), floorMat);
        }

        private static void Pillar(string name, float x, float z, float size, float h)
        {
            Box(name, new Vector3(x, h * 0.5f, z), new Vector3(size, h, size), wallMat);
        }

        private static void Lamp(string name, float x, float z, float y = 4.5f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(x, y, z);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.86f, 0.66f);
            light.range = 22f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ salas

        /// <summary>Atrio de entrada: 20x20, puerta al norte.</summary>
        private static void BuildAtrium()
        {
            Slab("Atrio_Floor", 0f, 0f, 20f, 20f);
            WallX("Atrio_Wall_S", -10f, -10f, 10f, WallH);
            WallX("Atrio_Wall_N", 10f, -10f, 10f, WallH, 0f);
            WallZ("Atrio_Wall_W", -10f, -10f, 10f, WallH);
            WallZ("Atrio_Wall_E", 10f, -10f, 10f, WallH);

            Pillar("Atrio_Pillar_1", -6f, -6f, 1.2f, WallH);
            Pillar("Atrio_Pillar_2", 6f, -6f, 1.2f, WallH);
            Pillar("Atrio_Pillar_3", -6f, 6f, 1.2f, WallH);
            Pillar("Atrio_Pillar_4", 6f, 6f, 1.2f, WallH);

            Lamp("Atrio_Lamp", 0f, 0f);
        }

        /// <summary>Corredor de 5 m con dos fosos que piden salto o dash.</summary>
        private static void BuildSpearCorridor()
        {
            Slab("CorrA_Floor_1", 0f, 13f, 5f, 6f);
            Slab("CorrA_Floor_2", 0f, 21f, 5f, 6f);
            Slab("CorrA_Floor_3", 0f, 28f, 5f, 4f);

            Pit("CorrA_Pit_1", 0f, 17f, 5f, 2f);
            Pit("CorrA_Pit_2", 0f, 25f, 5f, 2f);

            WallZ("CorrA_Wall_W", -2.5f, 10f, 30f, 5f);
            WallZ("CorrA_Wall_E", 2.5f, 10f, 30f, 5f);

            Lamp("CorrA_Lamp_1", 0f, 14f, 3.6f);
            Lamp("CorrA_Lamp_2", 0f, 22f, 3.6f);
        }

        /// <summary>Pozo central de 8x8 con tres repisas para colgarse y trepar.</summary>
        private static void BuildPitRoom()
        {
            Slab("Pozo_Floor_S", 0f, 32.5f, 18f, 5f);
            Slab("Pozo_Floor_N", 0f, 45.5f, 18f, 5f);
            Slab("Pozo_Floor_W", -6.5f, 39f, 5f, 8f);
            Slab("Pozo_Floor_E", 6.5f, 39f, 5f, 8f);

            Pit("Pozo_Centre", 0f, 39f, 8f, 8f);

            WallX("Pozo_Wall_S", 30f, -9f, 9f, WallH, 0f);
            WallX("Pozo_Wall_N", 48f, -9f, 9f, WallH, 0f);
            WallZ("Pozo_Wall_W", -9f, 30f, 48f, WallH);
            WallZ("Pozo_Wall_E", 9f, 30f, 48f, WallH);

            // Repisas escalonadas: 0.0 / 1.2 / 2.4 m sobre el borde del pozo.
            Box("Pozo_Ledge_1", new Vector3(0f, -0.6f, 35.4f), new Vector3(4f, 0.6f, 1.2f), wallMat);
            Box("Pozo_Ledge_2", new Vector3(3.4f, 0.6f, 39f), new Vector3(1.2f, 0.6f, 4f), wallMat);
            Box("Pozo_Ledge_3", new Vector3(0f, 1.8f, 42.6f), new Vector3(4f, 0.6f, 1.2f), wallMat);

            Lamp("Pozo_Lamp", 0f, 39f, 6f);
        }

        /// <summary>Patio de armas: sala abierta con columnas, pensada para el combate.</summary>
        private static void BuildArmory()
        {
            Slab("Patio_Floor", 0f, 74f, 24f, 24f);
            WallX("Patio_Wall_S", 62f, -12f, 12f, WallH, 0f);
            WallX("Patio_Wall_N", 86f, -12f, 12f, WallH, 0f);
            WallZ("Patio_Wall_W", -12f, 62f, 86f, WallH);
            WallZ("Patio_Wall_E", 12f, 62f, 86f, WallH);

            Pillar("Patio_Pillar_1", -8f, 68f, 1.5f, WallH);
            Pillar("Patio_Pillar_2", 8f, 68f, 1.5f, WallH);
            Pillar("Patio_Pillar_3", -8f, 80f, 1.5f, WallH);
            Pillar("Patio_Pillar_4", 8f, 80f, 1.5f, WallH);

            Lamp("Patio_Lamp_1", -5f, 74f);
            Lamp("Patio_Lamp_2", 5f, 74f);
        }

        /// <summary>Galería: corredor con una pasarela elevada y un paso bajo.</summary>
        private static void BuildGallery()
        {
            Slab("Gal_Floor", 0f, 95f, 6f, 18f);
            WallZ("Gal_Wall_W", -3f, 86f, 104f, 5f);
            WallZ("Gal_Wall_E", 3f, 86f, 104f, 5f);

            // Pasarela elevada contra la pared oeste, con dos escalones de acceso.
            // Arranca en z = 92 para no tapar las dos puertas del corredor (86 y 104).
            Box("Gal_Walkway", new Vector3(-1.4f, 1.3f, 97f), new Vector3(3.2f, 0.4f, 9f), wallMat);
            Box("Gal_Step_1", new Vector3(-1.4f, 0.25f, 91f), new Vector3(3.2f, 0.5f, 2f), wallMat);
            Box("Gal_Step_2", new Vector3(-1.4f, 0.75f, 92.5f), new Vector3(3.2f, 0.5f, 1.6f), wallMat);
            Box("Gal_Rail", new Vector3(0.2f, 1.9f, 97f), new Vector3(0.3f, 0.9f, 9f), wallMat);

            Lamp("Gal_Lamp_1", 0f, 91f, 3.8f);
            Lamp("Gal_Lamp_2", 0f, 99f, 3.8f);
        }

        /// <summary>Cámara del Guardián: arena de 26x26, puerta única al sur.</summary>
        private static void BuildGuardianArena()
        {
            Slab("Guardian_Floor", 0f, 117f, 26f, 26f);
            WallX("Guardian_Wall_S", 104f, -13f, 13f, WallH, 0f);
            WallX("Guardian_Wall_N", 130f, -13f, 13f, WallH, 0f);
            WallZ("Guardian_Wall_W", -13f, 104f, 130f, WallH);
            WallZ("Guardian_Wall_E", 13f, 104f, 130f, WallH);

            // Dais central: marca el sitio del boss sin ser un obstáculo.
            Box("Guardian_Dais", new Vector3(0f, 0.15f, 117f), new Vector3(9f, 0.3f, 9f), floorMat);

            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -10f : 10f;
                float z = i < 2 ? 110f : 124f;
                Pillar("Guardian_Pillar_" + i, x, z, 1.5f, WallH);
            }

            Lamp("Guardian_Lamp_1", -6f, 117f, 6f);
            Lamp("Guardian_Lamp_2", 6f, 117f, 6f);
        }

        /// <summary>Descenso: corredor con un tramo de piso hundido y viga angosta.</summary>
        private static void BuildDescent()
        {
            Slab("Desc_Floor_S", 0f, 132.5f, 5f, 5f);
            Slab("Desc_Floor_N", 0f, 141.5f, 5f, 5f);
            Pit("Desc_Pit", 0f, 137f, 5f, 4f);

            // Viga sobre el foso: el paso seguro si no querés saltar.
            Box("Desc_Beam", new Vector3(0f, 0.1f, 137f), new Vector3(1.2f, 0.3f, 4f), wallMat);

            WallZ("Desc_Wall_W", -2.5f, 130f, 144f, 5f);
            WallZ("Desc_Wall_E", 2.5f, 130f, 144f, 5f);

            Lamp("Desc_Lamp", 0f, 137f, 3.8f);
        }

        /// <summary>Cámara del Orochi: arena final de 32x32 con columnas perimetrales.</summary>
        private static void BuildOrochiArena()
        {
            Slab("Orochi_Floor", 0f, 160f, 32f, 32f);
            WallX("Orochi_Wall_S", 144f, -16f, 16f, WallH, 0f);
            WallX("Orochi_Wall_N", 176f, -16f, 16f, WallH);
            WallZ("Orochi_Wall_W", -16f, 144f, 176f, WallH);
            WallZ("Orochi_Wall_E", 16f, 144f, 176f, WallH);

            Box("Orochi_Dais", new Vector3(0f, 0.2f, 168f), new Vector3(12f, 0.4f, 10f), floorMat);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Pillar("Orochi_Pillar_" + i, Mathf.Cos(angle) * 12f, 160f + Mathf.Sin(angle) * 12f, 1.8f, WallH);
            }

            Lamp("Orochi_Lamp_1", -8f, 160f, 6f);
            Lamp("Orochi_Lamp_2", 8f, 160f, 6f);
        }
    }
}
