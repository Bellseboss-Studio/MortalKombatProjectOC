// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;


namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Curvy Generator module base class
    /// </summary>
    [ExecuteAlways]
    public abstract partial class CGModule : DTVersionedMonoBehaviour
    {
        #region ### Events ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [Group(
            "Events",
            Expanded = false,
            Sort = 1000
        )]
        [SerializeField]
        protected CurvyCGEvent m_OnBeforeRefresh = new CurvyCGEvent();

        [Group("Events")]
        [SerializeField]
        protected CurvyCGEvent m_OnRefresh = new CurvyCGEvent();

#endif

        public CurvyCGEvent OnBeforeRefresh
        {
            get => m_OnBeforeRefresh;
            set => m_OnBeforeRefresh = value;
        }

        public CurvyCGEvent OnRefresh
        {
            get => m_OnRefresh;
            set => m_OnRefresh = value;
        }

        protected CurvyCGEventArgs OnBeforeRefreshEvent(CurvyCGEventArgs e)
        {
            if (OnBeforeRefresh != null)
                OnBeforeRefresh.Invoke(e);
            return e;
        }

        protected CurvyCGEventArgs OnRefreshEvent(CurvyCGEventArgs e)
        {
            if (OnRefresh != null)
                OnRefresh.Invoke(e);
            return e;
        }

        #endregion


        #region --- Private Fields ---

        #region --- Serialized Fields ---

        [SerializeField, HideInInspector]
        private string m_ModuleName;

        [SerializeField, HideInInspector]
        private bool m_Active = true;

        [Group(
            "Seed Options",
            Expanded = false,
            Sort = 1001
        )]
        [GroupCondition(nameof(UsesRandom))]
        [FieldAction(
            "CBSeedOptions",
            ShowBelowProperty = true
        )]
        [SerializeField]
        private bool m_RandomizeSeed;

        [SerializeField, HideInInspector]
        private int m_Seed = unchecked((int)DateTime.Now.Ticks);

        [SerializeField, HideInInspector]
        private int m_UniqueID;

        #endregion

        private CurvyGenerator generator;
        private bool isInitialized;

        [NotNull]
        private readonly ResourceNamer resourceNamer;

        [NotNull]
        private readonly InformationProvider informationProvider;

        [NotNull]
        private readonly DirtinessManager dirtinessManager;

        [NotNull]
        private readonly Slots slots;

        [NotNull]
        private readonly Identifier identifier;

        [NotNull]
        private readonly List<(Component ResourceManager, string ResourceName)> resourceManagers;

        #endregion

        #region ### Public Fields & Properties ###

        public string ModuleName
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    resourceNamer.ClearCache();
#pragma warning disable CS0612
                    renameManagedResourcesINTERNAL();
