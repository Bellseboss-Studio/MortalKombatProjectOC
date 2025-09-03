// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// A pool for UnityEngine.GameObject instances
    /// </summary>
    [RequireComponent(typeof(PoolManager))]
    [HelpURL(DTUtility.HelpUrlBase + "dtprefabpool")]
    public class PrefabPool : UnityObjectPool<GameObject>
    {
        [FieldCondition(nameof(m_Identifier), "", false, ActionAttribute.ActionEnum.ShowWarning, "Please enter an identifier! (Select a prefab to set automatically)")]
        [SerializeField]
        private string m_Identifier = String.Empty;

        [SerializeField] private List<GameObject> m_Prefabs = new List<GameObject>();

        public override string Identifier
        {
            get => m_Identifier;
            set => m_Identifier = value;
        }

        public List<GameObject> Prefabs
        {
            get => m_Prefabs;
            set => m_Prefabs = value;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_Identifier == String.Empty
                && Prefabs.Any(p => p != null))
                Identifier = Prefabs.First().name;
        }

        public void Initialize([NotNull] string identifier, PoolSettings settings, params GameObject[] prefabs)
        {
            Identifier = identifier;
            Prefabs = new List<GameObject>(prefabs);
            Initialize(settings);
        }

        protected override GameObject CreateObject()
        {
            if (Prefabs.Count == 0)
                throw new InvalidOperationException($"[Curvy] The Prefab Pool '{Identifier}' in game object '{gameObject.name}' could not create a pool element because its Prefabs list is empty");

            //TODO should this Random.Range call be deterministic?
            GameObject prefab = Prefabs[UnityEngine.Random.Range(0, Prefabs.Count)];
            if (prefab == null)
                throw new InvalidOperationException($"[Curvy] The Prefab Pool '{Identifier}' in game object '{gameObject.name}' could not create a pool element because its Prefabs list contains a null or destroyed object");

            GameObject result;
            {
#if UNITY_EDITOR
                bool isPrefabAsset;
                //We have to check GetPrefabInstanceStatus first, because GetPrefabAssetType returns (as far as I understand) the same thing for both a prefab asset and a prefab instance
                if (PrefabUtility.GetPrefabInstanceStatus(prefab) == PrefabInstanceStatus.NotAPrefab)
                {
                    PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);
                    isPrefabAsset = prefabAssetType == PrefabAssetType.Regular || prefabAssetType == PrefabAssetType.Variant;
                }
                else
                    isPrefabAsset = false;

                result = isPrefabAsset
                    ? PrefabUtility.InstantiatePrefab(prefab) as GameObject
                    : Instantiate(prefab);
#else
                result = Instantiate(prefab);
#endif
            }

            if (result == null)
                throw new InvalidOperationException($"[Curvy] The Prefab Pool '{Identifier}' in game object '{gameObject.name}' could not instantiate prefab {prefab.name}");

            ConfigureCreatedGameObject(result, prefab.name);

            return result;
        }

        protected override GameObject GetItemGameObject(GameObject item)
            => item;
    }
}
