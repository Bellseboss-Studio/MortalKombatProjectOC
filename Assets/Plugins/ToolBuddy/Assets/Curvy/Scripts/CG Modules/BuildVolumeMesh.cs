// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Build/Volume Mesh",
        ModuleName = "Volume Mesh",
        Description = "Build a volume mesh"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgbuildvolumemesh")]
    public class BuildVolumeMesh : CGModule
    {
        private const float DefaultUnscalingOrigin = 0.5f;
        private const int DefaultSplitLength = 100;


        [HideInInspector]
        [InputSlotInfo(typeof(CGVolume))]
        public CGModuleInputSlot InVolume = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGVMesh),
            Array = true
        )]
        public CGModuleOutputSlot OutVMesh = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [Tab("General")]
        [FieldAction("CBAddMaterial")]
        [SerializeField, FormerlySerializedAs("m_ReverseNormals")]
        private bool m_ReverseTriOrder;

        [Section("Default/General/UV")]
        [SerializeField]
        private bool m_GenerateUV = true;

        [SerializeField]
        [Tooltip(
            "When set to true, and if the input Shape Extrusion module is set to apply scaling, the U coordinate of the generated mesh will be modified to compensate that scaling.\nOnly the X component of the scaling is taken into consideration.\nThe unscaling works best on volumes with flat shapes."
        )]
        [FieldCondition(
            nameof(m_GenerateUV),
            true
        )]
        private bool unscaleU;

        [SerializeField]
        [FieldCondition(
            nameof(unscaleU),
            true,
            false,
            ConditionalAttribute.OperatorEnum.AND,
            nameof(m_GenerateUV),
            true,
            false
        )]
        [Tooltip(
            "When unscaling the U coordinate, this field defines what is the scaling origin.\n0.5 gives usually the best results, but you might need to set it to a different value, usually between 0 and 1"
        )]
        private float unscalingOrigin = DefaultUnscalingOrigin;

        [SerializeField]
        private bool m_GenerateUV2 = true;

        [Section("Default/General/Split")]
        [Tooltip("Split the mesh into submeshes")]
        [SerializeField]
        private bool m_Split;

        [Positive(MinValue = 1)]
        [FieldCondition(
            nameof(m_Split),
            true
        )]
        [SerializeField]
        private float m_SplitLength = DefaultSplitLength;

        [Group(
            "Default/General/Backward Compatibility",
            Expanded = false
        )]
        [Tooltip(
            "Is ignored when Split or Generate UV2 is false.\nIf enabled, UV2s of a split mesh will be computed as in Curvy versions prior to 8.0.0, which had a bug: all the split submeshes used the full range of UV2 coordinates, instead of keeping the same UV2s from the unsplit mesh."
        )]
        [FieldCondition(
            nameof(IsSplitUV2Togglable),
            true,
            false,
            ActionAttribute.ActionEnum.Enable
        )]
        [SerializeField]
        private bool splitUV2;

        // SubMesh-Settings

        [SerializeField, HideInInspector]
        private List<CGMaterialSettingsEx> m_MaterialSettings = new List<CGMaterialSettingsEx>();

        [SerializeField, HideInInspector]
        private Material[] m_Material = new Material[0];

        #endregion

        #region ### Public Properties ###

        public bool GenerateUV
        {
            get => m_GenerateUV;
            set
            {
                if (m_GenerateUV != value)
                {
                    m_GenerateUV = value;
                    Dirty = true;
                }
            }
        }

        public bool GenerateUV2
        {
            get => m_GenerateUV2;
            set
            {
                if (m_GenerateUV2 != value)
                {
                    m_GenerateUV2 = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// When set to true, and if the input Shape Extrusion module is set to apply scaling, the U coordinate of the generated mesh will be modified to compensate that scaling.
        /// Only the X component of the scaling is taken into consideration.
        /// The unscaling works best on volumes with flat shapes.
        /// </summary>
        public bool UnscaleU
        {
            get => unscaleU;
            set
            {
                if (unscaleU != value)
                {
                    unscaleU = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// When unscaling the U coordinate, this field defines what is the scaling origin.
        /// 0.5 gives usually the best results, but you might need to set it to a different value, usually between 0 and 1
        /// </summary>
        public float UnscalingOrigin
        {
            get => unscalingOrigin;
            set
            {
                if (unscalingOrigin != value)
                {
                    unscalingOrigin = value;
                    Dirty = true;
                }
            }
        }

        public bool ReverseTriOrder
        {
            get => m_ReverseTriOrder;
            set
            {
                if (m_ReverseTriOrder != value)
                {
                    m_ReverseTriOrder = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Split the mesh into submeshes
        /// </summary>
        public bool Split
        {
            get => m_Split;
            set
            {
                if (m_Split != value)
                {
                    m_Split = value;
                    Dirty = true;
                }
            }
        }

        public float SplitLength
        {
            get => m_SplitLength;
            set
            {
                float v = Mathf.Max(
                    1,
                    value
                );
                if (m_SplitLength != v)
                {
                    m_SplitLength = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// For backward compatibility only.
        /// Is ignored when Split or Generate UV2 is false.
        /// If enabled, UV2s of a split mesh will be computed as in Curvy versions prior to 8.0.0, which had a bug: all the split submeshes used the full range of UV2 coordinates, instead of keeping the same UV2s from the unsplit mesh.
        /// </summary>
        public bool SplitUV2
        {
            get => splitUV2;
            set
            {
                if (splitUV2 != value)
                {
                    splitUV2 = value;
                    Dirty = true;
                }
            }
        }

        [Obsolete("Use MaterialSettings (with the correct number of Ts) instead")]
        public List<CGMaterialSettingsEx> MaterialSetttings => MaterialSettings;

        public List<CGMaterialSettingsEx> MaterialSettings => m_MaterialSettings;

        public int MaterialCount => m_MaterialSettings.Count;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void Awake()
        {
            base.Awake();
            if (MaterialCount == 0)
                AddMaterial();
        }

        public override void Reset()
        {
            base.Reset();
            GenerateUV = true;
            GenerateUV2 = true;
            UnscaleU = false;
            UnscalingOrigin = DefaultUnscalingOrigin;
            Split = false;
            SplitLength = DefaultSplitLength;
            SplitUV2 = false;
            ReverseTriOrder = false;
            m_MaterialSettings = new List<CGMaterialSettingsEx>(new CGMaterialSettingsEx[1] { new CGMaterialSettingsEx() });
            m_Material = new Material[1] { CurvyUtility.GetDefaultMaterial() };
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();
            CGVolume vol = InVolume.GetData<CGVolume>(out bool isDisposable);

            if (vol && vol.Count > 0 && vol.CrossSize > 0 && vol.CrossMaterialGroups.Count > 0)
            {
                List<IntRegion> volSets = new List<IntRegion>();
                if (Split)
                {
                    float dist;
                    float lastdist = 0;
                    int lastIndex = 0;
                    for (int sample = 0; sample < vol.Count; sample++)
                    {
                        /*OPTIM F here, contrary to splines, is proportional to distance. So instead of working with distances, which means a call to vol.FToDistance at each iteration, just work with Fs
                         After some tests, this optimization is really not important for now. Maybe in the future, when other parts will be optimized, it would be worth doing
                         */
                        dist = vol.FToDistance(vol.RelativeDistances.Array[sample]);
                        if (dist - lastdist >= SplitLength)
                        {
                            volSets.Add(
                                new IntRegion(
                                    lastIndex,
                                    sample
                                )
                            );
                            lastdist = dist;
                            lastIndex = sample;
                        }
                    }

                    if (lastIndex < vol.Count - 1)
                        volSets.Add(
                            new IntRegion(
                                lastIndex,
                                vol.Count - 1
                            )
                        );
                }
                else
                    volSets.Add(
                        new IntRegion(
                            0,
                            vol.Count - 1
                        )
                    );

                CGVMesh[] data = new CGVMesh[volSets.Count];
                // We have groups (different MaterialID) of patches (e.g. by Hard Edges).
                // Create Collection of groups sharing the same material ID
                List<SamplePointsMaterialGroupCollection> materialIdGroups = getMaterialIDGroups(vol);

                for (int sub = 0; sub < volSets.Count; sub++)
                {
                    CGVMesh cgvMesh = CGVMesh.Get(
                        null,
                        vol,
                        volSets[sub],
                        GenerateUV,
                        GenerateUV2,
                        ReverseTriOrder
                    );
                    build(
                        cgvMesh,
                        vol,
                        volSets[sub],
                        materialIdGroups
                    );
                    data[sub] = cgvMesh;
                }

                OutVMesh.SetDataToCollection(data);
            }
            else
                OutVMesh.ClearData();

            if (isDisposable)
                vol.Dispose();
        }

        public int AddMaterial()
        {
#if UNITY_EDITOR
            Undo.RecordObject(
                this,
                "Add Material"
            );
#endif
            m_MaterialSettings.Add(new CGMaterialSettingsEx());
            m_Material = m_Material.Add(CurvyUtility.GetDefaultMaterial());
            Dirty = true;
            return MaterialCount;
        }

        public void RemoveMaterial(int index)
        {
            if (!validateMaterialIndex(index))
                return;

#if UNITY_EDITOR
            Undo.RecordObject(
                this,
                "Remove Material"
            );
#endif
            m_MaterialSettings.RemoveAt(index);
            m_Material = m_Material.RemoveAt(index);
            Dirty = true;
        }

        public void SetMaterial(int index, Material mat)
        {
            if (!validateMaterialIndex(index) || mat == m_Material[index])
                return;
            if (m_Material[index] != mat)
            {
#if UNITY_EDITOR
                Undo.RecordObject(
                    this,
                    "Set Material"
                );
#endif
                m_Material[index] = mat;
                Dirty = true;
            }
        }

        public Material GetMaterial(int index)
        {
            if (!validateMaterialIndex(index))
                return null;
            return m_Material[index];
        }

        #endregion

        #region ### Privates ###

        private void build([NotNull] CGVMesh vmesh, CGVolume vol, IntRegion subset,
            List<SamplePointsMaterialGroupCollection> materialIdGroups)
        {
            // Because each Material ID forms a submesh
            // Do we need to calculate localU?

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(GenerateUV == false || vmesh.UVs.Count == vmesh.Vertices.Count);
            Assert.IsTrue(GenerateUV2 == false || vmesh.UV2s.Count == vmesh.Vertices.Count);
#endif

            // Prepare Submeshes
            prepareSubMeshes(
                vmesh,
                materialIdGroups,
                subset.Length,
                ref m_Material
            );
            //prepareSubMeshes(vmesh, materialIdGroups, vol.Count - 1, ref m_Material);

            SamplePointsMaterialGroupCollection col;
            SamplePointsMaterialGroup grp;

            int vtIdx = 0;
            SubArray<int> triIdx = ArrayPools.Int32.Allocate(materialIdGroups.Count); // triIdx for each submesh
            // for all sample segments (except the last) along the path, create Triangles to the next segment 
            for (int sample = subset.From; sample < subset.To; sample++)
            {
                // for each submesh (collection)
                for (int subMeshIdx = 0; subMeshIdx < materialIdGroups.Count; subMeshIdx++)
                {
                    col = materialIdGroups[subMeshIdx];
                    // create UV and triangles for all groups in submesh
                    for (int g = 0; g < col.Count; g++)
                    {
                        grp = col[g];
                        if (GenerateUV)
                            createMaterialGroupUV(
                                vmesh,
                                vol,
                                grp,
                                col.MaterialID,
                                col.AspectCorrectionV,
                                col.AspectCorrectionU,
                                sample,
                                vtIdx
                            );

                        if (GenerateUV2)
                            createMaterialGroupUV2(
                                vmesh,
                                vol,
                                grp,
                                sample,
                                vtIdx
                            );
                        for (int p = 0; p < grp.Patches.Count; p++)
                            createPatchTriangles(
                                vmesh.SubMeshes[subMeshIdx].TrianglesList.Array,
                                ref triIdx.Array[subMeshIdx],
                                vtIdx + grp.Patches[p].Start,
                                grp.Patches[p].Count,
                                vol.CrossSize,
                                ReverseTriOrder
                            );
                    }
                }

                vtIdx += vol.CrossSize;
            }

            // UV && UV2 for last path segment
            // for each submesh (collection)
            for (int subMeshIdx = 0; subMeshIdx < materialIdGroups.Count; subMeshIdx++)
            {
                col = materialIdGroups[subMeshIdx];
                // create triangles
                for (int g = 0; g < col.Count; g++)
                {
                    grp = col[g];
                    if (GenerateUV)
                        createMaterialGroupUV(
                            vmesh,
                            vol,
                            grp,
                            col.MaterialID,
                            col.AspectCorrectionV,
                            col.AspectCorrectionU,
                            subset.To,
                            vtIdx
                        );

                    if (GenerateUV2)
                        createMaterialGroupUV2(
                            vmesh,
                            vol,
                            grp,
                            subset.To,
                            vtIdx
                        );
                }
            }

            ArrayPools.Int32.Free(triIdx);

            //normalize UV2's V coordinate
            if (Split && GenerateUV2 && SplitUV2)
            {
                Vector2[] uv2sArray = vmesh.UV2s.Array;

                float minV = uv2sArray[0].y;
                float maxV = uv2sArray[vmesh.UV2s.Count - 1].y;

#if CURVY_SANITY_CHECKS_PRIVATE
                {
                    //Checks the assumption used in finding max an min V values
                    float minVTest = float.MaxValue;
                    float maxVTest = 0;
                    for (int i = 0; i < vmesh.UV2s.Count; i++)
                    {
                        minVTest = Mathf.Min(
                            minVTest,
                            uv2sArray[i].y
                        );
                        maxVTest = Mathf.Max(
                            maxVTest,
                            uv2sArray[i].y
                        );
                    }

                    Assert.IsTrue(maxVTest.Approximately(maxV));
                    Assert.IsTrue(minVTest.Approximately(minV));
                }
#endif

                float vCorrection = 1f / (maxV - minV);
                for (int i = 0; i < vmesh.UV2s.Count; i++)
                    uv2sArray[i].y = (uv2sArray[i].y - minV) * vCorrection;
            }
        }

        private static void prepareSubMeshes([NotNull] CGVMesh vmesh, List<SamplePointsMaterialGroupCollection> groupsBySubMeshes,
            int extrusions, ref Material[] materials)
        {
            vmesh.SetSubMeshCount(groupsBySubMeshes.Count);
            for (int g = 0; g < groupsBySubMeshes.Count; g++)
            {
                CGVSubMesh sm = vmesh.SubMeshes[g];
                vmesh.SubMeshes[g] = CGVSubMesh.Get(
                    sm,
                    groupsBySubMeshes[g].TriangleCount * extrusions * 3,
                    materials[Mathf.Min(
                        groupsBySubMeshes[g].MaterialID,
                        materials.Length - 1
                    )]
                );
            }
        }

        // OPTIMIZE: Store array of U values and just copy them
        private void createMaterialGroupUV(CGVMesh vmesh, CGVolume volume, SamplePointsMaterialGroup materialGroup, int matIndex,
            float aspectCorrectionV, float aspectCorrectionU, int sample, int baseVertex)
        {
            CGMaterialSettingsEx mat = m_MaterialSettings[matIndex];
            int hi = materialGroup.EndVertex;
            bool swapUV = mat.SwapUV;
            Vector2[] uvsArray = vmesh.UVs.Array;
            float[] crossCustomValues = volume.CrossCustomValues.Array;

            float uMultiplier;
            {
                uMultiplier = mat.UVScale.x * aspectCorrectionU;
                if (UnscaleU)
                    uMultiplier *= volume.Scales.Array[sample].x;
            }

            float v = mat.UVOffset.y + (volume.RelativeDistances.Array[sample] * mat.UVScale.y * aspectCorrectionV);

            for (int c = materialGroup.StartVertex; c <= hi; c++)
            {
                float u = UnscaleU
                    ? mat.UVOffset.x + unscalingOrigin + ((crossCustomValues[c] - unscalingOrigin) * uMultiplier)
                    : mat.UVOffset.x + (crossCustomValues[c] * uMultiplier);
                uvsArray[baseVertex + c].x = swapUV
                    ? v
                    : u;
                uvsArray[baseVertex + c].y = swapUV
                    ? u
                    : v;
            }
        }

        private void createMaterialGroupUV2(CGVMesh vmesh, CGVolume volume, SamplePointsMaterialGroup materialGroup, int sample,
            int baseVertex)
        {
            int hi = materialGroup.EndVertex;
            Vector2[] uv2sArray = vmesh.UV2s.Array;
            for (int c = materialGroup.StartVertex; c <= hi; c++)
            {
                uv2sArray[baseVertex + c].x = volume.CrossRelativeDistances.Array[c];
                uv2sArray[baseVertex + c].y = volume.RelativeDistances.Array[sample];
            }
        }

        /// <summary>
        /// Creates triangles for a cross section
        /// </summary>
        /// <param name="triangles">the triangle array</param>
        /// <param name="triIdx">current tri index</param>
        /// <param name="curVTIndex">base vertex index of this cross section (i.e. the first vertex)</param>
        /// <param name="patchSize"></param>
        /// <param name="crossSize">number of vertices per cross section</param>
        /// <param name="reverse">whether triangles should flip (i.e. a reversed triangle order should be used)</param>
        /// <param name="patchEndVT">size of the cross group (i.e. number of sample points to connect)</param>
        /// <returns></returns>
        private static void createPatchTriangles(int[] triangles, ref int triIdx, int curVTIndex, int patchSize, int crossSize,
            bool reverse)
        {
            int rv0 = reverse
                ? 1
                : 0; // flipping +0 and +1 when reversing
            int rv1 = 1 - rv0;
            int nextCrossVT = curVTIndex + crossSize;
            for (int vt = 0; vt < patchSize; vt++)
            {
                triangles[triIdx + rv0] = curVTIndex + vt;
                triangles[triIdx + rv1] = nextCrossVT + vt;
                triangles[triIdx + 2] = curVTIndex + vt + 1;
                triangles[triIdx + rv0 + 3] = curVTIndex + vt + 1;
                triangles[triIdx + rv1 + 3] = nextCrossVT + vt;
                triangles[triIdx + 5] = nextCrossVT + vt + 1;
                triIdx += 6;
            }
        }

        /// <summary>
        /// Create collections of groups sharing same Material ID. Also ensures collection's MaterialID is valid!
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        private List<SamplePointsMaterialGroupCollection> getMaterialIDGroups(CGVolume volume)
        {
            Dictionary<int, SamplePointsMaterialGroupCollection> matCollections =
                new Dictionary<int, SamplePointsMaterialGroupCollection>();

            SamplePointsMaterialGroupCollection col;

            for (int g = 0; g < volume.CrossMaterialGroups.Count; g++)
            {
                int materialID;
                if (volume.CrossMaterialGroups[g].MaterialID <= MaterialCount - 1)
                    materialID = volume.CrossMaterialGroups[g].MaterialID;
                else
                {
                    UIMessages.Add(
                        $"Input Volume is using material id {volume.CrossMaterialGroups[g].MaterialID}, which has no associate Material in this module. Use the 'Add Material Group'"
                    );
                    materialID = MaterialCount - 1;
                }

                if (!matCollections.TryGetValue(
                        materialID,
                        out col
                    ))
                {
                    col = new SamplePointsMaterialGroupCollection();
                    col.MaterialID = materialID;
                    matCollections.Add(
                        materialID,
                        col
                    );
                }

                col.Add(volume.CrossMaterialGroups[g]);
            }

            List<SamplePointsMaterialGroupCollection> res = new List<SamplePointsMaterialGroupCollection>();

            foreach (SamplePointsMaterialGroupCollection item in matCollections.Values)
            {
                item.CalculateAspectCorrection(
                    volume,
                    MaterialSettings[item.MaterialID]
                );
                res.Add(item);
            }

            return res;
        }

        private bool validateMaterialIndex(int index)
        {
            if (index < 0 || index >= m_MaterialSettings.Count)
            {
                Debug.LogError("TriangulateTube: Invalid Material Index!");
                return false;
            }

            return true;
        }

        private bool IsSplitUV2Togglable => Split && GenerateUV2;

        #endregion
    }
}