#pragma warning restore CS0612
                }
            }
        }

        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active != value)
                {
                    m_Active = value;
                    Dirty = true;
                    Generator.sortModulesINTERNAL();
                }
            }
        }

        /// <summary>
        /// If <see cref="RandomizeSeed"/> is set to false, Seed is used to initialize Unity's random numbers generator before refreshing the
        /// If <see cref="RandomizeSeed"/> is set to true, a random seed will be used
        /// current module
        /// </summary>
        public int Seed
        {
            get => m_Seed;
            set
            {
                if (m_Seed != value)
                {
                    m_Seed = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If is set to false, <see cref="Seed"/> is used to initialize Unity's random numbers generator before refreshing the current module.
        /// If set to true, a random seed will be used
        /// </summary>
        public bool RandomizeSeed
        {
            get => m_RandomizeSeed;
            set => m_RandomizeSeed = value;
        }

        public bool Dirty
        {
            get => dirtinessManager.IsDirty;
            set => dirtinessManager.IsDirty = value;
        }

        /// <summary>
        /// Gets whether the module is properly configured i.e. has everything to work like intended
        /// </summary>
        public virtual bool IsConfigured
        {
            get
            {
                if (!IsInitialized || Generator.HasCircularReference(this) || !Active)
                    return false;

                return slots.IsConfigured;
            }
        }

        /// <summary>
        /// Gets whether the module and all its dependencies are fully initialized
        /// </summary>
        public virtual bool IsInitialized => isInitialized;

        public CurvyGenerator Generator
        {
            get
            {
                if (!generator)
                    generator = transform.parent != null
                        ? transform.parent.GetComponent<CurvyGenerator>()
                        : null;

                return generator;
            }
        }

        /// <summary>
        /// Unique identifier to identify the module in the generator
        /// </summary>
        public int UniqueID
        {
            get => identifier.ID;
            set
            {
                if (identifier.ID != value)
                {
                    identifier.ID = value;
                    resourceNamer.ClearCache();
#pragma warning disable CS0612
                    renameManagedResourcesINTERNAL();
#pragma warning restore CS0612
                }
            }
        }

        [NonSerialized]
        public List<string> UIMessages = new List<string>();

        #endregion

        #region ### Graph Related ###

        /// <summary>
        /// Whether this module has circular reference errors
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Generator.HasCircularReference instead")]
        public bool CircularReferenceError
        {
            get => Generator.HasCircularReference(this);
            set => throw new NotSupportedException(" CircularReferenceError is read-only");
        }

        [HideInInspector]
        public CGModuleProperties Properties = new CGModuleProperties();

        /// <summary>
        /// List of links between this module's input slots and other modules' output slots. Do not manipulate this list directly. To add/remove links, use the link manipulating methods of the slots in <see cref="Input"/> and <see cref="Output"/> properties.
        /// </summary>
        /// <seealso cref="CGModuleInputSlot"/>
        /// <seealso cref="CGModuleOutputSlot"/>
        [HideInInspector]
        public List<CGModuleLink> InputLinks = new List<CGModuleLink>();

        /// <summary>
        /// List of links between this module's output slots and other modules' input slots. Do not manipulate this list directly. To add/remove links, use the link manipulating methods of the slots in <see cref="Input"/> and <see cref="Output"/> properties.
        /// </summary>
        /// <seealso cref="CGModuleInputSlot"/>
        /// <seealso cref="CGModuleOutputSlot"/>
        [HideInInspector]
        public List<CGModuleLink> OutputLinks = new List<CGModuleLink>();

        /// <summary>
        /// Input slots mapped by their slot name
        /// </summary>
        /// <seealso cref="Output"/>
        /// <seealso cref="CGModuleSlot.Info"/>
        /// <seealso cref="SlotInfo.Name"/>
        [NotNull]
        public Dictionary<string, CGModuleInputSlot> InputByName => slots.InputSlotsByName;

        /// <summary>
        /// Output slots mapped by their slot name
        /// </summary>
        /// <seealso cref="Output"/>
        /// <seealso cref="CGModuleSlot.Info"/>
        /// <seealso cref="SlotInfo.Name"/>
        [NotNull]
        public Dictionary<string, CGModuleOutputSlot> OutputByName => slots.OutputSlotsByName;

        /// <summary>
        /// The input slots of the module
        /// </summary>
        [NotNull]
        public List<CGModuleInputSlot> Input => slots.InputSlots;

        /// <summary>
        /// The output slots of the module
        /// </summary>
        [NotNull]
        public List<CGModuleOutputSlot> Output => slots.OutputSlots;

        //-----

        #endregion

        #region ### Debugging ###

#if UNITY_EDITOR || CURVY_DEBUG
        public DateTime DEBUG_LastUpdateTime; //todo rework variable so it is no more a debug variable, since it is used in non debug features
        public TimeMeasure DEBUG_ExecutionTime = new TimeMeasure(5);
#endif

        #endregion

        #region ### Unity Callbacks (Virtual) ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected virtual void Awake() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Generator)
                Initialize();
        }

        protected virtual void OnDestroy()
        {
            dirtinessManager.OnDestroy();
            List<Component> res;
            List<string> resNames;
            // Resources
            if (GetManagedResources(
                    out res,
                    out resNames
                ))
                for (int i = res.Count - 1; i >= 0; i--)
                    DeleteManagedResource(
                        resNames[i],
                        res[i],
                        string.Empty,
                        true
                    );

            slots.ReinitializeLinkedModulesLinkedSlots();

            if (Generator)
                Generator.RemoveModule(this);

            isInitialized = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Dirty = true;
            resourceNamer.ClearCache();
#pragma warning disable CS0612
            renameManagedResourcesINTERNAL();
#pragma warning restore CS0612
        }

        [UsedImplicitly]
        private void Update()
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
#pragma warning disable CS0612
                renameManagedResourcesINTERNAL();
#pragma warning restore CS0612
#endif
        }

        /// <summary>
        /// Used both as the Reset method of Monobehaviour, and the method called when clicking on the Reset entry in the right click menu of modules.
        /// </summary>
        public new virtual void Reset()
        {
#pragma warning disable CS0612
            ModuleName = string.IsNullOrEmpty(Info.ModuleName)
                ? GetType().Name
                : Info.ModuleName;
#pragma warning restore CS0612

            if (Generator && Generator.Modules.Any(m => m != this && m.UniqueID == UniqueID))
                UniqueID = Generator.GetModuleUniqueID(this);

            //Remove all non-persisent (ie created from script) listeners from the events. Might help with garbage collection
            if (OnBeforeRefresh != null)
                OnBeforeRefresh.RemoveAllListeners();
            if (OnRefresh != null)
                OnRefresh.RemoveAllListeners();

            OnBeforeRefresh = new CurvyCGEvent();
            OnRefresh = new CurvyCGEvent();

            DeleteAllOutputManagedResources();

            base.Reset();
        }

#endif

        #endregion

        #region ### Virtual Methods & Properties ###

        /// <summary>
        /// Add Module processing code in here
        /// </summary>
        public virtual void Refresh()
        {
            //OPTIM? remove this check, or make its compilation conditional
            slots.CheckInputModulesNotDirty();
            //Debug.Log(name + ".Refresh()");
            UIMessages.Clear();
        }

        /// <summary>
        /// Delete all the managed resources acting as an output. One example of this are the generated meshes by the <see cref="FluffyUnderware.Curvy.Generator.Modules.CreateMesh"/> module
        /// </summary>
        /// <returns>True if there were deleted resources</returns>
        public virtual bool DeleteAllOutputManagedResources()
            => false;

        /// <summary>
        /// Called when a module's state changes (Link added/removed, Active toggles etc..)
        /// </summary>
        public virtual void OnStateChange()
        {
            //            Debug.Log(name + ".OSC, configured="+IsConfigured);
            Dirty = true;

            slots.ClearOutputData();
            slots.ResetInputSlotsLastDataCount();
            if (!IsConfigured)
                DeleteAllOutputManagedResources();
        }

        /// <summary>
        /// Called after a module was copied to a template
        /// </summary>
        /// <remarks>Use this handle references that can't be templated etc...</remarks>
        public virtual void OnTemplateCreated() =>
            DeleteAllOutputManagedResources();

        #endregion

        #region ### Helpers ###

        /// <summary>
        /// Gets a request parameter of a certain type
        /// </summary>
        /// <typeparam name="T">Type derived from PCGDataRequestParameter</typeparam>
        /// <param name="requests">reference to the list of request parameters</param>
        /// <returns>the wanted request parameter or null</returns>
        protected static T GetRequestParameter<T>(ref CGDataRequestParameter[] requests) where T : CGDataRequestParameter
        {
            for (int i = 0; i < requests.Length; i++)
                if (requests[i] is T)
                    return (T)requests[i];

            return null;
        }

        /// <summary>
        /// Removes a certain request parameter from the requests array
        /// </summary>
        /// <param name="requests">reference to the requests array</param>
        /// <param name="request">the request to remove</param>
        protected static void RemoveRequestParameter(ref CGDataRequestParameter[] requests, CGDataRequestParameter request)
        {
            for (int i = 0; i < requests.Length; i++)
                if (requests[i] == request)
                {
                    requests = requests.RemoveAt(i);
                    return;
                }
        }

        #endregion

        #region ### Public Methods ###

        public CGModule()
        {
            resourceNamer = new ResourceNamer(this);
            dirtinessManager = new DirtinessManager(this);
            identifier = new Identifier(this);
            informationProvider = new InformationProvider(this);
            slots = new Slots(this);
#pragma warning disable CS0618
            resourceManagers = GetResourceManagers();
#pragma warning restore CS0618
        }

        public void Initialize()
        {
            if (!Generator)
                Invoke(
                    nameof(Delete),
                    0
                );
            else
            {
                if (string.IsNullOrEmpty(ModuleName))
                    SetModuleName();
                slots.ReInitializeLinkedSlots();
                isInitialized = true;
            }
        }

        /// <summary>
        /// Get the first link, if any, between this module's outputSlot and another module's inputSlot
        /// </summary>
        [CanBeNull]
        public CGModuleLink GetOutputLink(CGModuleOutputSlot outputSlot, CGModuleInputSlot inputSlot)
            => GetLink(
                OutputLinks,
                outputSlot,
                inputSlot
            );

        /// <summary>
        /// Get all the links between this module's outputSlot and other modules' inputSlots
        /// </summary>
        [NotNull]
        public List<CGModuleLink> GetOutputLinks(CGModuleOutputSlot outputSlot)
            => GetLinks(
                OutputLinks,
                outputSlot
            );

        /// <summary>
        /// Get the first link, if any, between this module's inputSlot and another module's outputSlot
        /// </summary>
        [CanBeNull]
        public CGModuleLink GetInputLink(CGModuleInputSlot inputSlot, CGModuleOutputSlot outputSlot)
            => GetLink(
                InputLinks,
                inputSlot,
                outputSlot
            );

        /// <summary>
        /// Get all the links between this module's inputSlot and other modules' outputSlots
        /// </summary>
        [NotNull]
        public List<CGModuleLink> GetInputLinks(CGModuleInputSlot inputSlot)
            => GetLinks(
                InputLinks,
                inputSlot
            );

        [UsedImplicitly]
        [Obsolete(
            "Use ComponentExt.DuplicateGameObject and CurvyGenerator.AddModule to duplicate the module then add it to the generator "
        )]
        public CGModule CopyTo(CurvyGenerator targetGenerator)
        {
            if (this == null)
                throw new InvalidOperationException("[Curvy] Trying to copy an already deleted module");

            CGModule newModule = this.DuplicateGameObject<CGModule>(targetGenerator.transform);
            newModule.name = name;
            targetGenerator.AddModule(newModule);
            return newModule;
        }

        public Component AddManagedResource([NotNull] string resourceName, string context = "", int index = -1)
        {
            Component res = CGResourceHandler.CreateResource(
                this,
                resourceName,
                context
            );
            RenameResource(
                context == ""
                    ? resourceName
                    : resourceName + context,
                res,
                index
            );
            res.transform.SetParent(transform);
            return res;
        }


        public void DeleteManagedResource(string resourceName, Component res, [NotNull] string context = "",
            bool dontUsePool = false)
        {
            if (res)
                CGResourceHandler.DestroyResource(
                    this,
                    resourceName,
                    res,
                    context,
                    dontUsePool
                );
        }

        public bool IsManagedResource(Component res)
            => res
               && res.transform.parent
               == transform; //res.gameObject.GetComponentInParent<CurvyGenerator>() == Generator);RetrieveGenerator


        public List<IPool> GetAllPrefabPools()
            => Generator.PoolManager.FindPools(identifier.StringID + "_");

        public void DeleteAllPrefabPools() =>
            Generator.PoolManager.DeletePools(identifier.StringID + "_");

        public void Delete()
        {
            OnStateChange();
            gameObject.Destroy(
                true,
                true
            );
        }

        public CGModuleInputSlot GetInputSlot(string name) => slots.GetInputSlot(name);

        public CGModuleOutputSlot GetOutputSlot(string name) => slots.GetOutputSlot(name);

        public bool GetManagedResources(out List<Component> components, out List<string> resourceNames)
        {
            components = new List<Component>();
            resourceNames = new List<string>();
            FieldInfo[] fields = GetType().GetAllFields(
                false,
                true
            );
            foreach (FieldInfo f in fields)
            {
                CGResourceManagerAttribute at = f.GetCustomAttribute<CGResourceManagerAttribute>();
                if (at != null)
                {
                    if (typeof(ICGResourceCollection).IsAssignableFrom(f.FieldType))
                    {
                        ICGResourceCollection col = f.GetValue(this) as ICGResourceCollection;
                        if (col != null)
                        {
                            Component[] items = col.ItemsArray;
                            foreach (Component component in items)
                                //component can be null if for example the user delete from the hierarchy a CGMeshResource game object
                                if (component && component.transform.parent == transform)
                                {
                                    components.Add(component);
                                    resourceNames.Add(at.ResourceName);
                                }
                        }
                    }
                    else
                    {
                        Component component = f.GetValue(this) as Component;
                        if (component && component.transform.parent == transform)
                        {
                            components.Add(component);
                            resourceNames.Add(at.ResourceName);
                        }
                    }
                }
            }

            return components.Count > 0;
        }

        #endregion

        #region ### Privates, Protected and Internals ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        #region Naming

        private void SetModuleName()
        {
#pragma warning disable CS0612
            string infoModuleName = Info.ModuleName;
            string moduleName = string.IsNullOrEmpty(infoModuleName)
                ? Info.MenuName.Substring(
                    Info.MenuName.LastIndexOf(
                        "/",
                        StringComparison.Ordinal
                    )
                    + 1
                )
                : infoModuleName;
            ModuleName = moduleName;
            ModuleName = Generator.GetModuleUniqueName(this);
#pragma warning restore CS0612
        }

        protected void RenameResource([NotNull] string resourceName, Component resource, int index = -1)
            => resourceNamer.Rename(
                resourceName,
                resource,
                index
            );

        #endregion

        [CanBeNull]
        private static CGModuleLink GetLink(List<CGModuleLink> lst, CGModuleSlot source, CGModuleSlot target)
            => lst
                .FirstOrDefault(
                    t => t.IsSame(
                        source,
                        target
                    )
                );

        [NotNull]
        private static List<CGModuleLink> GetLinks(List<CGModuleLink> lst, CGModuleSlot source)
            => lst
                .Where(t => t.IsFrom(source))
                .ToList();

        protected PrefabPool GetPrefabPool(GameObject prefab)
            => Generator.PoolManager.GetPrefabPool(
                identifier.StringID + "_" + prefab.name,
                prefab
            );

        protected bool TryDeleteChildrenFromAssociatedPrefab()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                int childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform item = transform.GetChild(i);
                    if (DTUtility.DoesPrefabStatusAllowDeletion(
                            item.gameObject,
                            out _
                        )
                        == false)
                    {
                        Generator.DeleteAllOutputManagedResourcesFromAssociatedPrefab();
                        return true;
                    }
                }
            }
