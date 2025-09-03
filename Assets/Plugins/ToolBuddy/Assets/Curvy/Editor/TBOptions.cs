// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        10,
        "Curvy",
        "Options",
        "Curvy Options",
        "curvyicon_dark,24,24",
        "curvyicon_light,24,24"
    )]
    public class TBOptions : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Open Curvy Options menu";


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
                        CurvyStyles.IconPrefs,
                        "Preferences"
                    )
                ))
            {
                DT.OpenPreferencesWindow(CurvyProject.CurvySettingsProvider.GetPreferencesPath());
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconAsmdef,
                        "Generate Assembly Definitions"
                    )
                ))
            {
                CurvyEditorUtility.GenerateAssemblyDefinitions();
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconHelp,
                        "Online Manual"
                    )
                ))
            {
                AboutWindow.OpenDocs();
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconWWW,
                        "Curvy Website"
                    )
                ))
            {
                AboutWindow.OpenWeb();
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconBugReporter,
                        "Report Bug"
                    )
                ))
            {
                CurvyEditorUtility.SendBugReport();
                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconAbout,
                        "About Curvy"
                    )
                ))
                AboutWindow.Open();
        }

        public override void OnSelectionChange() =>
            Visible = CurvyProject.Instance.ShowGlobalToolbar
                      || CurvyEditorUtility.IsToolbarActivatingObjectSelected();
    }

    /*
    [ToolbarItem(180, "Curvy", "Limit Len", "Constraint max. Spline length", "constraintlength,24,24")]
    public class TBCPLengthConstraint : DTToolbarToggleButton
    {
        public float MaxSplineLength;
        CurvySpline Spline;

        public TBCPLengthConstraint()
        {
            KeyBindings.Add(new EditorKeyBinding("Constraint Length", "Spline: Constraint Length"));
        }
        Vector3[] storedPosPrev = new Vector3[0];
        Vector3[] storedPos = new Vector3[0];


        void StorePos()
        {
            storedPosPrev = storedPos;
            storedPos = new Vector3[Selection.transforms.Length];
            for (int i = 0; i < storedPos.Length; i++)
                storedPos[i] = Selection.transforms[i].position;
        }
        void RestorePos()
        {
            Debug.Log("Restore");
            for (int i = 0; i < storedPosPrev.Length; i++)
                Selection.transforms[i].position = storedPosPrev[i];
        }

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);
            SetElementSize(ref r, 84, 22);
            Background(r, 84, 22);
            r.width = 60;
            MaxSplineLength = EditorGUI.FloatField(r, MaxSplineLength);
            r.x += 62;
            r.width = 22;
            if (GUI.Button(r, "<"))
            {
                var cp = DTSelection.GetAs<CurvySplineSegment>();
                if (cp)
                    MaxSplineLength = cp.Spline.Length;
            }
        }

        public override void OnSelectionChange()
        {
            var cp = DTSelection.GetAs<CurvySplineSegment>();
            Visible = cp != null;
            Spline = (cp) ? cp.Spline : null;
        }

        public override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (On && Spline)
            {
                if (Spline.Length > MaxSplineLength)
                {
                    RestorePos();
                    Spline.SetDirtyAll();
                    Spline.Refresh();
                }
                else
                    StorePos();
            }

        }
    }
    */
}