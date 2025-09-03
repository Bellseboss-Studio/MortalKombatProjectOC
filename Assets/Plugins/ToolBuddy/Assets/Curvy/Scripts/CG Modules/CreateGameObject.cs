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

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Create/GameObject",
        ModuleName = "Create GameObject"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgcreategameobject")]
    public class CreateGameObject : ResourceExportingModule
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGGameObject),
            Array = true,
            Name = "GameObject"
        )]
        public CGModuleInputSlot InGameObjectArray = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGSpots),
            Name = "Spots"
        )]
        public CGModuleInputSlot InSpots = new CGModuleInputSlot();

        [SerializeField, CGResourceCollectionManager(
             "GameObject",
             ShowCount = true
         )]
        private CGGameObjectResourceCollection m_Resources = new CGGameObjectResourceCollection();

        #region ### Serialized Fields ###

        [Tab("General")]
        [SerializeField]
        private bool m_MakeStatic;

        [SerializeField]
        [Layer]
        private int m_Layer;

        [Tooltip(
            "Whether Layer should be applied only on the root of a created game object, or it should be applied on its whole hierarchy"
        )]
        [SerializeField]
        private bool applyLayerOnChildren;

        #endregion

        #region ### Public Properties ###

        public int Layer
        {
            get => m_Layer;
            set
            {
                int v = Mathf.Clamp(
                    value,
                    0,
                    32
                );
                if (m_Layer != v)
                {
                    m_Layer = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Whether the layer defined in <see cref="Layer"/> should be applied only on the root of a created game object, or it should be applied on its whole hierarchy
        /// </summary>
        public bool ApplyLayerOnChildren
        {
            get => applyLayerOnChildren;
            set
            {
                if (applyLayerOnChildren != value)
                {
                    applyLayerOnChildren = value;
                    Dirty = true;
                }
            }
        }

        public bool MakeStatic
        {
            get => m_MakeStatic;
            set
            {
                if (m_MakeStatic != value)
                {
                    m_MakeStatic = value;
                    Dirty = true;
                }
            }
        }

        public CGGameObjectResourceCollection GameObjects => m_Resources;

        public int GameObjectCount => GameObjects.Count;

        #endregion

        #region ### Private Fields & Properties ###

        /// <summary>
        /// Key is the created game objects' transforms. Value is the associated pool name. Is filled in the Refresh method.
        /// </summary>
        private readonly Dictionary<Transform, string> usedPoolsDictionary = new Dictionary<Transform, string>();

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false


        public override void Reset()
        {
            base.Reset();
            MakeStatic = false;
            Layer = 0;
            ApplyLayerOnChildren = false;
        }

        protected override void OnDestroy()
        {
            if (!Generator.Destroying)
                DeleteAllPrefabPools();
            base.OnDestroy();
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override bool DeleteAllOutputManagedResources()
        {
            bool result = base.DeleteAllOutputManagedResources();

            //delete all children
            int childCount = transform.childCount;
            result |= childCount > 0;

            Transform[] destructionTargets = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
                destructionTargets[i] = transform.GetChild(i);

            //it might seem a good idea to not use destructionTargets, and just iterate through all children and delete them, but the deletion code can, depending on different on edit/play mode and prefab status, either delete instantly the object, delete it at the end of the frame, or not delete it at all, leading to the iteration logic having to handle all of those cases in deciding what should be the iteration index. I prefer to play it safe, and use the destructionTargets list
            foreach (Transform child in destructionTargets)
                if (usedPoolsDictionary.TryGetValue(
                        child,
                        out string poolName
                    ))
                    DeleteManagedResource(
                        "GameObject",
                        child,
                        poolName
                    );
                else
                    DeleteManagedResource(
                        "GameObject",
                        child,
                        string.Empty,
                        true
                    );

            GameObjects.Items.Clear();
            GameObjects.PoolNames.Clear();
            usedPoolsDictionary.Clear();

            return result;
        }

        [UsedImplicitly]
        [Obsolete("Use DeleteAllOutputManagedResources instead")]
        public void Clear() =>
            DeleteAllOutputManagedResources();

        public override void Refresh()
        {
            base.Refresh();

            TryDeleteChildrenFromAssociatedPrefab();
            DeleteAllOutputManagedResources();

            List<CGGameObject> VGO = InGameObjectArray.GetAllData<CGGameObject>(out bool isGOsDisposable);
            CGSpots Spots = InSpots.GetData<CGSpots>(out bool isSpotsDisposable);

            List<IPool> existingPools = GetAllPrefabPools();
            HashSet<string> usedPools = new HashSet<string>();

            GameObjects.Items.Clear();
            GameObjects.PoolNames.Clear();
            usedPoolsDictionary.Clear();

            if (VGO.Count > 0 && Spots.Count > 0)
            {
                CGSpot spot;
                for (int s = 0; s < Spots.Count; s++)
                {
                    spot = Spots.Spots.Array[s];
                    int id = spot.Index;
                    if (id >= 0 && id < VGO.Count && VGO[id].Object != null)
                    {
                        CGGameObject inputCGGameObject = VGO[id];

                        string poolIdent = GetPrefabPool(inputCGGameObject.Object).Identifier;
                        usedPools.Add(poolIdent);
                        Transform res = (Transform)AddManagedResource(
                            "GameObject",
                            poolIdent,
                            s
                        );
                        res.gameObject.isStatic = MakeStatic;
                        res.gameObject.layer = Layer;
                        if (ApplyLayerOnChildren)
                        {
                            //Optim: GetComponentsInChildren does an allocation to make the array, which GetChild doesn't need. So an alternative would be to make a recursive method that goes through all children an apply an action on them. For now, I will just stick with the simplest implementation
                            Transform[] descendants = res.gameObject.GetComponentsInChildren<Transform>(true);
                            foreach (Transform descendantTransform in descendants)
                                descendantTransform.gameObject.layer = Layer;
                        }

                        res.localPosition = spot.Position;
                        res.localRotation = spot.Rotation;
                        res.localScale = new Vector3(
                            inputCGGameObject.Object.transform.localScale.x * spot.Scale.x * inputCGGameObject.Scale.x,
                            inputCGGameObject.Object.transform.localScale.y * spot.Scale.y * inputCGGameObject.Scale.y,
                            inputCGGameObject.Object.transform.localScale.z * spot.Scale.z * inputCGGameObject.Scale.z
                        );

                        if (inputCGGameObject.Translate != Vector3.zero)
                            res.Translate(inputCGGameObject.Translate);
                        if (inputCGGameObject.Rotate != Vector3.zero)
                            res.Rotate(inputCGGameObject.Rotate);

                        GameObjects.Items.Add(res);
                        GameObjects.PoolNames.Add(poolIdent);

                        usedPoolsDictionary[res] = poolIdent;
                    }
                }
            }

            // Remove unused pools
            foreach (IPool pool in existingPools)
                if (!usedPools.Contains(pool.Identifier))
                    Generator.PoolManager.DeletePool(pool);

            if (isGOsDisposable)
                VGO.ForEach(d => d.Dispose());
            if (isSpotsDisposable)
                Spots.Dispose();
        }

        #endregion

        protected override GameObject SaveResourceToScene(Component managedResource, Transform newParent)
        {
            GameObject duplicateGameObject = managedResource.gameObject.DuplicateGameObject(newParent);
            duplicateGameObject.name = managedResource.name;
            return duplicateGameObject;
        }

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            usedPoolsDictionary.Clear();
        }
    }
#endif
}