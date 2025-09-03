// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        200,
        "Curvy",
        "Tools",
        "Spline Tools",
        "tools,24,24"
    )]
    public class TBSplineTools : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Open Spline Tools menu";


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
                    new GUIContent(
                        CurvyStyles.IconMeshExport,
                        "Spline to Mesh"
                    )
                ))
            {
                CurvySplineExportWizard.Create();
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconSyncFromHierarchy,
                        "Sync from Hierarchy"
                    )
                ))
            {
                List<CurvySpline> sel = DTSelection.GetAllAs<CurvySpline>();
                foreach (CurvySpline spl in sel)
                {
                    spl.SyncSplineFromHierarchy();
                    spl.ApplyControlPointsNames();
                    spl.Refresh();
                    On = false;
                }
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconSelectContainingConnections,
                        "Select connections connecting only CPs within the selected spline(s)"
                    )
                ))
            {
                List<CurvySpline> selectedSplines = DTSelection.GetAllAs<CurvySpline>();
                DTSelection.SetGameObjects(GetContainingConnections(selectedSplines));
            }
        }

        /// <summary>
        /// Returns all the connections that are exclusively connecting cps within the splines parameter
        /// </summary>
        /// <param name="splines"></param>
        /// <returns></returns>
        private Component[] GetContainingConnections(List<CurvySpline> splines)
        {
            List<Component> connectionsResult = new List<Component>();
            List<CurvySpline> splinesList = new List<CurvySpline>(splines);
            foreach (CurvySpline spline in splinesList)
            {
                foreach (CurvySplineSegment controlPoint in spline.ControlPointsList)
                    if (controlPoint.Connection != null && !connectionsResult.Contains(controlPoint.Connection))
                    {
                        bool add = true;
                        // only process connections if all involved splines are part of the prefab
                        foreach (CurvySplineSegment connectedControlPoint in controlPoint.Connection.ControlPointsList)
                            if (connectedControlPoint.Spline != null && !splinesList.Contains(connectedControlPoint.Spline))
                            {
                                add = false;
                                break;
                            }

                        if (add)
                            connectionsResult.Add(controlPoint.Connection);
                    }
            }

            return connectionsResult.ToArray();
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySpline>(true);
    }
}