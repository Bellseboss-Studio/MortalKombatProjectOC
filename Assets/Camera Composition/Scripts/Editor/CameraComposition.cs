/*  This file is part of the "Camera Composition" editor tool by Jordan Cassady.
 *  You are only permitted to use this software if purchased and downloaded from
 *  the Unity Asset Store. You shall not sell, license, transfer, distribute or
 *  otherwise make this software available to any third party.
 */

// Enable Camera Composition in Unity editor only. Do not include in builds.
#if UNITY_EDITOR
    using UnityEngine;
    using UnityEditor;

    namespace JordanCassady
    {
        /// <summary>
        /// Draw GUI elements of the Camera Composition editor window.
        /// </summary>
        public class CameraComposition : EditorWindow
        {
            #region PROPERTIES
            private GameObject compositionObject;
            private GUIStyle guiStyle;
            private readonly string[] tabNames;
            private int tabIndex;
            private GameObject targetCamera;
            private Vector3 revertCameraPosition;
            private Quaternion revertCameraRotation;
            #endregion

            #region EDITOR_TEXTURES
            private Color greyColor = new Color(56f/255f, 56f/255f, 56f/255f, 1f);

            private Texture2D gridSectionTexture;
            private Rect gridSection;

            private Texture2D rotationSectionTexture;
            private Rect rotationSection;

            private Texture2D colorSectionTexture;
            private Rect colorSection;

            private Texture2D opacitySectionTexture;
            private Rect opacitySection;

            private Texture2D tabSectionTexture;
            private Rect tabSection;

            private Texture2D overrideSectionTexture;
            private Rect overrideSection;

            private Texture2D cameraSectionTexture;
            private Rect cameraSection;
            #endregion

            // Ctrl/Cmd + Shift + Alt + C keyboard shortcut.
            [MenuItem("Tools/Camera Composition %#&C")]
            static private void InitWindow()
            {
                CameraComposition window = (CameraComposition)EditorWindow.GetWindowWithRect
                        (typeof(CameraComposition), new Rect(0, 0, 370, 427), false, "Camera Composition");
                window.minSize = new Vector2(370, 186);
                window.Show();
            }

            /// <summary>
            /// Initialize the editor window's default GUIStyle for text formatting.
            /// </summary>
            private void OnEnable()
            {
                InitGUIStyle();
            }

            /// <summary>
            /// Remove the "Camera Composition" GameObject from the scene hierarchy
            /// when closing the EditorWindow.
            /// </summary>
            private void OnDestroy()
            {
                DestroyImmediate(compositionObject);
            }

            /// <summary>
            /// Draw the editor window GUI components.
            /// Setup the "Camera Composition (Clone)" editor object in the
            /// scene hierarchy and create instance of CameraCompositionGUI.
            /// </summary>
            private void OnGUI()
            {
                if (compositionObject == null)
                    compositionObject = Instantiate(Resources.Load<GameObject>("Prefabs/Camera Composition"));

                DrawLayouts();
                DrawTabSection(compositionObject);
                DrawOverrideSection(compositionObject);
                DrawCameraSection();
            }

            /// <summary>
            /// Constructor to get navigation tab names from the composition object's
            /// configured overlays and initialize editor window style elements.
            /// </summary>
            public CameraComposition()
            {
                tabNames = new string[] {"Rule of Thirds", "Diagonal",
                    "Golden Ratio", "Golden Spiral"};
            }

            /// <summary>
            /// Initialize the editor window's default GUIStyle for text formatting.
            /// Properties such as font size and style can be optionally overridden.
            /// </summary>
            private void InitGUIStyle()
            {
                guiStyle = new GUIStyle();
                guiStyle.fontSize = 12;
                guiStyle.fontStyle = FontStyle.Bold;
                guiStyle.normal.textColor = Color.white;
            }

            /// <summary>
            /// Initialize the editor window's 2D textures for
            /// DrawLayouts()'s GUI.DrawTexture() method calls.
            /// </summary>
            public void InitEditorWindowTextures()
            {
                // Grid section
                gridSectionTexture = new Texture2D(1, 1);
                gridSectionTexture.SetPixel(0, 0, greyColor);
                gridSectionTexture.Apply();

                // Rotation section
                rotationSectionTexture = new Texture2D(1, 1);
                rotationSectionTexture.SetPixel(0, 0, greyColor);
                rotationSectionTexture.Apply();

                // Color section
                colorSectionTexture = new Texture2D(1, 1);
                colorSectionTexture.SetPixel(0, 0, greyColor);
                colorSectionTexture.Apply();

                // Opacity section
                opacitySectionTexture = new Texture2D(1, 1);
                opacitySectionTexture.SetPixel(0, 0, greyColor);
                opacitySectionTexture.Apply();

                // Tab section
                tabSectionTexture = new Texture2D(1, 1);
                tabSectionTexture.SetPixel(0, 0, greyColor);
                tabSectionTexture.Apply();

                // Override section
                overrideSectionTexture = new Texture2D(1, 1);
                overrideSectionTexture.SetPixel(0, 0, greyColor);
                overrideSectionTexture.Apply();

                // Camera section
                cameraSectionTexture = new Texture2D(1, 1);
                cameraSectionTexture.SetPixel(0, 0, greyColor);
                cameraSectionTexture.Apply();
            }

            /// <summary>
            /// Define Rect coordinates and draw GUI textures.
            /// </summary>
            public void DrawLayouts()
            {
                // Initialize the editor window's texture and colors.
                InitEditorWindowTextures();

                // Tab section
                tabSection.x = 0;
                tabSection.y = 0;
                tabSection.width = Screen.width;
                tabSection.height = Screen.height;
                GUI.DrawTexture(tabSection, tabSectionTexture);

                // Grid section
                gridSection.x = 0;
                gridSection.y = 30;
                gridSection.width = Screen.width;
                gridSection.height = Screen.height;
                GUI.DrawTexture(gridSection, gridSectionTexture);

                // Rotation section
                rotationSection.x = 0;
                rotationSection.y = 60;
                rotationSection.width = Screen.width;
                rotationSection.height = Screen.height;
                GUI.DrawTexture(rotationSection, rotationSectionTexture);

                // Color section
                colorSection.x = 0;
                colorSection.y = 70;
                colorSection.width = Screen.width;
                colorSection.height = Screen.height;
                GUI.DrawTexture(colorSection, colorSectionTexture);

                // Opacity section
                opacitySection.x = 0;
                opacitySection.y = 110;
                opacitySection.width = Screen.width;
                opacitySection.height = Screen.height;
                GUI.DrawTexture(opacitySection, opacitySectionTexture);

                // Override section
                overrideSection.x = 0;
                overrideSection.y = 148;
                overrideSection.width = Screen.width;
                overrideSection.height = Screen.height;
                GUI.DrawTexture(overrideSection, overrideSectionTexture);

                // Camera Section
                cameraSection.x = 0;
                cameraSection.y = 183;
                cameraSection.width = Screen.width;
                cameraSection.height = Screen.height;
                GUI.DrawTexture(cameraSection, cameraSectionTexture);
            }

            /// <summary>
            /// Draw the tab section.
            /// </summary>
            /// <param name="compositionObject"></param>
            public void DrawTabSection(GameObject compositionObject)
            {
                GUILayout.BeginArea(tabSection);
                GUILayout.Space(7);
                EditorGUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                tabIndex = GUILayout.Toolbar(tabIndex, tabNames);
                switch(tabIndex)
                {
                    // Rule of Thirds
                    case 0:
                        DrawGridSection(compositionObject.transform.GetChild(0).gameObject);
                        DrawColorSection(compositionObject.transform.GetChild(0).gameObject);
                        DrawOpacitySection(compositionObject.transform.GetChild(0).gameObject);
                        break;
                    // Diagonal
                    case 1:
                        DrawGridSection(compositionObject.transform.GetChild(1).gameObject);
                        DrawColorSection(compositionObject.transform.GetChild(1).gameObject);
                        DrawOpacitySection(compositionObject.transform.GetChild(1).gameObject);
                        break;
                    // Golden Ratio
                    case 2:
                        DrawGridSection(compositionObject.transform.GetChild(2).gameObject);
                        DrawColorSection(compositionObject.transform.GetChild(2).gameObject);
                        DrawOpacitySection(compositionObject.transform.GetChild(2).gameObject);
                        break;
                    // Golden Spiral
                    case 3:
                        DrawGridSection(compositionObject.transform.GetChild(3).gameObject);
                        DrawRotationSection(compositionObject.transform.GetChild(3).gameObject);
                        DrawColorSection(compositionObject.transform.GetChild(3).gameObject);
                        DrawOpacitySection(compositionObject.transform.GetChild(3).gameObject);
                        break;
                }

                EditorGUILayout.EndVertical();
                GUILayout.BeginHorizontal(); // Keep tabs constrained to editor window size.
                GUILayout.EndArea();
            }

            /// <summary>
            /// Draw contents of the grid Section.
            /// </summary>
            /// <param name="overlayObject"></param>
            public void DrawGridSection(GameObject overlayObject)
            {
                // Grid section font size and style overrides.
                guiStyle.fontSize = 12;
                guiStyle.fontStyle = FontStyle.Bold;

                GUILayout.BeginArea(gridSection);
                GUILayout.Label("  Grid", guiStyle);
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("On", GUILayout.Height(20), GUILayout.MaxWidth(181)))
                    overlayObject.GetComponent<CompositionOverlay>().Activate(true);

                GUI.backgroundColor = Color.grey;
                if (GUILayout.Button("Off", GUILayout.Height(20), GUILayout.MaxWidth(181)))
                    overlayObject.GetComponent<CompositionOverlay>().Activate(false);

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            /// <summary>
            /// Draw contents of the golden spiral rotation section.
            /// </summary>
            public void DrawRotationSection(GameObject overlayObject)
            {
                // Expand editor window sections to accomodate rotation buttons.
                if (overlayObject.name == "Golden Spiral")
                {
                    colorSection.y = 157;
                    opacitySection.y = 197;
                    overrideSection.y = 236;
                    cameraSection.y = 273;
                }

                // Rotation section font and style overrides.
                guiStyle.fontSize = 12;
                guiStyle.fontStyle = FontStyle.Bold;

                GUILayout.BeginArea(rotationSection);
                EditorGUILayout.Space(10);
                GUILayout.Label("  Rotation", guiStyle);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("\u27f8\nTop Left", GUILayout.Height(32), GUILayout.MaxWidth(180)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().Position("Top Left");
                    ToggleGrid(overlayObject);
                }

                if (GUILayout.Button("\u27f9\nTop Right", GUILayout.Height(32), GUILayout.MaxWidth(180)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().Position("Top Right");
                    ToggleGrid(overlayObject);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("\u27f8\nBottom Left", GUILayout.Height(32), GUILayout.MaxWidth(180)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().Position("Bottom Left");
                    ToggleGrid(overlayObject);
                }

                if (GUILayout.Button("\u27f9\nBottom Right", GUILayout.Height(32), GUILayout.MaxWidth(180)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().Position("Bottom Right");
                    ToggleGrid(overlayObject);
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            /// <summary>
            ///  Draw contents of the color selection section.
            /// </summary>
            public void DrawColorSection(GameObject overlayObject)
            {
                // Color section font size and style overrides.
                guiStyle.fontSize = 12;
                guiStyle.fontStyle = FontStyle.Bold;

                GUILayout.BeginArea(colorSection);
                GUILayout.Label("  Color", guiStyle);
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("", GUILayout.Height(20), GUILayout.MaxWidth(181)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().InvertLineColor(false);
                    ToggleGrid(overlayObject);
                }

                GUI.backgroundColor = Color.black;
                if (GUILayout.Button("", GUILayout.Height(20), GUILayout.MaxWidth(181)))
                {
                    overlayObject.GetComponent<CompositionOverlay>().InvertLineColor(true);
                    ToggleGrid(overlayObject);
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            /// <summary>
            /// Draw contents of the opacity section.
            /// </summary>
            /// <param name="overlayObject"></param>
            public void DrawOpacitySection(GameObject overlayObject)
            {
                GUILayout.BeginArea(opacitySection);
                GUILayout.Label("  Opacity", guiStyle);

                var opacity = overlayObject.GetComponent<CompositionOverlay>().Opacity;
                opacity = EditorGUI.Slider(new Rect(8, 18, 355, 20), opacity, 0f, 1f);
                overlayObject.GetComponent<CompositionOverlay>().UpdateOpacity(opacity);
                ToggleGrid(overlayObject);

                GUILayout.EndArea();
            }

            /// <summary>
            /// Draw contents of the overrides section.
            /// /// </summary>
            public void DrawOverrideSection(GameObject compositionObject)
            {
                GUILayout.BeginArea(overrideSection);
                GUILayout.Label("  Overrides", guiStyle);

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Turn Off All Grids", GUILayout.Height(20), GUILayout.MaxWidth(362)))
                {
                    foreach (Transform overlayObject in compositionObject.transform)
                    {
                        overlayObject.gameObject.GetComponent<CompositionOverlay>().Activate(false);
                    }
                }

                GUILayout.EndArea();
            }

            /// <summary>
            /// Draw contents of the camera section.
            /// </summary>
            public void DrawCameraSection()
            {
                GUILayout.BeginArea(cameraSection);
                EditorGUILayout.Space();

                GUILayout.Label("  Target Camera", guiStyle);
                GUI.backgroundColor = Color.cyan;
                targetCamera = (GameObject)EditorGUI.ObjectField(new Rect(6, 25, 357, 20),
                    targetCamera, typeof(GameObject), true);

                // Display a helpbox when a target camera has not been selected
                // in the editor window.
                if (targetCamera == null)
                {
                    const string targetCameraInfo = "HELP: Select a target camera to " +
                        "adjust position & rotation.";
                    GUI.TextArea(new Rect(6, 50, 357, 18), targetCameraInfo, 75,
                        GUI.skin.GetStyle("HelpBox"));
                    EditorGUILayout.Space(25);
                }

                EditorGUILayout.Space(28);

                // Hide position and rotation controls when the target camera
                // has not been selected.
                if (targetCamera != null)
                {
                    EditorGUI.BeginDisabledGroup(false);
                    GUILayout.Label("  Position", guiStyle);
                    Undo.RecordObject(targetCamera.transform, "Camera Transform Change");
                    targetCamera.transform.position = EditorGUILayout.Vector3Field("",
                        targetCamera.transform.position, GUILayout.MaxWidth(360));

                    GUILayout.Label("  Rotation", guiStyle);
                    Undo.RecordObject(targetCamera.transform, "Camera Transform Change");
                    var rotationVector3 = EditorGUILayout.Vector3Field("",
                        targetCamera.transform.rotation.eulerAngles, GUILayout.MaxWidth(360));
                    targetCamera.transform.rotation = Quaternion.Euler(rotationVector3);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space(1);
                EditorGUILayout.BeginHorizontal();

                // Hide "Align Camera With Scene View" and "Revert" buttons when a
                // target camera has not been selected.
                if (targetCamera != null)
                {
                    EditorGUI.BeginDisabledGroup(false);
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Align Camera With Scene View", GUILayout.Height(25), GUILayout.MaxWidth(276)))
                    {
                        // Save current Camera transform for revert operation.
                        revertCameraPosition = targetCamera.transform.position;
                        revertCameraRotation = targetCamera.transform.rotation;

                        // Get scene view transform and assign to the target camera. 
                        var sceneView = SceneView.lastActiveSceneView;
                        targetCamera.transform.position = sceneView.camera.transform.position;
                        targetCamera.transform.rotation = sceneView.camera.transform.rotation;
                    }

                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("Revert", GUILayout.Height(25), GUILayout.MaxWidth(84)))
                    {
                        // Apply original camera position and rotation transform
                        // values to the target camera.
                        if (targetCamera != null)
                        {
                            targetCamera.transform.position = revertCameraPosition;
                            targetCamera.transform.rotation = revertCameraRotation;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            /// <summary>
            /// Toggle grid when inverting line color and rotating overlays.
            /// </summary>
            private void ToggleGrid(GameObject overlayObject)
            {
                // Ensure composition overlay is active before toggling.
                if (overlayObject.GetComponent<CompositionOverlay>().IsActive)
                {
                    overlayObject.GetComponent<CompositionOverlay>().Activate(false);
                    overlayObject.GetComponent<CompositionOverlay>().Activate(true);
                }
            }
        }
    }
#endif