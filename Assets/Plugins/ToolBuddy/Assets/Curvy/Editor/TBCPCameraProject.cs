// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        190,
        "Curvy",
        "Camera Project",
        "Project camera",
        "camproject,24,24"
    )]
    public class TBCPCameraProject : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Raycast and move Control Points";

        private List<CurvySplineSegment> mCPSelection;

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);
            SetElementSize(
                ref r,
                32,
                32
            );

            if (GUI.Button(
                    r,
                    "OK"
                ))
            {
                foreach (CurvySplineSegment cp in mCPSelection)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(
                            new Ray(
                                cp.transform.position,
                                SceneView.currentDrawingSceneView.camera.transform.forward
                            ),
                            out hit
                        ))
                    {
                        Undo.RecordObject(
                            cp.transform,
                            "Project Control Points"
                        );
                        cp.transform.position = hit.point;
                    }
                }

                On = false;
            }
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();
            if (On && SceneView.currentDrawingSceneView != null)
            {
                DTHandles.PushHandlesColor(Color.red);
                foreach (CurvySplineSegment cp in mCPSelection)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(
                            new Ray(
                                cp.transform.position,
                                SceneView.currentDrawingSceneView.camera.transform.forward
                            ),
                            out hit
                        ))
                    {
                        Handles.DrawDottedLine(
                            cp.transform.position,
                            hit.point,
                            2
                        );
                        Handles.SphereHandleCap(
                            0,
                            hit.point,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(hit.point) * 0.1f,
                            EventType.Repaint
                        );
                    }
                }

                DTHandles.PopHandlesColor();
            }
        }

        public override void OnSelectionChange()
        {
            mCPSelection = DTSelection.GetAllAs<CurvySplineSegment>();
            Visible = mCPSelection.Count > 0;
            if (!Visible)
                On = false;
        }

        public override void HandleEvents(Event e)
        {
            base.HandleEvents(e);
            if (On)
                _StatusBar.Set(
                    "Click <b>OK</b> to apply the preview changes",
                    "CameraProject"
                );
            else
                _StatusBar.Clear("CameraProject");
        }
    }
}