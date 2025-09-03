// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public class CurvySplineAlignWizard : EditorWindow
    {
        private CurvySpline Spline;
        private float StartOffset;
        private float EndOffset;
        private float Step;
        private bool UseWorldUnits;
        private bool SetPosition = true;
        private bool SetOrientation = true;
        private int OrientationType;

        private int selcount;

        private Vector3[] pos = new Vector3[0];
        private Vector3[] up = new Vector3[0];
        private Vector3[] tan = new Vector3[0];

        public static void Create()
        {
            CurvySplineAlignWizard win = GetWindow<CurvySplineAlignWizard>(
                true,
                "Align Transforms to spline",
                true
            );
            win.Init(Selection.activeGameObject.GetComponent<CurvySpline>());
            win.maxSize = new Vector2(
                400,
                205
            );
            win.minSize = win.maxSize;
            Selection.activeTransform = null;
            SceneView.duringSceneGui -= win.Preview;
            SceneView.duringSceneGui += win.Preview;
        }

        [UsedImplicitly]
        private void OnDestroy() =>
            SceneView.duringSceneGui -= Preview;

        [UsedImplicitly]
        private void OnFocus()
        {
            SceneView.duringSceneGui -= Preview;
            SceneView.duringSceneGui += Preview;
        }

        [UsedImplicitly]
        private void OnSelectionChange()
        {
            if (Selection.activeGameObject)
            {
                CurvySpline spl = Selection.activeGameObject.GetComponent<CurvySpline>();
                if (spl)
                    Init(spl);
            }

            Repaint();
        }

        private void Init(CurvySpline spline) =>
            Spline = spline;

        [UsedImplicitly]
        private void OnGUI()
        {
            selcount = Selection.transforms != null
                ? Selection.transforms.Length
                : 0;

            GUILayout.Label(
                "Spline '"
                + Spline.name
                + "': Length="
                + string.Format(
                    "{0:0.00}",
                    new object[] { Spline.Length }
                )
                + " / Selected: "
                + selcount
                + " transforms"
            );
            GUILayout.Label(
                "Select Transforms and hit Apply!",
                EditorStyles.boldLabel
            );

            StartOffset = EditorGUILayout.FloatField(
                "Offset: Start",
                StartOffset
            );
            EndOffset = EditorGUILayout.FloatField(
                "Offset: End",
                EndOffset
            );
            EditorGUILayout.BeginHorizontal();
            Step = EditorGUILayout.FloatField(
                "Step",
                Step
            );
            if (GUILayout.Button("Auto"))
                SetAutoStep();
            EditorGUILayout.EndHorizontal();
            UseWorldUnits = EditorGUILayout.Toggle(
                "Use World Units",
                UseWorldUnits
            );

            SetPosition = EditorGUILayout.Toggle(
                "Set Position",
                SetPosition
            );
            SetOrientation = EditorGUILayout.Toggle(
                "Set Orientation",
                SetOrientation
            );
            if (SetOrientation)
            {
                EditorGUILayout.BeginHorizontal();
                OrientationType = GUILayout.SelectionGrid(
                    OrientationType,
                    new[]
                    {
                        new GUIContent(
                            "Up-Vector",
                            "Rotate to match Up-Vectors"
                        ),
                        new GUIContent(
                            "Tangent",
                            "Rotate to match Tangent"
                        )
                    },
                    2
                );
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Apply"))
                DoAlign();
            GUI.enabled = true;
            Calculate();
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.Repaint();
        }

        private void SetAutoStep()
        {
            if (selcount == 0) return;
            float len = UseWorldUnits
                ? Spline.Length - StartOffset - EndOffset
                : 1 - StartOffset - EndOffset;
            if (selcount > 1)
                Step = len / (selcount - 1);
            else
                Step = len / (selcount - 1);
        }

        private void Calculate()
        {
            if (selcount == 0) return;
            pos = new Vector3[selcount];
            up = new Vector3[selcount];
            tan = new Vector3[selcount];

            for (int i = 0; i < selcount; i++)
            {
                //OPTIM use InterpolateAndGetTangent
                pos[i] = UseWorldUnits
                    ? Spline.InterpolateByDistance(StartOffset + (Step * i))
                    : Spline.Interpolate(StartOffset + (Step * i));
                up[i] = UseWorldUnits
                    ? Spline.GetOrientationUpFast(Spline.DistanceToTF(StartOffset + (Step * i)))
                    : Spline.GetOrientationUpFast(StartOffset + (Step * i));
                tan[i] = UseWorldUnits
                    ? Spline.GetTangentByDistance(StartOffset + (Step * i))
                    : Spline.GetTangent(StartOffset + (Step * i));
            }
        }

        private void DoAlign()
        {
            if (selcount == 0) return;
            List<Transform> trans = new List<Transform>(Selection.transforms);
            trans.Sort(
                (a, b) => string.Compare(
                    a.name,
                    b.name
                )
            );

            Undo.RecordObjects(
                trans.ToArray(),
                "Align To Spline"
            );

            for (int i = 0; i < selcount; i++)
            {
                if (SetPosition)
                    trans[i].position = pos[i];
                if (SetOrientation)
                    switch (OrientationType)
                    {
                        case 0:
                            trans[i].rotation = Quaternion.LookRotation(
                                tan[i],
                                up[i]
                            );
                            break;
                        case 1:
                            trans[i].rotation = Quaternion.LookRotation(
                                up[i],
                                tan[i]
                            );
                            break;
                    }
            }
        }


        private void Preview(SceneView sceneView)
        {
            Handles.color = Color.blue;
            for (int i = 0; i < pos.Length; i++)
            {
                Vector3 rv = OrientationType == 0
                    ? up[i]
                    : tan[i];
                Handles.ArrowHandleCap(
                    0,
                    pos[i],
                    rv != Vector3.zero
                        ? Quaternion.LookRotation(rv)
                        : Quaternion.identity,
                    2,
                    EventType.Repaint
                );
            }
        }
    }
}