#endif
            return false;
        }


        internal void doRefresh()
        {
#if UNITY_EDITOR || CURVY_DEBUG
            DEBUG_LastUpdateTime = DateTime.Now;
            DEBUG_ExecutionTime.Start();
#endif

            if (RandomizeSeed)
                Random.InitState(unchecked((int)DateTime.Now.Ticks));
            else
                Random.InitState(Seed);
            OnBeforeRefreshEvent(new CurvyCGEventArgs(this));
            Refresh();
            Random.InitState(unchecked((int)DateTime.Now.Ticks));

#if UNITY_EDITOR || CURVY_DEBUG
            DEBUG_ExecutionTime.Stop();
#endif
            OnRefreshEvent(new CurvyCGEventArgs(this));

            dirtinessManager.UnsetDirtyFlag();
        }

        public void checkOnStateChangedINTERNAL() => dirtinessManager.CheckOnStateChanged();

        [NotNull]
        [UsedImplicitly]
        [Obsolete("This does not return all resource managers. Read todo inside and fix it first")]
        private List<(Component ResourceManager, string ResourceName)> GetResourceManagers()
        {
            List<(Component ResourceManager, string ResourceName)> managers =
                new List<(Component ResourceManager, string ResourceName)>();
            FieldInfo[] fields = GetType().GetAllFields(
                false,
                true
            );
            foreach (FieldInfo field in fields)
            {
                CGResourceManagerAttribute resourceManagerAttribute = field.GetCustomAttribute<CGResourceManagerAttribute>();
                if (resourceManagerAttribute == null)
                    continue;
                //by restricting resource managers to Components, we ignore fields like CreateMesh.m_MeshResources which are annotated with instances of ICGResourceCollection. Todo fix this
                Component component = field.GetValue(this) as Component;
                if (ReferenceEquals(
                        component,
                        null
                    )
                    == false)
                    managers.Add((component, resourceManagerAttribute.ResourceName));
            }

            return managers;
        }


        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            identifier.Reset();
            dirtinessManager.Reset();
            UIMessages.Clear();
            resourceNamer.ClearCache();
        }


        //@}
