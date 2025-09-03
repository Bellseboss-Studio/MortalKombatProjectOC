// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections.Generic;
using System;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// Manages pools of Unity objects
    /// </summary>
    /// <seealso cref="PrefabPool"/>
    /// <seealso cref="ComponentPool"/>
    [HelpURL(DTUtility.HelpUrlBase + "dtpoolmanager")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PoolManager : DTVersionedMonoBehaviour
    {
        [Section("General")]
        [SerializeField]
        [Tooltip("Whether pools should be automatically created when requested but not found")]
        private bool m_AutoCreatePools = true;

        [AsGroup(Expanded = false)]
        [SerializeField]
        private PoolSettings m_DefaultSettings = new PoolSettings();

        /// <summary>
        /// Whether pools should be automatically created when requested but not found
        /// </summary>
        public bool AutoCreatePools
        {
            get => m_AutoCreatePools;
            set
            {
                if (m_AutoCreatePools != value)
                    m_AutoCreatePools = value;
            }
        }

        public PoolSettings DefaultSettings
        {
            get => m_DefaultSettings;
            set
            {
                if (m_DefaultSettings != value)
                    m_DefaultSettings = value;
                if (m_DefaultSettings != null)
                    m_DefaultSettings.Validate();
            }
        }

        public bool IsInitialized { get; private set; }
#pragma warning disable CS0618
        public int Count => Pools.Count + TypePools.Count;
#pragma warning restore CS0618

        public Dictionary<string, IPool> Pools = new Dictionary<string, IPool>();

        [UsedImplicitly] [Obsolete("TypePools are no more part of Curvy Splines")]
        public Dictionary<Type, IPool> TypePools = new Dictionary<Type, IPool>();

        [UsedImplicitly] [Obsolete] private IPool[] mPools = new IPool[0];

        protected override void OnValidate()
        {
            base.OnValidate();

            DefaultSettings = m_DefaultSettings;
        }

        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            IsInitialized = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            IsInitialized = false;
        }

        [UsedImplicitly] private void Update()
        {
            bool initialize;
#if UNITY_EDITOR
            //todo this forces the pool manager to initialize every frame. Not a good idea.
            //I assume it was done to force the checks done in the Initialize method to be executed every frame, to handle pools duplication for example.
            //A better way to do this would be to separate initialization with checks, and execute the checks only when needed
            initialize = !Application.isPlaying;
#else
            initialize = !IsInitialized;
#endif
            if (initialize)
                Initialize();

#pragma warning disable CS0618
#pragma warning disable CS0612
            if (mPools.Length != TypePools.Count)
            {
                Array.Resize(ref mPools, TypePools.Count);
                TypePools.Values.CopyTo(mPools, 0);
            }

            //todo remove this once mPools contains only UnityObjectPools
            for (int i = 0; i < mPools.Length; i++)
                mPools[i].Update(); //no need 
#pragma warning restore CS0612
#pragma warning restore CS0618
        }

        private void Initialize()
        {
            Pools.Clear();
            IPool[] goPools = GetComponents<IPool>();
            foreach (IPool p in goPools)
            {
                if (p is ComponentPool)
                {
                    if (Pools.ContainsKey(p.Identifier) == false)
                        Pools.Add(p.Identifier, p);
                    else
                    {
                        DTLog.Log("[DevTools] Found a duplicated ComponentPool for type " + p.Identifier + ". The duplicated pool will be destroyed", this);
                        (p as ComponentPool).Destroy(false, false);
                    }
                }
                else
                {
                    p.Identifier = GetUniqueIdentifier(p.Identifier);
                    Pools.Add(p.Identifier, p);
                }
            }

            IsInitialized = true;
        }

        [NotNull]
        public string GetUniqueIdentifier([NotNull] string ident)
        {
            int num = 0;
            string id = ident;
            while (Pools.ContainsKey(id))
                id = ident + (++num).ToString();
            return id;
        }

        [UsedImplicitly] [Obsolete("TypePools are no more part of Curvy Splines")]
        public Pool<T> GetTypePool<T>()
        {
            IPool res;
            if (!TypePools.TryGetValue(typeof(T), out res))
            {
                if (AutoCreatePools)
                {
                    res = CreateTypePool<T>();
                }
            }
            return (Pool<T>)res;
        }

        public ComponentPool GetComponentPool<T>() where T : Component
        {
            if (!IsInitialized)
                Initialize();
            IPool res;
            if (!Pools.TryGetValue(typeof(T).AssemblyQualifiedName, out res))
            {
                if (AutoCreatePools)
                {
                    res = CreateComponentPool<T>();
                }
            }
            return (ComponentPool)res;
        }

        public PrefabPool GetPrefabPool([NotNull] string identifier, params GameObject[] prefabs)
        {
            if (!IsInitialized)
                Initialize();
            IPool pool;
            if (!Pools.TryGetValue(identifier, out pool))
            {
                if (AutoCreatePools)
                    pool = CreatePrefabPool(identifier, null, prefabs);
            }
            return (PrefabPool)pool;
        }

        [UsedImplicitly] [Obsolete("TypePools are no more part of Curvy Splines")]
        public Pool<T> CreateTypePool<T>(PoolSettings settings = null)
        {
            PoolSettings s = settings ?? new PoolSettings(DefaultSettings);
            IPool res;
            if (!TypePools.TryGetValue(typeof(T), out res))
            {
                res = new Pool<T>(s);
                TypePools.Add(typeof(T), res);
            }
            return (Pool<T>)res;
        }

        public ComponentPool CreateComponentPool<T>(PoolSettings settings = null) where T : Component
        {
            if (!IsInitialized)
                Initialize();
            PoolSettings s = settings ?? new PoolSettings(DefaultSettings);
            IPool res;
            if (!Pools.TryGetValue(typeof(T).AssemblyQualifiedName, out res))
            {
                res = gameObject.AddComponent<ComponentPool>();
                ((ComponentPool)res).Initialize(typeof(T), s);
                Pools.Add(res.Identifier, res);
            }
            return (ComponentPool)res;
        }

        public PrefabPool CreatePrefabPool([NotNull] string name, PoolSettings settings = null, params GameObject[] prefabs)
        {
            if (!IsInitialized)
                Initialize();
            PoolSettings s = settings ?? new PoolSettings(DefaultSettings);

            IPool pool;
            if (!Pools.TryGetValue(name, out pool))
            {
                PrefabPool p = gameObject.AddComponent<PrefabPool>();
                p.Initialize(name, s, prefabs);
                Pools.Add(name, p);
                return p;
            }
            return (PrefabPool)pool;
        }

        public List<IPool> FindPools(string identifierStartsWith)
        {
            List<IPool> res = new List<IPool>();
            foreach (KeyValuePair<string, IPool> kv in Pools)
                if (kv.Key.StartsWith(identifierStartsWith))
                    res.Add(kv.Value);
            return res;
        }

        public void DeletePools(string startsWith)
        {
            List<IPool> toDelete = FindPools(startsWith);
            for (int i = toDelete.Count - 1; i >= 0; i--)
                DeletePool(toDelete[i]);

        }

        public void DeletePool(IPool pool)
        {
            if (pool is PrefabPool || pool is ComponentPool)
            {
                ((MonoBehaviour)pool).Destroy(false, false);
                Pools.Remove(pool.Identifier);
            }
        }

        [UsedImplicitly] [Obsolete("TypePools are no more part of Curvy Splines")]
        public void DeletePool<T>()
        {
            TypePools.Remove(typeof(T));
        }
    }
}
