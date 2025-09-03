// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Base class for spline input modules
    /// </summary>
    public abstract class SplineInputModuleBase : CGModule
    {
        #region ### Serialized Fields ###

        /// <summary>
        /// Makes this module use the cached approximations of the spline's positions and tangents
        /// </summary>
        [Tab("General")]
        [SerializeField]
        [Tooltip("Makes this module use the cached approximations of the spline's positions and tangents")]
        private bool m_UseCache;

        [Tooltip(
            "Whether to use local or global coordinates of the input's control points.\r\nUsing the global space when the input's transform is updating every frame will lead to the generator refreshing too frequently"
        )]
        [SerializeField]
        private bool m_UseGlobalSpace;

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [Tab("Range")]
        [SerializeField]
        protected CurvySplineSegment m_StartCP;

        [FieldCondition(
            nameof(m_StartCP),
            null,
            true,
            Action = ActionAttribute.ActionEnum.Enable
        )]
        [SerializeField]
        protected CurvySplineSegment m_EndCP;

#endif

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Makes this module use the cached approximations of the spline's positions and tangents
        /// </summary>
        public bool UseCache
        {
            get => m_UseCache;
            set
            {
                if (m_UseCache != value)
                {
                    m_UseCache = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If not null, the input spline will not be considered fully, but only the range between <see cref="StartCP"/> and <see cref="EndCP"/>.
        /// </summary>
        /// <remarks>Valid values of <see cref="StartCP"/> and <see cref="EndCP"/> are such as those CPs are part of <see cref="InputSpline"/>, and <see cref="StartCP"/> is prior to <see cref="EndCP"/> in that spline</remarks>
        public CurvySplineSegment StartCP
        {
            get => m_StartCP;
            set
            {
                if (m_StartCP != value)
                {
                    m_StartCP = value;
                    ValidateStartAndEndCps();
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If not null, the input spline will not be considered fully, but only the range between <see cref="StartCP"/> and <see cref="EndCP"/>.
        /// </summary>
        /// <remarks>Valid values of <see cref="StartCP"/> and <see cref="EndCP"/> are such as those CPs are part of <see cref="InputSpline"/>, and <see cref="StartCP"/> is prior to <see cref="EndCP"/> in that spline</remarks>
        public CurvySplineSegment EndCP
        {
            get => m_EndCP;
            set
            {
                if (m_EndCP != value)
                {
                    m_EndCP = value;
                    ValidateStartAndEndCps();
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Whether to use local or global coordinates of the input's control points.
        /// Using the global space will dirty the module whenever the spline's transform is updated
        /// </summary>
        public bool UseGlobalSpace
        {
            get => m_UseGlobalSpace;
            set
            {
                if (m_UseGlobalSpace != value)
                {
                    m_UseGlobalSpace = value;
                    Dirty = true;
                }
            }
        }

        public override bool IsConfigured => base.IsConfigured && InputSpline != null;

        public override bool IsInitialized => base.IsInitialized && (InputSpline == null || InputSpline.IsInitialized);

        public bool PathIsClosed => IsConfigured && getPathClosed(InputSpline);

        #endregion

        #region Public mehtods

        /// <summary>
        /// Set the input range, defined by <see cref="StartCP"/> and <see cref="EndCP"/>
        /// </summary>
        public void SetRange(CurvySplineSegment rangeStart, CurvySplineSegment rangeEnd)
        {
            if (StartCP != rangeStart || EndCP != rangeEnd)
            {
                m_StartCP = rangeStart;
                m_EndCP = rangeEnd;
                ValidateStartAndEndCps();
                Dirty = true;
            }
        }

        /// <summary>
        /// Clear the input range, defined by <see cref="StartCP"/> and <see cref="EndCP"/>
        /// </summary>
        public void ClearRange() =>
            SetRange(
                null,
                null
            );

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            OnSplineAssigned();
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            if (InputSpline)
            {
                InputSpline.OnRefresh.RemoveListener(OnSplineRefreshed);
                InputSpline.OnInitialized.RemoveListener(OnSplineInitialized);
                InputSpline.OnGlobalCoordinatesChanged -= OnInputSplineCoordinatesChanged;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            ValidateStartAndEndCps();

            if (IsActiveAndEnabled) OnSplineAssigned();
        }

        public override void Reset()
        {
            base.Reset();
            InputSpline = null;
            UseCache = false;
            StartCP = null;
            EndCP = null;
            UseGlobalSpace = false;
        }

#endif

        #endregion

        private void OnSplineRefreshed(CurvySplineEventArgs e)
        {
            if (IsActiveAndEnabled == false)
                return;

            if (InputSpline == e.Spline)
                ForceRefresh();
            else
                e.Spline.OnRefresh.RemoveListener(OnSplineRefreshed);
        }

        private void OnSplineInitialized(CurvySplineEventArgs e)
        {
            if (IsActiveAndEnabled == false)
                return;


            if (InputSpline == e.Spline)
            {
                //Validation is ignored when spline is disabled. We do validation here in case invalid CPs were assigned while spline was disabled
                ValidateStartAndEndCps();
                Dirty = true;
            }
            else
                e.Spline.OnInitialized.RemoveListener(OnSplineInitialized);
        }

        private void OnInputSplineCoordinatesChanged(CurvySpline sender)
        {
            if (IsActiveAndEnabled == false)
                return;

            if (InputSpline == sender)
            {
                if (UseGlobalSpace)
                    ForceRefresh();
            }
            else
                InputSpline.OnGlobalCoordinatesChanged -= OnInputSplineCoordinatesChanged;
        }

        private void ForceRefresh()
        {
            Dirty = true;
#if UNITY_EDITOR
            // The Update order makes the spline and CP update happen before the generator, so from first look, no need to force the generator update, it should happen naturally. But unfortunately things are not that simple :(
            // So the explanation is that the whole chain of actions that lead to this method being called starts with the change of the transform of a CP. For some reason (I am sure about this for 90%) the transform change's is acknowledged in the spline's (or CP's) Update while in Play mode, but only in editorUpdate when in Edit mode. And editorUpdate gets called after all the Updates (including the generator's update), so to make sure the generator is up to date after the spline's update we have to force it's update when not in Play mode
            if (!Application.isPlaying)
                Generator.TryAutoRefresh();
#endif
        }

        private bool getPathClosed(CurvySpline spline)
        {
            if (!spline || !spline.Closed)
                return false;
            return EndCP == null;
        }

        #region GetSplineData

        [CanBeNull]
        protected CGData GetSplineData(CurvySpline spline, bool fullPath /*is spline a path*/, CGDataRequestRasterization raster,
            CGDataRequestMetaCGOptions options)
        {
            if (spline == null || spline.Count == 0)
                return null;

            // calc start & end point (distance)
            float pathLength = StartCP && EndCP
                ? EndCP.Distance - StartCP.Distance
                : spline.Length;
            float startDist;
            float endDist;
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(raster.Start >= 0);
                Assert.IsTrue(raster.Start <= 1);
#endif
                startDist = pathLength * raster.Start;
                if (StartCP)
                    startDist = (startDist + StartCP.Distance) % spline.Length;
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(startDist >= 0);
                Assert.IsTrue(startDist <= spline.Length);

                Assert.IsTrue(raster.RasterizedRelativeLength >= 0);
                Assert.IsTrue(raster.RasterizedRelativeLength <= 1);
#endif
                endDist = startDist + (pathLength * raster.RasterizedRelativeLength);
            }

            float stepDist;
            {
                float samplingPointsPerUnit = CurvySpline.CalculateSamplingPointsPerUnit(
                    raster.Resolution,
                    spline.MaxPointsPerUnit
                );

                float sampledDistance = endDist - startDist;
                stepDist = Mathf.Min(
                    sampledDistance / (pathLength * raster.RasterizedRelativeLength * samplingPointsPerUnit),
                    //To ensure that rasterized shapes have at least 3 vertices to generate valid meshes
                    sampledDistance / 3f
                );
            }

            CGShape data;
            {
                data = fullPath
                    ? new CGPath()
                    : new CGShape();
                data.Length = endDist - startDist;
                data.SourceIsManaged = IsManagedResource(spline);
                data.Closed = spline.Closed;
                data.Seamless = spline.Closed && raster.RasterizedRelativeLength == 1;
            }


            if (data.Length == 0)
                return data;

            // Scan input spline and fetch a list of control points that provide special options (Hard Edge, MaterialID etc...)
            int materialID;
            float maxStep;
            List<ControlPointOption> controlPointsOptions;
            {
                if (options)
                    controlPointsOptions = CGUtility.GetControlPointsWithOptions(
                        options,
                        spline,
                        startDist,
                        endDist,
                        raster.Mode == CGDataRequestRasterization.ModeEnum.Optimized,
                        out materialID,
                        out maxStep
                    );
                else
                {
                    controlPointsOptions = new List<ControlPointOption>();
                    materialID = 0;
                    maxStep = float.MaxValue;
                }
            }

            // initialize with start TF
            float tf = spline.DistanceToTF(startDist);
            float startTF = tf;
            float endTF = endDist > spline.Length && spline.Closed
                ? spline.DistanceToTF(endDist - spline.Length) + 1
                : spline.DistanceToTF(endDist);
            float currentDistance = startDist;

            // Setup vars
            SubArrayList<Vector3> positions, tangents, normals;
            SubArrayList<float> sourceFs, relativeFs;
            {
                int initialArraysLength;
                switch (raster.Mode)
                {
                    case CGDataRequestRasterization.ModeEnum.Even:
                        initialArraysLength = Mathf.Max(
                            20,
                            Mathf.CeilToInt((1.1f * (endDist - startDist)) / stepDist)
                        );
                        break;
                    case CGDataRequestRasterization.ModeEnum.Optimized:
                        initialArraysLength = Mathf.Max(
                            20,
                            Mathf.CeilToInt((0.2f * (endDist - startDist)) / stepDist)
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                positions = new SubArrayList<Vector3>(
                    initialArraysLength,
                    ArrayPools.Vector3
                );
                relativeFs = new SubArrayList<float>(
                    initialArraysLength,
                    ArrayPools.Single
                );
                sourceFs = new SubArrayList<float>(
                    initialArraysLength,
                    ArrayPools.Single
                );
                tangents = new SubArrayList<Vector3>(
                    fullPath
                        ? initialArraysLength
                        : 0,
                    ArrayPools.Vector3
                );
                normals = new SubArrayList<Vector3>(
                    fullPath
                        ? initialArraysLength
                        : 0,
                    ArrayPools.Vector3
                );
            }

            List<DuplicateSamplePoint> duplicatePoints = new List<DuplicateSamplePoint>();
            List<SamplePointUData> extendedUVData = new List<SamplePointUData>();
            bool
                duplicatePoint =
                    false; //TODO BUG? why is duplicatePoint not used before assigning it in the ModeEnum.Optimized case?
            // we have at least one Material Group
            SamplePointsMaterialGroup materialGroup = new SamplePointsMaterialGroup(materialID);
            // and at least one patch within that group
            SamplePointsPatch patch = new SamplePointsPatch(0);
            CurvyClamping clampMode = data.Closed
                ? CurvyClamping.Loop
                : CurvyClamping.Clamp;
            int dead = 2000000;

            //BUG? there is a lot of code that is quite the same, but not completly, between the two following cases. I sens potential bugs here
            //OPTIM in the following, a lot of spline methods have a call to TFToSegment inside them. Instead of letting each one of these methods call TFToSegment, call it once and give it to all the methods
            switch (raster.Mode)
            {
                case CGDataRequestRasterization.ModeEnum.Even:

                    #region --- Even ---

                    {
                        // we advance the spline using a fixed distance
                        while (currentDistance <= endDist && --dead > 0)
                        {
                            tf = spline.DistanceToTF(
                                spline.ClampDistance(
                                    currentDistance,
                                    clampMode
                                )
                            );

                            float currentF;
                            {
                                currentF = (currentDistance - startDist) / data.Length; //curDist / endDist;
                                if (Mathf.Approximately(
                                        1,
                                        currentF
                                    ))
                                    currentF = 1;
                            }

                            //Position, tangent and up
                            Vector3 currentPosition;
                            Vector3 currentTangent;
                            Vector3 currentUp;
                            {
                                float localF;
                                CurvySplineSegment segment = spline.TFToSegment(
                                    tf,
                                    out localF,
                                    CurvyClamping.Clamp
                                );
                                if (fullPath) // add path values
                                {
                                    if (UseCache)
                                        segment.InterpolateAndGetTangentFast(
                                            localF,
                                            out currentPosition,
                                            out currentTangent
                                        );
                                    else
                                    {
                                        currentPosition = segment.Interpolate(
                                            localF,
                                            Space.Self
                                        );
                                        currentTangent = segment.GetTangent(
                                            localF,
                                            Space.Self
                                        );
                                    }

                                    //OPTIM get orientation at the same time you get position and tangent
                                    currentUp = segment.GetOrientationUpFast(localF);
                                }
                                else
                                {
                                    currentPosition = UseCache
                                        ? segment.InterpolateFast(localF)
                                        : segment.Interpolate(localF);
                                    currentTangent = Vector3.zero;
                                    currentUp = Vector3.zero;
                                }
                            }

                            AddPoint(
                                currentDistance / spline.Length,
                                currentF,
                                fullPath,
                                currentPosition,
                                currentTangent,
                                currentUp,
                                ref sourceFs,
                                ref relativeFs,
                                ref positions,
                                ref tangents,
                                ref normals
                            );

                            if (duplicatePoint) // HardEdge, IncludeCP, MaterialID changes etc. need an extra vertex
                            {
                                AddPoint(
                                    currentDistance / spline.Length,
                                    currentF,
                                    fullPath,
                                    currentPosition,
                                    currentTangent,
                                    currentUp,
                                    ref sourceFs,
                                    ref relativeFs,
                                    ref positions,
                                    ref tangents,
                                    ref normals
                                );
                                duplicatePoint = false;
                            }

                            // Advance
                            currentDistance += stepDist;

                            // Check next Sample Point's options. If the next point would be past a CP with options
                            if (controlPointsOptions.Count > 0 && currentDistance >= controlPointsOptions[0].Distance)
                            {
                                ControlPointOption cpOptions = controlPointsOptions[0];

                                ProcessControlPointOptions(
                                    cpOptions,
                                    positions.Count,
                                    data.MaterialGroups,
                                    extendedUVData,
                                    duplicatePoints,
                                    ref materialGroup,
                                    ref patch,
                                    out currentDistance,
                                    out duplicatePoint
                                );

                                // and remove the CP from the options
                                controlPointsOptions.RemoveAt(0);
                            }

                            // Ensure last sample point position is at the desired end distance
                            if (currentDistance > endDist && currentF < 1) // next loop curF will be 1
                                currentDistance = endDist;
                        }

                        if (dead <= 0)
                            Debug.LogError(
                                "[Curvy] He's dead, Jim! Deadloop in SplineInputModuleBase.GetSplineData (Even)! Please send a bug report."
                            );
                        // store the last open patch
                        patch.End = positions.Count - 1;
                        materialGroup.Patches.Add(patch);
                        // ExplicitU on last Vertex?
                        //if (optionsSegs.Count > 0 && optionsSegs[0].UVShift)
                        //    extendedUVData.Add(new SamplePointUData(pos.Count - 1, optionsSegs[0].UVEdge, optionsSegs[0].FirstU, optionsSegs[0].SecondU));
                        // if path is closed and no hard edges involved, we need to smooth first normal
                        if (data.Closed)
                            duplicatePoints.Add(
                                new DuplicateSamplePoint(
                                    positions.Count - 1,
                                    0,
                                    spline[0].GetMetadata<MetaCGOptions>(true).CorrectedHardEdge
                                )
                            );

                        FillData(
                            data,
                            materialGroup,
                            sourceFs,
                            relativeFs,
                            fullPath,
                            positions,
                            tangents,
                            normals,
                            UseGlobalSpace,
                            spline.transform,
                            Generator.transform
                        );
                    }

                    #endregion

                    break;
                case CGDataRequestRasterization.ModeEnum.Optimized:

                    #region --- Optimized ---

                    {
                        float stepSizeTF = stepDist / spline.Length;
                        float maxAngle = raster.AngleThreshold;

                        Vector3 currentPosition;
                        Vector3 currentTangent;
                        {
                            if (UseCache)
                                spline.InterpolateAndGetTangentFast(
                                    tf,
                                    out currentPosition,
                                    out currentTangent
                                );
                            else
                                spline.InterpolateAndGetTangent(
                                    tf,
                                    out currentPosition,
                                    out currentTangent
                                );
                        }

                        while (tf < endTF && dead-- > 0)
                        {
                            AddPoint(
                                currentDistance / spline.Length,
                                (currentDistance - startDist) / data.Length,
                                fullPath,
                                currentPosition,
                                currentTangent,
                                spline.GetOrientationUpFast(tf % 1),
                                ref sourceFs,
                                ref relativeFs,
                                ref positions,
                                ref tangents,
                                ref normals
                            );
                            // Advance
                            float stopAt = controlPointsOptions.Count > 0
                                ? controlPointsOptions[0].TF
                                : endTF;

                            bool atStopPoint = MoveByAngleExt(
                                spline,
                                UseCache,
                                ref tf,
                                maxStep,
                                maxAngle,
                                out currentPosition,
                                out currentTangent,
                                stopAt,
                                data.Closed,
                                stepSizeTF
                            );

                            currentDistance = spline.TFToDistance(
                                tf,
                                clampMode
                            );
                            if (currentDistance < startDist)
                                currentDistance += spline.Length;

                            if (Mathf.Approximately(
                                    tf,
                                    endTF
                                )
                                || tf > endTF)
                            {
                                currentDistance = endDist;
                                endTF = data.Closed
                                    ? DTMath.Repeat(
                                        endTF,
                                        1
                                    )
                                    : Mathf.Clamp01(endTF);
                                if (fullPath)
                                {
                                    if (UseCache)
                                        spline.InterpolateAndGetTangentFast(
                                            endTF,
                                            out currentPosition,
                                            out currentTangent
                                        );
                                    else
                                        spline.InterpolateAndGetTangent(
                                            endTF,
                                            out currentPosition,
                                            out currentTangent
                                        );
                                }
                                else
                                    currentPosition = UseCache
                                        ? spline.InterpolateFast(endTF)
                                        : spline.Interpolate(endTF);

                                AddPoint(
                                    currentDistance / spline.Length,
                                    (currentDistance - startDist) / data.Length,
                                    fullPath,
                                    currentPosition,
                                    currentTangent,
                                    spline.GetOrientationUpFast(endTF),
                                    ref sourceFs,
                                    ref relativeFs,
                                    ref positions,
                                    ref tangents,
                                    ref normals
                                );
                                break;
                            }

                            if (atStopPoint)
                            {
                                if (controlPointsOptions.Count > 0)
                                {
                                    ControlPointOption cpOptions = controlPointsOptions[0];
                                    ProcessControlPointOptions(
                                        cpOptions,
                                        positions.Count,
                                        data.MaterialGroups,
                                        extendedUVData,
                                        duplicatePoints,
                                        ref materialGroup,
                                        ref patch,
                                        out currentDistance,
                                        out duplicatePoint
                                    );
                                    // and remove the CP from the options
                                    controlPointsOptions.RemoveAt(0);

                                    maxStep = cpOptions.MaxStepDistance;
                                    if (duplicatePoint)
                                        AddPoint(
                                            currentDistance / spline.Length,
                                            (currentDistance - startDist) / data.Length,
                                            fullPath,
                                            currentPosition,
                                            currentTangent,
                                            spline.GetOrientationUpFast(tf),
                                            ref sourceFs,
                                            ref relativeFs,
                                            ref positions,
                                            ref tangents,
                                            ref normals
                                        );
                                }
                                else
                                {
                                    AddPoint(
                                        currentDistance / spline.Length,
                                        (currentDistance - startDist) / data.Length,
                                        fullPath,
                                        currentPosition,
                                        currentTangent,
                                        spline.GetOrientationUpFast(tf),
                                        ref sourceFs,
                                        ref relativeFs,
                                        ref positions,
                                        ref tangents,
                                        ref normals
                                    );
                                    break;
                                }
                            }
                        }

                        if (dead <= 0)
                            Debug.LogError(
                                "[Curvy] He's dead, Jim! Deadloop in SplineInputModuleBase.GetSplineData (Optimized)! Please send a bug report."
                            );
                        // store the last open patch
                        patch.End = positions.Count - 1;
                        materialGroup.Patches.Add(patch);
                        // ExplicitU on last Vertex?
                        if (controlPointsOptions.Count > 0 && controlPointsOptions[0].UVShift)
                            extendedUVData.Add(
                                new SamplePointUData(
                                    positions.Count - 1,
                                    controlPointsOptions[0]
                                )
                            );

                        // if path is closed and no hard edges involved, we need to smooth first normal
                        if (data.Closed)
                            duplicatePoints.Add(
                                new DuplicateSamplePoint(
                                    positions.Count - 1,
                                    0,
                                    spline[0].GetMetadata<MetaCGOptions>(true).CorrectedHardEdge
                                )
                            );

                        FillData(
                            data,
                            materialGroup,
                            sourceFs,
                            relativeFs,
                            fullPath,
                            positions,
                            tangents,
                            normals,
                            UseGlobalSpace,
                            spline.transform,
                            Generator.transform
                        );
                    }

                    #endregion

                    break;
            }

            data.CustomValues = ArrayPools.Single.Clone(data.RelativeDistances);
            data.DuplicatePoints = duplicatePoints;

            if (!fullPath)
            {
                data.RecalculateNormals();
                if (extendedUVData.Count > 0)
                {
                    CalculateExtendedUV(
                        spline,
                        startTF,
                        endTF,
                        extendedUVData,
                        data
                    );
                    if (spline.Closed)
                        UIMessages.Add(
                            "Extended UV features (UV Edge, Explicit U) are used in the Meta CG Options of a closed spline. Those features are supported only for open splines"
                        );
                }
            }

            return data;
        }

        private static void ProcessControlPointOptions(ControlPointOption options,
            int positionsCount,
            List<SamplePointsMaterialGroup> shapeMaterialGroups,
            List<SamplePointUData> extendedUVData,
            List<DuplicateSamplePoint> duplicatePoints,
            ref SamplePointsMaterialGroup currentMaterialGroup,
            ref SamplePointsPatch currentPatch,
            out float currentDistance,
            out bool duplicatePoint)
        {
            if (options.UVEdge || options.UVShift)
                extendedUVData.Add(
                    new SamplePointUData(
                        positionsCount,
                        options
                    )
                );

            // clamp point at CP and maybe duplicate the next sample point
            currentDistance = options.Distance;
            duplicatePoint = options.HardEdge || options.MaterialID != currentMaterialGroup.MaterialID || options.UVEdge;
            // end the current patch...
            if (duplicatePoint)
            {
                duplicatePoints.Add(
                    new DuplicateSamplePoint(
                        positionsCount,
                        positionsCount + 1,
                        options.HardEdge
                    )
                );

                currentPatch.End = positionsCount;
                currentMaterialGroup.Patches.Add(currentPatch);
                // if MaterialID changes, we start a new MaterialGroup
                if (currentMaterialGroup.MaterialID != options.MaterialID)
                {
                    shapeMaterialGroups.Add(currentMaterialGroup);
                    currentMaterialGroup = new SamplePointsMaterialGroup(options.MaterialID);
                }

                // in any case we start a new patch
                currentPatch = new SamplePointsPatch(positionsCount + 1);
                // Extended UV
                if (options.UVEdge || options.UVShift)
                    extendedUVData.Add(
                        new SamplePointUData(
                            positionsCount + 1,
                            options
                        )
                    );
            }
        }

        private static void FillData(CGShape dataToFill,
            SamplePointsMaterialGroup materialGroup,
            SubArrayList<float> sourceFs,
            SubArrayList<float> relativeFs,
            bool isFullPath,
            SubArrayList<Vector3> positions,
            SubArrayList<Vector3> tangents,
            SubArrayList<Vector3> normals,
            bool considerSplineTransform,
            Transform splineTransform,
            Transform generatorTransform)
        {
            if (considerSplineTransform)
            {
                //OPTIM do not do the transform if the spline and generator transforms are the same
                Vector3[] positionsArray = positions.Array;
                for (int i = 0; i < positions.Count; i++)
                    positionsArray[i] =
                        generatorTransform.InverseTransformPoint(splineTransform.TransformPoint(positionsArray[i]));

                if (isFullPath)
                {
                    Vector3[] normalsArray = normals.Array;
                    Vector3[] tangentsArray = tangents.Array;
                    for (int i = 0; i < tangents.Count; i++)
                        tangentsArray[i] =
                            generatorTransform.InverseTransformDirection(splineTransform.TransformDirection(tangentsArray[i]));
                    for (int i = 0; i < normals.Count; i++)
                        normalsArray[i] =
                            generatorTransform.InverseTransformDirection(splineTransform.TransformDirection(normalsArray[i]));
                }
            }

            //OPTIM find a way to have the inputs already as arrays, instead of calling ToArray on them
            dataToFill.MaterialGroups.Add(materialGroup);

            dataToFill.SourceRelativeDistances = sourceFs.ToSubArray();
            dataToFill.RelativeDistances = relativeFs.ToSubArray();
            dataToFill.Positions = positions.ToSubArray();

            if (isFullPath)
            {
                ((CGPath)dataToFill).Directions = tangents.ToSubArray();
                dataToFill.Normals = normals.ToSubArray();
            }
        }

        private static void AddPoint(float sourceF,
            float relativeF,
            bool isFullPath,
            Vector3 position,
            Vector3 tangent,
            Vector3 up,
            ref SubArrayList<float> sourceFList,
            ref SubArrayList<float> relativeFList,
            ref SubArrayList<Vector3> positionList,
            ref SubArrayList<Vector3> tangentList,
            ref SubArrayList<Vector3> upList)
        {
            sourceF = sourceF.Approximately(1f)
                ? 1f
                : sourceF % 1;

#if CURVY_SANITY_CHECKS
            if (relativeF < 0 || relativeF > 1)
                DTLog.LogError("[Curvy] Invalid point's relativeF value " + relativeF);

            if (sourceF < 0 || sourceF > 1)
                DTLog.LogError("[Curvy] Invalid point's sourceF value " + sourceF);
#endif
            sourceFList.Add(sourceF);
            positionList.Add(position);
            relativeFList.Add(relativeF);
            if (isFullPath)
            {
                tangentList.Add(tangent);
                upList.Add(up);
            }
        }


        private static bool MoveByAngleExt(CurvySpline spline,
            bool useCache,
            ref float tf,
            float maxDistance,
            float maxAngle,
            out Vector3 pos,
            out Vector3 tan,
            float stopTF,
            bool loop,
            float stepDist)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(maxAngle >= 0);
            Assert.IsTrue(spline.Count != 0);
#endif

            if (!loop)
                tf = Mathf.Clamp01(tf);
            float tn = loop
                ? tf % 1
                : tf;
            float localF;
            CurvySplineSegment segment;

            segment = spline.TFToSegment(
                tn,
                out localF,
                CurvyClamping.Clamp
            );
            if (useCache)
                segment.InterpolateAndGetTangentFast(
                    localF,
                    out pos,
                    out tan
                );
            else
            {
                pos = segment.Interpolate(
                    localF,
                    Space.Self
                );
                tan = segment.GetTangent(
                    localF,
                    Space.Self
                );
            }

            Vector3 lastPos = pos;
            Vector3 lastTan = tan;

            float movedDistance = 0;
            float angleAccumulator = 0;

            if (stopTF < tf && loop)
                stopTF++;

            bool earlyExitConditionMet = false;
            while (tf < stopTF && earlyExitConditionMet == false)
            {
                tf = Mathf.Min(
                    stopTF,
                    tf + stepDist
                );
                tn = loop
                    ? tf % 1
                    : tf;

                segment = spline.TFToSegment(
                    tn,
                    out localF,
                    CurvyClamping.Clamp
                );
                if (useCache)
                    segment.InterpolateAndGetTangentFast(
                        localF,
                        out pos,
                        out tan
                    );
                else
                {
                    pos = segment.Interpolate(
                        localF,
                        Space.Self
                    );
                    tan = segment.GetTangent(
                        localF,
                        Space.Self
                    );
                }

                Vector3 movement;
                {
                    //Optimized way of substracting lastPos from pos. Optimization works with Mono platforms
                    movement.x = pos.x - lastPos.x;
                    movement.y = pos.y - lastPos.y;
                    movement.z = pos.z - lastPos.z;
                }
                movedDistance += movement.magnitude;

                float tangentsAngle = Vector3.Angle(
                    lastTan,
                    tan
                );
                angleAccumulator += tangentsAngle;

                // Check if conditions are met
                if (movedDistance >= maxDistance // max distance reached
                    || angleAccumulator >= maxAngle // max angle reached
                    || (tangentsAngle == 0 && angleAccumulator > 0)) // current step is linear while the whole movement is not.
                    earlyExitConditionMet = true;
                else
                {
                    lastPos = pos;
                    lastTan = tan;
                }
            }

            return Mathf.Approximately(
                tf,
                stopTF
            );
        }

        #region CalculateExtendedUV

        private static void CalculateExtendedUV(CurvySpline spline, float startTF, float endTF, List<SamplePointUData> ext,
            CGShape data)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(ext.Count > 0);
            Assert.IsTrue(startTF.IsBetween0And1());
            if (spline.Closed)
                DTLog.LogWarning(
                    $"[Curvy] Extended UV is supported only on open splines. Spline name: {spline.name}",
                    spline
                );
#endif

            // we have a list of data, either UV Edge (double then) or Explicit
            // unlike easy mode, U is bound to Shape's SourceRelativeDistances, not RelativeDistances!

            // for the first vertex, find the reference CP and calculate starting U (first vertex never has matching Udata, even if it's over a reference CP!!!)    
            {
                CurvySplineSegment previousReferenceCP, nextReferenceCP;
                MetaCGOptions previousReferenceCPOptions, nextReferenceCPOptions;
                {
                    previousReferenceCPOptions = findPreviousReferenceCPOptions(
                        spline,
                        startTF,
                        out previousReferenceCP
                    );
                    nextReferenceCPOptions = findNextReferenceCPOptions(
                        spline,
                        startTF,
                        out nextReferenceCP
                    );
                }

                // we now know the U range the first vertex is in, so let's calculate it's actual U value
                // get the distance delta within that range
                float frag;
                {
                    float nextReferenceCPDistance;
                    if (spline.FirstVisibleControlPoint == nextReferenceCP)
                    {
                        // Special case: nextReferenceCP is first CP (implies closed spline)
                        nextReferenceCPDistance = spline.Length;
#if CURVY_SANITY_CHECKS
                        Assert.IsTrue(spline.Closed);
#endif
                    }
                    else
                        nextReferenceCPDistance = nextReferenceCP.Distance;

                    frag = ((data.SourceRelativeDistances.Array[0] * spline.Length) - previousReferenceCP.Distance)
                           / (nextReferenceCPDistance - previousReferenceCP.Distance);
                }


                float firstU = Mathf.LerpUnclamped(
                    previousReferenceCPOptions.GetDefinedFirstU(0),
                    nextReferenceCPOptions.GetDefinedFirstU(0),
                    frag
                );

                float secondU = previousReferenceCPOptions.GetDefinedSecondU(0);

                ext.Insert(
                    0,
                    new SamplePointUData(
                        0,
                        startTF == 0 && previousReferenceCPOptions.CorrectedUVEdge,
                        startTF == 0 && previousReferenceCPOptions.CorrectedUVEdge,
                        firstU,
                        secondU
                    )
                );
            }

            // Do the same for the last vertex, find the reference CP and calculate starting U (first vertex never has matching Udata, even if it's over a reference CP!!!)
            if (ext[ext.Count - 1].Vertex < data.Count - 1)
            {
                CurvySplineSegment previousReferenceCP, nextReferenceCP;
                MetaCGOptions previousReferenceCPOptions, nextReferenceCPOptions;
                {
                    previousReferenceCPOptions = findPreviousReferenceCPOptions(
                        spline,
                        endTF,
                        out previousReferenceCP
                    );
                    nextReferenceCPOptions = findNextReferenceCPOptions(
                        spline,
                        endTF,
                        out nextReferenceCP
                    );
                }

                float nextReferenceCPU;
                // Special case: nextReferenceCP is first CP (implies closed spline)
                float frag;
                if (spline.FirstVisibleControlPoint == nextReferenceCP)
                {
#if CURVY_SANITY_CHECKS
                    Assert.IsTrue(spline.Closed);
#endif
                    frag = ((data.SourceRelativeDistances.Array[data.Count - 1] * spline.Length) - previousReferenceCP.Distance)
                           / (spline.Length - previousReferenceCP.Distance);
                    // either take the ending U from 2nd U of first CP or raise last U to next int
                    if (nextReferenceCPOptions.CorrectedUVEdge)
                        nextReferenceCPU = nextReferenceCPOptions.FirstU;
                    else if (ext.Count > 1)
                        nextReferenceCPU = Mathf.FloorToInt(
                                               ext[ext.Count - 1].UVEdge
                                                   ? ext[ext.Count - 1].SecondU
                                                   : ext[ext.Count - 1].FirstU
                                           )
                                           + 1;
                    else
                        nextReferenceCPU = 1;
                }
                else
                {
                    frag = ((data.SourceRelativeDistances.Array[data.Count - 1] * spline.Length) - previousReferenceCP.Distance)
                           / (nextReferenceCP.Distance - previousReferenceCP.Distance);
                    nextReferenceCPU = nextReferenceCPOptions.GetDefinedFirstU(1);
                }

                ext.Add(
                    new SamplePointUData(
                        data.Count - 1,
                        false,
                        false,
                        Mathf.LerpUnclamped(
                            previousReferenceCPOptions.GetDefinedSecondU(0),
                            nextReferenceCPU,
                            frag
                        ),
                        0
                    )
                );
            }

            float startF = 0;
            float lowerBoundU = ext[0].UVEdge
                ? ext[0].SecondU
                : ext[0].FirstU;
            float upperBoundU = ext[1].FirstU;
            float length = data.RelativeDistances.Array[ext[1].Vertex] - data.RelativeDistances.Array[ext[0].Vertex];
            int current = 1;
            for (int vertexIndex = 0; vertexIndex < data.Count - 1; vertexIndex++)
            {
                float curF = (data.RelativeDistances.Array[vertexIndex] - startF) / length;
                data.CustomValues.Array[vertexIndex] = ((upperBoundU - lowerBoundU) * curF) + lowerBoundU;

                if (ext[current].Vertex == vertexIndex
                    //reached last iteration, so no need to update data that will not get used, especially that
                    //the update leads to exceptions in certain cases, where  upperBoundU = ext[current + 1].FirstU accesses
                    //the "ext" array with out of bound index. This happens with open spline of 4 CPs, all having
                    //Hard Edge, and only the last one having Explicit U set
                    && vertexIndex + 1 < data.Count - 1)
                {
                    float nextDistance = data.RelativeDistances.Array[ext[current + 1].Vertex];
                    float currentDistance = data.RelativeDistances.Array[ext[current].Vertex];
                    bool isDuplicatedVertex = nextDistance.Approximately(currentDistance);

                    if (isDuplicatedVertex)
                    {
                        lowerBoundU = ext[current].UVEdge
                            ? ext[current].SecondU
                            : ext[current].FirstU;
                        current++;
                        //update distances
                        currentDistance = nextDistance;
                        nextDistance = data.RelativeDistances.Array[ext[current + 1].Vertex];
                    }
                    else
                        lowerBoundU = ext[current].FirstU;

                    upperBoundU = ext[current + 1].FirstU;
                    length = nextDistance - currentDistance;
                    startF = data.RelativeDistances.Array[vertexIndex];
                    current++;
                }
            }

            data.CustomValues.Array[data.Count - 1] = ext[ext.Count - 1].FirstU;
        }

        private static MetaCGOptions findPreviousReferenceCPOptions(CurvySpline spline, float tf, out CurvySplineSegment cp)
        {
            MetaCGOptions options;
            cp = spline.TFToSegment(tf);
            do
            {
                options = cp.GetMetadata<MetaCGOptions>(true);
                if (spline.FirstVisibleControlPoint == cp)
                    return options;
                cp = spline.GetPreviousSegment(cp);
            } while (cp && !options.CorrectedUVEdge && !options.ExplicitU);

            return options;
        }

        private static MetaCGOptions findNextReferenceCPOptions(CurvySpline spline, float tf, out CurvySplineSegment cp)
        {
            MetaCGOptions options;
            cp = spline.TFToSegment(
                tf,
                out _
            );

            do
            {
                cp = spline.GetNextControlPoint(cp);
                options = cp.GetMetadata<MetaCGOptions>(true);
                if (!spline.Closed && spline.LastVisibleControlPoint == cp)
                    return options;
            } while (!options.CorrectedUVEdge && !options.ExplicitU && !(spline.FirstSegment == cp));

            return options;
        }

        #endregion

        #endregion

        #region Protected members

        protected abstract CurvySpline InputSpline { get; set; }

        protected virtual void OnSplineAssigned()
        {
            //todo why does this class has nothing similar to SplineProcessor's UnbindEvents?
            if (InputSpline)
            {
                InputSpline.OnRefresh.AddListenerOnce(OnSplineRefreshed);
                InputSpline.OnInitialized.AddListenerOnce(OnSplineInitialized);

                //OnSplineAssigned can be called multiple times, in OnValidate for example. This line is to avoid setting OnInputSplineCoordinatesChanged as a listener multiple times
                InputSpline.OnGlobalCoordinatesChanged -= OnInputSplineCoordinatesChanged;

                InputSpline.OnGlobalCoordinatesChanged += OnInputSplineCoordinatesChanged;
            }
        }

        protected void ValidateStartAndEndCps()
        {
            if (InputSpline == null)
                return;
            if (InputSpline.IsInitialized == false)
                return;

            if (m_StartCP && m_StartCP.Spline != InputSpline)
            {
                DTLog.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[Curvy] Input module {0}: StartCP is not part of the input spline ({1})",
                        name,
                        InputSpline.name
                    ),
                    this
                );
                m_StartCP = null;
            }

            if (m_EndCP && m_EndCP.Spline != InputSpline)
            {
                DTLog.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[Curvy] Input module {0}: EndCP is not part of the input spline ({1})",
                        name,
                        InputSpline.name
                    ),
                    this
                );
                m_EndCP = null;
            }

            if (m_EndCP != null
                && m_StartCP != null
                && InputSpline.GetControlPointIndex(m_EndCP) <= InputSpline.GetControlPointIndex(m_StartCP))
            {
                DTLog.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[Curvy] Input module {0}: EndCP has an index ({1}) less or equal than StartCP ({2})",
                        name,
                        InputSpline.GetControlPointIndex(m_EndCP),
                        InputSpline.GetControlPointIndex(m_StartCP)
                    ),
                    this
                );
                m_EndCP = null;
            }
        }

        #endregion
    }
}