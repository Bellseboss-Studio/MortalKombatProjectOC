// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Class defining a module's output slot
    /// </summary>
    [Serializable]
    //todo design: why is this class not generic, with T being the type of the slot's data (CGData)?
    public class CGModuleOutputSlot : CGModuleSlot
    {
        [NotNull]
        [ItemNotNull]
        private CGData[] data = Array.Empty<CGData>();

        /// <summary>
        /// The output data. Can contain either:
        /// - no element if not set yet or cleared using <see cref="ClearData"/>.
        /// - one element if set through <see cref="SetDataToElement{T}"/>.
        /// - zero, one or multiple elements if set through <see cref="SetDataToCollection{T}"/>.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public CGData[] Data
        {
            get => data;
            [UsedImplicitly]
            [Obsolete("Use ClearData, SetDataToElement or SetDataToCollection instead.")]
            set => data = value;
        }

        /// <summary>
        /// Information about the output slot
        /// </summary>
        [CanBeNull]
        public OutputSlotInfo OutputInfo => Info as OutputSlotInfo;

        /// <summary>
        /// The request parameters used the last time the <see cref="IOnRequestProcessing"/> module returned data.
        /// </summary>
        [CanBeNull]
        public CGDataRequestParameter[] LastRequestParameters; // used for caching of Virtual Modules

        #region Links

        protected override void LoadLinkedSlots()
        {
            if (!Module.Generator.IsInitialized)
                return;

            base.LoadLinkedSlots();
            mLinkedSlots = new List<CGModuleSlot>();
            List<CGModuleLink> outputLinks = Module.GetOutputLinks(this);
            foreach (CGModuleLink outputLink in outputLinks)
            {
                CGModule module = Module.Generator.GetModule(
                    outputLink.TargetModuleID,
                    true
                );
                if (module)
                {
                    CGModuleInputSlot slot = module.InputByName[outputLink.TargetSlotName];

                    // Sanitize missing links
                    if (!slot.Module.GetInputLink(
                            slot,
                            this
                        ))
                    {
                        slot.Module.InputLinks.Add(
                            new CGModuleLink(
                                slot,
                                this
                            )
                        );
                        slot.ReInitializeLinkedSlots();
                    }

                    if (!mLinkedSlots.Contains(slot))
                        mLinkedSlots.Add(slot);
                }
                else
                    Module.OutputLinks.Remove(outputLink);
            }
        }

        public override void LinkTo(
            CGModuleSlot inputSlot)
        {
            if (HasLinkTo(inputSlot))
                return;

            LinkInputAndOutput(
                inputSlot,
                this
            );
            base.LinkTo(inputSlot);
        }

        public override void UnlinkFrom(
            CGModuleSlot inputSlot)
        {
            if (!HasLinkTo(inputSlot))
                return;

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObjects(
                new UnityEngine.Object[] { Module, inputSlot.Module },
                UnlinkModuleUndoName
            );
#endif

            CGModuleInputSlot cgModuleInputSlot = (CGModuleInputSlot)inputSlot;
            CGModuleLink l1 = Module.GetOutputLink(
                this,
                cgModuleInputSlot
            );
            Module.OutputLinks.Remove(l1);

            CGModuleLink l2 = inputSlot.Module.GetInputLink(
                cgModuleInputSlot,
                this
            );
            inputSlot.Module.InputLinks.Remove(l2);

            base.UnlinkFrom(inputSlot);
        }

        /// <inheritdoc />
        public override bool CanLinkTo(
            CGModuleSlot other)
        {
            CGModuleInputSlot otherInput = other as CGModuleInputSlot;

            if (otherInput == false)
                return false;

            return base.CanLinkTo(other)
                   && CGModuleLink.AreSlotsCompatible(
                       otherInput.InputInfo,
                       otherInput.IsOnRequest,
                       this.OutputInfo,
                       this.IsOnRequest
                   );
        }

        #endregion


        #region Data

        /// <summary>
        /// Sets <see cref="Data"/> to an empty array.
        /// </summary>
        public void ClearData() =>
            AssignNewData(Array.Empty<CGData>());

        /// <summary>
        /// Set <see cref="Data"/> to a single element.
        /// </summary>
        /// <typeparam name="T">A type inheriting from <see cref="CGData"/></typeparam>
        /// <param name="element">A non null element</param>
        /// <exception cref="ArgumentNullException">If element is null</exception>
        public void SetDataToElement<T>(
            [NotNull] T element) where T : CGData
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            AssignNewData(new CGData[] { element });
        }

        /// <summary>
        /// Set <see cref="Data"/> to a collection of elements.
        /// </summary>
        /// <typeparam name="T">A type inheriting from <see cref="CGData"/></typeparam>
        /// <param name="elements">A non null collection that contains non null elements</param>
        /// <exception cref="ArgumentNullException">If elements is null, or has elements that are null</exception>
        public void SetDataToCollection<T>(
            [NotNull] [ItemNotNull] T[] elements) where T : CGData
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            AssignNewData(elements);
        }

        #region Obsolete

        [UsedImplicitly]
        [Obsolete("Use Data instead")]
        public bool HasData => Data.Length > 0;

        [UsedImplicitly]
        [Obsolete("Use SetDataToElement or SetDataToCollection instead.")]
        public void SetData<T>(
            [CanBeNull] [ItemNotNull] List<T> newData) where T : CGData
        {
            if (newData == null)
                newData = new List<T>();

            if (newData.Contains(null))
                newData = newData.Where(d => d != null).ToList();

            SetDataToCollection(newData.ToArray());
        }

        [UsedImplicitly]
        [Obsolete("Use SetDataToElement or SetDataToCollection instead.")]
        public void SetData(
            [CanBeNull] params CGData[] newData)
        {
            if (newData == null)
                newData = Array.Empty<CGData>();

            if (newData.Contains(null))
                newData = newData.Where(d => d != null).ToArray();

            SetDataToCollection(newData);
        }

        [CanBeNull]
        [UsedImplicitly]
        [Obsolete("Use Data instead")]
        public T GetData<T>() where T : CGData
            => Data.Length == 0
                ? null
                : Data[0] as T;


        [CanBeNull]
        [UsedImplicitly]
        [Obsolete("Use Data instead")]
        public T[] GetAllData<T>() where T : CGData
        //todo design: avoid Data not being T[]
            => Data as T[];

        #endregion

        private void AssignNewData(
            [NotNull] [ItemNotNull] CGData[] newData)
        {
            if (newData == null)
                throw new ArgumentNullException(nameof(newData));

            for (int i = 0; i < newData.Length; i++)
                if (newData[i] == null)
                    throw new ArgumentNullException(
                        nameof(newData),
                        "Data array contains null elements"
                    );

            if (!Info.Array && newData.Length > 1)
                Debug.LogWarning(
                    "[Curvy] "
                    + Module.GetType().Name
                    + " ("
                    + Info.DisplayName
                    + ") only supports a single data item! Either avoid calculating unnecessary data or define the slot as an array, by setting its Info.Array to true"
                );

            if (Data == newData)
                return;

            foreach (CGData cgData in Data)
                if (newData.Contains(cgData) == false)
                    cgData.Dispose();
#pragma warning disable 618
            Data = newData;
#pragma warning restore 618
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        /// <inheritdoc />
        public override string GetLabelText()
        {
            string postfix;
            {
                if (Info.Array && Info.ArrayType == SlotInfo.SlotArrayType.Normal)
                    postfix = Data.Length > 1
                        ? $"[{Data.Length}]"
                        : "";
                else
                    postfix = string.Empty;
            }

            return $"{base.GetLabelText()}{postfix}";
        }

#endif

        #endregion
    }
}