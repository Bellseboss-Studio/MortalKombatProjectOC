// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        12,
        "Curvy",
        "View",
        "View Settings",
        "viewsettings,24,24"
    )]
    public class TBViewSetttings : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Set Curvy Scene View visibility";


        public override void OnSelectionChange() =>
            Visible = CurvyProject.Instance.ShowGlobalToolbar
                      || CurvyEditorUtility.IsToolbarActivatingObjectSelected();

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);
            bool b;
            bool v;

            Background(
                r,
                134,
                230
            );
            SetElementSize(
                ref r,
                134,
                19
            );

            EditorGUI.BeginChangeCheck();
            b = CurvyGlobalManager.Gizmos == CurvySplineGizmos.None;
            b = GUI.Toggle(
                r,
                b,
                "None"
            );
            if (b)
                CurvyGlobalManager.Gizmos = CurvySplineGizmos.None;
            // Curve
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowCurveGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Curve"
            );
            if (b != v)
                CurvyGlobalManager.ShowCurveGizmo = v;
            // Connection
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowConnectionsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Connections"
            );
            if (b != v)
                CurvyGlobalManager.ShowConnectionsGizmo = v;
            // Orientation Anchor
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowOrientationAnchorsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Orientation Anchors"
            );
            if (b != v)
                CurvyGlobalManager.ShowOrientationAnchorsGizmo = v;
            // Approximation
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowApproximationGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Approximations"
            );
            if (b != v)
                CurvyGlobalManager.ShowApproximationGizmo = v;
            // Orientation
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowOrientationGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Orientation"
            );
            if (b != v)
                CurvyGlobalManager.ShowOrientationGizmo = v;
            // Tangents
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowTangentsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Tangents"
            );
            if (b != v)
                CurvyGlobalManager.ShowTangentsGizmo = v;
            // TF
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowTFsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "TF"
            );
            if (b != v)
                CurvyGlobalManager.ShowTFsGizmo = v;
            // Distance
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowRelativeDistancesGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Relative Distances"
            );
            if (b != v)
                CurvyGlobalManager.ShowRelativeDistancesGizmo = v;
            // UserValues
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowMetadataGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Metadata"
            );
            if (b != v)
                CurvyGlobalManager.ShowMetadataGizmo = v;
            // Labels
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowLabelsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Labels"
            );
            if (b != v)
                CurvyGlobalManager.ShowLabelsGizmo = v;
            // Bounds
            AdvanceBelow(ref r);
            b = CurvyGlobalManager.ShowBoundsGizmo;
            v = GUI.Toggle(
                r,
                b,
                "Bounds"
            );
            if (b != v)
                CurvyGlobalManager.ShowBoundsGizmo = v;

            if (EditorGUI.EndChangeCheck())
                CurvyProject.Instance.SavePreferences();
        }
    }
}