// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Mesh Resource Component used by Curvy Generator
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgmeshresource")]
#pragma warning disable CS0618
    public class CGMeshResource : DuplicateEditorMesh, IPoolable
#pragma warning restore CS0618
    {
        /// <summary>
        /// The value of the "Everything" entry in a <see cref="MeshCollider.cookingOptions"/>'s inspector
        /// </summary>
        public const MeshColliderCookingOptions EverMeshColliderCookingOptions =
            MeshColliderCookingOptions.EnableMeshCleaning
            | MeshColliderCookingOptions.CookForFasterSimulation
            | MeshColliderCookingOptions.UseFastMidphase
            | MeshColliderCookingOptions.WeldColocatedVertices;

        private MeshRenderer mRenderer;
        private Collider mCollider;

        public MeshRenderer Renderer
        {
            get
            {
                if (mRenderer == null)
                    mRenderer = GetComponent<MeshRenderer>();
                return mRenderer;
            }
        }

        public Collider Collider
        {
            get
            {
                if (mCollider == null)
                    mCollider = GetComponent<Collider>();
                return mCollider;
            }
        }

        [UsedImplicitly]
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public Mesh Prepare()
            => Filter.PrepareNewShared();

        public bool ColliderMatches(
            CGColliderEnum type)
        {
            if (Collider == null && type == CGColliderEnum.None)
                return true;
            if (Collider is MeshCollider && type == CGColliderEnum.Mesh)
                return true;
            if (Collider is BoxCollider && type == CGColliderEnum.Box)
                return true;
            if (Collider is SphereCollider && type == CGColliderEnum.Sphere)
                return true;
            if (Collider is CapsuleCollider && type == CGColliderEnum.Capsule)
                return true;

            return false;
        }

        public void RemoveCollider()
        {
            if (Collider)
            {
                mCollider.Destroy(
                    false,
                    false
                );
                mCollider = null;
            }
        }

        /// <summary>
        /// Updates the collider if existing, and create a new one if not.
        /// </summary>
        /// <param name="mode">The collider's type</param>
        /// <param name="convex">Used only when mode is CGColliderEnum.Mesh</param>
        /// <param name="isTrigger">Is the collider a Trigger</param>
        /// <param name="material">The collider's material</param>
        /// <param name="meshCookingOptions">Used only when mode is CGColliderEnum.Mesh</param>
        /// <returns></returns>
        public bool UpdateCollider(
            CGColliderEnum mode,
            bool convex,
            bool isTrigger,
#if UNITY_6000_0_OR_NEWER
            PhysicsMaterial
#else
            PhysicMaterial
#endif
                material,
            MeshColliderCookingOptions meshCookingOptions = EverMeshColliderCookingOptions
        )
        {
            if (Collider == null)
                switch (mode)
                {
                    case CGColliderEnum.Mesh:
                        mCollider = gameObject.AddComponent<MeshCollider>();
                        break;
                    case CGColliderEnum.Box:
                        mCollider = gameObject.AddComponent<BoxCollider>();
                        break;
                    case CGColliderEnum.Sphere:
                        mCollider = gameObject.AddComponent<SphereCollider>();
                        break;
                    case CGColliderEnum.Capsule:
                        mCollider = gameObject.AddComponent<CapsuleCollider>();
                        break;
                    case CGColliderEnum.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if (mode != CGColliderEnum.None)
            {
                switch (mode)
                {
                    case CGColliderEnum.Mesh:
                        MeshCollider meshCollider = Collider as MeshCollider;
                        if (meshCollider != null)
                        {
                            meshCollider.sharedMesh = null;
                            meshCollider.convex = convex;
                            meshCollider.isTrigger = isTrigger;
                            meshCollider.cookingOptions = meshCookingOptions;
                            try
                            {
                                meshCollider.sharedMesh = Filter.sharedMesh;
                            }
#if CURVY_SANITY_CHECKS
                            catch (Exception e)
                            {
                                DTLog.LogException(
                                    e,
                                    this
                                );
#else
                            catch
                            {
#endif
                                return false;
                            }
                        }
                        else
                            DTLog.LogError(
                                "[Curvy] Collider of wrong type",
                                this
                            );

                        break;
                    case CGColliderEnum.Box:
                        BoxCollider boxCollider = Collider as BoxCollider;
                        if (boxCollider != null)
                        {
                            boxCollider.isTrigger = isTrigger;
                            boxCollider.center = Filter.sharedMesh.bounds.center;
                            boxCollider.size = Filter.sharedMesh.bounds.size;
                        }
                        else
                            DTLog.LogError(
                                "[Curvy] Collider of wrong type",
                                this
                            );

                        break;
                    case CGColliderEnum.Sphere:
                        SphereCollider sphereCollider = Collider as SphereCollider;
                        if (sphereCollider != null)
                        {
                            sphereCollider.isTrigger = isTrigger;
                            sphereCollider.center = Filter.sharedMesh.bounds.center;
                            sphereCollider.radius = Filter.sharedMesh.bounds.extents.magnitude;
                        }
                        else
                            DTLog.LogError(
                                "[Curvy] Collider of wrong type",
                                this
                            );

                        break;
                    case CGColliderEnum.Capsule:
                        CapsuleCollider capsuleCollider = Collider as CapsuleCollider;
                        if (capsuleCollider != null)
                        {
                            Bounds sharedMeshBounds = Filter.sharedMesh.bounds;
                            capsuleCollider.isTrigger = isTrigger;
                            capsuleCollider.center = sharedMeshBounds.center;
                            capsuleCollider.radius = new Vector2(
                                sharedMeshBounds.extents.x,
                                sharedMeshBounds.extents.y
                            ).magnitude;
                            capsuleCollider.height = sharedMeshBounds.size.z;
                            capsuleCollider.direction = 2; //Z
                        }
                        else
                            DTLog.LogError(
                                "[Curvy] Collider of wrong type",
                                this
                            );

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Collider.material = material;
            }

            return true;
        }

        public void OnBeforePush()
        {
            MeshFilter meshFilter = Filter;
            Mesh sharedMesh = meshFilter.sharedMesh;
            if (ReferenceEquals(
                    sharedMesh,
                    null
                )
                == false)
            {
                sharedMesh.Clear();
                sharedMesh.subMeshCount = 0;
            }

            transform.DeleteChildren(
                false,
                true
            );
        }

        public void OnAfterPop()
        {
            MeshFilter meshFilter = Filter;
            if (ReferenceEquals(
                    meshFilter.sharedMesh,
                    null
                ))
            {
                Mesh mesh = GetNewMesh();
                meshFilter.sharedMesh = mesh;
            }
        }

        private static Mesh GetNewMesh()
        {
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            UsedMeshes.Add(mesh);

            return mesh;
        }

        private static Mesh GetNewMesh(
            [NotNull] Mesh oldMesh)
        {
            Mesh mesh = Instantiate(oldMesh);
            mesh.MarkDynamic();
            UsedMeshes.Add(mesh);

            return mesh;
        }

        #region Duplication handling

        /// <summary>
        /// A set of all the meshes used by CGMeshResource instances
        /// </summary>
        private static readonly HashSet<Mesh> UsedMeshes = new HashSet<Mesh>();

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        [UsedImplicitly]
        protected void Awake()
        {
            //If there is another instance using the same SharedMesh, then make this instance use a different mesh. CGMeshResource assumes that its mesh is not shared with other instances

            MeshFilter filter = Filter;
            Mesh mesh = filter.sharedMesh;
            if (ReferenceEquals(
                    mesh,
                    null
                ))
                return;

            if (UsedMeshes.Contains(mesh))
            {
                //The mesh is shared
#if CURVY_SANITY_CHECKS_PRIVATE
#pragma warning disable CS0618
                Assert.IsTrue(UsesSharedMesh(this));
#pragma warning restore CS0618
#endif
                if (mesh.isReadable)
                {
                    //duplicate mesh
                    Mesh newMesh = GetNewMesh(mesh);
                    //reference it in MeshFilter
                    filter.sharedMesh = newMesh;
                    //reference it in MeshCollider
                    MeshCollider meshCollider = Collider as MeshCollider;
                    if (meshCollider != null
                        && ReferenceEquals(
                            meshCollider.sharedMesh,
                            mesh
                        ))
                        meshCollider.sharedMesh = newMesh;
                }
            }
            else
            {
                //the msh is not shared
#if CURVY_SANITY_CHECKS_PRIVATE
#pragma warning disable CS0618
                Assert.IsFalse(UsesSharedMesh(this));
#pragma warning restore CS0618
#endif
                UsedMeshes.Add(mesh);
            }

#if CURVY_SANITY_CHECKS_PRIVATE
            Assert.IsTrue(UsedMeshes.Contains(filter.sharedMesh));
#endif
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            //Remove this instance's mesh from the used meshes set
            Mesh mesh = Filter.sharedMesh;
            if (ReferenceEquals(
                    mesh,
                    null
                ))
                return;

            UsedMeshes.Remove(mesh);

#if CURVY_SANITY_CHECKS_PRIVATE
            Assert.IsFalse(UsedMeshes.Contains(Filter.sharedMesh));
#endif
        }

#endif

        #endregion

        /// <summary>
        /// A brut force method to find CGMeshResource instances using the same mesh
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Too slow, used only in sanity checks")]
        private static bool UsesSharedMesh(
            CGMeshResource meshResource)
        {
            MeshFilter meshFilter = meshResource.Filter;

            if (meshFilter
                && ReferenceEquals(
                    meshFilter.sharedMesh,
                    null
                )
                == false)
            {
                Object[] otherWatchdogs = FindObjectsOfType(typeof(CGMeshResource));

                foreach (CGMeshResource other in otherWatchdogs)
                    if (ReferenceEquals(
                            other,
                            meshResource
                        )
                        == false)
                    {
                        MeshFilter otherMF = other.Filter;
                        if (ReferenceEquals(
                                otherMF,
                                null
                            )
                            == false
                            && ReferenceEquals(
                                otherMF.sharedMesh,
                                meshFilter.sharedMesh
                            ))
                            return true;
                    }
            }

            return false;
        }

        #endregion
    }
}