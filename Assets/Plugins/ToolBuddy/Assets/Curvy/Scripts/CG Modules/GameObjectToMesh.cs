// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// Creates <see cref="CGVMesh"/>s from the meshes of GameObjects
    /// </summary>
    [ModuleInfo(
        "Convert/GameObject To Mesh",
        ModuleName = "GameObject To Mesh",
        Description = "Converts GameObjects to Volume Meshes"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cggameobject2mesh")]
    public class GameObjectToMesh : CGModule
    {
        /// <summary>
        /// Input Game Objects
        /// </summary>
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGGameObject),
            Array = true
        )]
        public CGModuleInputSlot InGameObjects = new CGModuleInputSlot();

        /// <summary>
        /// Output Volume Meshes
        /// </summary>
        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGVMesh),
            Array = true
        )]
        public CGModuleOutputSlot OutVMesh = new CGModuleOutputSlot();


        #region ### Serialized Fields ###

        [SerializeField]
        [Tooltip("Whether to include or not the meshes from the input Game Objects' children")]
        private bool useChildrenMeshes;

        [SerializeField]
        [Tooltip("Forces the output mesh to be centered")]
        private bool centerMesh;

        #endregion


        /// <summary>
        /// Whether to include or not the meshes from the input Game Objects' children
        /// </summary>
        public bool UseChildrenMeshes
        {
            get => useChildrenMeshes;
            set
            {
                if (value != useChildrenMeshes)
                {
                    useChildrenMeshes = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Forces the output mesh to be centered
        /// </summary>
        public bool CenterMesh
        {
            get => centerMesh;
            set
            {
                if (value != centerMesh)
                {
                    centerMesh = value;
                    Dirty = true;
                }
            }
        }

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            UseChildrenMeshes = false;
            CenterMesh = false;
        }

#endif

        #endregion

        public override void Refresh()
        {
            base.Refresh();

            if (!OutVMesh.IsLinked)
                return;

            List<CGGameObject> gameObjects = InGameObjects.GetAllData<CGGameObject>(out bool isDataDisposable);
            List<CGVMesh> data = new List<CGVMesh>(gameObjects.Count);
            foreach (CGGameObject cgGameObject in gameObjects)
            {
                GameObject inputGameObject = cgGameObject.Object;

                if (inputGameObject == null)
                    continue;

                Mesh mesh;
                Material[] materials;
                if (UseChildrenMeshes)
                {
                    mesh = CombineMeshFilters(
                        inputGameObject.GetComponentsInChildren<MeshFilter>(false),
                        out List<Material> materialsList,
                        inputGameObject.transform.worldToLocalMatrix,
                        UIMessages
                    );

                    materials = materialsList.ToArray();
                }
                else
                {
                    MeshFilter meshFilter = inputGameObject.GetComponent<MeshFilter>();

                    if (meshFilter == null)
                    {
                        UIMessages.Add(
                            $"GameObject '{inputGameObject.name}' has no Mesh Filter associated to it. If you want to use Mesh Filters in its children, set the 'Use Children Mesh' parameter to true"
                        );
                        continue;
                    }

                    mesh = meshFilter.sharedMesh;

                    //materials;
                    {
                        MeshRenderer meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer == null)
                        {
                            UIMessages.Add(
                                $"GameObject '{inputGameObject.name}' has a Mesh Filter but no Mesh Renderer associated to it. No material will be assigned to this mesh"
                            );
                            materials = new Material[0];
                        }
                        else
                            materials = meshRenderer.sharedMaterials;
                    }
                }

                Matrix4x4 trsMatrix = cgGameObject.Matrix;
                if (centerMesh)
                    trsMatrix *= Matrix4x4.Translate(-mesh.bounds.center);

                if (mesh.isReadable == false)
                    UIMessages.Add(
                        $"GameObject '{inputGameObject.name}' has a mesh '{mesh.name}' that is not readable. Please set the 'Read/Write Enabled' parameter to true in the mesh model import settings"
                    );

                data.Add(
                    new CGVMesh(
                        mesh,
                        materials,
                        trsMatrix
                    )
                );
            }

            OutVMesh.SetDataToCollection(data.ToArray());

            if (isDataDisposable)
                foreach (CGGameObject cgGameObject in gameObjects)
                    cgGameObject.Dispose();
        }

        /// <summary>
        /// Takes multiple <see cref="MeshFilter"/>s and return a mesh containing all their meshes, each one of them being assigned to a subMesh id.
        /// </summary>
        /// <param name="meshFilters">mesh filters from which the meshes to combine will be taken</param>
        /// <param name="materials">The materials for all the subMeshes.Those materials are taken from <see cref="MeshRenderer"/>s associated with the input <see cref="MeshFilter"/>s. If none, the material wiL be set to null</param>
        /// <param name="originTrs">The TRS matrix of the origin point</param>
        /// <param name="errorMessages">An array in which error messages will be added. Can be null</param>
        /// <returns></returns>
        public static Mesh CombineMeshFilters(MeshFilter[] meshFilters, out List<Material> materials, Matrix4x4 originTrs
            , [CanBeNull] List<string> errorMessages)
        {
            List<CombineInstance> combiners = new List<CombineInstance>(meshFilters.Length);
            materials = new List<Material>(meshFilters.Length);
            List<Material> tempMaterialsList = new List<Material>(1);

            int vertexTotalCount = 0;
            int vertexTotalCount_submeshDuplicate = 0;

            Mesh combinedMesh = new Mesh();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;

                if (mesh.isReadable == false)
                    errorMessages?.Add(
                        $"Mesh '{mesh.name}' is not readable. Please set the 'Read/Write Enabled' parameter to true in the mesh model import settings."
                    );

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    combiners.Add(
                        new CombineInstance
                        {
                            transform = originTrs * meshFilter.transform.localToWorldMatrix,
                            mesh = mesh,
                            subMeshIndex = i
                        }
                    );
                    vertexTotalCount_submeshDuplicate += mesh.vertexCount;
                }

                vertexTotalCount += mesh.vertexCount;


                MeshRenderer meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    errorMessages?.Add(
                        $"GameObject '{meshFilter.gameObject.name}' has a Mesh Filter but no Mesh Renderer associated to it. No material will be assigned to this mesh"
                    );
                    for (int k = 0; k < mesh.subMeshCount; k++)
                        materials.Add(null);
                }
                else
                {
                    meshRenderer.GetSharedMaterials(tempMaterialsList);
                    materials.AddRange(tempMaterialsList);
                }
            }

            //it seems there is a bug in CombineMeshes where it counts the vertex count for each submesh as equal to the whole mesh (in some circumstances, happened to me only at scene opening, go figure). So before the call to CombineMeshes, I set indexFormat accordingly, then after the call I set it to according to the real value of vertexTotalCount
            combinedMesh.indexFormat = vertexTotalCount_submeshDuplicate >= UInt16.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            combinedMesh.CombineMeshes(
                combiners.ToArray(),
                false
            );
            IndexFormat realIndexFormat = vertexTotalCount >= UInt16.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            if (combinedMesh.indexFormat != realIndexFormat)
                combinedMesh.indexFormat = realIndexFormat;

            return combinedMesh;
        }
    }
}