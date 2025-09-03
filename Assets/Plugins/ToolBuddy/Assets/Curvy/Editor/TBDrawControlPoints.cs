// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        32,
        "Curvy",
        "Draw Spline",
        "Draw Splines",
        "draw,24,24"
    )]
    public class TBDrawControlPoints : DTToolbarToggleButton
    {
        private enum RayCastPlane
        {
            Undefinded,
            LocalXY,
            GlobalXY,
            LocalYZ,
            GlobalYZ,
            LocalXZ,
            GlobalXZ,
            ViewPlane
        }

        private RayCastPlane rayCastPlane = RayCastPlane.LocalXZ;

        public override string StatusBarInfo => "Create splines and Control Points";

        private enum ModeEnum
        {
            None = 0,
            Add = 1,
            Pick = 2
        }

        private ModeEnum Mode = ModeEnum.None;

        private CurvySplineSegment selectedCP;
        private CurvySpline selectedSpline;

        public TBDrawControlPoints() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Toggle Draw Mode",
                    "",
                    KeyCode.Space
                )
            );


        public override void HandleEvents(Event e)
        {
            base.HandleEvents(e);
            if (On && DTHandles.MouseOverSceneView)
            {
                Mode = ModeEnum.None;
                if (e.control && !e.alt) // Prevent that Panning (CTRL+ALT+LMB) creates CP'S
                {
                    Mode = ModeEnum.Add;
                    if (e.shift)
                        Mode |= ModeEnum.Pick;
                }

                if (e.type == EventType.MouseDown)
                    if (Mode.HasFlag(ModeEnum.Add))
                    {
                        addCP(
                            e.mousePosition,
                            Mode.HasFlag(ModeEnum.Pick),
                            e.button == 1
                        );
                        DTGUI.UseEvent(
                            GetHashCode(),
                            e
                        );
                    }

                if (Mode.HasFlag(ModeEnum.Add))
                {
                    string bLmbBAddControlPointBRmbBAddSmartConnect =
                        "<b>[LMB] on empty space:</b> Add Control Point\n"
                        + "<b>[LMB] on first Control Point:</b> Close/Open the spline\n"
                        + "<b>[RMB] on empty space:</b> Add & Connect to new spline\n"
                        + "<b>[RMB] on segment:</b> Add & Connect to existing spline";

                    if (Mode.HasFlag(ModeEnum.Pick))
                        //<b>[LMB On first Control Point]</b> Close/Open the spline
                        _StatusBar.Set(
                            bLmbBAddControlPointBRmbBAddSmartConnect,
                            "DrawMode"
                        );
                    else
                        _StatusBar.Set(
                            $"{bLmbBAddControlPointBRmbBAddSmartConnect}\n<b>[Shift]</b> Raycast",
                            "DrawMode"
                        );
                }
                else
                    _StatusBar.Set(
                        "Hold <b>[Ctrl]</b> to add Control Points",
                        "DrawMode"
                    );
            }
            else
                _StatusBar.Clear("DrawMode");
        }

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);

            bool raycastAgainstGeometry = Mode.HasFlag(ModeEnum.Pick);

            SetElementSize(
                ref r,
                32,
                32
            );

            if (raycastAgainstGeometry) { }
            else if (SceneView.currentDrawingSceneView.in2DMode)
                rayCastPlane = RayCastPlane.GlobalXY;
            else if (selectedSpline != null && selectedSpline.RestrictTo2D)
                switch (selectedSpline.Restricted2DPlane)
                {
                    case CurvyPlane.XY:
                        rayCastPlane = RayCastPlane.LocalXY;
                        break;
                    case CurvyPlane.XZ:
                        rayCastPlane = RayCastPlane.LocalXZ;
                        break;
                    case CurvyPlane.YZ:
                        rayCastPlane = RayCastPlane.LocalYZ;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            else
            {
                if (GUI.Toggle(
                        r,
                        rayCastPlane == RayCastPlane.ViewPlane,
                        new GUIContent(
                            CurvyStyles.IconView,
                            "Use view plane"
                        ),
                        GUI.skin.button
                    ))
                    rayCastPlane = RayCastPlane.ViewPlane;
                Advance(ref r);

                if (GUI.Toggle(
                        r,
                        rayCastPlane == RayCastPlane.GlobalXY || rayCastPlane == RayCastPlane.LocalXY,
                        new GUIContent(
                            CurvyStyles.IconAxisXY,
                            "Use XY plane"
                        ),
                        GUI.skin.button
                    ))
                    rayCastPlane = Tools.pivotRotation == PivotRotation.Local
                        ? RayCastPlane.LocalXY
                        : RayCastPlane.GlobalXY;
                Advance(ref r);

                if (GUI.Toggle(
                        r,
                        rayCastPlane == RayCastPlane.GlobalXZ || rayCastPlane == RayCastPlane.LocalXZ,
                        new GUIContent(
                            CurvyStyles.IconAxisXZ,
                            "Use XZ plane"
                        ),
                        GUI.skin.button
                    ))
                    rayCastPlane = Tools.pivotRotation == PivotRotation.Local
                        ? RayCastPlane.LocalXZ
                        : RayCastPlane.GlobalXZ;
                Advance(ref r);

                if (GUI.Toggle(
                        r,
                        rayCastPlane == RayCastPlane.GlobalYZ || rayCastPlane == RayCastPlane.LocalYZ,
                        new GUIContent(
                            CurvyStyles.IconAxisYZ,
                            "Use YZ plane"
                        ),
                        GUI.skin.button
                    ))
                    rayCastPlane = Tools.pivotRotation == PivotRotation.Local
                        ? RayCastPlane.LocalYZ
                        : RayCastPlane.GlobalYZ;
                Advance(ref r);
            }

            SetElementSize(
                ref r,
                24,
                24
            );
            r.y += 4;
            GUI.DrawTexture(
                r,
                Mode.HasFlag(ModeEnum.Add)
                    ? CurvyStyles.IconCP
                    : CurvyStyles.IconCPOff
            );

            Advance(ref r);
            GUI.DrawTexture(
                r,
                raycastAgainstGeometry
                    ? CurvyStyles.IconRaycast
                    : CurvyStyles.IconRaycastOff
            );
        }

        private bool pickScenePoint(Vector2 mousePos, bool castAgainstGeometry, CurvySpline referenceSpline,
            CurvySplineSegment referenceCP, out Vector3 pickedPoint)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(mousePos);
            Vector3 referencePosition = (referenceCP != null
                ? referenceCP.transform
                : referenceSpline.transform).position;

            pickedPoint = Vector3.zero;
            bool didPickAPoint = false;

            if (castAgainstGeometry
                && Physics.Raycast(
                    mouseRay,
                    out RaycastHit hit
                ))
            {
                pickedPoint = hit.point;
                didPickAPoint = true;
            }
            else
            {
                Plane plane;
                switch (rayCastPlane)
                {
                    case RayCastPlane.LocalXY:
                        plane = new Plane(
                            referenceSpline.transform.forward,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.GlobalXY:
                        plane = new Plane(
                            Vector3.forward,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.LocalYZ:
                        plane = new Plane(
                            referenceSpline.transform.right,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.GlobalYZ:
                        plane = new Plane(
                            Vector3.right,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.LocalXZ:
                        plane = new Plane(
                            referenceSpline.transform.up,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.GlobalXZ:
                        plane = new Plane(
                            Vector3.up,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.ViewPlane:
                        plane = new Plane(
                            SceneView.currentDrawingSceneView.camera.transform.forward,
                            referencePosition
                        );
                        break;
                    case RayCastPlane.Undefinded:
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                float hitDistance;
                if (plane.Raycast(
                        mouseRay,
                        out hitDistance
                    ))
                {
                    pickedPoint = mouseRay.GetPoint(hitDistance);
                    didPickAPoint = true;
                }
                //fallback: this can happen if you select xy plane but scene view is orthogonal and set to yz. Meaning that all rays are parallel to xy plane. We use the same logic than in RayCastPlane.ViewPlane as a fallback
                else if (new Plane(
                             SceneView.currentDrawingSceneView.camera.transform.forward,
                             referencePosition
                         ).Raycast(
                             mouseRay,
                             out hitDistance
                         ))
                {
                    pickedPoint = mouseRay.GetPoint(hitDistance);
                    didPickAPoint = true;
                }
            }

            if (didPickAPoint == false)
                DTLog.LogWarning("[Curvy] Draw Splines tool: could not pick a valid position. Please raise a bug report.");

            return didPickAPoint;
        }

        private void addCP(Vector2 cursor, bool castRay, bool connectNew)
        {
            const string undoingOperationLabel = "Add ControlPoint";

            Func<CurvySpline, CurvySplineSegment, Vector3, CurvySplineSegment> insertControlPoint =
                (spline, current, worldPos) =>
                {
                    CurvySplineSegment seg = spline.InsertAfter(
                        current,
                        worldPos
                    );
                    Undo.RegisterCreatedObjectUndo(
                        seg.gameObject,
                        undoingOperationLabel
                    );
                    return seg;
                };


            if (selectedSpline) //selecp the last cp if none selected
            {
                if (!selectedCP && selectedSpline.ControlPointCount > 0)
                    selectedCP = selectedSpline.ControlPointsList[selectedSpline.ControlPointCount - 1];
            }
            else //create a spline if none selected
            {
                selectedSpline = CurvySpline.Create();

                SetSplineRestricted2DPlane(
                    selectedSpline,
                    rayCastPlane
                );

                CurvyMenu.ApplyIncrementalNameToSpline(selectedSpline);

                Undo.RegisterCreatedObjectUndo(
                    selectedSpline.gameObject,
                    undoingOperationLabel
                );

                Transform parent = DTSelection.GetAs<Transform>();
                selectedSpline.transform.UndoableSetParent(
                    parent,
                    true,
                    undoingOperationLabel
                );
                if (parent == null)
                    selectedSpline.transform.position = HandleUtility.GUIPointToWorldRay(cursor).GetPoint(10);
            }

            // Pick a point to add the CP at
            Vector3 pickedPoint;
            if (!pickScenePoint(
                    cursor,
                    castRay,
                    selectedSpline,
                    selectedCP,
                    out pickedPoint
                ))
                return;

            CurvySplineSegment pickedControlPoint;
            {
                GameObject pickedGameObject = HandleUtility.PickGameObject(
                    cursor,
                    false
                );
                pickedControlPoint = pickedGameObject
                    ? pickedGameObject.GetComponent<CurvySplineSegment>()
                    : null;
            }

            CurvySplineSegment newCP;
            // Connect by creating a new spline with 2 CP, the first "over" selectedCP, the second at the desired new position
            // OR connect to existing CP
            if (connectNew && selectedCP)
            {
                CurvySplineSegment cpToConnect; //To connect with the selected cp

                // if mouse is over an existing CP, connect to this (if possible)
                // if we picked a target cp, it may be a pick on it's segment, so check distance to CP
                if (pickedControlPoint)
                {
                    Plane plane = new Plane(
                        SceneView.currentDrawingSceneView.camera.transform.forward,
                        pickedControlPoint.transform.position
                    );
                    Ray ray = HandleUtility.GUIPointToWorldRay(cursor);
                    float dist;
                    if (plane.Raycast(
                            ray,
                            out dist
                        ))
                    {
                        //Setting connectedCp
                        {
                            Vector3 hit = ray.GetPoint(dist);
                            if ((hit - pickedControlPoint.transform.position).magnitude
                                <= HandleUtility.GetHandleSize(hit) * CurvyGlobalManager.GizmoControlPointSize)
                                cpToConnect = pickedControlPoint;
                            else
                            {
                                if (pickedControlPoint.Spline.Dirty)
                                    pickedControlPoint.Spline.Refresh();

                                Vector3 position = pickedControlPoint.Interpolate(
                                    pickedControlPoint.GetNearestPointF(
                                        hit,
                                        Space.World
                                    ),
                                    Space.World
                                );
                                cpToConnect = insertControlPoint(
                                    pickedControlPoint.Spline,
                                    pickedControlPoint,
                                    position
                                );
                            }
                        }
                        newCP = insertControlPoint(
                            selectedSpline,
                            selectedCP,
                            cpToConnect.transform.position
                        );
                        selectedCP = newCP;
                    }
                    else
                        newCP = cpToConnect = null;
                }
                else
                    newCP = cpToConnect = null;

                if (!cpToConnect)
                {
                    CurvySpline newSpline = CurvySpline.Create(selectedSpline);
                    CurvyMenu.ApplyIncrementalNameToSpline(newSpline);

                    Undo.RegisterCreatedObjectUndo(
                        newSpline.gameObject,
                        undoingOperationLabel
                    );

                    newSpline.Closed = false;
                    cpToConnect = insertControlPoint(
                        newSpline,
                        null,
                        selectedCP.transform.position
                    );
                    newCP = insertControlPoint(
                        newSpline,
                        cpToConnect,
                        pickedPoint
                    );
                    selectedSpline = newSpline;
                }

                {
#if CURVY_SANITY_CHECKS
                    UnityEngine.Assertions.Assert.IsFalse(
                        selectedCP.Connection != cpToConnect.Connection
                        && selectedCP.Connection != null
                        && cpToConnect.Connection != null,
                        "Both Control Points should not have different non null connections"
                    );
#endif
                    CurvySplineSegment connectionSourceCp;
                    CurvySplineSegment connectionDestinationCp;
                    if (selectedCP.Connection != null)
                    {
                        connectionSourceCp = selectedCP;
                        connectionDestinationCp = cpToConnect;
                    }
                    else
                    {
                        if (cpToConnect.Connection == null)
                        {
                            CurvyConnection.Create(
                                cpToConnect,
                                selectedCP
                            );

                            cpToConnect.Connection.SetSynchronisationPositionAndRotation(
                                cpToConnect.transform.position,
                                cpToConnect.transform.rotation
                            );
                            cpToConnect.ConnectionSyncPosition = true;
                            cpToConnect.ConnectionSyncRotation = true;
                            cpToConnect.FollowUpHeading = ConnectionHeadingEnum.Auto;
                        }

                        connectionSourceCp = cpToConnect;
                        connectionDestinationCp = selectedCP;
                    }

                    CurvyConnection connection = connectionSourceCp.Connection;
                    Undo.RecordObject(
                        connection,
                        undoingOperationLabel
                    );
                    if (connection.ControlPointsList.Contains(connectionDestinationCp) == false)
                        connection.AddControlPoints(connectionDestinationCp);
                    connectionDestinationCp.ConnectionSyncPosition = connectionSourceCp.ConnectionSyncPosition;
                    connectionDestinationCp.ConnectionSyncRotation = connectionSourceCp.ConnectionSyncRotation;
                    connectionDestinationCp.FollowUpHeading = ConnectionHeadingEnum.Auto;
                    connection.SetSynchronisationPositionAndRotation(
                        connection.transform.position,
                        connection.transform.rotation
                    );
                }
            }
            else
            {
                if (selectedSpline.Count >= 1 && pickedControlPoint == selectedSpline.FirstSegment)
                {
                    Undo.RecordObject(
                        selectedSpline,
                        undoingOperationLabel
                    );
                    selectedSpline.Closed = !selectedSpline.Closed;
                    newCP = selectedSpline.Closed
                        ? selectedSpline.LastSegment
                        : selectedSpline.LastVisibleControlPoint;
                }
                else
                    newCP = insertControlPoint(
                        selectedSpline,
                        selectedCP,
                        pickedPoint
                    );
            }

            DTSelection.SetGameObjects(newCP);
        }

        private static void SetSplineRestricted2DPlane(CurvySpline curvySpline, RayCastPlane rayCastPlane)
        {
            switch (rayCastPlane)
            {
                case RayCastPlane.ViewPlane: //todo why ViewPlane is handled as XY?
                case RayCastPlane.LocalXY:
                case RayCastPlane.GlobalXY:
                    curvySpline.Restricted2DPlane = CurvyPlane.XY;
                    break;
                case RayCastPlane.LocalYZ:
                case RayCastPlane.GlobalYZ:
                    curvySpline.Restricted2DPlane = CurvyPlane.YZ;
                    break;
                case RayCastPlane.LocalXZ:
                case RayCastPlane.GlobalXZ:
                    curvySpline.Restricted2DPlane = CurvyPlane.XZ;
                    break;
                case RayCastPlane.Undefinded:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void OnSelectionChange()
        {
            Visible = CurvyProject.Instance.ShowGlobalToolbar
                      || CurvyEditorUtility.IsToolbarActivatingObjectSelected();
            // Ensure we have a spline and a CP. If a spline is selected, choose the last CP
            selectedCP = DTSelection.GetAs<CurvySplineSegment>();
            selectedSpline = selectedCP
                ? selectedCP.Spline
                : DTSelection.GetAs<CurvySpline>();
        }
    }
}