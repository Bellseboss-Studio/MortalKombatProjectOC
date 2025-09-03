// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.CurvyEditor
{
    [CustomEditor(typeof(MetaCGOptions))]
    [CanEditMultipleObjects]
    public class MetaCGOptionsEditor : DTEditor<MetaCGOptions>
    {
        [UsedImplicitly]
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void MetaGizmoDrawer(MetaCGOptions data, GizmoType context)
        {
            if (data.Spline == null)
                return;

            if (CurvyGlobalManager.ShowMetadataGizmo && data.Spline.ShowGizmos)
            {
                if (data.CorrectedHardEdge)
                {
                    Vector3 position = data.ControlPoint.transform.position;
#pragma warning disable CS0618
                    CurvyGizmo.PointLabel(
                        position,
                        "^",
                        OrientationAxisEnum.Down
                    );
#pragma warning restore CS0618
                }

                if (data.Spline.Dirty == false && data.MaterialID != 0)
                {
                    Vector3 position = data.Spline.ToWorldPosition(data.ControlPoint.Interpolate(0.5f));
#pragma warning disable CS0618
                    CurvyGizmo.PointLabel(
                        position,
                        data.MaterialID.ToString(),
                        OrientationAxisEnum.Forward
                    );
#pragma warning restore CS0618
                }
            }
        }

        [UsedImplicitly]
        private void CBSetFirstU()
        {
#if CONTRACTS_FULL
            Contract.Requires(Target.ControlPoint.Spline != null);
#endif
            if (!Target.CorrectedUVEdge && GUILayout.Button("Set U from neighbours"))
            {
                CurvySplineSegment targetControlPoint = Target.ControlPoint;
                CurvySpline targetSpline = targetControlPoint.Spline;

                float uValue;
                if (targetSpline.IsControlPointVisible(targetControlPoint))
                {
                    if (targetSpline.Count == 0)
                        uValue = 0;
                    else
                    {
                        //TODO this implementation has a lot in common with SplineInputModuleBase.CalculateExtendedUV. I am sure there is some duplicated code between these two, and there might be bugs due to those two implementations calculating U differently in some cases

                        CurvySplineSegment previousUWithDefinedCp;
                        CurvySpline curvySpline = Target.Spline;
                        {
                            CurvySplineSegment currentCp = curvySpline.GetPreviousControlPoint(targetControlPoint);
                            if (currentCp == null || targetControlPoint == curvySpline.FirstVisibleControlPoint)
                                previousUWithDefinedCp = targetControlPoint;
                            else
                            {
                                while (currentCp != curvySpline.FirstVisibleControlPoint)
                                {
                                    MetaCGOptions currentCpOptions = currentCp.GetMetadata<MetaCGOptions>(true);
                                    if (currentCpOptions.CorrectedUVEdge || currentCpOptions.ExplicitU)
                                        break;
                                    currentCp = curvySpline.GetPreviousControlPoint(currentCp);
                                }

                                previousUWithDefinedCp = currentCp;
                            }
                        }
                        MetaCGOptions previousDefinedOptions = previousUWithDefinedCp.GetMetadata<MetaCGOptions>(true);

                        CurvySplineSegment nextCpWithDefinedU;
                        {
                            CurvySplineSegment currentCp = curvySpline.GetNextControlPoint(targetControlPoint);
                            if (currentCp == null || targetControlPoint == curvySpline.LastVisibleControlPoint)
                                nextCpWithDefinedU = targetControlPoint;
                            else
                            {
                                while (currentCp != curvySpline.LastVisibleControlPoint)
                                {
                                    MetaCGOptions currentCpOptions = currentCp.GetMetadata<MetaCGOptions>(true);
                                    if (currentCpOptions.CorrectedUVEdge || currentCpOptions.ExplicitU)
                                        break;
                                    currentCp = curvySpline.GetNextControlPoint(currentCp);
                                }

                                nextCpWithDefinedU = currentCp;
                            }
                        }
                        if (curvySpline.Closed && nextCpWithDefinedU == curvySpline.LastVisibleControlPoint)
                            nextCpWithDefinedU = curvySpline.GetPreviousControlPoint(nextCpWithDefinedU);
                        MetaCGOptions nextDefinedOptions = nextCpWithDefinedU.GetMetadata<MetaCGOptions>(true);

                        float frag = (targetControlPoint.Distance - previousUWithDefinedCp.Distance)
                                     / (nextCpWithDefinedU.Distance - previousUWithDefinedCp.Distance);
#if CURVY_SANITY_CHECKS
                        Assert.IsFalse(float.IsNaN(frag));
#endif

                        float startingU = previousUWithDefinedCp == targetControlPoint
                            ? 0
                            : previousDefinedOptions.GetDefinedSecondU(0);
                        float endingU = nextCpWithDefinedU == targetControlPoint
                            ? 1
                            : nextDefinedOptions.GetDefinedFirstU(1);
                        uValue = Mathf.Lerp(
                            startingU,
                            endingU,
                            frag
                        );
                    }
                }
                else
                    uValue = 0;

                Target.FirstU = uValue;


                EditorUtility.SetDirty(target);
            }
        }
    }
}