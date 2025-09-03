// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Pools;
using UnityEditor;
using Object = UnityEngine.Object;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Class defining a module's input slot
    /// </summary>
    [Serializable]
    public class CGModuleInputSlot : CGModuleSlot
    {
        public InputSlotInfo InputInfo => Info as InputSlotInfo;

        /// <inheritdoc/>
        public override bool IsOnRequest => InputInfo.RequestDataOnly || base.IsOnRequest;

        protected override void LoadLinkedSlots()
        {
            if (!Module.Generator.IsInitialized)
                return;

            base.LoadLinkedSlots();
            mLinkedSlots = new List<CGModuleSlot>();
            List<CGModuleLink> inputLinks = Module.GetInputLinks(this);
            foreach (CGModuleLink inputLink in inputLinks)
            {
                CGModule module = Module.Generator.GetModule(
                    inputLink.TargetModuleID,
                    true
                );
                if (module)
                {
                    CGModuleOutputSlot slot = module.OutputByName[inputLink.TargetSlotName];
                    // Sanitize missing links
                    if (!slot.Module.GetOutputLink(
                            slot,
                            this
                        ))
                    {
                        slot.Module.OutputLinks.Add(
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
                    Module.InputLinks.Remove(inputLink);
            }
        }

        public override void LinkTo(
            CGModuleSlot outputSlot)
        {
            if (HasLinkTo(outputSlot))
                return;

            LinkInputAndOutput(
                this,
                outputSlot
            );
            base.LinkTo(outputSlot);
        }

        public override void UnlinkFrom(
            CGModuleSlot outputSlot)
        {
            if (!HasLinkTo(outputSlot))
                return;

#if UNITY_EDITOR
            Undo.RecordObjects(
                new Object[] { Module, outputSlot.Module },
                UnlinkModuleUndoName
            );
#endif

            CGModuleOutputSlot cgModuleOutputSlot = (CGModuleOutputSlot)outputSlot;
            CGModuleLink l1 = Module.GetInputLink(
                this,
                cgModuleOutputSlot
            );
            Module.InputLinks.Remove(l1);
            CGModuleLink l2 = outputSlot.Module.GetOutputLink(
                cgModuleOutputSlot,
                this
            );
            outputSlot.Module.OutputLinks.Remove(l2);

            base.UnlinkFrom(outputSlot);
        }


        /// <summary>
        /// Gets a linked Output slot
        /// </summary>
        public CGModuleOutputSlot SourceSlot(
            int index = 0)
            => index < Count && index >= 0
                ? (CGModuleOutputSlot)LinkedSlots[index]
                : null;

        /// <inheritdoc />
        public override bool CanLinkTo(
            CGModuleSlot other)
        {
            CGModuleOutputSlot otherOutput = other as CGModuleOutputSlot;

            if (otherOutput == false)
                return false;

            return base.CanLinkTo(other)
                   && CGModuleLink.AreSlotsCompatible(
                       InputInfo,
                       IsOnRequest,
                       otherOutput.OutputInfo,
                       otherOutput.IsOnRequest
                   );
        }


        [Obsolete("Use CanLinkTo or CGModuleLink.AreSlotsCompatible instead")]
        public static bool AreInputAndOutputSlotsCompatible(
            InputSlotInfo inputSlotInfo,
            bool inputSlotModuleIsOnRequest,
            OutputSlotInfo outputSlotInfo,
            bool outputSlotModuleIsOnRequest)
        {
            bool isDataTypeValid = inputSlotInfo.IsValidFrom(outputSlotInfo.DataType);
            return isDataTypeValid
                   && ((outputSlotModuleIsOnRequest && (inputSlotInfo.RequestDataOnly || inputSlotModuleIsOnRequest))
                       || (outputSlotModuleIsOnRequest == false && !inputSlotInfo.RequestDataOnly));
        }


        /// <summary>
        /// Gets the data from the module connected to a certain input slot. If more than one module is connected, the first module's data is returned
        /// </summary>
        /// <typeparam name="T">type of requested data</typeparam>
        /// <param name="requests">request parameters</param>
        /// <returns>the data</returns>
        [CanBeNull]
        public T GetData<T>(
            params CGDataRequestParameter[] requests) where T : CGData
            => GetData<T>(
                out _,
                requests
            );

        /// <summary>
        /// Gets the data from the module connected to a certain input slot. If more than one module is connected, the first module's data is returned
        /// </summary>
        /// <typeparam name="T">type of requested data</typeparam>
        /// <param name="isDataDisposable">Whether the returned data can be disposed safely after using it, in order to make its resources available for future use, and thus reducing garbage collection.
        /// It is set to false when returned data is a direct reference to data stored by the module, and not a copy of it
        /// <seealso cref="CGData.Dispose(bool)"/>
        /// <seealso cref="ArrayPool{T}"/>
        /// </param>
        /// <param name="requests">request parameters</param>
        /// <returns>the data</returns>
        [CanBeNull]
        //todo design bug: make sure all consumers of this method handle properly that it can return null
        public T GetData<T>(
            out bool isDataDisposable,
            params CGDataRequestParameter[] requests) where T : CGData
        {
            CGData[] data = GetData<T>(
                0,
                out isDataDisposable,
                requests
            );
#if UNITY_EDITOR
            LastDataCount = data.Length == 0
                ? 0
                : data.Length;
#endif
            if (data.Length == 0 || data[0] == null)
            {
                isDataDisposable = false;
                return null;
            }

            return data[0] as T;
        }

        /// <summary>
        /// Gets the data from all modules connected to a certain input slot.
        /// </summary>
        /// <typeparam name="T">type of requested data</typeparam>
        /// <param name="requests">request parameters</param>
        /// <returns>the data</returns>
        [NotNull]
        public List<T> GetAllData<T>(
            params CGDataRequestParameter[] requests) where T : CGData
            => GetAllData<T>(
                out _,
                requests
            );

        /// <summary>
        /// Gets the data from all modules connected to a certain input slot.
        /// </summary>
        /// <typeparam name="T">type of requested data</typeparam>
        /// <param name="isDataDisposable">Whether the returned data can be disposed safely after using it, in order to make its resources available for future use, and thus reducing garbage collection.
        /// It is set to false when returned data is a direct reference to data stored by the module, and not a copy of it
        /// <seealso cref="CGData.Dispose(bool)"/>
        /// <seealso cref="ArrayPool{U}"/>
        /// </param>
        /// <param name="requests">request parameters</param>
        /// <returns>the data</returns>
        [NotNull]
        public List<T> GetAllData<T>(
            out bool isDataDisposable,
            params CGDataRequestParameter[] requests) where T : CGData
        {
            isDataDisposable = true;
            List<T> res = new List<T>();
            for (int i = 0; i < Count; i++)
            {
                CGData[] data = GetData<T>(
                    i,
                    out bool isDisposable,
                    requests
                );
                //If in the returned there are multiple CGData, some needing to be disposed, others not, then we consider that none should be disposed
                //This means that some data might not be flagged as disposable, which means it will be dispose once Finalize is called. Missed opportunity to instantly reuse its data. Not good for reducing garbage collection
                isDataDisposable &= isDisposable;
#if CURVY_SANITY_CHECKS
                //TODO right now there are no graph that I am aware of that can trigger the situation explained above, but in the future, if there are modules that take multiple paths as inputs, one of them can be from Input Path module (isDisposable true) and one from Shape Extrusion (output Volume, which is also a path, isDisposable false)
                if (isDisposable && isDataDisposable == false)
                    DTLog.LogWarning("[Curvy] A disposable data was treated as not disposable");
#endif
                if (!Info.Array)
                {
                    res.Add(data[0] as T);
                    break;
                }

                for (int a = 0; a < data.Length; a++)
                    res.Add(data[a] as T);
            }

#if UNITY_EDITOR
            LastDataCount = res.Count;
#endif
            return res;
        }

        /// <summary>
        /// Gets the data from the module connected to a certain input slot
        /// </summary>
        /// <typeparam name="T">type of requested data</typeparam>
        /// <param name="slotIndex">slot index (if the slot supports multiple inputs)</param>
        /// <param name="isDataDisposable">Whether the returned data can be disposed safely after using it, in order to make its resources available for future use, and thus reducing garbage collection.
        /// It is set to false when returned data is a direct reference to data stored by the module, and not a copy of it
        /// <seealso cref="CGData.Dispose(bool)"/>
        /// <seealso cref="ArrayPool{U}"/>
        /// </param>
        /// <param name="requests">request parameters</param>
        /// <returns>the data</returns>
        [NotNull]
        private CGData[] GetData<T>(
            int slotIndex,
            out bool isDataDisposable,
            params CGDataRequestParameter[] requests
        ) where T : CGData
        {
            CGModuleOutputSlot source = SourceSlot(slotIndex);
            if (source == null || !source.Module.Active)
            {
                isDataDisposable = true;
                return new CGData[0];
            }

            // Handles IOnRequestProcessing modules (i.e. modules that provides data on the fly)
            if (source.Module is IOnRequestProcessing)
            {
                bool needNewData = source.Data.Length == 0;
                // Return last data?
                if (!needNewData
                    && source.LastRequestParameters != null
                    && source.LastRequestParameters.Length == requests.Length)
                {
                    for (int i = 0; i < requests.Length; i++)
                        if (!requests[i].Equals(source.LastRequestParameters[i]))
                        {
                            needNewData = true;
                            break;
                        }
                }
                else
                    needNewData = true;

                if (needNewData)
                {
                    source.LastRequestParameters = requests;
#if UNITY_EDITOR || CURVY_DEBUG
                    source.Module.DEBUG_LastUpdateTime = DateTime.Now;
                    Module.DEBUG_ExecutionTime.Pause();
                    source.Module.DEBUG_ExecutionTime.Start();
#endif
                    //TODO Find a way to move this line of code inside OnSlotDataRequest
                    source.Module.UIMessages.Clear();

                    CGData[] slotData = ((IOnRequestProcessing)source.Module).OnSlotDataRequest(
                        this,
                        source,
                        requests
                    );

                    //todo remove these checks in version 9, once users should have made sure OnSlotDataRequest always returns valid data
                    if (slotData == null)
                    {
#if CURVY_SANITY_CHECKS
                        DTLog.LogWarning(
                            $"[Curvy] {source.Module.name}'s output data is invalid. Data is null. Modify the module's IOnRequestProcessing.OnSlotDataRequest's implementation to always return arrays that are not null and contain no null element."
                        );
#endif
                        source.ClearData();
                    }
                    else if (slotData.Length == 0)
                        source.ClearData();
                    else if (slotData.All(d => d == null))
                    {
                        DTLog.LogWarning(
                            $"[Curvy] {source.Module.name}'s output data is invalid. All data elements are null. Modify the module's IOnRequestProcessing.OnSlotDataRequest's implementation to always return arrays that are not null and contain no null element."
                        );

                        source.ClearData();
                    }
                    else if (slotData.Contains(null))
                    {
                        DTLog.LogWarning(
                            $"[Curvy] {source.Module.name}'s output data is invalid. Some data elements are null. Modify the module's IOnRequestProcessing.OnSlotDataRequest's implementation to always return arrays that are not null and contain no null element."
                        );

                        source.SetDataToCollection(slotData.Where(d => d != null).ToArray());
                    }
                    else
                        source.SetDataToCollection(slotData);


#if UNITY_EDITOR || CURVY_DEBUG
                    source.Module.DEBUG_ExecutionTime.Stop();
                    Module.DEBUG_ExecutionTime.Start();
#endif
                }
            }

            bool copyData = InputInfo.ModifiesData || source.Module is IOnRequestProcessing;

            CGData[] result = copyData
                ? CloneData<T>(source.Data)
                : source.Data;

            isDataDisposable = copyData;

            return result;

            ;
        }

        [NotNull]
        private static CGData[] CloneData<T>(
            [NotNull] CGData[] source) where T : CGData
        {
            T[] d = new T[source.Length];
            for (int i = 0; i < source.Length; i++)
                d[i] = source[i] == null
                    ? null
                    : source[i].Clone<T>();
            return d;
        }


#if UNITY_EDITOR

        #region Editor

        private int LastDataCount { get; set; }

        public void ResetLastDataCount() => LastDataCount = 0;


        /// <inheritdoc />
        public override string GetLabelText()
        {
            string postfix;
            {
                if (Info.Array && Info.ArrayType == SlotInfo.SlotArrayType.Normal)
                    postfix = LastDataCount > 0
                        ? $"[{LastDataCount}]"
                        : "[]";
                else
                    postfix = string.Empty;
            }

            return $"{base.GetLabelText()}{postfix}";
        }

        #endregion

#endif
    }
}