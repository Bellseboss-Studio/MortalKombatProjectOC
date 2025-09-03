// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        private static CGModuleEditorBase inPlaceEditTarget;
        private static CGModuleEditorBase inPlaceEditInitiatedBy;

        /// <summary>
        /// Initiates an IPE session or terminates it
        /// </summary>
        /// <remarks>IPE stands for In Place Edit</remarks>
        public static void SetIPE(
            IExternalInput target = null,
            CGModuleEditorBase initiatedBy = null)
        {
            if (inPlaceEditTarget != null)
                inPlaceEditTarget.EndIPE();

            inPlaceEditInitiatedBy = initiatedBy;

            if (target != null)
            {
                inPlaceEditTarget = initiatedBy.Graph.GetModuleEditor((CGModule)target);

                if (SceneView.currentDrawingSceneView)
                    SceneView.currentDrawingSceneView.Focus();

                SyncIPE();
                inPlaceEditTarget.BeginIPE();
            }
        }

        /// <summary>
        /// Sets IPE target's TRS
        /// </summary>
        /// <remarks>IPE stands for In Place Edit</remarks>
        private static void SyncIPE()
        {
            if (inPlaceEditInitiatedBy != null && inPlaceEditTarget != null)
            {
                Vector3 pos;
                Quaternion rot;
                Vector3 scl;
                inPlaceEditInitiatedBy.OnIPEGetTRS(
                    out pos,
                    out rot,
                    out scl
                );
                inPlaceEditTarget.OnIPESetTRS(
                    pos,
                    rot,
                    scl
                );
            }
        }
    }
}