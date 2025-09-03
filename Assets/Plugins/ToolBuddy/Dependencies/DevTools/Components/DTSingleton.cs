// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace FluffyUnderware.DevTools
{

    public class DTSingleton<T> : MonoBehaviour, IDTSingleton where T : MonoBehaviour, IDTSingleton
    {
        private static volatile T _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes static fields
        /// </summary>
        protected static void InitializeStaticFields()
        {
            _instance = default;
        }

        public static bool HasInstance => _instance != null;

        [CanBeNull]
        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                // Instance returns null when in prefab stage, to avoid two things:
                // 1- creating a new instance in scene or prefab
                // 2- interfering with the potentially existing instance in the scene
                bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
                if (isInPrefabStage)
                    return null;
#endif 

                if (_instance != null)
                    return _instance;

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        Object[] tObjects = ObjectExt.FindObjectsOfTypeInCurrentStage<T>(
                            false,
                            true
                        );

                        foreach (T tObject in tObjects)
                        {
                            if (tObject != null)
                            {
                                _instance = tObject;
                                break;
                            }
#if CURVY_SANITY_CHECKS
                            DTLog.LogError($"[Curvy] DTSingleton.Instance : FindObjectsOfType {typeof(T).FullName} returned an invalid reference");
#endif
                        }

                        if (_instance == null)
                        {

#if UNITY_2022_2_OR_NEWER
                            //TODO Consider using SceneManager.loadedSceneCount if performance is significantly better than SceneManager.GetActiveScene().isLoaded
#endif
                            bool noSceneLoaded = SceneManager.GetActiveScene().isLoaded == false;

                            if (noSceneLoaded)
                            {
                                // No scene to instantiate the manager in
                            }
                            else
                            {
                                GameObject singleton = new GameObject();
                                //The component is added while the object is disabled to avoid triggering the awake method, so that _instance is assigned before Awake is called (since Awake will call Instance) 
                                singleton.SetActive(false);
                                _instance = singleton.AddComponent<T>();
                                singleton.SetActive(true);
                            }
                        }
                    }
                }

#if CURVY_SANITY_CHECKS_PRIVATE
                if (_instance == null && SceneManager.GetActiveScene().isLoaded)
                    DTLog.LogError("[Curvy] Couldn't find Curvy Global Manager. Please raise a bug report.");
#endif

                return _instance;
            }
        }

        public virtual void Awake()
        {
            bool destroySelf = false;
            lock (_lock)
            {
                T instance = Instance;

                if (instance != null && instance.GetInstanceID() != GetInstanceID())
                {
                    {
                        instance.MergeDoubleLoaded(this);
                        destroySelf = true;
                    }
                }
            }

            if (destroySelf)
            {
                bool destroyed = gameObject.Destroy(false, true);
                if (destroyed == false)
                {
                    DTLog.LogError($"[Curvy] Couldn't destroy duplicate singleton {gameObject.name} gameobject. Will destroy only its singleton component instead.");
                    this.Destroy(false, false);
                }
            }
        }

        public virtual void MergeDoubleLoaded(IDTSingleton newInstance)
        {
        }
    }

    public interface IDTSingleton
    {
        void MergeDoubleLoaded(IDTSingleton newInstance);
    }
}
