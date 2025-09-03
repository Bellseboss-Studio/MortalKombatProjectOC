// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Class defining a module slot
    /// </summary>
    public class CGModuleSlot
    {
        /// <summary>
        /// The Module this Slot belongs to
        /// </summary>
        public CGModule Module { get; internal set; }

        /// <summary>
        /// Gets the SlotInfo Attribute
        /// </summary>
        public SlotInfo Info { get; internal set; }

        /// <summary>
        /// Origin of Link-Wire, in Canvas space
        /// </summary>
        public Vector2 Origin { get; set; }

        /// <summary>
        /// Mouse-Hotzone, in module space
        /// </summary>
        public Rect DropZone { get; set; }

        /// <summary>
        /// Mouse-Hotzone, in canvas space
        /// </summary>
        public Rect CanvasSpaceDropZone => DropZone.ShiftBy(Module.Properties.Dimensions.position);

        /// <summary>
        /// Whether the link is wired or not
        /// </summary>
        //todo design: IsLinked is called in multiple Refresh methods to exit early if output is not linked. Is this necessary? Does the generator call Refresh() on unlinked modules?
        public bool IsLinked => LinkedSlots != null && LinkedSlots.Count > 0;

        /// <summary>
        /// Whether the link is wired and all connected modules are configured
        /// </summary>
        [Obsolete]
        public bool IsLinkedAndConfigured
        {
            get
            {
                if (!IsLinked)
                    return false;
                for (int i = 0; i < LinkedSlots.Count; i++)
                    if (!LinkedSlots[i].Module.IsConfigured)
                        return false;
                return true;
            }
        }

        /// <summary>
        /// Gets the module as an <see cref="IOnRequestProcessing"/>
        /// </summary>
        public IOnRequestProcessing OnRequestModule => Module as IOnRequestProcessing;

        /// <summary>
        /// Gets the module as an <see cref="IPathProvider"/>
        /// </summary>
        public IPathProvider PathProvider => Module as IPathProvider;

        /// <summary>
        /// Gets the module as an <see cref="IExternalInput"/>
        /// </summary>
        public IExternalInput ExternalInput => Module as IExternalInput;

        /// <summary>
        /// All slots of linked modules
        /// </summary>
        public List<CGModuleSlot> LinkedSlots
        {
            get
            {
                //todo design bug: Linked Slots being lazy loaded creates issues. See CurvyGenerator.ModuleSorter.EnsureLinkedSlotsBuild
                if (mLinkedSlots == null)
                    LoadLinkedSlots();
                return mLinkedSlots ?? new List<CGModuleSlot>();
            }
        }

        /// <summary>
        /// Gets the number of connected links, i.e. shortcut to this.Links.Count
        /// </summary>
        public int Count => LinkedSlots.Count;

        public string Name => Info != null
            ? Info.Name
            : "";

        protected List<CGModuleSlot> mLinkedSlots;

        /// <summary>
        /// Does this slot receive/provide data only on demand, as opposed to on refresh?
        /// </summary>
        /// <seealso cref="IOnRequestProcessing"/>
        public virtual bool IsOnRequest => OnRequestModule != null;

        public bool HasLinkTo(CGModuleSlot other)
        {
            for (int i = 0; i < LinkedSlots.Count; i++)
                if (LinkedSlots[i] == other)
                    return true;

            return false;
        }

        /// <summary>
        /// Can a link be established between this slot and the other link?
        /// </summary>
        public virtual bool CanLinkTo(
            [NotNull] CGModuleSlot other)
        {
            return other.Module != Module;
        }

        /// <summary>
        /// Gets a list of all Links' modules
        /// </summary>
        public List<CGModule> GetLinkedModules()
        {
            List<CGModule> res = new List<CGModule>();
            for (int i = 0; i < LinkedSlots.Count; i++)
                res.Add(LinkedSlots[i].Module);
            return res;
        }

        public virtual void LinkTo(CGModuleSlot other)
        {
            if (Module)
            {
                Module.Generator.sortModulesINTERNAL();
                Module.Dirty = true;
            }

            if (other.Module)
                other.Module.Dirty = true;
        }

        protected static void LinkInputAndOutput(CGModuleSlot inputSlot, CGModuleSlot outputSlot)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { inputSlot.Module, outputSlot.Module }, LinkModuleUndoName);
#endif

            if ((!inputSlot.Info.Array || inputSlot.Info.ArrayType == SlotInfo.SlotArrayType.Hidden) && inputSlot.IsLinked)
                inputSlot.UnlinkAll();

            outputSlot.Module.OutputLinks.Add(
                new CGModuleLink(
                    outputSlot,
                    inputSlot
                )
            );
            inputSlot.Module.InputLinks.Add(
                new CGModuleLink(
                    inputSlot,
                    outputSlot
                )
            );
            if (!outputSlot.LinkedSlots.Contains(inputSlot))
                outputSlot.LinkedSlots.Add(inputSlot);
            if (!inputSlot.LinkedSlots.Contains(outputSlot))
                inputSlot.LinkedSlots.Add(outputSlot);
        }

        /// <summary>
        /// Unlink the current slot from the specified other slot.
        /// </summary>
        /// <param name="other">The other CGModuleSlot to unlink from.</param>
        public virtual void UnlinkFrom([NotNull] CGModuleSlot other)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { Module, other.Module }, UnlinkModuleUndoName);
#endif
            LinkedSlots.Remove(other);
            other.LinkedSlots.Remove(this);

            if (Module)
            {
                Module.Generator.sortModulesINTERNAL();
                Module.Dirty = true;
            }

            if (other.Module)
                //todo why not other.Module.Generator.sortModulesINTERNAL();
                //if you end up adding other.Module.Generator.sortModulesINTERNAL, then you can refactor the current method into to calls to a method that removes from the list and dirties the module/sorting
                other.Module.Dirty = true;
        }

        /// <summary>
        /// Unlink this slot from all its linked slots
        /// </summary>
        public virtual void UnlinkAll()
        {
            List<CGModuleSlot> linkedSlots = new List<CGModuleSlot>(LinkedSlots);
            foreach (CGModuleSlot linkedSlot in linkedSlots)
                UnlinkFrom(linkedSlot);
        }

        public void ReInitializeLinkedSlots() =>
            mLinkedSlots = null;

        protected virtual void LoadLinkedSlots() { }

        public void SetInfoFromField(FieldInfo fieldInfo)
        {
            Info = fieldInfo.GetCustomAttribute<SlotInfo>();

            string fieldName = fieldInfo.Name;

            if (Info == null)
                Debug.LogError($"The Slot '{fieldName}' of type '{fieldInfo.DeclaringType?.Name}' needs a SlotInfo attribute!");
            else
            {
                if (string.IsNullOrEmpty(Info.Name))
                    Info.Name = fieldName.TrimStart("In").TrimStart("Out");

                Info.CheckDataTypes();
            }
        }

        public static implicit operator bool(CGModuleSlot a)
            => !ReferenceEquals(
                a,
                null
            );

        public override string ToString()
            => string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1}.{2}",
                GetType().Name,
                Module.name,
                Name
            );

        #region Editor

#if UNITY_EDITOR

        protected const string LinkModuleUndoName = "Link Module";
        protected const string UnlinkModuleUndoName = "Unlink Module";

        /// <summary>
        /// Gets the text displayed in the slot's label
        /// </summary>
        [NotNull]
        public virtual string GetLabelText() =>
            UnityEditor.ObjectNames.NicifyVariableName(Info.DisplayName);

#endif

        #endregion
    }
}