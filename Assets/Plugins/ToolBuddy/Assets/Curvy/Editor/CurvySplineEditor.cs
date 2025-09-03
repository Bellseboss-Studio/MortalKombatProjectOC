// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Globalization;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CustomEditor(typeof(CurvySpline)), CanEditMultipleObjects]
    public class CurvySplineEditor : CurvyEditorBase<CurvySpline>
    {
        private SerializedProperty tT;
        private SerializedProperty tC;
        private SerializedProperty tB;

        private static readonly GUIStyle GuiStyle = new GUIStyle();

        [UsedImplicitly]
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void DrawTextGizmos(CurvySpline spline, GizmoType context)
        {
            bool drawLabels = CurvyGlobalManager.ShowLabelsGizmo && spline.ShowGizmos;
            bool drawRelativeDistance = CurvyGlobalManager.ShowRelativeDistancesGizmo && spline.ShowGizmos;
            bool drawTF = CurvyGlobalManager.ShowTFsGizmo && spline.ShowGizmos;

            if ((drawTF || drawRelativeDistance || drawLabels) == false)
                return;

            if (spline.Dirty)
                return;

            Bounds splineBounds = spline.Bounds;
            Camera camera = Camera.current;

            float alpha;
            {
                if (CurvyProject.Instance.AutoFadeLabels)
                {
                    Matrix4x4 m = spline.transform.localToWorldMatrix;
                    Vector2 min = new Vector2(
                        float.MaxValue,
                        float.MaxValue
                    );
                    Vector2 max = new Vector2(
                        float.MinValue,
                        float.MinValue
                    );
                    int height = Screen.height;

                    for (int index = 0; index < spline.Count; index++)
                    {
                        CurvySplineSegment curvySplineSegment = spline[index];
                        Vector2 segmentMin = new Vector2(
                            float.MaxValue,
                            float.MaxValue
                        );
                        Vector2 segmentMax = new Vector2(
                            float.MinValue,
                            float.MinValue
                        );
                        SubArray<Vector3> positions = curvySplineSegment.PositionsApproximation;
                        for (int i = 0; i < positions.Count; i++)
                        {
                            // World space 
                            Vector3 p = m.MultiplyPoint3x4(positions.Array[i]);
                            // GUI space 
                            p = camera.WorldToScreenPoint(
                                p,
                                Camera.MonoOrStereoscopicEye.Mono
                            );
                            p.y = height - p.y;

                            segmentMin.x = Mathf.Min(
                                segmentMin.x,
                                p.x
                            );
                            segmentMin.y = Mathf.Min(
                                segmentMin.y,
                                p.y
                            );
                            segmentMax.x = Mathf.Max(
                                segmentMax.x,
                                p.x
                            );
                            segmentMax.y = Mathf.Max(
                                segmentMax.y,
                                p.y
                            );
                        }

                        min.x = Mathf.Min(
                            min.x,
                            segmentMin.x
                        );
                        min.y = Mathf.Min(
                            min.y,
                            segmentMin.y
                        );
                        max.x = Mathf.Max(
                            max.x,
                            segmentMax.x
                        );
                        max.y = Mathf.Max(
                            max.y,
                            segmentMax.y
                        );
                    }

                    Rect screenBounds = Rect.MinMaxRect(
                        min.x,
                        min.y,
                        max.x,
                        max.y
                    );
                    float maxBoundLength = Mathf.Max(
                        screenBounds.width / Screen.width,
                        screenBounds.height / Screen.height
                    );
                    alpha = Mathf.Clamp01((maxBoundLength - 0.02f) / (0.07f - 0.02f));
                }
                else
                    alpha = 1;
            }

            Color color = spline.GizmoColor * 1.3f;

            bool isSplineClosed = spline.Closed;
            float handleSizeMultiplier = 0.08f;
            Quaternion cameraRotation = Camera.current.transform.rotation;

            lock (GuiStyle)
            {
                GuiStyle.fontSize = 11;
                GuiStyle.alignment = TextAnchor.MiddleCenter;

                if (drawTF && alpha != 0)
                {
                    GuiStyle.normal.textColor = Handles.color = new Color(
                        color.r * 0.85f,
                        color.g * 0.85f,
                        (color.b * 0.85f) + 0.15f,
                        alpha
                    );

                    //first point and last point for closed splines
                    {
                        Vector3 worldPoint = spline.Interpolate(
                            0,
                            Space.World
                        );
                        float handleSize = HandleUtility.GetHandleSize(worldPoint);
                        DrawPointHandle(
                            worldPoint,
                            handleSize * handleSizeMultiplier,
                            cameraRotation
                        );
#pragma warning disable CS0618
                        CurvyGizmo.PointLabel(
                            worldPoint,
                            $"TF : {(spline.Closed ? "0 / 1" : "0")}",
#pragma warning restore CS0618
                            OrientationAxisEnum.Right,
                            handleSize,
                            GuiStyle
                        );
                    }

                    for (int i = 1;
                         i
                         <= (isSplineClosed
                             ? 9
                             : 10);
                         i++)
                    {
                        float tf = (float)i / 10;
                        Vector3 worldPoint = spline.Interpolate(
                            tf,
                            Space.World
                        );
                        float handleSize = HandleUtility.GetHandleSize(worldPoint);
                        DrawPointHandle(
                            worldPoint,
                            handleSize * handleSizeMultiplier,
                            cameraRotation
                        );
#pragma warning disable CS0618
                        CurvyGizmo.PointLabel(
                            worldPoint,
                            $"TF : {tf}",
                            OrientationAxisEnum.Right,
                            handleSize,
                            GuiStyle
                        );
#pragma warning restore CS0618
                    }
                }

                if (drawRelativeDistance && alpha != 0)
                {
                    GuiStyle.normal.textColor = Handles.color = new Color(
                        color.r * 0.85f,
                        (color.g * 0.85f) + 0.15f,
                        color.b * 0.85f,
                        alpha
                    );

                    //first point and last point for closed splines
                    {
                        Vector3 worldPoint = spline.InterpolateByDistance(
                            0,
                            Space.World
                        );
                        float handleSize = HandleUtility.GetHandleSize(worldPoint);
                        DrawPointHandle(
                            worldPoint,
                            handleSize * handleSizeMultiplier,
                            cameraRotation
                        );
#pragma warning disable CS0618
                        CurvyGizmo.PointLabel(
                            worldPoint,
                            $"RD : {(spline.Closed ? "0 / 1" : "0")}",
                            OrientationAxisEnum.Left,
                            handleSize,
                            GuiStyle
                        );
#pragma warning restore CS0618
                    }

                    for (int i = 1;
                         i
                         <= (isSplineClosed
                             ? 9
                             : 10);
                         i++)
                    {
                        float relativeDistance = (float)i / 10;
                        Vector3 worldPoint = spline.InterpolateByDistance(
                            spline.Length * relativeDistance,
                            Space.World
                        );
                        float handleSize = HandleUtility.GetHandleSize(worldPoint);
                        DrawPointHandle(
                            worldPoint,
                            handleSize * handleSizeMultiplier,
                            cameraRotation
                        );
#pragma warning disable CS0618
                        CurvyGizmo.PointLabel(
                            worldPoint,
                            $"RD : {relativeDistance}",
                            OrientationAxisEnum.Left,
                            handleSize,
                            GuiStyle
                        );
#pragma warning restore CS0618
                    }
                }

                if (drawLabels)
                {
                    GuiStyle.normal.textColor = color;
#pragma warning disable CS0618
                    CurvyGizmo.PointLabel(
                        splineBounds.center,
                        spline.name,
                        OrientationAxisEnum.Down,
                        null,
                        GuiStyle
                    );
#pragma warning restore CS0618

                    if (alpha != 0)
                    {
                        GuiStyle.normal.textColor = new Color(
                            (color.r * 0.85f) + 0.15f,
                            color.g * 0.85f,
                            color.b * 0.85f,
                            alpha
                        );
                        foreach (CurvySplineSegment cp in spline.ControlPointsList)
#pragma warning disable CS0618
                            CurvyGizmo.PointLabel(
                                cp.transform.position,
                                cp.name,
                                OrientationAxisEnum.Up,
                                null,
                                GuiStyle
                            );
#pragma warning restore CS0618
                    }
                }
            }
        }

        private static void DrawPointHandle(Vector3 worldPoint, float handleSize, Quaternion cameraRotation)
        {
            Vector3 axis1 = cameraRotation * Vector3.right;
            Vector3 axis2 = cameraRotation * Vector3.up;
            Handles.DrawLine(
                worldPoint + (axis1 * handleSize),
                worldPoint + (axis1 * -handleSize)
                ,
                1.5f
            );
            Handles.DrawLine(
                worldPoint + (axis2 * handleSize),
                worldPoint + (axis2 * -handleSize)
                ,
                1.5f
            );

            //Handles.DrawWireDisc(worldPoint, cameraRotation * Vector3.forward, handleSize);
        }

        #region BSpline

        [UsedImplicitly]
        [DrawGizmo(GizmoType.NonSelected)]
        private static void DrawBSplineAdditionalGizmo_NonSelected(CurvySpline spline, GizmoType context)
        {
            if (spline == null)
                return;
            if (spline.Interpolation != CurvyInterpolation.BSpline)
                return;
            if (spline.ShowGizmos == false)
                return;

            DrawBSplineAdditionalGizmo(spline,
                spline.GizmoColor * 0.9f
            );
        }

        [UsedImplicitly]
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy)]
        private static void DrawBSplineAdditionalGizmo_Selected(
            CurvySpline spline,
            GizmoType context)
        {
            if (spline == null)
                return;
            if (spline.Interpolation != CurvyInterpolation.BSpline)
                return;

            DrawBSplineAdditionalGizmo(
                spline,
                spline.GizmoSelectionColor * 0.9f
            );
        }

        private static void DrawBSplineAdditionalGizmo(
            CurvySpline spline,
            Color gizmoColor)
        {
            Handles.color = gizmoColor;
            foreach (CurvySplineSegment cp in spline.ControlPointsList)
                if (spline.IsControlPointASegment(cp))
                    Handles.DrawDottedLine(
                        cp.transform.position,
                        spline.GetNextControlPoint(cp).transform.position,
                        4
                    );
        }

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            tT = serializedObject.FindProperty("m_Tension");
            tC = serializedObject.FindProperty("m_Continuity");
            tB = serializedObject.FindProperty("m_Bias");
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            // Bounds
            bool targetIsNotNull = Target != null;
            if (targetIsNotNull && Target.IsInitialized && CurvyGlobalManager.ShowBoundsGizmo)
            {
                DTHandles.PushHandlesColor(
                    new Color(
                        0.3f,
                        0,
                        0
                    )
                );
                DTHandles.WireCubeCap(
                    Target.Bounds.center,
                    Target.Bounds.size
                );
                DTHandles.PopHandlesColor();
            }

            // Snap Transform
            if (targetIsNotNull && DT._UseSnapValuePrecision)
            {
                Target.transform.localPosition = DTMath.SnapPrecision(
                    Target.transform.localPosition,
                    3
                );
                Target.transform.localEulerAngles = DTMath.SnapPrecision(
                    Target.transform.localEulerAngles,
                    3
                );
            }
        }


        [UsedImplicitly]
        private void TCBOptionsGUI()
        {
            if (Target == null)
                return;
            if (Target.Interpolation != CurvyInterpolation.TCB)
                return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent(
                        "Set Catmul",
                        "Set TCB to match Catmul Rom"
                    )
                ))
            {
                tT.floatValue = 0;
                tC.floatValue = 0;
                tB.floatValue = 0;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Set Cubic",
                        "Set TCB to match Simple Cubic"
                    )
                ))
            {
                tT.floatValue = -1;
                tC.floatValue = 0;
                tB.floatValue = 0;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Set Linear",
                        "Set TCB to match Linear"
                    )
                ))
            {
                tT.floatValue = 0;
                tC.floatValue = -1;
                tB.floatValue = 0;
            }

            EditorGUILayout.EndHorizontal();
        }

        [UsedImplicitly]
        private void ShowGizmoGUI() =>
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CurvySpline.ShowGizmos)));

        [UsedImplicitly]
        private void CheckGizmoColor()
        {
            if (Target == null)
                return;
            if (!Target.GizmoColor.a.Approximately(0f))
                return;
            EditorGUILayout.HelpBox(
                "Color is transparent (alpha value set to 0).",
                MessageType.Warning
            );
        }

        [UsedImplicitly]
        private void CheckGizmoSelectionColor()
        {
            if (Target == null)
                return;
            if (!Target.GizmoSelectionColor.a.Approximately(0f))
                return;
            EditorGUILayout.HelpBox(
                "Active Color is transparent (alpha value set to 0).",
                MessageType.Warning
            );
        }

        [UsedImplicitly]
        private void CBCheck2DPlanar()
        {
            if (Target == null)
                return;
            if (!Target.RestrictTo2D)
                return;
            if (Target.IsPlanar(Target.Restricted2DPlane))
                return;

            EditorGUILayout.HelpBox(
                "The spline isn't restricted to the selected plane. Click the button below to correct this.",
                MessageType.Warning
            );
            if (GUILayout.Button("Make planar"))
                Target.MakePlanar(Target.Restricted2DPlane);
        }

        protected override void OnCustomInspectorGUI()
        {
            base.OnCustomInspectorGUI();
            if (Target == null)
                return;
            GUILayout.Space(5);
            if (Target.ControlPointsList.Count == 0)
                EditorGUILayout.HelpBox(
                    "To add Control Points to your curve, please use the Toolbar in the SceneView window",
                    MessageType.Warning
                );
            if (Target.IsInitialized == false)
                EditorGUILayout.HelpBox(
                    "Spline is not initialized",
                    MessageType.Info
                );
            else if (Target.Dirty)
                EditorGUILayout.HelpBox(
                    "Spline is dirty",
                    MessageType.Info
                );
            else
                EditorGUILayout.HelpBox(
                    "Control Points: "
                    + Target.ControlPointCount.ToString(CultureInfo.InvariantCulture)
                    + "\nSegments: "
                    + Target.Count.ToString(CultureInfo.InvariantCulture)
                    + "\nLength: "
                    + Target.Length.ToString(CultureInfo.InvariantCulture)
                    + "\nCache Points: "
                    + Target.CacheSize.ToString(CultureInfo.InvariantCulture)
                    ,
                    MessageType.Info
                );
        }
    }
}