// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Build/Volume Caps",
        ModuleName = "Volume Caps",
        Description = "Build volume caps"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgbuildvolumecaps")]
    public class BuildVolumeCaps : CGModule
    {
        [HideInInspector]
        [InputSlotInfo(typeof(CGVolume))]
        public CGModuleInputSlot InVolume = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGVolume),
            Optional = true,
            Array = true
        )]
        public CGModuleInputSlot InVolumeHoles = new CGModuleInputSlot();

        // change this to fit your requirements
        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGVMesh),
            Array = true
        )]
        public CGModuleOutputSlot OutVMesh = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [Tab("General")]
        [SerializeField]
        private CGYesNoAuto m_StartCap = CGYesNoAuto.Auto;

        [SerializeField]
        private CGYesNoAuto m_EndCap = CGYesNoAuto.Auto;

        [SerializeField, FormerlySerializedAs("m_ReverseNormals")]
        private bool m_ReverseTriOrder;

        [SerializeField]
        private bool m_GenerateUV = true;

        [SerializeField]
        private bool m_GenerateUV2 = true;

        [Tab("Start Cap")]
        [Inline]
        [SerializeField]
        private CGMaterialSettings m_StartMaterialSettings = new CGMaterialSettings();

        [Label("Material")]
        [SerializeField]
        private Material m_StartMaterial;

        [Tab("End Cap")]
        [SerializeField]
        private bool m_CloneStartCap = true;

        [AsGroup(Invisible = true)]
        [GroupCondition(
            nameof(m_CloneStartCap),
            false
        )]
        [SerializeField]
        private CGMaterialSettings m_EndMaterialSettings = new CGMaterialSettings();

        [Group("Default/End Cap")]
        [Label("Material")]
        [FieldCondition(
            nameof(m_CloneStartCap),
            false
        )]
        [SerializeField]
        private Material m_EndMaterial;

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

        public CGYesNoAuto StartCap
        {
            get => m_StartCap;
            set
            {
                if (m_StartCap != value)
                {
                    m_StartCap = value;
                    Dirty = true;
                }
            }
        }

        public Material StartMaterial
        {
            get => m_StartMaterial;
            set
            {
                if (m_StartMaterial != value)
                {
                    m_StartMaterial = value;
                    Dirty = true;
                }
            }
        }

        public CGMaterialSettings StartMaterialSettings => m_StartMaterialSettings;

        public CGYesNoAuto EndCap
        {
            get => m_EndCap;
            set
            {
                if (m_EndCap != value)
                {
                    m_EndCap = value;
                    Dirty = true;
                }
            }
        }

        public bool CloneStartCap
        {
            get => m_CloneStartCap;
            set
            {
                if (m_CloneStartCap != value)
                {
                    m_CloneStartCap = value;
                    Dirty = true;
                }
            }
        }

        public CGMaterialSettings EndMaterialSettings => m_EndMaterialSettings;

        public Material EndMaterial
        {
            get => m_EndMaterial;
            set
            {
                if (m_EndMaterial != value)
                {
                    m_EndMaterial = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void Awake()
        {
            base.Awake();

            if (StartMaterial == null)
                StartMaterial = CurvyUtility.GetDefaultMaterial();
            if (EndMaterial == null)
                EndMaterial = CurvyUtility.GetDefaultMaterial();
        }

        public override void Reset()
        {
            base.Reset();
            StartCap = CGYesNoAuto.Auto;
            EndCap = CGYesNoAuto.Auto;
            ReverseTriOrder = false;
            GenerateUV = true;
            GenerateUV2 = true;
            m_StartMaterialSettings = new CGMaterialSettings();
            m_EndMaterialSettings = new CGMaterialSettings();
            StartMaterial = CurvyUtility.GetDefaultMaterial();
            EndMaterial = CurvyUtility.GetDefaultMaterial();
            CloneStartCap = true;
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();
            CGVolume vol = InVolume.GetData<CGVolume>(out bool isVolumeDisposable);
            List<CGVolume> holes = InVolumeHoles.GetAllData<CGVolume>(out bool isHolesDisposable);

            if (vol)
            {
                bool genStart = StartCap == CGYesNoAuto.Yes || (StartCap == CGYesNoAuto.Auto && !vol.Seamless);
                bool genEnd = EndCap == CGYesNoAuto.Yes || (EndCap == CGYesNoAuto.Auto && !vol.Seamless);

                if (!genStart && !genEnd)
                {
                    OutVMesh.ClearData();
                    return;
                }

                CGVMesh vmesh = new CGVMesh();
                SubArray<Vector3> vtStart = ArrayPools.Vector3.Allocate(0);

                vmesh.AddSubMesh(new CGVSubMesh());
                CGVSubMesh submesh = vmesh.SubMeshes[0];

                if (genStart)
                {
                    #region --- Start Cap ---

                    Tess tess = new Tess();
                    tess.UsePooling = true;
                    tess.AddContour(
                        make2DSegment(
                            vol,
                            0
                        )
                    );

                    for (int h = 0; h < holes.Count; h++)
                    {
                        if (holes[h].Count < 3)
                        {
                            OutVMesh.ClearData();
                            UIMessages.Add("Hole Cross has less than 3 Vertices: Can't create Caps!");
                            return;
                        }

                        tess.AddContour(
                            make2DSegment(
                                holes[h],
                                0
                            )
                        );
                    }

                    tess.Tessellate(
                        WindingRule.EvenOdd,
                        ElementType.Polygons,
                        3
                    );
                    ArrayPools.Vector3.Free(vtStart);
                    vtStart = UnityLibTessUtility.ContourVerticesToPositions(tess.Vertices);
                    Bounds b;
                    int capIndex = 0;
                    vmesh.Vertices = applyMatrix(
                        vtStart,
                        getMatrix(
                            vol,
                            capIndex,
                            true
                        ),
                        out b
                    );
                    //normals
                    {
                        SubArray<Vector3> normals = ArrayPools.Vector3.Allocate(vmesh.Vertices.Count);
                        {
                            Vector3 capNormal = -vol.Directions.Array[capIndex];
                            for (int i = 0; i < normals.Count; i++)
                                normals.Array[i] = capNormal;
                        }

                        vmesh.NormalsList = normals;
                    }

                    submesh.Material = StartMaterial;
                    submesh.TrianglesList = tess.ElementsArray.Value;
                    if (ReverseTriOrder)
                        flipTris(
                            submesh.TrianglesList,
                            0,
                            submesh.TrianglesList.Count
                        );
                    if (GenerateUV)
                    {
                        vmesh.UVs = ArrayPools.Vector2.Allocate(vtStart.Count);
                        applyUV(
                            vtStart,
                            vmesh.UVs,
                            0,
                            vtStart.Count,
                            StartMaterialSettings,
                            b
                        );
                    }

                    if (GenerateUV2)
                    {
                        vmesh.UV2s = ArrayPools.Vector2.Allocate(vtStart.Count);
                        applyUV2(
                            vtStart,
                            vmesh.UV2s,
                            0,
                            vtStart.Count,
                            b
                        );
                    }

                    #endregion
                }

                if (genEnd)
                {
                    #region --- End Cap ---

                    Tess tess = new Tess();
                    tess.UsePooling = true;
                    tess.AddContour(
                        make2DSegment(
                            vol,
                            vol.Count - 1
                        )
                    );

                    for (int h = 0; h < holes.Count; h++)
                    {
                        if (holes[h].Count < 3)
                        {
                            OutVMesh.ClearData();
                            UIMessages.Add("Hole Cross has <3 Vertices: Can't create Caps!");
                            return;
                        }

                        tess.AddContour(
                            make2DSegment(
                                holes[h],
                                holes[h].Count - 1
                            )
                        );
                    }

                    tess.Tessellate(
                        WindingRule.EvenOdd,
                        ElementType.Polygons,
                        3
                    );

                    SubArray<Vector3> vtEnd = UnityLibTessUtility.ContourVerticesToPositions(tess.Vertices);
                    Bounds b;
                    int preEndCapVertexLength = vmesh.Vertices.Count;
                    int capIndex = vol.Count - 1;
                    SubArray<Vector3> subVertices = applyMatrix(
                        vtEnd,
                        getMatrix(
                            vol,
                            capIndex,
                            true
                        ),
                        out b
                    );

                    SubArray<Vector3> newVertices;
                    {
                        newVertices = ArrayPools.Vector3.Allocate(vmesh.Vertices.Count + subVertices.Count);
                        Array.Copy(
                            vmesh.Vertices.Array,
                            0,
                            newVertices.Array,
                            0,
                            vmesh.Vertices.Count
                        );
                        Array.Copy(
                            subVertices.Array,
                            0,
                            newVertices.Array,
                            vmesh.Vertices.Count,
                            subVertices.Count
                        );
                    }

                    vmesh.Vertices = newVertices;

                    ArrayPools.Vector3.Free(subVertices);

                    //normals
                    {
                        SubArray<Vector3> normals = ArrayPools.Vector3.Allocate(preEndCapVertexLength);
                        {
                            Vector3 capNormal = vol.Directions.Array[capIndex];
                            for (int i = 0; i < normals.Count; i++)
                                normals.Array[i] = capNormal;
                        }

                        SubArray<Vector3> newNormals;
                        {
                            newNormals = ArrayPools.Vector3.Allocate(vmesh.NormalsList.Count + normals.Count);
                            Array.Copy(
                                vmesh.NormalsList.Array,
                                0,
                                newNormals.Array,
                                0,
                                vmesh.NormalsList.Count
                            );
                            Array.Copy(
                                normals.Array,
                                0,
                                newNormals.Array,
                                vmesh.NormalsList.Count,
                                normals.Count
                            );
                        }
                        vmesh.NormalsList = newNormals;

                        ArrayPools.Vector3.Free(normals);
                    }
                    SubArray<int> tris = tess.ElementsArray.Value;
                    if (!ReverseTriOrder)
                        flipTris(
                            tris,
                            0,
                            tris.Count
                        );
                    for (int i = 0; i < tris.Count; i++)
                        tris.Array[i] += preEndCapVertexLength;
                    if (!CloneStartCap && StartMaterial != EndMaterial)
                        vmesh.AddSubMesh(
                            new CGVSubMesh(
                                tris,
                                EndMaterial
                            )
                        );
                    else
                    {
                        submesh.Material = StartMaterial;

                        SubArray<int> newTrianglesList;
                        {
                            newTrianglesList = ArrayPools.Int32.Allocate(submesh.TrianglesList.Count + tris.Count);
                            Array.Copy(
                                submesh.TrianglesList.Array,
                                0,
                                newTrianglesList.Array,
                                0,
                                submesh.TrianglesList.Count
                            );
                            Array.Copy(
                                tris.Array,
                                0,
                                newTrianglesList.Array,
                                submesh.TrianglesList.Count,
                                tris.Count
                            );
                        }
                        submesh.TrianglesList = newTrianglesList;
                    }

                    if (GenerateUV)
                    {
                        SubArray<Vector2> newUVs = ArrayPools.Vector2.Allocate(vmesh.UVs.Count + vtEnd.Count);
                        Array.Copy(
                            vmesh.UVs.Array,
                            0,
                            newUVs.Array,
                            0,
                            vmesh.UVs.Count
                        );
                        vmesh.UVs = newUVs;

                        applyUV(
                            vtEnd,
                            vmesh.UVs,
                            vtStart.Count,
                            vtEnd.Count,
                            CloneStartCap
                                ? StartMaterialSettings
                                : EndMaterialSettings,
                            b
                        );
                    }

                    if (GenerateUV2)
                    {
                        SubArray<Vector2> newUV2s = ArrayPools.Vector2.Allocate(vmesh.UV2s.Count + vtEnd.Count);
                        Array.Copy(
                            vmesh.UV2s.Array,
                            0,
                            newUV2s.Array,
                            0,
                            vmesh.UV2s.Count
                        );
                        vmesh.UV2s = newUV2s;
                        applyUV2(
                            vtEnd,
                            vmesh.UV2s,
                            vtStart.Count,
                            vtEnd.Count,
                            b
                        );
                    }

                    ArrayPools.Vector3.Free(vtEnd);

                    #endregion
                }

                ArrayPools.Vector3.Free(vtStart);

                OutVMesh.SetDataToElement(vmesh);
            }

            if (isVolumeDisposable)
                vol.Dispose();

            if (isHolesDisposable)
                holes.ForEach(h => h.Dispose());
        }

        #endregion

        #region ### Privates ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        private static Matrix4x4 getMatrix(CGVolume vol, int index, bool inverse)
        {
            if (inverse)
            {
                Quaternion Q = Quaternion.LookRotation(
                    vol.Directions.Array[index],
                    vol.Normals.Array[index]
                );
                return Matrix4x4.TRS(
                    vol.Positions.Array[index],
                    Q,
                    Vector3.one
                );
            }
            else
            {
                Quaternion Q = Quaternion.Inverse(
                    Quaternion.LookRotation(
                        vol.Directions.Array[index],
                        vol.Normals.Array[index]
                    )
                );
                return Matrix4x4.TRS(
                    -(Q * vol.Positions.Array[index]),
                    Q,
                    Vector3.one
                );
            }
        }


        private static void flipTris(SubArray<int> indices, int start, int end)
        {
            int tmp;
            for (int i = start; i < end; i += 3)
            {
                tmp = indices.Array[i];
                indices.Array[i] = indices.Array[i + 2];
                indices.Array[i + 2] = tmp;
            }
        }

        private static SubArray<Vector3> applyMatrix(SubArray<Vector3> vt, Matrix4x4 matrix, out Bounds bounds)
        {
            SubArray<Vector3> res = ArrayPools.Vector3.Allocate(vt.Count);
            float lx = float.MaxValue;
            float ly = float.MaxValue;
            float hx = float.MinValue;
            float hy = float.MinValue;

            for (int i = 0; i < vt.Count; i++)
            {
                lx = Mathf.Min(
                    vt.Array[i].x,
                    lx
                );
                ly = Mathf.Min(
                    vt.Array[i].y,
                    ly
                );
                hx = Mathf.Max(
                    vt.Array[i].x,
                    hx
                );
                hy = Mathf.Max(
                    vt.Array[i].y,
                    hy
                );
                res.Array[i] = matrix.MultiplyPoint3x4(vt.Array[i]);
            }

            Vector3 sz = new Vector3(
                Mathf.Abs(hx - lx),
                Mathf.Abs(hy - ly)
            );
            bounds = new Bounds(
                new Vector3(
                    lx + (sz.x / 2),
                    ly + (sz.y / 2),
                    0
                ),
                sz
            );
            return res;
        }


        /// <summary>
        /// trs vertices to eliminate Z and eliminate duplicates
        /// </summary>
        private static ContourVertex[] make2DSegment(CGVolume vol, int segmentIndex)
        {
            Matrix4x4 m = getMatrix(
                vol,
                segmentIndex,
                false
            );
            int vertexIndex = vol.GetSegmentIndex(segmentIndex);

            ContourVertex[] res = new ContourVertex[vol.CrossSize];
            for (int i = 0; i < vol.CrossSize; i++)
                res[i] = m.MultiplyPoint3x4(vol.Vertices.Array[vertexIndex + i]).ContourVertex();

            return res;
        }

        // Attention: p needs to be 2D (X/Y-Plane)
        private static void applyUV(SubArray<Vector3> vts, SubArray<Vector2> uvArray, int index, int count,
            CGMaterialSettings mat, Bounds bounds)
        {
            float u, v;
            float w = bounds.size.x;
            float h = bounds.size.y;

            float mx = bounds.min.x;
            float my = bounds.min.y;

            float fx = mat.UVScale.x;
            float fy = mat.UVScale.y;

            switch (mat.KeepAspect)
            {
                case CGKeepAspectMode.ScaleU:
                    float sw = w * mat.UVScale.y;
                    float sh = h * mat.UVScale.x;
                    fx *= sw / sh;
                    break;
                case CGKeepAspectMode.ScaleV:
                    float sw1 = w * mat.UVScale.y;
                    float sh1 = h * mat.UVScale.x;
                    fy *= sh1 / sw1;
                    break;
            }

            bool swapUv = mat.SwapUV;

            if (mat.UVRotation != 0)
            {
                float uvRotRad = mat.UVRotation * Mathf.Deg2Rad;
                float sn = Mathf.Sin(uvRotRad);
                float cs = Mathf.Cos(uvRotRad);
                float ox, oy;
                float fx2 = fx * 0.5f;
                float fy2 = fy * 0.5f;
                for (int i = 0; i < count; i++)
                {
                    u = ((vts.Array[i].x - mx) / w) * fx;
                    v = ((vts.Array[i].y - my) / h) * fy;
                    ox = u - fx2;
                    oy = v - fy2;
                    u = ((cs * ox) - (sn * oy)) + fx2 + mat.UVOffset.x;
                    v = (sn * ox) + (cs * oy) + fy2 + mat.UVOffset.y;

                    int uvArrayIndex = i + index;
                    Vector2 uv;
                    uv.x = swapUv
                        ? v
                        : u;
                    uv.y = swapUv
                        ? u
                        : v;
                    uvArray.Array[uvArrayIndex] = uv;
                }
            }
            else
                for (int i = 0; i < count; i++)
                {
                    u = mat.UVOffset.x + (((vts.Array[i].x - mx) / w) * fx);
                    v = mat.UVOffset.y + (((vts.Array[i].y - my) / h) * fy);
                    int uvArrayIndex = i + index;
                    Vector2 uv;
                    uv.x = swapUv
                        ? v
                        : u;
                    uv.y = swapUv
                        ? u
                        : v;
                    uvArray.Array[uvArrayIndex] = uv;
                }
        }

        private static void applyUV2(SubArray<Vector3> vertice, SubArray<Vector2> uv2Array, int index, int count, Bounds bounds)
        {
            float inverseW = 1 / bounds.size.x;
            float inverseH = 1 / bounds.size.y;

            float mx = bounds.min.x;
            float my = bounds.min.y;

            for (int i = 0; i < count; i++)
            {
                Vector2 uv;
                uv.x = (vertice.Array[i].x - mx) * inverseW;
                uv.y = (vertice.Array[i].y - my) * inverseH;
                uv2Array.Array[i + index] = uv;
            }
        }


#endif

        #endregion
    }
}