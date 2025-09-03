// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
#endif



namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Curvy Generator component
    /// </summary>
    [ExecuteAlways]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "generator")]
    [AddComponentMenu("Curvy/Generator")]
    [RequireComponent(typeof(PoolManager))]
    public partial class CurvyGenerator : DTVersionedMonoBehaviour
    {
        #region ### Serialized Fields ###

        [Tooltip("Whether to automatically refresh the generator's output when necessary")]
        [SerializeField]
        private bool m_AutoRefresh = true;

        [FieldCondition(
            nameof(m_AutoRefresh),
            true
        )]
        [Positive(
            Tooltip =
                "The minimum delay between two automatic generator's refreshing while in Play mode. Expressed in milliseconds of real time"
        )]
        [SerializeField]
        private int m_RefreshDelay;

        [FieldCondition(
            nameof(m_AutoRefresh),
            true
        )]
        [Positive(
            Tooltip =
                "The minimum delay between two automatic generator's refreshing while in Edit mode. Expressed in milliseconds of real time"
        )]
        [SerializeField]
        private int m_RefreshDelayEditor = 10;

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [Section(
            "Events",
            false,
            false,
            1000,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "generator_events"
        )]
        [SerializeField]
        protected CurvyCGEvent m_OnRefresh = new CurvyCGEvent();

#endif

#if UNITY_EDITOR
        [Section(
            "Advanced Settings",
            Sort = 2000,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "generator_events",
            Expanded = false
        )]
        [Label(Tooltip = "Force this script to update in Edit mode as often as in Play mode. Most users don't need that.")]
        [SerializeField]
        private bool m_ForceFrequentUpdates;
#endif

        /// <summary>
        /// List of modules this Generator contains
        /// </summary>
        [HideInInspector]
        public List<CGModule> Modules = new List<CGModule>();

        [UsedImplicitly]
        [Obsolete("No more used. Retrieve the Ids from Modules by using Modules[x].UniqueID")]
        internal int m_LastModuleID
        {
            get => Modules.Max(m => m.UniqueID);
            set => throw new InvalidOperationException("ModulesByID can't be set");
        }

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Gets or sets whether to automatically call <see cref="Refresh"/> if necessary
        /// </summary>
        public bool AutoRefresh
        {
            get => m_AutoRefresh;
            set => m_AutoRefresh = value;
        }

        /// <summary>
        /// Gets or sets the minimum delay between two consecutive calls to <see cref="Refresh"></see> in play mode. Expressed in milliseconds of real time
        /// </summary>
        public int RefreshDelay
        {
            get => m_RefreshDelay;
            set
            {
                int v = Mathf.Max(
                    0,
                    value
                );
                if (m_RefreshDelay != v)
                    m_RefreshDelay = v;
            }
        }

        /// <summary>
        /// Gets or sets the minimum delay between two consecutive calls to <see cref="Refresh"></see> in edit mode. Expressed in milliseconds of real time
        /// </summary>
        public int RefreshDelayEditor
        {
            get => m_RefreshDelayEditor;
            set
            {
                int v = Mathf.Max(
                    0,
                    value
                );
                if (m_RefreshDelayEditor != v)
                    m_RefreshDelayEditor = v;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// By default Unity calls scripts' update less frequently in Edit mode. ForceFrequentUpdates forces this script to update in Edit mode as often as in Play mode. Most users don't need that.
        /// </summary>
        public bool ForceFrequentUpdates
        {
            get => m_ForceFrequentUpdates;
            set => m_ForceFrequentUpdates = value;
        }
#endif

        /// <summary>
        /// Gets the PoolManager
        /// </summary>
        public PoolManager PoolManager
        {
            get
            {
                if (poolManager == null)
                    poolManager = GetComponent<PoolManager>();
                return poolManager;
            }
        }

        /// <summary>
        /// Event raised after refreshing the Generator
        /// </summary>
        public CurvyCGEvent OnRefresh
        {
            get => m_OnRefresh;
            set => m_OnRefresh = value;
        }

        /// <summary>
        /// Gets whether the generator and all its dependencies are fully initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets whether the Generator is about to get destroyed
        /// </summary>
        public bool Destroying { get; private set; }

        /// <summary>
        /// Dictionary to get a module by it's ID
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Dictionary no more used. Retrieve he Ids from Modules by using Modules[x].UniqueID")]
        public Dictionary<int, CGModule> ModulesByID
        {
            get => Modules.ToDictionary(
                m => m.UniqueID,
                m => m
            );
            set => throw new InvalidOperationException("ModulesByID can't be set");
        }

        #endregion

        #region ### Private Fields ###

        private bool isInitialized;
        private bool isInitializedPhaseOne;
        private PoolManager poolManager;


        [NotNull]
        private readonly Timer autoRefreshTimer = new Timer();

        [NotNull]
        private readonly ModuleSorter moduleSorter = new ModuleSorter();

        [NotNull]
        private readonly ModulesSynchronizer modulesSynchronizer = new ModulesSynchronizer();

#if UNITY_EDITOR || CURVY_DEBUG
        // Debugging:
        public TimeMeasure DEBUG_ExecutionTime = new TimeMeasure(5);

#endif

        /// <summary>
        /// Used in the modules reordering logic. Value's unit is pixels.
        /// </summary>
        private const int ModulesReorderingDeltaX = 50;

        /// <summary>
        /// Used in the modules reordering logic. Value's unit is pixels.
        /// </summary>
        private const int ModulesReorderingDeltaY = 20;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            PoolManager.AutoCreatePools = true;
#if UNITY_EDITOR
            EditorApplication.update += editorUpdate;
            if (!Application.isPlaying)
                ComponentUtility.MoveComponentUp(this);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            isInitialized = false;
            isInitializedPhaseOne = false;
#if UNITY_EDITOR
            EditorApplication.update -= editorUpdate;
#endif
        }


        [UsedImplicitly]
        private void OnDestroy() =>
            Destroying = true;

#if UNITY_EDITOR
        private void editorUpdate()
        {
            if (AutoRefresh && Application.isPlaying == false)
            {
                if (ForceFrequentUpdates)
                    EditorApplication.QueuePlayerLoopUpdate();
                else
                    Update();
            }
        }
#endif

        [UsedImplicitly]
        private void Update()
        {
            if (!IsInitialized)
                Initialize();
            else
            {
                modulesSynchronizer.ProcessRequests(this);
                TryAutoRefresh();
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTransformChildrenChanged()
        {
            if (IsInitialized == false)
                return;

            modulesSynchronizer.RequestSynchronization();
        }
#endif


#endif

        #endregion

        #region ### Public Static Methods ###

        /// <summary>
        /// Creates a new GameObject with a CurvyGenerator attached
        /// </summary>
        /// <returns>the Generator component</returns>
        public static CurvyGenerator Create()
        {
            //todo use UndoableCreateGameObject?
            GameObject go = new GameObject(
                "Curvy Generator",
                typeof(CurvyGenerator)
            );
            return go.GetComponent<CurvyGenerator>();
        }

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Adds a Module
        /// </summary>
        /// <typeparam name="T">type of the Module</typeparam>
        /// <returns>the new Module</returns>
        public T AddModule<T>() where T : CGModule
            => (T)AddModule(typeof(T));

        /// <summary>
        /// Adds a new module
        /// </summary>
        /// <param name="type">type of the Module</param>
        /// <returns>the new Module</returns>
        [NotNull]
        public CGModule AddModule(Type type)
        {
            GameObject go = new GameObject("");
            go.transform.SetParent(
                transform,
                false
            );
            CGModule module = (CGModule)go.AddComponent(type);
            AddModule(module);
            return module;
        }

        /// <summary>
        /// Adds an existing module. Will set the UniqueID of the module.
        /// </summary>
        /// <param name="module">The module, which needs to be a direct child of the generator</param>
        /// <exception cref= "ArgumentNullException">module is null</exception>
        /// <exception cref= "ArgumentException">module is not a child of the generator</exception>
        public void AddModule([NotNull] CGModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
            if (module.transform.parent != transform)
                throw new ArgumentException("Module must be a child of the Generator");

            Modules.Add(module);

            if (module.IsInitialized == false)
                module.Initialize();
            module.UniqueID = GetModuleUniqueID(module);
            module.ModuleName = GetModuleUniqueName(module);

            moduleSorter.SortingNeeded = true;

#if CURVY_SANITY_CHECKS
            if (HasModulesWithSameID)
                throw new InvalidOperationException("[Curvy] Modules with the same UniqueID found. This is not allowed.");
#endif
        }

        /// <summary>
        /// Removes the specified module from the Modules list.
        /// </summary>
        /// <param name="module">The CGModule object to be removed.</param>
        public void RemoveModule([NotNull] CGModule module)
        {
            bool moduleRemoved = Modules.Remove(module);
            if (moduleRemoved && Modules.Any())
                moduleSorter.SortingNeeded = true;
        }

        /// <summary>
        /// Auto-Arrange modules' graph canvas position
        /// In other words, this alligns the graph with the top left corner of the canvas. This does not modify the modules position relatively to each other
        /// </summary>
        public void ArrangeModules()
        {
            Vector2 min = new Vector2(
                float.MaxValue,
                float.MaxValue
            );
            foreach (CGModule mod in Modules)
            {
                min.x = Mathf.Min(
                    mod.Properties.Dimensions.x,
                    min.x
                );
                min.y = Mathf.Min(
                    mod.Properties.Dimensions.y,
                    min.y
                );
            }

            min -= new Vector2(
                10,
                10
            );
            foreach (CGModule mod in Modules)
            {
                mod.Properties.Dimensions.x -= min.x;
                mod.Properties.Dimensions.y -= min.y;
            }
        }

        /// <summary>
        /// Changes the modules' positions to make the graph easier to read.
        /// </summary>
        public void ReorderModules()
        {
            Dictionary<CGModule, Rect> initialModulesPositions;
            {
                initialModulesPositions = new Dictionary<CGModule, Rect>(Modules.Count);
                foreach (CGModule cgModule in Modules)
                    initialModulesPositions[cgModule] = cgModule.Properties.Dimensions;
            }

            List<CGModule> endpointModules = Modules.Where(m => m.OutputLinks.Any() == false).ToList();


            //A dictionary that gives for each module the set of all the modules that are connected to its inputs, whether directly or indirectly
            Dictionary<CGModule, HashSet<CGModule>> modulesRecursiveInputs =
                new Dictionary<CGModule, HashSet<CGModule>>(Modules.Count);
            foreach (CGModule module in endpointModules)
                UpdateModulesRecursiveInputs(
                    modulesRecursiveInputs,
                    module
                );

            HashSet<int> reordredModuleIds = new HashSet<int>();
            for (int index = 0; index < endpointModules.Count; index++)
            {
                float endPointY = index == 0
                    ? 0
                    //Draw under the previous endpoint recursive inputs
                    : modulesRecursiveInputs[endpointModules[index - 1]].Max(m => m.Properties.Dimensions.yMax)
                      + ModulesReorderingDeltaY;

                CGModule endpointModule = endpointModules[index];
                //Set the endpoint's position
                endpointModule.Properties.Dimensions.position = new Vector2(
                    0,
                    endPointY
                );
                reordredModuleIds.Add(endpointModule.UniqueID);
                //And then its children's positions, recursively
                ReorderEndpointRecursiveInputs(
                    endpointModule,
                    reordredModuleIds,
                    modulesRecursiveInputs
                );
            }

            ArrangeModules();
#if UNITY_EDITOR
            if (Application.isPlaying == false)
                //Dirty scene if something changed
                if (Modules.Exists(m => m.Properties.Dimensions != initialModulesPositions[m]))
                    gameObject.MarkParentSceneAsDirty();
#endif
        }


        /// <summary>
        /// Clears the whole generator
        /// </summary>
        public void Clear()
        {
            //BUG when a module is a child of another one, destroying the first destroys the second, which lead to unwanted behavior in this loop

            bool isInEditMode = DTUtility.IsInEditMode;
            if (isInEditMode)
            {
#if UNITY_EDITOR
                bool skipAllMessages = false;

                for (int i = Modules.Count - 1; i >= 0; i--)
                {
                    CGModule module = Modules[i];
                    GameObject moduleGO = module.gameObject;

                    if (DTUtility.DoesPrefabStatusAllowDeletion(
                            moduleGO,
                            out string errorMessage
                        ))
                    {
                        if (moduleGO.Destroy(
                                true,
                                false
                            )
                            == false)
                            Debug.LogError("Could not destroy a CG module. This is not expected. Please send a bug report.");
                    }
                    else
                    {
                        if (skipAllMessages == false)
                            skipAllMessages = false
                            == EditorUtility.DisplayDialog(
                                $"Cannot delete Game Object '{moduleGO.name}'",
                                errorMessage,
                                "Ok",
                                "Skip All"
                            );
                    }
                }
#endif
            }
            else
            {
                for (int i = Modules.Count - 1; i >= 0; i--)
                    if (Modules[i].gameObject.Destroy(
                            true,
                            false
                        )
                        == false)
                        Debug.LogError("Could not destroy a CG module. This is not expected. Please send a bug report.");

                Modules.Clear();
            }
        }

        /// <summary>
        /// Deletes a module (same as PCGModule.Delete())
        /// </summary>
        /// <param name="module">a module</param>
        public void DeleteModule(CGModule module)
        {
            if (module)
                module.Delete();
        }


        /// <summary>
        /// Deletes a link between two modules in the generator.
        /// </summary>
        /// <param name="link">The link to be deleted.</param>
        /// <exception cref="InvalidOperationException">Thrown when the start or end module of the link is null.</exception>
        public void DeleteLink(
            [NotNull] CGModuleLink link)
        {
            CGModule linkStartModule = GetModule(
                link.ModuleID,
                true
            );

            if (linkStartModule == null)
                throw new InvalidOperationException("The start module of the link is null");

            CGModule linkEndModule = GetModule(
                link.TargetModuleID,
                true
            );

            if (linkEndModule == null)
                throw new InvalidOperationException("The end module of the link is null");

            linkStartModule.OutputByName[link.SlotName].UnlinkFrom(linkEndModule.InputByName[link.TargetSlotName]);
        }

        /// <summary>
        /// Find modules of a given type
        /// </summary>
        /// <typeparam name="T">the module type</typeparam>
        /// <returns>a list of zero or more modules</returns>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public List<T> FindModules<T>() where T : CGModule => FindModules<T>(false);

        /// <summary>
        /// Find modules of a given type
        /// </summary>
        /// <typeparam name="T">the module type</typeparam>
        /// <param name="includeOnRequestProcessing">whether to include IOnRequestProcessing modules</param>
        /// <returns>a list of zero or more modules</returns>
        public List<T> FindModules<T>(bool includeOnRequestProcessing) where T : CGModule
        {
            List<T> res = new List<T>();
            for (int i = 0; i < Modules.Count; i++)
                if (Modules[i] is T && (includeOnRequestProcessing || !(Modules[i] is IOnRequestProcessing)))
                    res.Add((T)Modules[i]);
            return res;
        }

        /// <summary>
        /// Gets a list of modules, either including or excluding IOnRequestProcessing modules
        /// </summary>
        /// <returns>a list of zero or more modules</returns>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public List<CGModule> GetModules() => GetModules(false);

        /// <summary>
        /// Gets a list of modules, either including or excluding IOnRequestProcessing modules
        /// </summary>
        /// <param name="includeOnRequestProcessing">whether to include IOnRequestProcessing modules</param>
        [UsedImplicitly]
        [Obsolete("Method will be removed. You can copy its implementation if needed.")]
        public List<CGModule> GetModules(bool includeOnRequestProcessing)
        {
            if (includeOnRequestProcessing)
                return new List<CGModule>(Modules);

            return Modules.Where(t => t is IOnRequestProcessing == false).ToList();
        }

        /// <summary>
        /// Gets a module by ID, either including or excluding IOnRequestProcessing modules
        /// </summary>
        /// <param name="moduleID">the ID of the module in question</param>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public CGModule GetModule(int moduleID) => GetModule(
            moduleID,
            false
        );

        /// <summary>
        /// Gets a module by ID, either including or excluding IOnRequestProcessing modules
        /// </summary>
        /// <param name="moduleID">the ID of the module in question</param>
        /// <param name="includeOnRequestProcessing">whether to include IOnRequestProcessing modules</param>
        [CanBeNull]
        public CGModule GetModule(int moduleID, bool includeOnRequestProcessing)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(Modules.Count(m => m.UniqueID == moduleID) <= 1);
#endif
            CGModule module = Modules.FirstOrDefault(m => m.UniqueID == moduleID);

            if (module == null)
                return null;

            if (includeOnRequestProcessing || !(module is IOnRequestProcessing))
                return module;

            return null;
        }

        /// <summary>
        /// Gets a module by ID, either including or excluding IOnRequestProcessing modules (Generic version)
        /// </summary>
        /// <typeparam name="T">type of the module</typeparam>
        /// <param name="moduleID">the ID of the module in question</param>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public T GetModule<T>(int moduleID) where T : CGModule => GetModule<T>(
            moduleID,
            false
        );

        /// <summary>
        /// Gets a module by ID, either including or excluding IOnRequestProcessing modules (Generic version)
        /// </summary>
        /// <typeparam name="T">type of the module</typeparam>
        /// <param name="moduleID">the ID of the module in question</param>
        /// <param name="includeOnRequestProcessing">whether to include IOnRequestProcessing modules</param>
        public T GetModule<T>(int moduleID, bool includeOnRequestProcessing) where T : CGModule
            => GetModule(
                moduleID,
                includeOnRequestProcessing
            ) as T;

        /// <summary>
        /// Gets a module by name, either including or excluding IOnRequestProcessing modules 
        /// </summary>
        /// <param name="moduleName"></param>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public CGModule GetModule(string moduleName) => GetModule(
            moduleName,
            false
        );

        /// <summary>
        /// Gets a module by name, either including or excluding IOnRequestProcessing modules 
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="includeOnRequestProcessing"></param>
        public CGModule GetModule(string moduleName, bool includeOnRequestProcessing)
        {
            for (int i = 0; i < Modules.Count; i++)
                if (Modules[i].ModuleName.Equals(
                        moduleName,
                        StringComparison.CurrentCultureIgnoreCase
                    )
                    && (includeOnRequestProcessing || !(Modules[i] is IOnRequestProcessing)))
                    return Modules[i];

            return null;
        }

        /// <summary>
        /// Gets a module by name, either including or excluding IOnRequestProcessing modules (Generic version)
        /// </summary>
        /// <typeparam name="T">type of the module</typeparam>
        /// <param name="moduleName">the ID of the module in question</param>
        [UsedImplicitly]
        [Obsolete("Use the overload that has a mandatory includeOnRequestProcessing parameter")]
        public T GetModule<T>(string moduleName) where T : CGModule => GetModule<
            T>(
            moduleName,
            false
        );

        /// <summary>
        /// Gets a module by name, either including or excluding IOnRequestProcessing modules (Generic version)
        /// </summary>
        /// <typeparam name="T">type of the module</typeparam>
        /// <param name="moduleName">the ID of the module in question</param>
        /// <param name="includeOnRequestProcessing">whether to include IOnRequestProcessing modules</param>
        public T GetModule<T>(string moduleName, bool includeOnRequestProcessing) where T : CGModule
            => GetModule(
                moduleName,
                includeOnRequestProcessing
            ) as T;

        /// <summary>
        /// Gets a module's output slot by module ID and slotName
        /// </summary>
        /// <param name="moduleId">Id of the module</param>
        /// <param name="slotName">Name of the slot</param>
        [UsedImplicitly]
        [Obsolete("Use GetModule and CGModule.GetOutputSlot instead")]
        public CGModuleOutputSlot GetModuleOutputSlot(int moduleId, string slotName)
        {
            CGModule mod = GetModule(
                moduleId,
                true
            );
            if (mod)
                return mod.GetOutputSlot(slotName);
            return null;
        }

        /// <summary>
        /// Gets a module's output slot by module name and slotName
        /// </summary>
        /// <param name="moduleName">Name of the module</param>
        /// <param name="slotName">Name of the slot</param>
        [UsedImplicitly]
        [Obsolete("Use GetModule and CGModule.GetOutputSlot instead")]
        public CGModuleOutputSlot GetModuleOutputSlot(string moduleName, string slotName)
        {
            CGModule mod = GetModule(
                moduleName,
                true
            );
            if (mod)
                return mod.GetOutputSlot(slotName);
            return null;
        }

        //TODO initialize earlier
        /// <summary>
        /// Initializes the Generator
        /// </summary>
        /// <param name="force">true to force reinitialization</param>
        public void Initialize(bool force = false)
        {
            if
                (this
                 == null) //Modifying a prefab of a shape extrusion generator (removing its input spline then adding it back again) the updated of the generator gets called with a null generator. Probably due to the refreshing of the prefab file (to remove generated mesh)
                return;

            if (!isInitializedPhaseOne || force)
            {
                SetModulesFromChildren();

                if (CorrectDuplicateModuleIDs())
                    ResetAllModuleLinks();

                foreach (CGModule module in Modules)
                    if (force || !module.IsInitialized)
                        module.Initialize();

                isInitializedPhaseOne = true;
            }

            for (int m = 0; m < Modules.Count; m++)
                if (Modules[m] is IExternalInput && !Modules[m].IsInitialized)
                    return;

            isInitialized = true;
            isInitializedPhaseOne = false;
            if (force)
                moduleSorter.SortingNeeded =
                    true; //todo is this needed knowing that SortingNeeded is set to true in SetModulesFromChildren?
            Refresh(true);
        }

        /// <summary>
        /// Refreshes the Generator
        /// </summary>
        /// <param name="forceUpdate">true to force a refresh of all modules</param>
        public void Refresh(bool forceUpdate = false)
        {
            if (!IsInitialized)
                return;

            moduleSorter.EnsureIsSorted(Modules);

            CGModule firstChanged = null;

            for (int i = 0; i < Modules.Count; i++)
            {
                CGModule module = Modules[i];

                if (module is IOnRequestProcessing)
                {
                    if (forceUpdate)
                        //todo design: get rid of the fact that setting Dirty to true sets it to false
                        module.Dirty =
                            true; // Dirty state will be reset to false, but last data will be deleted - forcing a recalculation
                    continue;
                }

                if (module is INoProcessing)
                    continue;

                if (module.Dirty == false && forceUpdate == false)
                    continue;

                module.checkOnStateChangedINTERNAL(); //BUG? this can set dirty to true, so shouldn't it be called before checking the value of Dirty earlier in this method?

                if (!module.IsInitialized || !module.IsConfigured)
                    continue;

                if (firstChanged == null)
                {
#if UNITY_EDITOR || CURVY_DEBUG
                    DEBUG_ExecutionTime.Start();
#endif
                    firstChanged = module;
                }

                module.doRefresh();
            }

            if (firstChanged != null)
            {
#if UNITY_EDITOR
                DEBUG_ExecutionTime.Stop();
#endif
                OnRefreshEvent(
                    new CurvyCGEventArgs(
                        this,
                        firstChanged
                    )
                );
            }
        }


        /// <summary>
        /// Will try to auto refresh the generator. Basically this calls <see cref="Refresh"/> if <see cref="AutoRefresh"/> is set and the refresh delays are respected
        /// </summary>
        public void TryAutoRefresh()
        {
            if (!AutoRefresh)
                return;

            bool refreshDelayReached = autoRefreshTimer.Update(
                RefreshDelay * 0.001f,
                RefreshDelayEditor * 0.001f
            );
            if (refreshDelayReached)
                Refresh();
        }

        /// <summary>
        /// Delete all the managed resources acting as an output. One example of this are the generated meshes by the <see cref="FluffyUnderware.Curvy.Generator.Modules.CreateMesh"/> module
        /// </summary>
        /// <param name="associatedPrefabWasModified">Is true if an associated prefab was modified to deleted the output resources from it too</param>
        /// <remarks>Due to how the prefab system works, this method has to delete output from associated prefab assets too</remarks>
        /// <returns>True if there were deleted resources</returns>
        public bool DeleteAllOutputManagedResources(out bool associatedPrefabWasModified)
        {
#if UNITY_EDITOR
            if (DTUtility.DoesPrefabStatusAllowDeletion(
                    gameObject,
                    out _
                )
                == false)
                associatedPrefabWasModified = DeleteAllOutputManagedResourcesFromAssociatedPrefab();
            else
                associatedPrefabWasModified = false;
#else
            associatedPrefabWasModified = false;
#endif

            bool result = false;
            foreach (CGModule module in Modules)
                result |= module.DeleteAllOutputManagedResources();
            return result;
        }

        /// <summary>
        /// Generates a unique name for the given module by appending an integer counter if necessary.
        /// </summary>
        /// <param name="module">The CGModule object for which a unique name is to be generated.</param>
        /// <returns>A unique name for the provided module.</returns>
        public string GetModuleUniqueName(CGModule module)
        {
            int counter = 1;
            string uniqueName = module.ModuleName;

            while (!IsModuleNameUnique(
                       module,
                       uniqueName
                   ))
            {
                uniqueName = $"{module.ModuleName}{counter.ToString()}";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// Generates a unique ID for the given module.
        /// </summary>
        /// <param name="module">The CGModule object for which a unique ID is to be generated.</param>
        /// <returns>A unique ID for the provided module.</returns>
        public int GetModuleUniqueID(CGModule module)
        {
            int id = 0;
            while (Modules.Exists(
                       m => ReferenceEquals(
                                m,
                                module
                            )
                            == false
                            && m.UniqueID == id
                   ))
                id++;
            return id;
        }

        #endregion

        #region ### Protected Members ###

        protected CurvyCGEventArgs OnRefreshEvent(CurvyCGEventArgs e)
        {
            if (OnRefresh != null)
                OnRefresh.Invoke(e);
            return e;
        }

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            autoRefreshTimer.Reset();
            moduleSorter.SortingNeeded = true;
            modulesSynchronizer.CancelRequests();
        }
#endif

        #endregion

        #region ### Privates and Internals ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

#if UNITY_EDITOR
        static CurvyGenerator() =>
            EditorSceneManager.sceneSaving += OnSceneSaving;

        private static void OnSceneSaving(
            Scene scene,
            string path)
        {
            if (CurvyGlobalManager.SaveGeneratorOutputs)
                return;

            //clear all output GOs to avoid saving them.
            CurvyGenerator[] generators = ObjectExt.FindObjectsOfTypeInCurrentStage<CurvyGenerator>(
                true,
                false
            );
            foreach (CurvyGenerator generator in generators)
                //only if the generator is supposed to refresh automatically, otherwise the users might not expect their generator to update
                if (generator.AutoRefresh)
                    foreach (CGModule module in generator.Modules)
                        if (module.DeleteAllOutputManagedResources())
                            module.Dirty = true; //to force update once saving is done
        }

#endif

        private bool IsModuleNameUnique(CGModule module, string uniqueName) =>
            Modules.All(
                m =>
                    ReferenceEquals(
                        m,
                        module
                    )
                    || !m.ModuleName.Equals(
                        uniqueName,
                        StringComparison.CurrentCultureIgnoreCase
                    )
            );

        /// <summary>
        /// Ensures a module name is unique
        /// </summary>
        /// <param name="name">desired name</param>
        /// <returns>unique name</returns>
        [UsedImplicitly]
        [Obsolete]
        public string getUniqueModuleNameINTERNAL(string name)
        {
            //todo this seems bugged
            string newName = name;
            bool isUnique;
            int c = 1;
            do
            {
                isUnique = true;
                foreach (CGModule mod in Modules)
                    if (mod.ModuleName.Equals(
                            newName,
                            StringComparison.CurrentCultureIgnoreCase
                        ))
                    {
                        newName = $"{name}{(c++).ToString(CultureInfo.InvariantCulture)}";
                        isUnique = false;
                        break;
                    }
            } while (!isUnique);

            return newName;
        }


        /// <summary>
        /// INTERNAL. Not supposed to be public. Don't call this by yourself. 
        /// </summary>
        //todo design get rid of this
        internal void sortModulesINTERNAL() =>
            moduleSorter.SortingNeeded = true;

        private bool CorrectDuplicateModuleIDs()
        {
            bool duplicateIDsFound = false;
            IEnumerable<IGrouping<int, CGModule>> sameIDModuleGroups = Modules.GroupBy(module => module.UniqueID);
            foreach (IGrouping<int, CGModule> sameIDGroup in sameIDModuleGroups)
            {
                if (sameIDGroup.Count() <= 1)
                    continue;

                duplicateIDsFound = true;

                DTLog.LogError(
                    $"[Curvy] Curvy Generator {name}: The following modules have the same ID. This is not allowed. Their IDs will be reset:"
                );
                foreach (CGModule module in sameIDGroup)
                {
                    DTLog.LogError($"[Curvy] Curvy Generator {name}: Module {module.ModuleName} with ID {module.UniqueID}.");
                    module.UniqueID = GetModuleUniqueID(module);
                }

                DTLog.LogError(
                    "[Curvy] Consequently all links were reset. Please raise a bug report if you encounter this error."
                );
            }

            return duplicateIDsFound;
        }

        [UsedImplicitly]
        private void ResetAllModuleLinks()
        {
            Modules.ForEach(
                m =>
                {
                    m.InputLinks.Clear();
                    m.OutputLinks.Clear();
#pragma warning disable CS0612
                    m.ReInitializeLinkedSlots();
#pragma warning restore CS0612
                }
            );
        }


        /// <summary>
        /// Whether this module has circular reference errors
        /// </summary>
        public bool HasCircularReference([NotNull] CGModule module)
        {
#if CURVY_SANITY_CHECKS
            if (module == null)
                throw new ArgumentNullException(nameof(module));
#endif
            return moduleSorter.HasCircularReference(module);
        }


        /// <summary>
        /// Sets the position of an endpoint module's recursive inputs in a way that makes the graph easy to read
        /// </summary>
        /// <param name="endPoint">The module which recursive inputs are to be reordred</param>
        /// <param name="reordredModuleIds">Set of modules already reordred</param>
        /// <param name="modulesRecursiveInputs"> A dictionary that gives for each module the set of all the modules that are connected to its inputs, whether directly or indirectly</param>
        private static void ReorderEndpointRecursiveInputs(CGModule endPoint, HashSet<int> reordredModuleIds,
            Dictionary<CGModule, HashSet<CGModule>> modulesRecursiveInputs)
        {
            float nextInputEndingX = endPoint.Properties.Dimensions.xMin - ModulesReorderingDeltaX;
            float nextInputStartingY = endPoint.Properties.Dimensions.yMin;

            List<CGModule> inputModules = endPoint.Input.SelectMany(i => i.GetLinkedModules()).ToList();
            foreach (CGModule inputModule in inputModules)
            {
                float inputModuleXPosition = nextInputEndingX - inputModule.Properties.Dimensions.width;
                //If module is processed for the first time, process it normally ...
                if (reordredModuleIds.Contains(inputModule.UniqueID) == false)
                {
                    inputModule.Properties.Dimensions.position = new Vector2(
                        inputModuleXPosition,
                        nextInputStartingY
                    );
                    reordredModuleIds.Add(inputModule.UniqueID);
                    ReorderEndpointRecursiveInputs(
                        inputModule,
                        reordredModuleIds,
                        modulesRecursiveInputs
                    );
                }
                //... otherwise allow it to be repositioned only when pushed to the left
                else if (inputModuleXPosition < inputModule.Properties.Dimensions.xMin)
                {
                    inputModule.Properties.Dimensions.position = new Vector2(
                        inputModuleXPosition,
                        inputModule.Properties.Dimensions.yMin
                    );
                    ReorderEndpointRecursiveInputs(
                        inputModule,
                        reordredModuleIds,
                        modulesRecursiveInputs
                    );
                }

                nextInputStartingY = Math.Max(
                    nextInputStartingY,
                    modulesRecursiveInputs[inputModule].Max(m => m.Properties.Dimensions.yMax) + ModulesReorderingDeltaY
                );
            }
        }

        /// <summary>
        /// Adds to the modules recursive inputs dictionary the entries corresponding to the given module 
        /// </summary>
        /// <returns>The recursive inputs of the given module</returns>
        private static HashSet<CGModule> UpdateModulesRecursiveInputs(
            Dictionary<CGModule, HashSet<CGModule>> modulesRecursiveInputs, CGModule moduleToAdd)
        {
            if (modulesRecursiveInputs.ContainsKey(moduleToAdd))
                return modulesRecursiveInputs[moduleToAdd];

            List<CGModule> inputModules = moduleToAdd.Input.SelectMany(i => i.GetLinkedModules()).ToList();
            HashSet<CGModule> result = new HashSet<CGModule>
            {
                moduleToAdd
            };
            result.UnionWith(
                inputModules.SelectMany(
                    i => UpdateModulesRecursiveInputs(
                        modulesRecursiveInputs,
                        i
                    )
                )
            );
            modulesRecursiveInputs[moduleToAdd] = result;
            return result;
        }

        private void SetModulesFromChildren()
        {
            Modules.Clear();
            //todo design: Use AddModule instead of filling to Modules via GetComponentsInChildren, in order to unify module adding code
            //I tried doing so, but that introduced regression. Did tried too much to fix it, will leave this for another day.
            GetComponentsInChildren(Modules);
            //Not all modules are part of this generator. This happens for example if a generator creates GameObjects that are generators themselves.
            Modules.RemoveAll(m => m.transform.parent != transform);

            if (Modules.Any())
                moduleSorter.SortingNeeded = true;
        }

        private bool HasModulesWithSameID
            => Modules.GroupBy(module => module.UniqueID).Any(group => group.Count() > 1);


        /// <summary>
        ///  Delete, from the associated prefab if any, all the managed resources acting as an output. One example of such resources is the generated meshes by the <see cref="FluffyUnderware.Curvy.Generator.Modules.CreateMesh"/> module
        /// <remarks>Prefabs instances are not allowed to do some operations, such as deleting a game object. Such operations are done by the Curvy Generator. So run this method before doing any of those operations</remarks>
        /// </summary>
        /// <returns>Whether an associated prefab was modified</returns>
        public bool DeleteAllOutputManagedResourcesFromAssociatedPrefab()
        {
#if UNITY_EDITOR
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
            if (String.IsNullOrEmpty(prefabPath))
                return false;

            GameObject prefabContentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            CurvyGenerator[] prefabGenerators = prefabContentsRoot.GetComponentsInChildren<CurvyGenerator>();

            bool modified = false;
            foreach (CurvyGenerator prefabGenerator in prefabGenerators)
                foreach (CGModule module in prefabGenerator.Modules)
                    modified |= module.DeleteAllOutputManagedResources();

            if (modified)
            {
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    prefabContentsRoot,
                    prefabPath
                );
                if (savedPrefab == null)
                {
                    DTLog.LogError(
                        $"[Curvy] The prefab asset '{prefabPath}' containing the generator '{name}' needs to be modified to delete generator output objects. Attempt to modify it failed. See other console messages to know what caused this failure.",
                        this
                    );
                    modified = false;
                }
                else
                {
                    object message =
                        $"[Curvy] The prefab asset '{prefabPath}' containing the generator '{name}' was modified to delete generator output objects.";

                    PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                    if (currentPrefabStage != null && currentPrefabStage.assetPath == prefabPath)
                    {
                        message +=
                            " This might happen when you save modifications to the prefab asset. You might want to disable Auto Save in Prefab Mode to make this happen less frequently.";
                        DTLog.LogWarning(
                            message,
                            this
                        );
                    }
                    else
                    {
                        message += " This might lead to the refreshing of the associated generator in the prefab instance.";
                        DTLog.Log(
                            message,
                            this
                        );
                    }
                }
            }

            PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
            return modified;
#else
            return false;
#endif
        }

        /// <summary>
        /// Save to scene all the managed resources acting as an output. One example of such resources is the generated meshes by the <see cref="FluffyUnderware.Curvy.Generator.Modules.CreateMesh"/> module
        /// </summary>
        public void SaveAllOutputManagedResources()
        {
            GameObject result = new GameObject($"{name} Exported Resources");
            result.transform.position = transform.position;
            result.transform.rotation = transform.rotation;
            result.transform.localScale = transform.localScale;
            Modules.Where(m => m is ResourceExportingModule)
                .ForEach(m => ((ResourceExportingModule)m).SaveToScene(result.transform));
        }


#endif

        #endregion
    }
}