// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Controllers
{
    [CanEditMultipleObjects]
    [CustomEditor(
        typeof(SplineController),
        true
    )]
    public class SplineControllerEditor : CurvyControllerEditor<SplineController>
    {
        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );

            float ArrayExElementHeightCallback(int index)
            {
                if (index >= node.serializedProperty.arraySize)
                    // this case happens due to this regression:  https://issuetracker.unity3d.com/issues/reorderablelist-dot-elementheightcallback-is-invoked-when-list-has-no-element
                    return 0;

                return OnPositionReachedSettingsDrawer.GetPropertyHeight(
                    node.serializedProperty.GetArrayElementAtIndex(index),
                    ((SplineController)target).OnPositionReachedList[index]
                );
            }

            node.ArrayEx.elementHeightCallback = ArrayExElementHeightCallback;
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            CurvySpline spline;

            if (Target != null && (spline = Target.Spline) != null)
                for (int index = 0; index < Target.OnPositionReachedList.Count; index++)
                {
                    OnPositionReachedSettings settings = Target.OnPositionReachedList[index];
                    DTHandles.PushHandlesColor(settings.GizmoColor);

                    Vector3 position;
                    {
                        switch (settings.PositionMode)
                        {
                            case CurvyPositionMode.Relative:
                                position = spline.Interpolate(
                                    settings.Position,
                                    Space.World
                                );
                                break;
                            case CurvyPositionMode.WorldUnits:
                                position = spline.InterpolateByDistance(
                                    settings.Position,
                                    Space.World
                                );
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    float handleSize = HandleUtility.GetHandleSize(position) * .2f;
                    Vector3 newPosition = Handles.FreeMoveHandle(
                        position,
#if UNITY_2022_1_OR_NEWER == false
                        Quaternion.identity,
#endif
                        handleSize,
                        Vector3.one
                        * 0.5f, //couldn't figure out what value to put here. I put the same value as the example in the documentation
                        Handles.SphereHandleCap
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(
                            Target,
                            "Modify Custom Event Position"
                        );
                        float nearestTf = spline.GetNearestPointTF(
                            newPosition,
                            Space.World
                        );
                        switch (settings.PositionMode)
                        {
                            case CurvyPositionMode.Relative:
                                settings.Position = nearestTf;
                                break;
                            case CurvyPositionMode.WorldUnits:
                                settings.Position = spline.TFToDistance(nearestTf);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    //draw label
                    {
                        Color textColor = new Color(
                            settings.GizmoColor.r * 0.2f,
                            settings.GizmoColor.g * 0.2f,
                            settings.GizmoColor.b * 0.2f,
                            1
                        );

                        GUIStyle guiStyle = CurvyStyles.ControllerCustomEventStyle;
                        lock (guiStyle)
                        {
                            guiStyle.normal.textColor = textColor;

                            //inlined version of CurvyGizmo.PointLabel(newPosition, settings.Name, OrientationAxisEnum.Up, handleSize * 4, guiStyle);
                            //I did the inline because CurvyGizmo.PointLabel has an issue with 2021.2, and I have a hack in that method to avoid the issue. The problem is the hack is counter-productive when the method is called from a OnSceneGUI method
                            Vector3 labelPosition = newPosition;
                            string label = settings.Name;

                            //ugly shit to bypass the joke that is style.alignment. Tried to bypass the issue by using style.CalcSize(new GUIContent(label)) to manually place the labels. No luck with that
                            while (label.Length <= 5)
                                label = $" {label} ";

                            labelPosition -= Camera.current.transform.right * (handleSize * 4) * 0.1f;
                            labelPosition += Camera.current.transform.up * (handleSize * 4) * 0.1f;
                            labelPosition += Camera.current.transform.up * (handleSize * 4) * 0.3f;

                            Handles.Label(
                                labelPosition,
                                label,
                                guiStyle
                            );
                        }
                    }

                    //direction handles
                    {
                        //optim if needed
                        float tf = settings.PositionMode == CurvyPositionMode.Relative
                            ? settings.Position
                            : spline.DistanceToTF(settings.Position);
                        Vector3 forward = spline.GetTangent(
                            tf,
                            Space.World
                        );
                        Vector3 backward = -spline.GetTangent(
                            tf * (1f - 0.001f),
                            Space.World
                        ); //todo not the best way to compute the backward tangent, but it is a decent one for now. Enhance this if needed once you rework tangents computing code

                        if (settings.TriggeringDirections != TriggeringDirections.Backward)
                            DTHandles.ArrowCap(
                                newPosition,
                                forward,
                                Camera.current.transform.forward,
                                settings.GizmoColor,
                                .7f,
                                .1f,
                                .3f,
                                .4f,
                                handleSize * 2
                            );
                        if (settings.TriggeringDirections != TriggeringDirections.Forward)
                            DTHandles.ArrowCap(
                                newPosition,
                                backward,
                                Camera.current.transform.forward,
                                settings.GizmoColor,
                                .7f,
                                .1f,
                                .3f,
                                .4f,
                                handleSize * 2
                            );
                    }
                    DTHandles.PopHandlesColor();
                }
        }
    }
}