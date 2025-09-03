// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        165,
        "Curvy",
        "Connect",
        "Create a connection",
        "connectionpos_dark,24,24",
        "connectionpos_light,24,24"
    )]
    public class TBCPConnect : DTToolbarButton
    {
        public override string StatusBarInfo => "Add a connection";

        public TBCPConnect() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Connect",
                    "Create connection"
                )
            );

        public override void OnClick()
        {
            List<CurvySplineSegment> selected = DTSelection.GetAllAs<CurvySplineSegment>();
            CurvySplineSegment[] unconnected = (from cp in selected
                where !cp.Connection
                select cp).ToArray();

            if (unconnected.Length > 0)
            {
                CurvyConnection con = (from cp in selected
                    where cp.Connection != null
                    select cp.Connection).FirstOrDefault();

                if (con == null)
                    con = CurvyConnection.Create(unconnected); // Undo inside
                //con.AddControlPoints(unconnected); // Undo inside
                //con.AutoSetFollowUp();
                else
                    con.AddControlPoints(unconnected); // Undo inside
            }

            /*
            if (unconnected.Length == 2)
            {
                var source = unconnected[1];
                var dest = unconnected[0];
                source.ConnectTo(dest, (source.transform.position == dest.transform.position), false);
            }
            else
            {
                if (con == null)
                {
                    con = CurvyConnection.Create(); // Undo inside
                }
                con.AddControlPoints(unconnected); // Undo inside
            }
            */
            foreach (CurvySplineSegment cp in unconnected)
                EditorUtility.SetDirty(cp);

            CurvyProject.Instance.ScanConnections();

            //EditorApplication.RepaintHierarchyWindow();
        }

        public override void OnSelectionChange()
        {
            List<CurvySplineSegment> selected = DTSelection.GetAllAs<CurvySplineSegment>();
            List<CurvySplineSegment> unconnected = (from cp in selected
                where !cp.Connection
                select cp).ToList();

            Visible = unconnected.Count > 0;
            /*
                      (unconnected.Count==1 ||
                      unconnected.Count>2 ||
                      (selected.Count == 2 && selected[0].CanConnectTo(selected[1])));
              */
        }
    }
}