// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Components;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Shapes;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyUnderware.CurvyEditor
{
    public static class CurvyMenu
    {
        #region ### GameObject Menu ###

        [MenuItem(
            "GameObject/Curvy/Spline",
            false,
            0
        )]
        public static void CreateCurvySpline(MenuCommand cmd)
        {
            CurvySpline spline = Create<CurvySpline>(cmd);
            ApplyIncrementalNameToSpline(spline);
        }

        [MenuItem(
            "GameObject/Curvy/UI Spline",
            false,
            1
        )]
        public static void CreateCurvyUISpline(MenuCommand cmd)
        {
            GameObject parent = cmd.context as GameObject;
            if (!parent || parent.GetComponentInParent<Canvas>() == null)
            {
                Canvas canvas =
#if UNITY_6000_0_OR_NEWER
                    Object.FindFirstObjectByType<Canvas>();
#else
                    Object.FindObjectOfType<Canvas>();
#endif

                if (canvas)
                    parent = canvas.gameObject;
                else
                    //todo use UndoableCreateGameObject?
                    parent = new GameObject(
                        "Canvas",
                        typeof(Canvas)
                    );
            }

            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length > 0 && cmd.context == selectedGameObjects[0])
                Selection.activeObject = null;

            CurvyUISpline uiSpline;
            {
                const string gameObjectName = "UI Spline";
                uiSpline = CurvyUISpline.CreateUISpline(gameObjectName);
                GameObjectUtility.SetParentAndAlign(
                    uiSpline.gameObject,
                    parent
                );
                Undo.RegisterCreatedObjectUndo(
                    uiSpline.gameObject,
                    "Create " + gameObjectName
                );
            }

            DTSelection.AddGameObjects(uiSpline);
        }

        [MenuItem(
            "GameObject/Curvy/Generator",
            false,
            5
        )]
        public static void CreateCG(MenuCommand cmd)
        {
            CurvyGenerator generator = Create<CurvyGenerator>(cmd);
            ApplyIncrementalNameToGenerator(generator);
        }

        [MenuItem(
            "GameObject/Curvy/Controllers/Spline",
            false,
            10
        )]
        public static void CreateSplineController(MenuCommand cmd) => Create<SplineController>(cmd);

        [MenuItem(
            "GameObject/Curvy/Controllers/CG Path",
            false,
            12
        )]
        public static void CreatePathController(MenuCommand cmd) => Create<PathController>(cmd);

        [MenuItem(
            "GameObject/Curvy/Controllers/CG Volume",
            false,
            13
        )]
        public static void CreateVolumeController(MenuCommand cmd) => Create<VolumeController>(cmd);

        [MenuItem(
            "GameObject/Curvy/Controllers/UI Text Spline",
            false,
            14
        )]
        public static void CreateUITextSplineController(MenuCommand cmd) => Create<UITextSplineController>(cmd);

        [MenuItem(
            "GameObject/" + CurvyLineRenderer.ComponentPath,
            false,
            14
        )]
        public static void CreateCurvyLineRenderer(MenuCommand cmd) => Create<CurvyLineRenderer>(cmd);

        [MenuItem(
            "GameObject/" + CurvySplineToEdgeCollider2D.ComponentPath,
            false,
            14
        )]
        public static void CreateCurvySplineToEdgeCollider2D(MenuCommand cmd) => Create<CurvySplineToEdgeCollider2D>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Circle",
            false,
            14
        )]
        public static void CreateCSCircle(MenuCommand cmd) => Create<CSCircle>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Pie",
            false,
            14
        )]
        public static void CreateCSPie(MenuCommand cmd) => Create<CSPie>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Rectangle",
            false,
            14
        )]
        public static void CreateCSRectangle(MenuCommand cmd) => Create<CSRectangle>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Rounded Rectangle",
            false,
            14
        )]
        public static void CreateCSRoundedRectangle(MenuCommand cmd) => Create<CSRoundedRectangle>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Spiral",
            false,
            14
        )]
        public static void CreateCSSpiral(MenuCommand cmd) => Create<CSSpiral>(cmd);

        [MenuItem(
            "GameObject/Curvy/Shapes/Star",
            false,
            14
        )]
        public static void CreateCSStar(MenuCommand cmd) => Create<CSStar>(cmd);

        [MenuItem(
            "GameObject/Curvy/Misc/Nearest Spline Point",
            false,
            14
        )]
        public static void CreateNearestSplinePoint(MenuCommand cmd) => Create<NearestSplinePoint>(cmd);

        [MenuItem(
            "GameObject/Curvy/Misc/Curvy GL Renderer",
            false,
            14
        )]
        public static void CreateCurvyGLRenderer(MenuCommand cmd) => Create<CurvyGLRenderer>(cmd);


        private static T Create<T>(MenuCommand cmd) where T : MonoBehaviour
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length > 0 && cmd.context == selectedGameObjects[0])
                Selection.activeObject = null;
#pragma warning disable CS0612
            T createdObject = CreateCurvyObjectAsChild<T>(
                cmd.context,
                typeof(T).Name
            );
#pragma warning restore CS0612
            DTSelection.AddGameObjects(createdObject);
            return createdObject;
        }

        #endregion

        #region ### Project window Create Menu ###

        [MenuItem("Assets/Create/Curvy/CG Module")]
        public static void CreatePCGModule() =>
            ModuleWizard.Open();

        [MenuItem("Assets/Create/Curvy/Shape")]
        public static void CreateShape() =>
            ShapeWizard.Open();

        #endregion

        [Obsolete]
        public static T CreateCurvyObject<T>(Object parent, string name) where T : MonoBehaviour
        {
            GameObject go = parent as GameObject;
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(
                    go,
                    "Create " + name
                );
            }

            T obj = go.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(
                obj,
                "Create " + name
            );

            return obj;
        }

        [Obsolete]
        public static T CreateCurvyObjectAsChild<T>(Object parent, string name) where T : MonoBehaviour
        {
            GameObject go = new GameObject(name);
            T obj = go.AddComponent<T>();
            GameObjectUtility.SetParentAndAlign(
                go,
                parent as GameObject
            );
            Undo.RegisterCreatedObjectUndo(
                go,
                "Create " + name
            );

            return obj;
        }

        /// <summary>
        /// Rename the given spline to "Curvy Spline number_of_existing_splines"
        /// </summary>
        public static void ApplyIncrementalNameToSpline(CurvySpline spline) =>
            ApplyIncrementalName(
                spline,
                "Curvy Spline"
            );

        /// <summary>
        /// Rename the given generator to "Curvy Generator number_of_existing_generators"
        /// </summary>
        public static void ApplyIncrementalNameToGenerator(CurvyGenerator generator) =>
            ApplyIncrementalName(
                generator,
                "Curvy Generator"
            );

        /// <summary>
        /// Rename the given component to "<paramref name="baseName"/> number_of_existing_components_of_type_T"
        /// </summary>
        public static void ApplyIncrementalName<T>(T component, string baseName) where T : Component =>
            component.name = $"{baseName} {ObjectExt.FindObjectsOfTypeInCurrentStage<T>( false, false ).Length}";
    }
}