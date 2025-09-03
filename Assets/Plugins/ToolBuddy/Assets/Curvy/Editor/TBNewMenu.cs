// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        30,
        "Curvy",
        "Create",
        "Create",
        "add,24,24"
    )]
    public class TBNewMenu : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Create";


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
                        CurvyStyles.IconNewShape,
                        "Shape"
                    )
                ))
            {
                CurvyMenu.CreateCurvySpline(new MenuCommand(Selection.activeGameObject));
                Project.FindItem<TBSplineSetShape>().OnClick();

                On = false;
            }

            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconNewCG,
                        "Generator"
                    )
                ))
            {
                //todo use UndoableCreateGameObject?
                CurvyGenerator cg = new GameObject(
                    "Curvy Generator",
                    typeof(CurvyGenerator)
                ).GetComponent<CurvyGenerator>();
                CurvyMenu.ApplyIncrementalNameToGenerator(cg);
                const string undoOperationName = "Create Generator";
                Undo.RegisterCreatedObjectUndo(
                    cg.gameObject,
                    undoOperationName
                );

                if (cg)
                {
                    GameObject parent = DTSelection.GetGameObject();
                    if (parent != null)
                    {
                        cg.transform.UndoableSetParent(
                            parent.transform,
                            true,
                            undoOperationName
                        );
                        cg.transform.localPosition = Vector3.zero;
                        cg.transform.localRotation = Quaternion.identity;
                        cg.transform.localScale = Vector3.one;
                    }

                    // if a spline is selected, create an Input module
                    if (DTSelection.HasComponent<CurvySpline>())
                    {
                        InputSplinePath mod = cg.AddModule<InputSplinePath>();
                        mod.Spline = DTSelection.GetAs<CurvySpline>();
                    }

                    DTSelection.SetGameObjects(cg);
                    CGGraph.Open(cg);
                }

                On = false;
            }
        }

        public override void OnSelectionChange() =>
            Visible = CurvyProject.Instance.ShowGlobalToolbar
                      || CurvyEditorUtility.IsToolbarActivatingObjectSelected();
    }
}