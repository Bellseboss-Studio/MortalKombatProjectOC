// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Modifier/Mix Shapes",
        ModuleName = "Mix Shapes",
        Description = "Interpolates between two shapes"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgmixshapes")]
#pragma warning disable 618
    public class ModifierMixShapes : CGModule, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        private const int MixMinValue = -1;
        private const int MixMaxValue = 1;

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGShape),
            Name = "Shape A"
        )]
        public CGModuleInputSlot InShapeA = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGShape),
            Name = "Shape B"
        )]
        public CGModuleInputSlot InShapeB = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(typeof(CGShape))]
        public CGModuleOutputSlot OutShape = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [SerializeField, RangeEx(
             MixMinValue,
             MixMaxValue,
             Tooltip = "Mix between the shapes. Values between -1 for Shape A and 1 for Shape B"
         )]
        private float m_Mix;

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Defines how the result is interpolated. Values between -1 for Shape A and 1 for Shape B
        /// </summary>
        public float Mix
        {
            get => m_Mix;
            set
            {
                float validatedValue = Mathf.Clamp(
                    value,
                    MixMinValue,
                    MixMaxValue
                );
                if (m_Mix != validatedValue)
                {
                    m_Mix = validatedValue;
                    Dirty = true;
                }
            }
        }

        public bool PathIsClosed => IsConfigured
                                    && InShapeA.SourceSlot().PathProvider.PathIsClosed
                                    && InShapeB.SourceSlot().PathProvider.PathIsClosed;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            Properties.LabelWidth = 50;
        }

        public override void Reset()
        {
            base.Reset();
            Mix = 0;
        }

