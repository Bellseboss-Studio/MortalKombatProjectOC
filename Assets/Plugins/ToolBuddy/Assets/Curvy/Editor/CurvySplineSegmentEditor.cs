// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CustomEditor(typeof(CurvySplineSegment)), CanEditMultipleObjects]
    public class CurvySplineSegmentEditor : CurvyEditorBase<CurvySplineSegment>
    {
        private SerializedProperty tT0;
        private SerializedProperty tB0;
        private SerializedProperty tC0;
        private SerializedProperty tT1;
        private SerializedProperty tB1;
        private SerializedProperty tC1;
        private SerializedProperty tOT;
        private SerializedProperty tOB;
        private SerializedProperty tOC;


        private Quaternion mBezierRot;

        private EditorKeyBinding hkToggleBezierAutoHandles;

        private CurvyConnectionEditor mConnectionEditor;

        [UsedImplicitly]
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void DrawOrientationAnchorGizmo(CurvySplineSegment controlPoint, GizmoType context)
        {
            CurvySpline spline = controlPoint.Spline;
            if (spline == null)
                return;
            if (!spline.ShowGizmos)
                return;
            if (!CurvyGlobalManager.ShowOrientationAnchorsGizmo)
                return;
            if (!spline.IsControlPointAnOrientationAnchor(controlPoint))
                return;

            Handles.color = spline.GizmoColor * 1.1f;
            Camera camera = Camera.current;
            Vector3 xAxis = -camera.transform.right;
            Vector3 yAxis = -camera.transform.up;
            Vector3 zAxis = -camera.transform.forward;

            Vector3 cpPosition = controlPoint.transform.position;
            float handleSize = HandleUtility.GetHandleSize(cpPosition);

            float outerRadius = handleSize * 0.12f;
            float innerRadius = outerRadius * 0.7f;

            Vector3 anchorPosition = controlPoint.transform.position + (yAxis * handleSize * 0.3f);

            //englobing circle
            Handles.DrawWireDisc(
                anchorPosition,
                zAxis,
                outerRadius
            );

            Vector3 neckPosition = anchorPosition - (yAxis * innerRadius * 0.7f);
            //head
            float headSize = outerRadius * 0.1f;
            Handles.DrawSolidDisc(
                neckPosition - (headSize * yAxis),
                zAxis,
                headSize
            );

            //trunk
            Handles.DrawLine(
                neckPosition,
                anchorPosition + (yAxis * innerRadius)
            );

            //arms
            Vector3 armsVerticalPosition = anchorPosition - (yAxis * innerRadius * 0.25f);
            float armsLength = innerRadius * 0.6f;
            Handles.DrawLine(
                armsVerticalPosition - (xAxis * armsLength),
                armsVerticalPosition + (xAxis * armsLength)
            );

            //feet
            float arcStartAngle = Mathf.Deg2Rad * -80f;
            Handles.DrawWireArc(
                anchorPosition,
                zAxis,
                (xAxis * Mathf.Sin(arcStartAngle)) + (yAxis * Mathf.Cos(arcStartAngle)),
                160,
                innerRadius
            );
        }

        private void CheckHotkeys(CurvyInterpolation splineInterpolation)
        {
            if (splineInterpolation == CurvyInterpolation.Bezier && hkToggleBezierAutoHandles.IsTriggered(Event.current))
                DTSelection.GetAllAs<CurvySplineSegment>().ForEach(seg => { seg.AutoHandles = !seg.AutoHandles; });
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            hkToggleBezierAutoHandles =
                CurvyProject.Instance.RegisterKeyBinding(
                    new EditorKeyBinding(
                        "Bezier: Toggle AutoHandles",
                        "",
                        KeyCode.H
                    )
                );

            tT0 = serializedObject.FindProperty("m_StartTension");
            tC0 = serializedObject.FindProperty("m_StartContinuity");
            tB0 = serializedObject.FindProperty("m_StartBias");
            tT1 = serializedObject.FindProperty("m_EndTension");
            tC1 = serializedObject.FindProperty("m_EndContinuity");
            tB1 = serializedObject.FindProperty("m_EndBias");
            tOT = serializedObject.FindProperty("m_OverrideGlobalTension");
            tOC = serializedObject.FindProperty("m_OverrideGlobalContinuity");
            tOB = serializedObject.FindProperty("m_OverrideGlobalBias");

            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;

            mBezierRot = Quaternion.identity;
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            DestroyImmediate(mConnectionEditor);
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            // just in case
            Tools.hidden = false;
        }

        protected override void OnReadNodes()
        {
            if (mConnectionEditor)
                return;
            if (Target == null)
                return;
            if (Target.Connection == null)
                return;
            // ensure all selected CPs join a single connection. Otherwise don't show Connections Inspector
            if (serializedObject.FindProperty("m_Connection").hasMultipleDifferentValues)
                return;

            mConnectionEditor = (CurvyConnectionEditor)CreateEditor(
                Target.Connection,
                typeof(CurvyConnectionEditor)
            );
            DTGroupNode sec = Node.AddSection(
                "Connection",
                ConnectionGUI
            );
            if (sec)
            {
                sec.SortOrder = 1;
                sec.HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyconnection";
            }
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Target == null)
                return;

            ShowBoundsIfNeeded(Target.Bounds);

            if (Target.Spline == null)
                return;

            CurvySpline targetSpline = Target.Spline;

            CheckHotkeys(targetSpline.Interpolation);

            if (targetSpline.RestrictTo2D && Tools.current == Tool.Move && !SceneView.currentDrawingSceneView.in2DMode)
            {
                Tools.hidden = true;
                Vector3 handlePos = Tools.handlePosition != Target.transform.position
                    ? DTSelection.GetPosition()
                    : Tools.handlePosition;
                Vector3 delta;
                EditorGUI.BeginChangeCheck();


                if (CurvyProject.Instance.UseTiny2DHandles)
                    delta = DTHandles.TinyHandle2D(
                                GUIUtility.GetControlID(
                                    GetHashCode(),
                                    FocusType.Passive
                                ),
                                handlePos,
                                targetSpline.transform.rotation,
                                1
                            )
                            - handlePos;
                else
                    delta = DTHandles.PositionHandle2D(
                                GUIUtility.GetControlID(
                                    GetHashCode(),
                                    FocusType.Passive
                                ),
                                handlePos,
                                targetSpline.transform.rotation,
                                1,
                                (int)targetSpline.Restricted2DPlane
                            )
                            - handlePos;

                if (EditorGUI.EndChangeCheck())
                    foreach (Transform t in Selection.transforms)
                    {
                        Undo.ClearUndo(t);
                        Undo.RecordObject(
                            t,
                            "Move"
                        );
                        t.position += delta;
                    }
            }
            else
                Tools.hidden = false;


            // Bezier-Handles
            if (targetSpline && targetSpline.Interpolation == CurvyInterpolation.Bezier && !Target.AutoHandles)
            {
                float positionHandleSize = .6f;
                float cubeSize = CurvyGlobalManager.GizmoControlPointSize * 0.6f;

                bool hasSynchronizedHandles;
                {
                    CurvyBezierModeEnum mode = CurvyProject.Instance.BezierMode;
                    bool syncDirections = (mode & CurvyBezierModeEnum.Direction) == CurvyBezierModeEnum.Direction;
                    bool syncLengths = (mode & CurvyBezierModeEnum.Length) == CurvyBezierModeEnum.Length;
                    bool syncConnectedCPs = (mode & CurvyBezierModeEnum.Connections) == CurvyBezierModeEnum.Connections;

                    hasSynchronizedHandles =
                        syncConnectedCPs
                        && (syncDirections || syncLengths)
                        && Target.Connection
                        && Target.Connection.ControlPointsList.Any(cp => cp != Target);
                }

                #region --- Bezier Rotation Handling ---

                if (Tools.current == Tool.Rotate)
                {
                    Event e = Event.current;
                    if (e.shift && DTHandles.MouseOverSceneView)
                    {
                        Tools.hidden = true;
                        DTToolbarItem._StatusBar.Set("BezierRotate");
                        // This looks good, but then diff down below isn't working like intended:
                        //mBezierRot = Quaternion.LookRotation(Vector3.Cross(Target.HandleIn, Target.GetOrientationUpFast(0)), Target.GetOrientationUpFast(0));
                        Quaternion newRot = Handles.RotationHandle(
                            mBezierRot,
                            Target.transform.position
                        );
                        if (newRot != mBezierRot)
                        {
                            Quaternion diff = Quaternion.Inverse(mBezierRot) * newRot;
                            mBezierRot = newRot;
                            CurvyBezierModeEnum mode = CurvyProject.Instance.BezierMode;

                            //Undo handling
                            {
                                string undoingOperationLabel = "Rotate Handles";
                                Undo.RecordObject(
                                    Target,
                                    undoingOperationLabel
                                );
                                if (hasSynchronizedHandles)
                                    Undo.RecordObjects(
                                        Target.Connection.ControlPointsList.Where(cp => cp != Target).Select(cp => (Object)cp)
                                            .ToArray()
                                        ,
                                        undoingOperationLabel
                                    );
                            }

                            Target.SetBezierHandleIn(
                                diff * Target.HandleIn,
                                Space.Self,
                                mode
                            );
                            Target.SetBezierHandleOut(
                                diff * Target.HandleOut,
                                Space.Self,
                                mode
                            );
                            EditorUtility.SetDirty(Target);
                        }
                    }
                    else
                    {
                        DTToolbarItem._StatusBar.Set(
                            "Hold <b>[Shift]</b> to rotate Handles",
                            "BezierRotate"
                        );
                        Tools.hidden = false;
                    }
                }
                else
                    DTToolbarItem._StatusBar.Set("BezierRotate");

                #endregion

                #region --- Bezier Movement Handling ---

                //Belongs to Constraint Length:
                //Vector3 handleOut = Target.HandleOutPosition;
                //Vector3 handleIn = Target.HandleInPosition;

                EditorGUI.BeginChangeCheck();
                Vector3 pOut;
                Vector3 pIn;
                bool chgOut = false;
                bool chgIn = false;
                if (targetSpline.RestrictTo2D)
                {
                    Quaternion r = Tools.pivotRotation == PivotRotation.Global
                        ? targetSpline.transform.localRotation
                        : targetSpline.transform.rotation;
                    Handles.color = Color.yellow;
                    pIn = Target.HandleInPosition;
                    pIn = DTHandles.TinyHandle2D(
                        GUIUtility.GetControlID(FocusType.Passive),
                        pIn,
                        r,
                        cubeSize,
                        Handles.CubeHandleCap
                    );

                    if (!CurvyProject.Instance.UseTiny2DHandles)
                        pIn = DTHandles.PositionHandle2D(
                            GUIUtility.GetControlID(FocusType.Passive),
                            pIn,
                            r,
                            positionHandleSize,
                            (int)targetSpline.Restricted2DPlane
                        );
                    chgIn = Target.HandleInPosition != pIn;
                    Handles.color = Color.green;
                    pOut = Target.HandleOutPosition;
                    pOut = DTHandles.TinyHandle2D(
                        GUIUtility.GetControlID(FocusType.Passive),
                        pOut,
                        r,
                        cubeSize,
                        Handles.CubeHandleCap
                    );

                    if (!CurvyProject.Instance.UseTiny2DHandles)
                        pOut = DTHandles.PositionHandle2D(
                            GUIUtility.GetControlID(FocusType.Passive),
                            pOut,
                            r,
                            positionHandleSize,
                            (int)targetSpline.Restricted2DPlane
                        );

                    chgOut = Target.HandleOutPosition != pOut;
                }
                else
                {
                    Color handlesGizmoAdditionalColor = targetSpline.GizmoSelectionColor * 0.4f;

                    pIn = DTHandles.PositionHandle(
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        Target.HandleInPosition,
                        Tools.handleRotation,
                        positionHandleSize,
                        0.2f,
                        handlesGizmoAdditionalColor
                    );
                    chgIn = Target.HandleInPosition != pIn;
                    DTHandles.PushHandlesColor(Color.yellow);
                    Handles.CubeHandleCap(
                        0,
                        pIn,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(pIn) * cubeSize,
                        EventType.Repaint
                    );
                    DTHandles.PopHandlesColor();

                    pOut = DTHandles.PositionHandle(
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        GUIUtility.GetControlID(
                            GetHashCode(),
                            FocusType.Passive
                        ),
                        Target.HandleOutPosition,
                        Tools.handleRotation,
                        positionHandleSize,
                        0.2f,
                        handlesGizmoAdditionalColor
                    );
                    chgOut = Target.HandleOutPosition != pOut;
                    DTHandles.PushHandlesColor(Color.green);
                    Handles.CubeHandleCap(
                        0,
                        pOut,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(pOut) * cubeSize,
                        EventType.Repaint
                    );

                    DTHandles.PopHandlesColor();
                }

                Handles.color = Color.yellow;
                Handles.DrawLine(
                    pIn,
                    Target.transform.position
                );
                Handles.color = Color.green;
                Handles.DrawLine(
                    pOut,
                    Target.transform.position
                );


                if (chgOut || chgIn)
                {
                    //Undo handling
                    {
                        const string undoingOperationLabel = "Move Handle";
                        Undo.RecordObject(
                            Target,
                            undoingOperationLabel
                        );
                        if (hasSynchronizedHandles)
                            Undo.RecordObjects(
                                Target.Connection.ControlPointsList.Where(cp => cp != Target).Select(cp => (Object)cp).ToArray()
                                ,
                                undoingOperationLabel
                            );
                    }

                    if (chgOut)
                    {
                        Target.SetBezierHandleOut(
                            pOut,
                            Space.World,
                            CurvyProject.Instance.BezierMode
                        );
                        Target.HandleOut = DTMath.SnapPrecision(
                            Target.HandleOut,
                            3
                        );
                    }
                    else
                    {
                        Target.SetBezierHandleIn(
                            pIn,
                            Space.World,
                            CurvyProject.Instance.BezierMode
                        );
                        Target.HandleIn = DTMath.SnapPrecision(
                            Target.HandleIn,
                            3
                        );
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(Target);
                    targetSpline.SetDirty(
                        Target,
                        SplineDirtyingType.Everything
                    );
                    /*
                    var lcons = CurvyProject.Instance.FindItem<TBCPLengthConstraint>();
                    if (lcons.On && Target.Spline.Length > lcons.MaxSplineLength)
                    {
                        Target.HandleOutPosition = handleOut;
                        Target.HandleInPosition = handleIn;
                        Target.SetDirty();
                    }
                     */
                }

                #endregion
            }

            if (mConnectionEditor)
                mConnectionEditor.OnSceneGUI();
            // Snap Transform
            if (Target && DT._UseSnapValuePrecision)
            {
                Target.transform.localPosition = DTMath.SnapPrecision(
                    Target.transform.localPosition,
                    3
                );
                Target.transform.localEulerAngles = DTMath.SnapPrecision(
                    Target.transform.localEulerAngles,
                    3
                );
                //Target.TTransform.FromTransform(Target.transform);
            }
        }

        private static void ShowBoundsIfNeeded(Bounds bounds)
        {
            if (!CurvyGlobalManager.ShowBoundsGizmo)
                return;

            DTHandles.PushHandlesColor(Color.gray);
            DTHandles.WireCubeCap(
                bounds.center,
                bounds.size
            );
            DTHandles.PopHandlesColor();
        }

        private void OnHierarchyWindowItemOnGUI(int instanceid, Rect selectionrect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceid) as GameObject;

            if (!obj)
                return;
            if (obj != Selection.activeObject)
                return;
            if (Target == null)
                return;
            if (Target.Spline == null)
                return;

            CheckHotkeys(Target.Spline.Interpolation);
        }


        [UsedImplicitly]
        private void TCBOptionsGUI()
        {
            if (Target == null)
                return;
            if (Target.Spline == null)
                return;
            if (Target.Spline.Interpolation != CurvyInterpolation.TCB)
                return;
            if (!tOT.boolValue && !tOC.boolValue && !tOB.boolValue)
                return;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Set Catmul"))
            {
                tT0.floatValue = 0;
                tC0.floatValue = 0;
                tB0.floatValue = 0;
                tT1.floatValue = 0;
                tC1.floatValue = 0;
                tB1.floatValue = 0;
            }

            if (GUILayout.Button("Set Cubic"))
            {
                tT0.floatValue = -1;
                tC0.floatValue = 0;
                tB0.floatValue = 0;
                tT1.floatValue = -1;
                tC1.floatValue = 0;
                tB1.floatValue = 0;
            }

            if (GUILayout.Button("Set Linear"))
            {
                tT0.floatValue = 0;
                tC0.floatValue = -1;
                tB0.floatValue = 0;
                tT1.floatValue = 0;
                tC1.floatValue = -1;
                tB1.floatValue = 0;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ConnectionGUI(DTInspectorNode node)
        {
            if (Target == null)
                return;
            if (Target.Connection == null)
                return;
            if (mConnectionEditor == null)
                return;

            EditorGUILayout.BeginHorizontal();
            Target.Connection.name = EditorGUILayout.TextField(
                "Name",
                Target.Connection.name
            );
            if (GUILayout.Button(
                    new GUIContent(
                        "Select",
                        "Select the Connection GameObject"
                    )
                ))
                DTSelection.SetGameObjects(Target.Connection);
            EditorGUILayout.EndHorizontal();
            mConnectionEditor.OnInspectorGUI();
        }

        protected override void OnCustomInspectorGUI()
        {
            if (Target == null)
                return;

            if (Target.Spline == null)
                return;

            CurvySpline spline = Target.Spline;

            GUILayout.Space(5);

            if (spline && spline.IsInitialized && spline.Dirty == false)
                EditorGUILayout.HelpBox(
                    "Control Point Distance: "
                    + Target.Distance
                    + "\nSpline Length: "
                    + spline.Length
                    + "\nControl Point Relative Distance: "
                    + (Target.Distance / spline.Length)
                    + " (= Distance / Length)"
                    + "\nControl Point TF: "
                    + Target.TF
                    + "\nSegment Length: "
                    + Target.Length
                    + "\nSegment Cache Points: "
                    + Target.CacheSize,
                    MessageType.Info
                );
            else
                EditorGUILayout.HelpBox(
                    "No parent spline or parent spline not ready",
                    MessageType.Info
                );

            CheckHotkeys(spline.Interpolation);
        }


        public override void OnInspectorGUI()
        {
            if (mConnectionEditor
                && mConnectionEditor.Target
                == null) //This happens when deleting a connection from the CurvySplineSegment inspector, and then re-adding a connection while still showing the same inspector
                mConnectionEditor = null;

            if (Target != null && Target.Connection && mConnectionEditor == null)
                ReadNodes();
            base.OnInspectorGUI();
        }

        [UsedImplicitly]
        private void CBBakeOrientation()
        {
            if (Target != null
                && !Target.AutoBakeOrientation
                && Target.Spline
                && Target.UpsApproximation.Count > 0
                && GUILayout.Button("Bake Orientation to Transform"))
            {
                Undo.RecordObject(
                    Target.transform,
                    "Bake Orientation"
                );
                Target.BakeOrientationToTransform();
            }

            GUI.enabled = true;
        }
    }
}