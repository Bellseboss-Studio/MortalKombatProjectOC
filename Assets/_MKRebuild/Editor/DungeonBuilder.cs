using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MK.Rebuild.EditorTools
{
    /// <summary>
    /// Generador del greybox de la mazmorra. Todo sale de primitivas con BoxCollider, igual que
    /// el resto del rebuild: sin FBX, sin MeshCollider, sin colliders a mano.
    ///
    /// MenuItem: MK/Level/Build Dungeon. Regenera el root "Dungeon" y reposiciona al Player en
    /// el spawn. Al terminar corre una AUDITORÍA y la escribe en consola: pisos, fosos y puertas.
    ///
    /// Estructura (hub-and-spoke, +Z al norte):
    ///
    ///                        [ ARENA OROCHI ]  z  63..97
    ///                               |
    ///                        [ DESCENSO ]      z  49..63
    ///                               |
    ///                       [ GUARDIÁN ]       z  23..49
    ///                               |
    ///   [ ALA OESTE ] ──────── [ ATRIO ] ──────── [ ALA ESTE ]
    ///     traversal x -63..-14   28x28       combate x 14..65
    ///                               |
    ///                        [ ALA SUR ]        z -68..-14
    ///                        agarre / peso
    ///
    /// Principios aplicados: Kishōtenketsu por mecánica (enseñar -> probar -> combinar), hub
    /// con landmark central, contraste de espacio (sala abierta / corredor opresivo) y riesgo
    /// opcional (el paso corto es peligroso, el largo es seguro).
    /// </summary>
    public static class DungeonBuilder
    {
        private const float WallH = 7f;
        private const float DoorW = 4f;
        private const float DoorH = 3.2f;
        private const float T = 0.5f;
        private const float PitDepth = 2.5f;

        private static Material wallMat;
        private static Material floorMat;
        private static Transform root;
        private static DungeonObjective objective;

        [MenuItem("MK/Level/Build Dungeon")]
        public static void Build()
        {
            GameObject existing = GameObject.Find("Dungeon");
            if (existing != null) Object.DestroyImmediate(existing);

            root = new GameObject("Dungeon").transform;
            LoadMaterials();

            BuildHub();
            BuildWestWing();
            BuildEastWing();
            BuildSouthWing();
            BuildObjective();
            BuildGuardianArena();
            BuildDescent();
            BuildOrochiArena();

            PlacePlayer();
            Physics.SyncTransforms();

            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Dungeon] greybox generado: " + root.childCount + " piezas.");
            Audit();
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

        private static void PlacePlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) return;
            player.transform.position = new Vector3(0f, 1.2f, 0f);
            player.transform.rotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ piezas

        private static GameObject Box(string name, Vector3 centre, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static void Slab(string name, float cx, float cz, float w, float d, float top = 0f)
        {
            Box(name, new Vector3(cx, top - 0.25f, cz), new Vector3(w, 0.5f, d), floorMat);
        }

        /// <summary>Pared que corre en X, a la altura fija z. gaps = centros de puerta en X.</summary>
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

        /// <summary>Pared que corre en Z, a la altura fija x. gaps = centros de puerta en Z.</summary>
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

        /// <summary>Corredor que corre en X: piso, dos paredes laterales y una lámpara al medio.</summary>
        private static void CorridorX(string name, float x0, float x1, float halfWidth, float h)
        {
            float cx = (x0 + x1) * 0.5f;
            Slab(name + "_Floor", cx, 0f, x1 - x0, halfWidth * 2f);
            WallX(name + "_S", -halfWidth, x0, x1, h);
            WallX(name + "_N", halfWidth, x0, x1, h);
            Lamp(name + "_Lamp", cx, 0f, h - 1.4f);
        }

        /// <summary>Corredor que corre en Z.</summary>
        private static void CorridorZ(string name, float z0, float z1, float halfWidth, float h)
        {
            float cz = (z0 + z1) * 0.5f;
            Slab(name + "_Floor", 0f, cz, halfWidth * 2f, z1 - z0);
            WallZ(name + "_W", -halfWidth, z0, z1, h);
            WallZ(name + "_E", halfWidth, z0, z1, h);
            Lamp(name + "_Lamp", 0f, cz, h - 1.4f);
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

        // ------------------------------------------------------------------ hub

        /// <summary>Atrio de las Tres Puertas: 28x28, cuatro vanos y un obelisco como landmark.</summary>
        private static void BuildHub()
        {
            Slab("Hub_Floor", 0f, 0f, 28f, 28f);
            WallX("Hub_Wall_S", -14f, -14f, 14f, 8f, 0f);
            WallX("Hub_Wall_N", 14f, -14f, 14f, 8f, 0f);
            WallZ("Hub_Wall_W", -14f, -14f, 14f, 8f, 0f);
            WallZ("Hub_Wall_E", 14f, -14f, 14f, 8f, 0f);

            // Landmark: obelisco partido, fuera del eje de las puertas para no tapar el paso.
            Box("Hub_Obelisk_Base", new Vector3(7f, 0.4f, 8f), new Vector3(3f, 0.8f, 3f), wallMat);
            Box("Hub_Obelisk", new Vector3(7f, 4f, 8f), new Vector3(1.6f, 7.2f, 1.6f), wallMat);

            Pillar("Hub_Pillar_1", -8f, -8f, 1.5f, 8f);
            Pillar("Hub_Pillar_2", 8f, -8f, 1.5f, 8f);
            Pillar("Hub_Pillar_3", -8f, 4f, 1.5f, 8f);
            Pillar("Hub_Pillar_4", 8f, 4f, 1.5f, 8f);

            Lamp("Hub_Lamp_1", -5f, -6f, 6.5f);
            Lamp("Hub_Lamp_2", 5f, -6f, 6.5f);
        }

        // ------------------------------------------------------------------ ala oeste

        /// <summary>
        /// Ala Oeste — El Vacío. Traversal: enseña el dash en un foso corto, lo prueba sobre un
        /// abismo, y combina dash + borde en el pozo final.
        /// </summary>
        private static void BuildWestWing()
        {
            CorridorX("West_Entry", -23f, -14f, 2.5f, 6f);

            // Sala de las Lanzas: un foso de 2 m a lo ancho. Enseñar sin castigar.
            Slab("West_Lances_Floor_E", -27f, 0f, 8f, 14f);
            Slab("West_Lances_Floor_W", -35f, 0f, 4f, 14f);
            Pit("West_Lances_Pit", -32f, 0f, 2f, 14f);
            WallX("West_Lances_Wall_S", -7f, -37f, -23f, 6f);
            WallX("West_Lances_Wall_N", 7f, -37f, -23f, 6f);
            WallZ("West_Lances_Wall_W", -37f, -7f, 7f, 6f, 0f);
            WallZ("West_Lances_Wall_E", -23f, -7f, 7f, 6f, 0f);
            Lamp("West_Lances_Lamp", -30f, 0f, 4.6f);

            // Corredor del Abismo: dos fosos de 2.5 m sobre un pozo profundo.
            Slab("West_Abyss_Floor_1", -39f, 0f, 4f, 5f);
            Slab("West_Abyss_Floor_2", -43.5f, 0f, 1.5f, 5f);
            Slab("West_Abyss_Floor_3", -46.5f, 0f, 3f, 5f);
            Pit("West_Abyss_Pit_1", -42.25f, 0f, 2.5f, 5f);
            Pit("West_Abyss_Pit_2", -45.25f, 0f, 2.5f, 5f);
            WallX("West_Abyss_Wall_S", -2.5f, -47f, -37f, 6f);
            WallX("West_Abyss_Wall_N", 2.5f, -47f, -37f, 6f);
            Lamp("West_Abyss_Lamp", -42f, 0f, 4.6f);

            // Pozo de los Estranguladores: tres repisas escalonadas para colgarse y trepar.
            Slab("West_Pit_Floor_S", -55f, -6.5f, 16f, 3f);
            Slab("West_Pit_Floor_N", -55f, 6.5f, 16f, 3f);
            Slab("West_Pit_Floor_W", -61f, 0f, 4f, 10f);
            Slab("West_Pit_Floor_E", -49f, 0f, 4f, 10f);
            Pit("West_Pit_Centre", -55f, 0f, 8f, 10f);

            WallX("West_Pit_Wall_S", -8f, -63f, -47f, 8f);
            WallX("West_Pit_Wall_N", 8f, -63f, -47f, 8f);
            WallZ("West_Pit_Wall_W", -63f, -8f, 8f, 8f);
            WallZ("West_Pit_Wall_E", -47f, -8f, 8f, 8f, 0f);

            Box("West_Pit_Ledge_1", new Vector3(-55f, -0.9f, -4.2f), new Vector3(4f, 0.6f, 1.2f), wallMat);
            Box("West_Pit_Ledge_2", new Vector3(-52.2f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 4f), wallMat);
            Box("West_Pit_Ledge_3", new Vector3(-55f, 1.5f, 4.2f), new Vector3(4f, 0.6f, 1.2f), wallMat);

            Lamp("West_Pit_Lamp", -55f, 0f, 6.5f);
        }

        // ------------------------------------------------------------------ ala este

        /// <summary>
        /// Ala Este — El Patio de Armas. Combate: espacio abierto con columnas, después un
        /// corredor de 4 m que quita la movilidad, y por último una galería multi-nivel.
        /// Mismos enemigos, tres lecturas distintas.
        /// </summary>
        private static void BuildEastWing()
        {
            CorridorX("East_Entry", 14f, 22f, 2.5f, 6f);

            // Sala de Guardia: abierta, con columnas para romper la línea de carga.
            Slab("East_Guard_Floor", 30f, 0f, 16f, 16f);
            WallX("East_Guard_Wall_S", -8f, 22f, 38f, 7f);
            WallX("East_Guard_Wall_N", 8f, 22f, 38f, 7f);
            WallZ("East_Guard_Wall_W", 22f, -8f, 8f, 7f, 0f);
            WallZ("East_Guard_Wall_E", 38f, -8f, 8f, 7f, 0f);
            Pillar("East_Guard_Pillar_1", 25f, -5f, 1.5f, 7f);
            Pillar("East_Guard_Pillar_2", 35f, -5f, 1.5f, 7f);
            Pillar("East_Guard_Pillar_3", 25f, 5f, 1.5f, 7f);
            Pillar("East_Guard_Pillar_4", 35f, 5f, 1.5f, 7f);
            Lamp("East_Guard_Lamp_1", 27f, 0f, 5.5f);
            Lamp("East_Guard_Lamp_2", 33f, 0f, 5.5f);

            // Corredor de los Ecos: 4 m de ancho. Sin espacio para esquivar.
            CorridorX("East_Echoes", 38f, 48f, 2f, 5f);

            // Galería: plataforma central elevada y columnas en anillo.
            Slab("East_Gallery_Floor", 56f, 0f, 18f, 18f);
            WallX("East_Gallery_Wall_S", -9f, 47f, 65f, 8f);
            WallX("East_Gallery_Wall_N", 9f, 47f, 65f, 8f);
            WallZ("East_Gallery_Wall_W", 47f, -9f, 9f, 8f, 0f);
            WallZ("East_Gallery_Wall_E", 65f, -9f, 9f, 8f);
            Box("East_Gallery_Dais", new Vector3(56f, 0.4f, 0f), new Vector3(5f, 0.8f, 5f), floorMat);

            for (int i = 0; i < 6; i++)
            {
                // Medio paso de desfase: con el anillo alineado, una columna caía justo
                // delante de la puerta oeste y tapaba el paso.
                float angle = i * Mathf.PI / 3f + Mathf.PI / 6f;
                Pillar("East_Gallery_Pillar_" + i,
                    56f + Mathf.Cos(angle) * 6.5f, Mathf.Sin(angle) * 6.5f, 1.2f, 8f);
            }

            Lamp("East_Gallery_Lamp", 56f, 0f, 6.5f);
        }

        // ------------------------------------------------------------------ ala sur

        /// <summary>
        /// Ala Sur — La Cámara del Peso. Agarre y lanzamiento: una repisa a 2 m que no se
        /// alcanza a pie (obliga a usar el peso), un foso y un pozo con repisa a 3 m.
        /// </summary>
        private static void BuildSouthWing()
        {
            CorridorZ("South_Entry", -22f, -14f, 2.5f, 6f);

            // Sala de Pesos: plataforma alta contra la pared oeste.
            Slab("South_Weight_Floor", 0f, -30f, 16f, 16f);
            WallX("South_Weight_Wall_S", -38f, -8f, 8f, 7f, 0f);
            WallX("South_Weight_Wall_N", -22f, -8f, 8f, 7f, 0f);
            WallZ("South_Weight_Wall_W", -8f, -38f, -22f, 7f);
            WallZ("South_Weight_Wall_E", 8f, -38f, -22f, 7f);
            Box("South_Weight_Platform", new Vector3(-6f, 1f, -30f), new Vector3(4f, 2f, 8f), wallMat);
            Lamp("South_Weight_Lamp", 2f, -30f, 5.5f);

            // Corredor con foso: el paso corto.
            Slab("South_Drop_Floor_1", 0f, -41f, 5f, 6f);
            Slab("South_Drop_Floor_2", 0f, -46.5f, 5f, 3f);
            Pit("South_Drop_Pit", 0f, -44.25f, 5f, 2.5f);
            WallZ("South_Drop_Wall_W", -2.5f, -48f, -38f, 5f);
            WallZ("South_Drop_Wall_E", 2.5f, -48f, -38f, 5f);
            Lamp("South_Drop_Lamp", 0f, -43f, 3.8f);

            // Cámara del Peso: pozo con repisa alta.
            Slab("South_Chamber_Floor_S", 0f, -62f, 18f, 12f);
            Slab("South_Chamber_Floor_N", 0f, -51f, 18f, 6f);
            Slab("South_Chamber_Floor_W", -7f, -58f, 4f, 8f);
            Slab("South_Chamber_Floor_E", 7f, -58f, 4f, 8f);
            Pit("South_Chamber_Pit", 0f, -58f, 10f, 8f);

            WallX("South_Chamber_Wall_S", -68f, -9f, 9f, 8f);
            WallX("South_Chamber_Wall_N", -48f, -9f, 9f, 8f, 0f);
            WallZ("South_Chamber_Wall_W", -9f, -68f, -48f, 8f);
            WallZ("South_Chamber_Wall_E", 9f, -68f, -48f, 8f);

            Box("South_Chamber_Ledge", new Vector3(0f, 2.4f, -63f), new Vector3(5f, 0.6f, 1.6f), wallMat);
            Lamp("South_Chamber_Lamp", 0f, -58f, 6.5f);
        }

        // ------------------------------------------------------------------ objetivo

        /// <summary>
        /// Objetivo de la mazmorra: tres palancas, una al fondo de cada ala, abren la gran
        /// puerta del atrio. La puerta es un colisionador móvil: mientras está cerrada tapa
        /// el vano norte de verdad, así que el objetivo no se puede saltear caminando.
        /// </summary>
        private static void BuildObjective()
        {
            var host = new GameObject("DungeonObjective");
            host.transform.SetParent(root, false);
            objective = host.AddComponent<DungeonObjective>();

            BuildLever("Lever_West", new Vector3(-62f, 0f, 0f), Vector3.right);
            BuildLever("Lever_East", new Vector3(64f, 0f, 0f), Vector3.left);
            BuildLever("Lever_South", new Vector3(0f, 0f, -67f), Vector3.forward);

            // Gran puerta: tapa el vano norte del atrio (4 m de ancho, 3.2 de alto).
            var doorRoot = new GameObject("GreatDoor");
            doorRoot.transform.SetParent(root, false);
            doorRoot.transform.localPosition = new Vector3(0f, 0f, 14f);

            GameObject slab = Box("GreatDoor_Slab", new Vector3(0f, 3f, 14f), new Vector3(4.2f, 6f, 0.4f), wallMat);
            slab.transform.SetParent(doorRoot.transform, true);

            GameObject stone = Box("GreatDoor_Stone", new Vector3(2.8f, 0.6f, 12.2f), new Vector3(1.2f, 1.2f, 1.2f), floorMat);

            var door = doorRoot.AddComponent<GreatDoor>();
            Wire(door, "door", slab.transform);
            Wire(door, "objective", objective);
            Wire(door, "stone", stone.transform);
        }

        /// <summary>Palanca: base fija, mango que se inclina hacia el jugador y ancla de pose.</summary>
        private static void BuildLever(string name, Vector3 basePosition, Vector3 facing)
        {
            var leverRoot = new GameObject(name);
            leverRoot.transform.SetParent(root, false);
            leverRoot.transform.localPosition = basePosition;

            Box(name + "_Base", basePosition + Vector3.up * 0.55f, new Vector3(0.9f, 1.1f, 0.9f), wallMat);

            var handle = new GameObject(name + "_Handle");
            handle.transform.SetParent(leverRoot.transform, false);
            handle.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            handle.transform.localRotation = Quaternion.LookRotation(facing);

            GameObject arm = Box(name + "_Arm", Vector3.zero, new Vector3(0.16f, 1.1f, 0.16f), wallMat);
            arm.transform.SetParent(handle.transform, false);
            arm.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            arm.transform.localRotation = Quaternion.identity;

            var anchor = new GameObject(name + "_Anchor");
            anchor.transform.SetParent(leverRoot.transform, false);
            anchor.transform.localPosition = facing * 1.15f;
            // El ancla mira DE VUELTA a la palanca: el jugador se planta mirándola.
            anchor.transform.localRotation = Quaternion.LookRotation(-facing);

            var lever = leverRoot.AddComponent<LeverInteractable>();
            Wire(lever, "handle", handle.transform);
            Wire(lever, "anchor", anchor.transform);
            Wire(lever, "objective", objective);

            Lamp(name + "_Lamp", basePosition.x - facing.x * 0.6f, basePosition.z - facing.z * 0.6f, 4.2f);
        }

        private static void Wire(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[Dungeon] Campo inexistente al cablear: " + field + " en " + target.name);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ final

        /// <summary>Cámara del Guardián: arena de 26x26, puertas al sur y al norte.</summary>
        private static void BuildGuardianArena()
        {
            CorridorZ("Guardian_Entry", 14f, 23f, 2.5f, 6f);

            Slab("Guardian_Floor", 0f, 36f, 26f, 26f);
            WallX("Guardian_Wall_S", 23f, -13f, 13f, 8f, 0f);
            WallX("Guardian_Wall_N", 49f, -13f, 13f, 8f, 0f);
            WallZ("Guardian_Wall_W", -13f, 23f, 49f, 8f);
            WallZ("Guardian_Wall_E", 13f, 23f, 49f, 8f);

            Box("Guardian_Dais", new Vector3(0f, 0.2f, 36f), new Vector3(9f, 0.4f, 9f), floorMat);

            Pillar("Guardian_Pillar_1", -10f, 28f, 1.5f, 8f);
            Pillar("Guardian_Pillar_2", 10f, 28f, 1.5f, 8f);
            Pillar("Guardian_Pillar_3", -10f, 44f, 1.5f, 8f);
            Pillar("Guardian_Pillar_4", 10f, 44f, 1.5f, 8f);

            Lamp("Guardian_Lamp_1", -6f, 36f, 6.5f);
            Lamp("Guardian_Lamp_2", 6f, 36f, 6.5f);
        }

        /// <summary>Descenso: corredor con foso y viga angosta como paso seguro.</summary>
        private static void BuildDescent()
        {
            Slab("Desc_Floor_S", 0f, 52f, 5f, 6f);
            Slab("Desc_Floor_N", 0f, 60.5f, 5f, 5f);
            Pit("Desc_Pit", 0f, 56.75f, 5f, 3.5f);
            Box("Desc_Beam", new Vector3(0f, 0.1f, 56.75f), new Vector3(1.2f, 0.3f, 3.5f), wallMat);
            WallZ("Desc_Wall_W", -2.5f, 49f, 63f, 5f);
            WallZ("Desc_Wall_E", 2.5f, 49f, 63f, 5f);
            Lamp("Desc_Lamp", 0f, 56f, 3.8f);
        }

        /// <summary>Cámara del Orochi: arena final de 34x34 con ocho columnas.</summary>
        private static void BuildOrochiArena()
        {
            Slab("Orochi_Floor", 0f, 80f, 34f, 34f);
            WallX("Orochi_Wall_S", 63f, -17f, 17f, 8f, 0f);
            WallX("Orochi_Wall_N", 97f, -17f, 17f, 8f);
            WallZ("Orochi_Wall_W", -17f, 63f, 97f, 8f);
            WallZ("Orochi_Wall_E", 17f, 63f, 97f, 8f);

            Box("Orochi_Dais", new Vector3(0f, 0.25f, 86f), new Vector3(12f, 0.5f, 10f), floorMat);

            for (int i = 0; i < 8; i++)
            {
                // Medio paso de desfase para que ninguna columna quede sobre el eje de la puerta.
                float angle = i * Mathf.PI * 0.25f + Mathf.PI * 0.125f;
                Pillar("Orochi_Pillar_" + i,
                    Mathf.Cos(angle) * 13f, 80f + Mathf.Sin(angle) * 13f, 1.8f, 8f);
            }

            Lamp("Orochi_Lamp_1", -8f, 80f, 6.5f);
            Lamp("Orochi_Lamp_2", 8f, 80f, 6.5f);
        }

        // ------------------------------------------------------------------ auditoría

        /// <summary>
        /// Se autoverifica: sin esto, un error de coordenadas se descubre caminando. Corre
        /// después de construir y escribe el resultado en consola.
        /// </summary>
        private static void Audit()
        {
            int failures = 0;

            (string label, float x, float z)[] floors =
            {
                ("Hub", 0f, 0f),
                ("Ala Oeste / Lanzas", -27f, 0f),
                ("Ala Oeste / Pozo", -61f, 0f),
                ("Ala Este / Guardia", 30f, 0f),
                ("Ala Este / Galería", 56f, 0f),
                ("Ala Sur / Pesos", 0f, -30f),
                ("Ala Sur / Cámara", 0f, -62f),
                ("Guardián", 0f, 36f),
                ("Orochi", 0f, 80f),
            };

            foreach ((string label, float x, float z) in floors)
            {
                // RaycastAll y no Raycast: el primer impacto puede ser el jugador o un dais.
                // Vale como piso cualquier superficie entre -1.0 y +1.2; un foso queda afuera.
                bool found = false;
                float y = 0f;
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 4f, z), Vector3.down, 10f);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform.root.name == "Player") continue;
                    if (!found || hit.point.y > y)
                    {
                        found = true;
                        y = hit.point.y;
                    }
                }

                if (!found || y < -1.0f || y > 1.2f)
                {
                    failures++;
                    Debug.LogError("[Dungeon] PISO ausente en " + label +
                                   " (" + x + ", " + z + "): " + (found ? y.ToString("0.00") : "sin impacto"));
                }
            }

            // Cada puerta se prueba también 2.5 m antes y después: un obstáculo dentro de la
            // sala (un pilar, un prop) bloquea el paso aunque el vano esté limpio.
            (string label, float x, float z, float dx, float dz)[] doors =
            {
                ("Hub oeste", -14f, 0f, 1f, 0f), ("Hub este", 14f, 0f, 1f, 0f),
                ("Hub sur", 0f, -14f, 0f, 1f),
                ("Lanzas oeste", -37f, 0f, 1f, 0f), ("Pozo este", -47f, 0f, 1f, 0f),
                ("Guardia oeste", 22f, 0f, 1f, 0f), ("Guardia este", 38f, 0f, 1f, 0f),
                ("Galería oeste", 47f, 0f, 1f, 0f),
                ("Pesos norte", 0f, -22f, 0f, 1f), ("Pesos sur", 0f, -38f, 0f, 1f),
                ("Cámara norte", 0f, -48f, 0f, 1f),
                ("Guardián sur", 0f, 23f, 0f, 1f), ("Guardián norte", 0f, 49f, 0f, 1f),
                ("Orochi sur", 0f, 63f, 0f, 1f),
            };

            foreach ((string label, float x, float z, float dx, float dz) in doors)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var probe = new Vector3(x + dx * 2.5f * side, 1.6f, z + dz * 2.5f * side);
                    if (Physics.CheckSphere(probe, 0.9f))
                    {
                        failures++;
                        Debug.LogError("[Dungeon] PASO bloqueado en " + label + " lado " + side +
                                       " (" + probe.x + ", " + probe.z + ")");
                    }
                }
            }

            // La gran puerta arranca cerrada y las tres palancas deben existir.
            if (!Physics.CheckSphere(new Vector3(0f, 1.6f, 14f), 0.8f))
            {
                failures++;
                Debug.LogError("[Dungeon] La gran puerta no bloquea el vano norte.");
            }

            foreach (string leverName in new[] { "Lever_West", "Lever_East", "Lever_South" })
            {
                if (root.Find(leverName) == null)
                {
                    failures++;
                    Debug.LogError("[Dungeon] Falta la palanca " + leverName);
                }
            }

            if (failures == 0)
            {
                Debug.Log("[Dungeon] AUDITORÍA OK: " + floors.Length + " pisos y " + doors.Length + " puertas.");
            }            else
            {
                Debug.LogError("[Dungeon] AUDITORÍA: " + failures + " problema(s).");
            }
        }
    }
}
