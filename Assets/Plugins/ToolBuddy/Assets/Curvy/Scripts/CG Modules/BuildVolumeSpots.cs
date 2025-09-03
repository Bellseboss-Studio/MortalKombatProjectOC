// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Build/Volume Spots",
        ModuleName = "Volume Spots",
        Description = "Generate spots along a path/volume",
        UsesRandom = true
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgvolumespots")]
    public partial class BuildVolumeSpots : CGModule, ISerializationCallbackReceiver
    {
        private const int MinCrossBase = -1;
        private const int MaxCrossBase = 1;

        private const int MinRange = 0;
        private const int MaxRange = 1;

        public BuildVolumeSpots() =>
            Version = "1";

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            Name = "Path/Volume",
            DisplayName = "Volume/Rasterized Path"
        )]
        public CGModuleInputSlot InPath = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGBounds),
            Array = true
        )]
        public CGModuleInputSlot InBounds = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(typeof(CGSpots))]
        public CGModuleOutputSlot OutSpots = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [SerializeField, HideInInspector]
#pragma warning disable 414
        private bool m_WasUpgraded;
#pragma warning restore 414

        [Tab("General")]
        [Section("Default/General/Volume Path")]
        [FloatRegion(
            RegionOptionsPropertyName = nameof(RangeOptions),
            Precision = 4
        )]
        [SerializeField]
        private FloatRegion m_Range = FloatRegion.ZeroOne;

        [Section("Default/General/Volume Cross")]
        [Tooltip("When the source is a Volume, you can choose if you want to use it's path or the volume")]
        [FieldCondition(
            nameof(IsInputAVolume),
            false,
            true
        )]
        [SerializeField]
        [Label("Use Volume's Surface")]
        private bool m_UseVolume = true;

        [SerializeField]
        [RangeEx(
            MinCrossBase,
            MaxCrossBase
        )]
        [Tooltip("Shifts the Cross origin value by constant value")]
        private float m_CrossBase;

        [SerializeField]
        [Label("Cross Base Variation")]
        [Tooltip(
            "Shifts the Cross origin value by a value that varies along the Volume's length. The Curve's X axis has values between 0 (start of the Range) and 1 (its end)"
        )]
        private AnimationCurve m_CrossCurve = AnimationCurve.Linear(
            0,
            0,
            1,
            0
        );

        [Section(
            "Default/General/Advanced Settings",
            false
        )]
        [Tooltip("Check to run a dry run without actually creating spots")]
        [SerializeField]
        private bool m_Simulate;

        [SerializeField]
        [Tooltip(
            "Until version 6.3.1, this module had a bug in the computation of the randomized values. Enable this value to keep that bugged behaviour if your project depends on it"
        )]
        private bool m_UseBuggedRNG;

        [Tab("Groups")]
        [ArrayEx(Space = 10)]
        [SerializeField]
        private List<CGBoundsGroup> m_Groups = new List<CGBoundsGroup>();

        [IntRegion(
            UseSlider = false,
            RegionOptionsPropertyName = nameof(RepeatingGroupsOptions),
            Options = AttributeOptionsFlags.Compact
        )]
        [SerializeField]
        [Tooltip(
            "The range of groups that will be placed repetitively along the volume. Groups that are not in this range will be placed only once"
        )]
        private IntRegion m_RepeatingGroups;

        [SerializeField]
        private CurvyRepeatingOrderEnum m_RepeatingOrder = CurvyRepeatingOrderEnum.Row;

        [SerializeField]
        [FieldCondition(
            nameof(ShowFitEnd),
            true
        )]
        [Label("Fits The End")]
        [Tooltip(
            "If checked, the last non repeating group is placed exactly at the end of the volume used for spots. If not, the last group is placed at the first available spot, which might leave some space between it and the end of the volume"
        )]
        private bool m_FitEnd;

        #endregion

        #region ### Public Properties ###

        #region - General Tab -

        public FloatRegion Range
        {
            get => m_Range;
            set
            {
                //todo bug? one can assign ranges that go beyond MinRange and MaxRange. Should this be fixed?
                if (m_Range != value)
                {
                    m_Range = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If the source is a Volume you can choose if you want to use it's path or the volume
        /// </summary>
        public bool UseVolume
        {
            get => m_UseVolume;
            set
            {
                if (m_UseVolume != value)
                {
                    m_UseVolume = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Set to true to dry run without actually creating spots
        /// </summary>
        public bool Simulate
        {
            get => m_Simulate;
            set
            {
                if (m_Simulate != value)
                {
                    m_Simulate = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Until version 6.3.1, this module had a bug in the computation of the randomized values. Set this to true to keep that bugged behaviour if your project depends on it
        /// </summary>
        public bool UseBuggedRng
        {
            get => m_UseBuggedRNG;
            set
            {
                if (m_UseBuggedRNG != value)
                {
                    m_UseBuggedRNG = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Shifts the Cross origin value by a constant value
        /// </summary>
        public float CrossBase
        {
            get => m_CrossBase;
            set
            {
                float v = value.Repeat(
                    MinCrossBase,
                    MaxCrossBase
                );
                if (m_CrossBase != v)
                {
                    m_CrossBase = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Shifts the Cross origin value by a value that varies along the Volume's length. The Curve's X axis has values between 0 (start of the <seealso cref="Range"/>) and 1 (its end)
        /// </summary>
        public AnimationCurve CrossCurve
        {
            get => m_CrossCurve;
            set
            {
                if (m_CrossCurve != value)
                {
                    m_CrossCurve = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        public List<CGBoundsGroup> Groups
        {
            get => m_Groups;
            set => m_Groups = value;
        }

        public CurvyRepeatingOrderEnum RepeatingOrder
        {
            get => m_RepeatingOrder;
            set
            {
                if (m_RepeatingOrder != value)
                {
                    m_RepeatingOrder = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// First index of the range of groups that will be placed repetitively along the volume. Groups that are not in this range will be placed only once
        /// </summary>
        public int FirstRepeating
        {
            get => m_RepeatingGroups.From;
            set
            {
                int v = Mathf.Clamp(
                    value,
                    0,
                    LastGroupIndex
                );
                if (m_RepeatingGroups.From != v)
                {
                    m_RepeatingGroups.From = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Last index of the range of groups that will be placed repetitively along the volume. Groups that are not in this range will be placed only once
        /// </summary>
        public int LastRepeating
        {
            get => m_RepeatingGroups.To;
            set
            {
                int v = Mathf.Clamp(
                    value,
                    FirstRepeating,
                    LastGroupIndex
                );
                if (m_RepeatingGroups.To != v)
                {
                    m_RepeatingGroups.To = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If true, the last non repeating group is placed exactly at the end of the volume used for spots. If not, the last group is placed at the first available spot, which might leave some space between it and the end of the volume
        /// </summary>
        public bool FitEnd
        {
            get => m_FitEnd;
            set
            {
                if (m_FitEnd != value)
                {
                    m_FitEnd = value;
                    Dirty = true;
                }
            }
        }


        public int GroupCount => Groups.Count;

        //todo design: get rid of this at some point
#if UNITY_EDITOR == false
        [JetBrains.Annotations.UsedImplicitly]
        [Obsolete("Will become an editor only method")]
#endif
        public GUIContent[] BoundsNames
        {
            get
            {
                return editorData.BoundsNames != null
                    ? editorData.BoundsNames.Select(n => new GUIContent(n)).ToArray()
                    : Array.Empty<GUIContent>();
            }
        }

        [UsedImplicitly]
        [Obsolete]
        public int[] BoundsIndices
        {
            get
            {
                GUIContent[] boundNames = BoundsNames;
                int[] indices = new int[boundNames.Length];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = i;
                return indices;
            }
        }

        //todo make editor only at some point
        public int Count => editorData.SpotsCount;


        public CGSpots SimulatedSpots; //TODO Understand the namming behind this variable, and rename it if needed

        #endregion

        #region ### Private Fields & Properties ###

        /// <summary>
        /// Data that exists solely for the purpose of this class' editor to display properly. Ideally, this field should be editor only, but alas, this data was made public in the past, and I don't want to break existing behaviour. At some point, I will make the public members sharing this data editor only, and then make this field editor only
        /// </summary>
        private EditorData editorData = new EditorData(
            new List<CGBounds>(),
            false,
            0
        );

        private bool IsInputAVolume() => editorData.InputIsAVolume;

        private int LastGroupIndex => Mathf.Max(
            0,
            GroupCount - 1
        );

        private RegionOptions<float> RangeOptions => RegionOptions<float>.MinMax(
            MinRange,
            MaxRange
        );

        private RegionOptions<int> RepeatingGroupsOptions => RegionOptions<int>.MinMax(
            0,
            LastGroupIndex
        );

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            //todo MinWidth should take into account the number of groups
            Properties.MinWidth = 350;
            //Properties.LabelWidth = 80;
        }

        public override void Reset()
        {
            base.Reset();
            Range = FloatRegion.ZeroOne;
            UseVolume = true;
            Simulate = false;
            UseBuggedRng = false;
            CrossBase = 0;
            CrossCurve = AnimationCurve.Linear(
                0,
                0,
                1,
                0
            );
            RepeatingOrder = CurvyRepeatingOrderEnum.Row;
            FirstRepeating = 0;
            LastRepeating = 0;
            FitEnd = false;

            Groups.Clear();
            AddGroup("Group");
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void OnStateChange()
        {
            base.OnStateChange();
            if (!IsConfigured)
                Clear();
        }

        public void Clear()
        {
            editorData = new EditorData(
                new List<CGBounds>(),
                false,
                0);
            SimulatedSpots = new CGSpots();
            OutSpots.SetDataToElement(SimulatedSpots);
        }

        public override void Refresh()
        {
            base.Refresh();

            List<CGBounds> bounds = InBounds.GetAllData<CGBounds>(out bool isBoundsDisposable);

            const float MinimumAllowedBoundDepth = 0.001f;

            bool isModuleMisconfigured = false;
            {
                if (bounds.Count == 0)
                {
                    isModuleMisconfigured = true;
                    UIMessages.Add("The input bounds list is empty. Add some to enable spots generation.");
                }

                if (Groups.Count == 0)
                {
                    isModuleMisconfigured = true;
                    UIMessages.Add("No group created. Create a group in the Groups tab to enable spots generation");
                }

                for (int i = 0; i < bounds.Count; i++)
                {
                    CGBounds
                        cgBounds = bounds[
                            i]; //TODO DESIGN BUG? elements of the bounds array can be null. Should this scenario be made impossible (as with any CGData arrays passed between modules) or is this ok, in which case this code should handle null. I got null while implementing a new module that did not return its data properly, so no know issue in release code so far
                    if (cgBounds is CGGameObject && ((CGGameObject)cgBounds).Object == null)
                    {
                        isModuleMisconfigured = true;
                        UIMessages.Add(
                            String.Format(
                                "Input object of index {0} has no Game Object attached to it. Correct this to enable spots generation.",
                                i
                            )
                        );
                    }
                    //Correcting invalid bounds
                    else if (cgBounds.Depth <= MinimumAllowedBoundDepth)
                    {
#if CURVY_SANITY_CHECKS
                        Assert.IsTrue(cgBounds.Bounds.size.z <= 0);
#endif
                        CGBounds correctedBounds = new CGBounds(cgBounds);

                        UIMessages.Add(
                            String.Format(
                                "Input object \"{0}\" has bounds with a depth of {1}. The minimal accepted depth is {2}. The depth value was overriden.",
                                correctedBounds.Name,
                                cgBounds.Depth,
                                MinimumAllowedBoundDepth
                            )
                        );

                        correctedBounds.Bounds = new Bounds(
                            cgBounds.Bounds.center,
                            new Vector3(
                                cgBounds.Bounds.size.x,
                                cgBounds.Bounds.size.y,
                                MinimumAllowedBoundDepth
                            )
                        );
                        bounds[i] = correctedBounds;
                    }
                }

                foreach (CGBoundsGroup cgBoundsGroup in Groups)
                    if (cgBoundsGroup.ItemCount == 0)
                    {
                        isModuleMisconfigured = true;
                        UIMessages.Add(
                            String.Format(
                                "Group \"{0}\" has 0 item in it. Add some to enable spots generation.",
                                cgBoundsGroup.Name
                            )
                        );
                    }
                    else
                        foreach (CGBoundsGroupItem cgBoundsGroupItem in cgBoundsGroup.Items)
                        {
                            int itemIndex = cgBoundsGroupItem.Index;
                            if (itemIndex < 0 || itemIndex >= bounds.Count) //This might happen when changing the module inputs
                            {
                                isModuleMisconfigured = true;
                                UIMessages.Add(
                                    String.Format(
                                        "Group \"{0}\" has a reference to an nonexistent item of index {1}. Correct the reference to enable spots generation.",
                                        cgBoundsGroup.Name,
                                        itemIndex
                                    )
                                );
                                break;
                            }
                        }
            }

            CGPath path = InPath.GetData<CGPath>(out bool isdiDisposable);
            SubArrayList<CGSpot> spots = new SubArrayList<CGSpot>(
                100,
                ArrayPools.CGSpot
            );

            Dictionary<CGBoundsGroup, WeightedRandom<int>> itemsBagDictionary = Prepare(out WeightedRandom<int> groupBag);

            if (path && isModuleMisconfigured == false)
            {
                const int MaxSpotsCount = 10000;
                bool reachedMaxSpotsCount = false;

                float endDistance = path.FToDistance(Range.To);
                float startDistance = path.FToDistance(Range.Low);
                float currentDistance = startDistance;

                // Place groups that are before the repeating items range
                for (int g = 0; g < FirstRepeating; g++)
                {
                    int groupIndex = g;
                    bool failedAddingAllItems;

                    reachedMaxSpotsCount = AddGroupItems(
                        bounds,
                        path,
                        groupIndex,
                        ref spots,
                        endDistance - currentDistance,
                        startDistance,
                        ref currentDistance,
                        out failedAddingAllItems,
                        itemsBagDictionary,
                        MaxSpotsCount
                    );
                    if (reachedMaxSpotsCount)
                        break;
                }

                List<EndGroupData> nonRepeatingEndGroups;
                // Compute data needed to place groups that are after the repeating range. They will be actually placed a the end of this method
                bool hasNonRepeatingEndGroups = GroupCount - LastRepeating - 1 > 0;
                if (reachedMaxSpotsCount == false && hasNonRepeatingEndGroups)
                {
                    nonRepeatingEndGroups = new List<EndGroupData>();
                    for (int g = LastRepeating + 1; g < GroupCount; g++)
                    {
                        CGBoundsGroup cgBoundsGroup = Groups[g];
#if CURVY_SANITY_CHECKS
                        Assert.IsTrue(cgBoundsGroup.ItemCount > 0);
#endif
                        SubArray<int> itemIndices = GetGroupItemIndices(
                            cgBoundsGroup,
                            itemsBagDictionary[cgBoundsGroup]
                        );

                        float spaceBefore = UseBuggedRng
                            ? cgBoundsGroup.SpaceBefore.Next
                            : GetRegionNextValue(cgBoundsGroup.SpaceBefore);
                        float spaceAfter = UseBuggedRng
                            ? cgBoundsGroup.SpaceAfter.Next
                            : GetRegionNextValue(cgBoundsGroup.SpaceAfter);

                        CGBounds[] itemBounds;
                        float groupDepth = GetGroupDepth(
                            bounds,
                            itemIndices,
                            spaceBefore,
                            spaceAfter,
                            out itemBounds
                        );

                        nonRepeatingEndGroups.Add(
                            new EndGroupData(
                                cgBoundsGroup,
                                itemIndices,
                                groupDepth,
                                itemBounds,
                                spaceBefore,
                                spaceAfter
                            )
                        );
                    }
                }
                else
                    nonRepeatingEndGroups = null;

                float repeatGroupsEndDistance;
                {
                    repeatGroupsEndDistance = endDistance;
                    if (hasNonRepeatingEndGroups)
                        foreach (EndGroupData endGroupData in nonRepeatingEndGroups)
                        {
                            float availableSpace = repeatGroupsEndDistance - currentDistance;

                            // the multiplication is to ensure a margin, otherwise float imprecision create issue
                            //The issue being that when FitEnd is true, we start spawning the end group starting from repeatGroupsEndDistance which is supposed to be enough to fit all end groups, but sometimes the last item of the end group has no place to be fit in
                            float possibleRepeatGroupsEndDistance =
                                repeatGroupsEndDistance - (endGroupData.GroupDepth * 1.00001f);

                            //the group can fit
                            if (endGroupData.GroupDepth <= availableSpace)
                            {
                                repeatGroupsEndDistance = possibleRepeatGroupsEndDistance;
                                continue;
                            }

                            //the whole group can't be fit
                            if (endGroupData.BoundsGroup.KeepTogether)
                                //group will not be spawned because it can't fit, so we ignore it
                                continue;

                            //Can we fit at least one item?
                            if (false
                                == endGroupData.ItemBounds.Any(
                                    i =>
                                        i.Depth + endGroupData.SpaceBefore + endGroupData.SpaceAfter <= availableSpace
                                ))
                                continue;

                            repeatGroupsEndDistance = possibleRepeatGroupsEndDistance;
                        }
                }

                // Place groups that are in the repeating items range
                if (RepeatingOrder == CurvyRepeatingOrderEnum.Row)
                {
                    int g = FirstRepeating;
                    bool failedAddingAllItems = false;
                    while (reachedMaxSpotsCount == false
                           && failedAddingAllItems == false
                           && repeatGroupsEndDistance > currentDistance)
                    {
                        int groupIndex = g++;
                        if (g > LastRepeating)
                            g = FirstRepeating;

                        reachedMaxSpotsCount = AddGroupItems(
                            bounds,
                            path,
                            groupIndex,
                            ref spots,
                            repeatGroupsEndDistance - currentDistance,
                            startDistance,
                            ref currentDistance,
                            out failedAddingAllItems,
                            itemsBagDictionary,
                            MaxSpotsCount
                        );
                        if (reachedMaxSpotsCount)
                            break;
                    }
                }
                else
                {
                    bool failedAddingAllItems = false;
                    while (reachedMaxSpotsCount == false
                           && failedAddingAllItems == false
                           && repeatGroupsEndDistance > currentDistance)
                    {
                        int groupIndex = groupBag.Next();

                        reachedMaxSpotsCount = AddGroupItems(
                            bounds,
                            path,
                            groupIndex,
                            ref spots,
                            repeatGroupsEndDistance - currentDistance,
                            startDistance,
                            ref currentDistance,
                            out failedAddingAllItems,
                            itemsBagDictionary,
                            MaxSpotsCount
                        );
                        if (reachedMaxSpotsCount)
                            break;
                    }
                }

                // Now we actually Place groups that are after the repeating items range
                if (reachedMaxSpotsCount == false && hasNonRepeatingEndGroups)
                {
                    if (FitEnd)
                        //BUG in the case where repeatGroupsEndDistance is smaller than currentDistance, FitEnd will have no effect. To fix that, you will need to compute a new repeatGroupsEndDistance that is bigger than currentDistance, that will consider not all end groups, but only the ones that can fit
                        currentDistance = Mathf.Max(
                            currentDistance,
                            repeatGroupsEndDistance
                        );

                    foreach (EndGroupData endGroupData in nonRepeatingEndGroups)
                    {
                        bool failedAddingAllItems;
                        AddGroupItems(
                            path,
                            endGroupData.BoundsGroup,
                            ref spots,
                            endDistance - currentDistance,
                            startDistance,
                            ref currentDistance,
                            out failedAddingAllItems,
                            endGroupData.ItemIndices,
                            endGroupData.GroupDepth,
                            endGroupData.ItemBounds,
                            endGroupData.SpaceBefore,
                            endGroupData.SpaceAfter
                        );

                        if (spots.Count >= MaxSpotsCount)
                        {
                            reachedMaxSpotsCount = true;
                            break;
                        }
                    }
                }

                if (reachedMaxSpotsCount)
                {
                    string errorMessage = String.Format(
                        "Number of generated spots reached the maximal allowed number, which is {0}. Spots generation was stopped. Try to reduce the number of spots needed by using bigger Bounds as inputs and/or setting bigger space between two spots.",
                        MaxSpotsCount
                    );
                    UIMessages.Add(errorMessage);
                    DTLog.LogError(
                        "[Curvy] Volume spots: " + errorMessage,
                        this
                    );
                }

                if (nonRepeatingEndGroups != null)
                {
                    foreach (EndGroupData endGroup in nonRepeatingEndGroups)
                        endGroup.Dispose();
                    nonRepeatingEndGroups = null;
                }
            }

            editorData = new EditorData(
                bounds,
                path is CGVolume,
                spots.Count
            );

            if (isBoundsDisposable)
                foreach (CGBounds cgBounds in bounds)
                    cgBounds.Dispose();

            SimulatedSpots = new CGSpots(spots.ToSubArray());

            if (Simulate)
                OutSpots.SetDataToElement(new CGSpots());
            else
                OutSpots.SetDataToElement(SimulatedSpots);

            if (isdiDisposable)
                path.Dispose();
        }

        public CGBoundsGroup AddGroup(string name)
        {
            //TODO unify this code with the one in BuildVolumeSpotEditor.SetupArrayEx()

            CGBoundsGroup grp = new CGBoundsGroup(name);
            grp.Items.Add(new CGBoundsGroupItem());
            Groups.Add(grp);
            Dirty = true;
            return grp;
        }

        public void RemoveGroup(CGBoundsGroup group)
        {
            Groups.Remove(group);
            Dirty = true;
        }

        #endregion

        #region ### Privates ###

        private static SubArray<int> GetGroupItemIndices(CGBoundsGroup boundsGroup, WeightedRandom<int> groupItemBag)
        {
            SubArray<int> result = ArrayPools.Int32.Allocate(
                boundsGroup.ItemCount,
                false
            );
            for (int i = 0; i < boundsGroup.ItemCount; i++)
            {
                int itemId =
                    boundsGroup.RandomizeItems && i >= boundsGroup.FirstRepeating && i <= boundsGroup.LastRepeating
                        ? groupItemBag.Next()
                        : i;

                result.Array[i] = boundsGroup.Items[itemId].Index;
            }

            return result;
        }

        private static float GetGroupDepth(List<CGBounds> bounds, SubArray<int> groupItemIndices, float spaceBefore,
            float spaceAfter, out CGBounds[] itemsBounds)
        {
            itemsBounds = new CGBounds[groupItemIndices.Count];
            float groupDepth = spaceBefore + spaceAfter;
            {
                for (int i = 0; i < groupItemIndices.Count; i++)
                {
                    CGBounds itemBounds = bounds[groupItemIndices.Array[i]];
                    itemsBounds[i] = itemBounds;
                    groupDepth += itemBounds.Depth;
#if CURVY_SANITY_CHECKS
                    Assert.IsTrue(itemBounds.Depth > 0);
#endif
                }
            }
            return groupDepth;
        }


        private bool AddGroupItems(List<CGBounds> bounds, CGPath path, int groupIndex, ref SubArrayList<CGSpot> spots,
            float remainingLength, float startDistance, ref float currentDistance, out bool failedAddingAllItems,
            Dictionary<CGBoundsGroup, WeightedRandom<int>> itemsBagDictionary, int MaxSpotsCount)
        {
            CGBoundsGroup cgBoundsGroup = Groups[groupIndex];
            WeightedRandom<int> groupItemBag = itemsBagDictionary[cgBoundsGroup];

            SubArray<int> itemIndices = GetGroupItemIndices(
                cgBoundsGroup,
                groupItemBag
            );

            float spaceBefore = UseBuggedRng
                ? cgBoundsGroup.SpaceBefore.Next
                : GetRegionNextValue(cgBoundsGroup.SpaceBefore);
            float spaceAfter = UseBuggedRng
                ? cgBoundsGroup.SpaceAfter.Next
                : GetRegionNextValue(cgBoundsGroup.SpaceAfter);

            CGBounds[] itemBounds;
            float groupDepth = GetGroupDepth(
                bounds,
                itemIndices,
                spaceBefore,
                spaceAfter,
                out itemBounds
            );

            AddGroupItems(
                path,
                cgBoundsGroup,
                ref spots,
                remainingLength,
                startDistance,
                ref currentDistance,
                out failedAddingAllItems,
                itemIndices,
                groupDepth,
                itemBounds,
                spaceBefore,
                spaceAfter
            );
            ArrayPools.Int32.Free(itemIndices);

            return spots.Count >= MaxSpotsCount;
        }

        private void AddGroupItems(CGPath path, CGBoundsGroup group, ref SubArrayList<CGSpot> spots, float remainingLength,
            float startDistance, ref float currentDistance, out bool failedAddingAllItems, SubArray<int> itemIndices,
            float groupDepth, CGBounds[] itemBounds, float spaceBefore, float spaceAfter)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(group.ItemCount > 0);
#endif
            if (remainingLength >= groupDepth || group.KeepTogether == false)
            {
                failedAddingAllItems = false;
                for (int index = 0; index < itemIndices.Count; index++)
                {
                    float distanceAtIterationStart = currentDistance;

                    int itemId = itemIndices.Array[index];
                    CGBounds currentBounds = itemBounds[index];

                    bool canAddItem;
                    if (index == 0)
                    {
                        canAddItem = remainingLength > spaceBefore + currentBounds.Depth;
                        if (canAddItem)
                            currentDistance += spaceBefore;
                    }
                    else if (index == itemIndices.Count - 1)
                        canAddItem = remainingLength > spaceAfter + currentBounds.Depth;
                    else
                        canAddItem = remainingLength > currentBounds.Depth;

                    if (canAddItem == false)
                    {
                        failedAddingAllItems = true;
                        break;
                    }

                    spots.Add(
                        GetSpot(
                            path,
                            itemId,
                            group,
                            currentBounds,
                            currentDistance,
                            startDistance
                        )
                    );

                    if (index == itemIndices.Count - 1)
                        currentDistance += currentBounds.Depth + spaceAfter;
                    else
                        currentDistance += currentBounds.Depth;

                    remainingLength -= currentDistance - distanceAtIterationStart;
                }
            }
            else
                failedAddingAllItems = true;
        }

        private CGSpot GetSpot(CGPath path, int itemID, CGBoundsGroup boundsGroup, CGBounds bounds, float currentDistance,
            float startDistance)
        {
            float pathRelativeDistance = path.DistanceToF(currentDistance + (bounds.Depth / 2));

            float pathLength = path.Length * Range.Length;
            float globalF = (currentDistance - startDistance) / pathLength;
            float crossF;
            {
                float rawCrossF = UseBuggedRng
                    ? boundsGroup.CrossBase.Next
                    : GetRegionNextValue(boundsGroup.CrossBase);
                if (boundsGroup.IgnoreModuleCrossBase == false)
                    rawCrossF += CrossBase + m_CrossCurve.Evaluate(globalF);

                //Warning, crossF can be beyond the range -0.5f;0.5f
                crossF = DTMath.MapValue(
                    -0.5f,
                    0.5f,
                    rawCrossF
                );
            }

            Vector3 interpolatedPosition;
            Vector3 tangent;
            Vector3 up;
            CGVolume volume = path as CGVolume;
            bool useVolume = UseVolume && volume;
            switch (boundsGroup.RotationMode)
            {
                case CGBoundsGroup.RotationModeEnum.Full:
                {
                    if (useVolume)
                        volume.InterpolateVolume(
                            pathRelativeDistance,
                            crossF,
                            out interpolatedPosition,
                            out tangent,
                            out up
                        );
                    else
                    {
                        path.Interpolate(
                            pathRelativeDistance,
                            out interpolatedPosition,
                            out tangent,
                            out up
                        );
                        if (crossF != 0)
                            up = Quaternion.AngleAxis(
                                     crossF * -360,
                                     tangent
                                 )
                                 * up;
                    }
                }
                    break;
                case CGBoundsGroup.RotationModeEnum.Direction:
                case CGBoundsGroup.RotationModeEnum.Horizontal:
                {
                    if (useVolume)
                        volume.InterpolateVolume(
                            pathRelativeDistance,
                            crossF,
                            out interpolatedPosition,
                            out tangent,
                            out _
                        );
                    else
                        path.Interpolate(
                            pathRelativeDistance,
                            out interpolatedPosition,
                            out tangent,
                            out _
                        );

                    up = Vector3.up;
                    if (boundsGroup.RotationMode == CGBoundsGroup.RotationModeEnum.Horizontal)
                        tangent.y = 0;
                }
                    break;
                case CGBoundsGroup.RotationModeEnum.Independent:
                {
                    if (useVolume)
                        interpolatedPosition = volume.InterpolateVolumePosition(
                            pathRelativeDistance,
                            crossF
                        );
                    else
                        interpolatedPosition = path.InterpolatePosition(pathRelativeDistance);
                    up = Vector3.up;
                    tangent = Vector3.forward;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            Vector3 translation;
            Quaternion rotation;
            Vector3 scale;
            if (UseBuggedRng)
                GetTRS630(
                    boundsGroup,
                    tangent,
                    up,
                    out rotation,
                    out translation,
                    out scale
                );
            else
                GetTRS(
                    boundsGroup,
                    tangent,
                    up,
                    out rotation,
                    out translation,
                    out scale
                );

            return new CGSpot(
                itemID,
                interpolatedPosition.Addition(
                    boundsGroup.RelativeTranslation
                        ? rotation * translation
                        : translation
                ),
                rotation,
                scale
            );
        }

        private static float GetRegionNextValue(FloatRegion floatRegion)
        {
            float result;
            if (floatRegion.SimpleValue)
            {
                result = floatRegion.From;
                //So that at the end of the if statement, whatever the value of SimpleValue is, the RNG has the same state.
                //Otherwise, the next FloatRegion's randomly generated value will be different depending on this FloatRegion's SimpleValue 
                Random.Range(
                    0f,
                    1f
                );
            }
            else
                result = Random.Range(
                    floatRegion.From,
                    floatRegion.To
                );

            return result;
        }

        private void GetTRS(CGBoundsGroup boundsGroup, Vector3 tangent, Vector3 up, out Quaternion rotation,
            out Vector3 translation, out Vector3 scale)
        {
            //rotation
            {
                Vector3 eulerVector;
                {
                    eulerVector.x = GetRegionNextValue(boundsGroup.RotationX);
                    eulerVector.y = GetRegionNextValue(boundsGroup.RotationY);
                    eulerVector.z = GetRegionNextValue(boundsGroup.RotationZ);
                }
                rotation = Quaternion.LookRotation(
                               tangent,
                               up
                           )
                           * Quaternion.Euler(eulerVector);
            }

            //translation
            {
                translation.x = GetRegionNextValue(boundsGroup.TranslationX);
                translation.y = GetRegionNextValue(boundsGroup.TranslationY);
                translation.z = GetRegionNextValue(boundsGroup.TranslationZ);
            }

            //Scale
            {
                scale.x = GetRegionNextValue(boundsGroup.ScaleX);

                if (boundsGroup.UniformScaling)
                {
                    scale.y = scale.z = scale.x;

                    //So that at the end of the if statement, whatever the value of UniformScaling is, the RNG has the same state.
                    //Otherwise, the next FloatRegion's randomly generated value will be different depending on UniformScaling 
                    Random.Range(
                        0f,
                        1f
                    );
                    Random.Range(
                        0f,
                        1f
                    );
                }
                else
                {
                    scale.y = GetRegionNextValue(boundsGroup.ScaleY);
                    scale.z = GetRegionNextValue(boundsGroup.ScaleZ);
                }
            }
        }

        private void GetTRS630(CGBoundsGroup boundsGroup, Vector3 tangent, Vector3 up, out Quaternion rotation,
            out Vector3 translation, out Vector3 scale)
        {
#pragma warning disable 618

            Vector3 boundsGroupRotationScatter = new Vector3(
                boundsGroup.RotationX.SimpleValue
                    ? 0
                    : (boundsGroup.RotationX.High - boundsGroup.RotationX.Low) * 0.5f,
                boundsGroup.RotationY.SimpleValue
                    ? 0
                    : (boundsGroup.RotationY.High - boundsGroup.RotationY.Low) * 0.5f,
                boundsGroup.RotationZ.SimpleValue
                    ? 0
                    : (boundsGroup.RotationZ.High - boundsGroup.RotationZ.Low) * 0.5f
            );
            Vector3 boundsGroupRotationOffset = new Vector3(
                boundsGroup.RotationX.SimpleValue
                    ? boundsGroup.RotationX.From
                    : (boundsGroup.RotationX.From + boundsGroup.RotationX.To) * 0.5f,
                boundsGroup.RotationY.SimpleValue
                    ? boundsGroup.RotationY.From
                    : (boundsGroup.RotationY.From + boundsGroup.RotationY.To) * 0.5f,
                boundsGroup.RotationZ.SimpleValue
                    ? boundsGroup.RotationZ.From
                    : (boundsGroup.RotationZ.From + boundsGroup.RotationZ.To) * 0.5f
            );
            rotation = Quaternion.LookRotation(
                           tangent,
                           up
                       )
                       * Quaternion.Euler(
                           boundsGroupRotationOffset.x
                           + (boundsGroupRotationScatter.x
                           * Random.Range(
                               -1,
                               1
                           )),
                           boundsGroupRotationOffset.y
                           + (boundsGroupRotationScatter.y
                           * Random.Range(
                               -1,
                               1
                           )),
                           boundsGroupRotationOffset.z
                           + (boundsGroupRotationScatter.z
                           * Random.Range(
                               -1,
                               1
                           ))
                       );
#pragma warning restore 618

            //The calls to Next can lead to calls to Random. To keep the random generations independent, the seed is saved and restored before each call to Next. This isn't done for all the calls to keep the same behaviour as in 6.3.0
            Random.State
                postRotationSeed; //OPTIM assign this variable only if reading it is needed, i.e we need to call Random.Range on one of boundsGroup members
            {
                FloatRegion positionX = boundsGroup.TranslationX;
                FloatRegion positionY = boundsGroup.TranslationY;
                FloatRegion positionZ = boundsGroup.TranslationZ;

                if (positionY.SimpleValue)
                    translation.y = positionY.From;
                else
                    translation.y = Random.Range(
                        positionY.From,
                        positionY.To
                    );

                postRotationSeed = Random.state;

                if (positionX.SimpleValue)
                    translation.x = positionX.From;
                else
                {
                    translation.x = Random.Range(
                        positionX.From,
                        positionX.To
                    );
                    Random.state = postRotationSeed;
                }

                if (positionZ.SimpleValue)
                    translation.z = positionZ.From;
                else
                {
                    translation.z = Random.Range(
                        positionZ.From,
                        positionZ.To
                    );
                    Random.state = postRotationSeed;
                }
            }

            //Scale
            {
                FloatRegion scaleX = boundsGroup.ScaleX;

                if (scaleX.SimpleValue)
                    scale.x = scaleX.From;
                else
                {
                    scale.x = Random.Range(
                        scaleX.From,
                        scaleX.To
                    );
                    Random.state = postRotationSeed;
                }

                if (boundsGroup.UniformScaling)
                    scale.y = scale.z = scale.x;
                else
                {
                    FloatRegion scaleY = boundsGroup.ScaleY;
                    FloatRegion scaleZ = boundsGroup.ScaleZ;

                    if (scaleY.SimpleValue)
                        scale.y = scaleY.From;
                    else
                    {
                        scale.y = Random.Range(
                            scaleY.From,
                            scaleY.To
                        );
                        Random.state = postRotationSeed;
                    }

                    if (scaleZ.SimpleValue)
                        scale.z = scaleZ.From;
                    else
                    {
                        scale.z = Random.Range(
                            scaleZ.From,
                            scaleZ.To
                        );
                        Random.state = postRotationSeed;
                    }
                }
            }
        }

        private Dictionary<CGBoundsGroup, WeightedRandom<int>> Prepare(out WeightedRandom<int> groupBag)
        {
            Dictionary<CGBoundsGroup, WeightedRandom<int>> itemsBagDictionary =
                new Dictionary<CGBoundsGroup, WeightedRandom<int>>();
            m_RepeatingGroups.MakePositive();
            m_RepeatingGroups.Clamp(
                0,
                GroupCount - 1
            );
            // Groups
            groupBag = new WeightedRandom<int>(
                0,
                UseBuggedRng
                    ? 0
                    : Random.Range(
                        0,
                        Int32.MaxValue
                    )
            );
            if (RepeatingOrder == CurvyRepeatingOrderEnum.Random)
            {
                List<CGWeightedItem> cgWeightedItems = Groups.Cast<CGWeightedItem>().ToList();
                CGBoundsGroup.FillItemBag(
                    groupBag,
                    cgWeightedItems,
                    FirstRepeating,
                    LastRepeating
                );
            }

            // Prepare Groups & ItemBags
            for (int g = 0; g < Groups.Count; g++)
            {
                CGBoundsGroup boundsGroup = Groups[g];
                boundsGroup.RepeatingItems.MakePositive();
                boundsGroup.RepeatingItems.Clamp(
                    0,
                    boundsGroup.ItemCount - 1
                );
                WeightedRandom<int> itemsBag;
                {
                    //We save and restore the random state to avoid that the number of groups would modify the randomly generated numbers inside the groups
                    Random.State previousState = Random.state;
                    itemsBag = new WeightedRandom<int>(
                        0,
                        UseBuggedRng
                            ? 0
                            : Random.Range(
                                0,
                                Int32.MaxValue
                            )
                    );
                    Random.state = previousState;
                }
                itemsBagDictionary[boundsGroup] = itemsBag;
                if (boundsGroup.Items.Count != 0 && boundsGroup.RandomizeItems)
                {
                    List<CGWeightedItem> cgWeightedItems = boundsGroup.Items.Cast<CGWeightedItem>().ToList();
                    CGBoundsGroup.FillItemBag(
                        itemsBag,
                        cgWeightedItems,
                        boundsGroup.FirstRepeating,
                        boundsGroup.LastRepeating
                    );
                }
            }

            return itemsBagDictionary;
        }

        private bool ShowFitEnd => LastRepeating != Groups.Count - 1;

        #endregion

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            editorData = new EditorData(
                new List<CGBounds>(),
                false,
                0);
        }
#endif


        #region ISerializationCallbackReceiver

        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnBeforeSerialize() { }

        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (String.IsNullOrEmpty(Version))
            {
                Version = "1";
                m_WasUpgraded = true;

                for (int index = 0; index < Groups.Count; index++)
                {
                    CGBoundsGroup cgBoundsGroup = Groups[index];
                    cgBoundsGroup.RelativeTranslation = true;
                    cgBoundsGroup.TranslationX = new FloatRegion(0f);
                    cgBoundsGroup.TranslationY = new FloatRegion(0f);
                    cgBoundsGroup.TranslationZ = new FloatRegion(0f);
                    cgBoundsGroup.RotationX = new FloatRegion(0f);
                    cgBoundsGroup.RotationY = new FloatRegion(0f);
                    cgBoundsGroup.RotationZ = new FloatRegion(0f);
                    cgBoundsGroup.UniformScaling = true;
                    cgBoundsGroup.ScaleX = new FloatRegion(1f);
                    cgBoundsGroup.ScaleY = new FloatRegion(1f);
                    cgBoundsGroup.ScaleZ = new FloatRegion(1f);
#pragma warning disable 618
                    cgBoundsGroup.ConvertObsoleteData();
#pragma warning restore 618
                }
            }
        }

        #endregion
    }
}