#endif

        #endregion

        #region ### IOnRequestProcessing ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            CGDataRequestRasterization raster = GetRequestParameter<CGDataRequestRasterization>(ref requests);
            if (!raster)
                return Array.Empty<CGData>();

            CGShape DataA = InShapeA.GetData<CGShape>(
                out bool isADisposable,
                requests
            );
            CGShape DataB = InShapeB.GetData<CGShape>(
                out bool isBDisposable,
                requests
            );
            CGShape data = MixShapes(
                DataA,
                DataB,
                Mix,
                UIMessages
            );

            if (isADisposable)
                DataA.Dispose();
            if (isBDisposable)
                DataB.Dispose();

            return data == null
                ? Array.Empty<CGData>()
                : new CGData[] { data };
        }

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Returns the mixed shape
        /// </summary>
        /// <param name="shapeA"></param>
        /// <param name="shapeB"></param>
        /// <param name="mix"> A value between -1 and 1. -1 will select the shape with the most points. 1 will select the other </param>
        /// <param name="warningsContainer">Is filled with warnings raised by the mixing logic</param>
        /// <param name="ignoreWarnings"> If true, warningsContainer will not be filled with warnings</param>
        /// <returns> The mixed shape</returns>
        [CanBeNull]
        public static CGShape MixShapes([CanBeNull] CGShape shapeA, [CanBeNull] CGShape shapeB, float mix,
            [NotNull] List<string> warningsContainer, bool ignoreWarnings = false)
        {
            if (shapeA == null)
                return shapeB;

            if (shapeB == null)
                return shapeA;

            CGShape data = new CGShape();
            InterpolateShape(
                data,
                shapeA,
                shapeB,
                mix,
                warningsContainer,
                ignoreWarnings
            );
            return data;
        }

        /// <summary>
        /// Returns the mixed shape
        /// </summary>
        /// <param name="resultShape">A shape which will be filled with the data of the mixed shape</param>
        /// <param name="mix"> A value between -1 and 1. -1 will select shape A. 1 will select shape B </param>
        /// <param name="shapeA"> One of the two interpolated shapes</param>
        /// <param name="shapeB"> One of the two interpolated shapes</param>
        /// <param name="warningsContainer">Is filled with warnings raised by the mixing logic</param>
        /// <param name="ignoreWarnings"> If true, warningsContainer will not be filled with warnings</param>
        /// <returns> The mixed shape</returns>
        public static void InterpolateShape([NotNull] CGShape resultShape, CGShape shapeA, CGShape shapeB, float mix,
            [NotNull] List<string> warningsContainer, bool ignoreWarnings = false)
        {
            float interpolationTime = (mix + 1) * 0.5f;
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(interpolationTime >= 0);
            Assert.IsTrue(interpolationTime <= 1);
#endif
            int shapeVertexCount = Mathf.Max(
                shapeA.Count,
                shapeB.Count
            );
            CGShape shapeWithMostVertices = shapeA.Count == shapeVertexCount
                ? shapeA
                : shapeB;

            SubArray<Vector3> positions = ArrayPools.Vector3.Allocate(shapeVertexCount);
            SubArray<Vector3> normals = ArrayPools.Vector3.Allocate(shapeVertexCount);

            Vector3[] shapeBPositionsList = shapeB.Positions.Array;
            Vector3[] shapeAPositionsList = shapeA.Positions.Array;
            Vector3[] positionsArray = positions.Array;
            Vector3[] shapeANormalsList = shapeA.Normals.Array;
            Vector3[] shapeBNormalsList = shapeB.Normals.Array;

            if (shapeWithMostVertices == shapeA)
                for (int i = 0; i < shapeVertexCount; i++)
                {
                    float frag;
                    int idx = shapeB.GetFIndex(
                        shapeA.RelativeDistances.Array[i],
                        out frag
                    );

                    Vector3 bPosition;
                    {
                        bPosition.x = shapeBPositionsList[idx].x
                                      + ((shapeBPositionsList[idx + 1].x - shapeBPositionsList[idx].x) * frag);
                        bPosition.y = shapeBPositionsList[idx].y
                                      + ((shapeBPositionsList[idx + 1].y - shapeBPositionsList[idx].y) * frag);
                        bPosition.z = shapeBPositionsList[idx].z
                                      + ((shapeBPositionsList[idx + 1].z - shapeBPositionsList[idx].z) * frag);
                    }

                    positionsArray[i].x =
                        shapeAPositionsList[i].x + ((bPosition.x - shapeAPositionsList[i].x) * interpolationTime);
                    positionsArray[i].y =
                        shapeAPositionsList[i].y + ((bPosition.y - shapeAPositionsList[i].y) * interpolationTime);
                    positionsArray[i].z =
                        shapeAPositionsList[i].z + ((bPosition.z - shapeAPositionsList[i].z) * interpolationTime);


                    Vector3 bNormal = Vector3.SlerpUnclamped(
                        shapeBNormalsList[idx],
                        shapeBNormalsList[idx + 1],
                        frag
                    );
                    normals.Array[i] = Vector3.SlerpUnclamped(
                        shapeANormalsList[i],
                        bNormal,
                        interpolationTime
                    );
                }
            else
                for (int i = 0; i < shapeVertexCount; i++)
                {
                    float frag;
                    int idx = shapeA.GetFIndex(
                        shapeB.RelativeDistances.Array[i],
                        out frag
                    );

                    Vector3 aPosition;
                    {
                        aPosition.x = shapeAPositionsList[idx].x
                                      + ((shapeAPositionsList[idx + 1].x - shapeAPositionsList[idx].x) * frag);
                        aPosition.y = shapeAPositionsList[idx].y
                                      + ((shapeAPositionsList[idx + 1].y - shapeAPositionsList[idx].y) * frag);
                        aPosition.z = shapeAPositionsList[idx].z
                                      + ((shapeAPositionsList[idx + 1].z - shapeAPositionsList[idx].z) * frag);
                    }

                    positionsArray[i].x = aPosition.x + ((shapeBPositionsList[i].x - aPosition.x) * interpolationTime);
                    positionsArray[i].y = aPosition.y + ((shapeBPositionsList[i].y - aPosition.y) * interpolationTime);
                    positionsArray[i].z = aPosition.z + ((shapeBPositionsList[i].z - aPosition.z) * interpolationTime);

                    Vector3 aNormal = Vector3.SlerpUnclamped(
                        shapeANormalsList[idx],
                        shapeANormalsList[idx + 1],
                        frag
                    );
                    normals.Array[i] = Vector3.SlerpUnclamped(
                        aNormal,
                        shapeBNormalsList[i],
                        interpolationTime
                    );
                }

            resultShape.Positions = positions;

            resultShape.RelativeDistances = ArrayPools.Single.Allocate(shapeVertexCount);
            // sets Length and F
            resultShape.Recalculate();

            /*TODO BUG the following 4 properties are tied to the shape geometry, and should be recomputed based on the mixed mesh's geometry instead of using an approximate result.
             This will be specially visible when shape A and shape B have very different values of those properties, such as one of them having different material groups while the other having only one.
             3 of the 4 properties use shapeWithMostVertices. The issue with this is that shapeWithMostVertices can switch between the shape A and shape B depending on the shape's rasterization properties. To test/reproduce, set a square and circle as shapes, and set their rasterization to Optimize = true and Angle Threshold = 120. In those conditions, the square has more vertices. Then set the threshold to 10. In those conditions the circle has more vertices
              */

            resultShape.Normals = normals;
            resultShape.CustomValues = ArrayPools.Single.Clone(shapeWithMostVertices.CustomValues);
            resultShape.SourceRelativeDistances = ArrayPools.Single.Clone(shapeWithMostVertices.SourceRelativeDistances);
            resultShape.MaterialGroups = shapeWithMostVertices.MaterialGroups.Select(g => g.Clone()).ToList();

            if (ignoreWarnings == false)
            {
                if (shapeA.Closed != shapeB.Closed)
                    warningsContainer.Add("Mixing inputs with different Closed values is not supported");
                if (shapeA.Seamless != shapeB.Seamless)
                    warningsContainer.Add("Mixing inputs with different Seamless values is not supported");
                if (shapeA.SourceIsManaged != shapeB.SourceIsManaged)
                    warningsContainer.Add("Mixing inputs with different SourceIsManaged values is not supported");
            }

            resultShape.Closed = shapeA.Closed;
            resultShape.Seamless = shapeA.Seamless;
            resultShape.SourceIsManaged = shapeA.SourceIsManaged;
        }

        #endregion
    }
}