#endif
        private bool UsesRandom()
        {
#pragma warning disable CS0612
            return Info != null && Info.UsesRandom;
#pragma warning restore CS0612
        }

        #endregion

        #region Obsolete sorting members

        /// <summary>
        /// Helper for topology sorting
        /// </summary>
        [UsedImplicitly]
        [Obsolete]
        internal int SortAncestors;

        /// <summary>
        /// Initializes SortAncestor with number of connected Input links
        /// </summary>
        [UsedImplicitly]
        [Obsolete]
        internal void initializeSort() =>
            SortAncestors = slots.InputSlots.Where(t => t.IsLinked).Sum(t => t.LinkedSlots.Count);

        /// <summary>
        /// Decrement SortAncestor of linked modules and return a list of childs where SortAncestor==0
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        [Obsolete]
        internal List<CGModule> decrementChilds()
        {
            IEnumerable<CGModuleSlot> outputLinkedSlots = slots.OutputSlots.SelectMany(outputSlot => outputSlot.LinkedSlots);
            foreach (CGModuleSlot linkedSlot in outputLinkedSlots)
                --linkedSlot.Module.SortAncestors;

            List<CGModule> orphanModules = new List<CGModule>();
            foreach (CGModuleOutputSlot slot in slots.OutputSlots)
                orphanModules.AddRange(from t in slot.LinkedSlots where t.Module.SortAncestors == 0 select t.Module);

            return orphanModules;
        }

        #endregion
    }
}