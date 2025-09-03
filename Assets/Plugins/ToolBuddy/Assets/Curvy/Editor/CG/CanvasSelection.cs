// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class CanvasSelection
    {
        [NotNull]
        public readonly List<CGModule> SelectedModules = new List<CGModule>();

        public CGModuleLink SelectedLink { get; private set; }

        [CanBeNull]
        public CGModule SelectedModule => SelectedModules.Count > 0
            ? SelectedModules[0]
            : null;

        /// <summary>
        /// Returns a new array with the <see cref="SelectedLink"/> if any, otherwise <see cref="SelectedModules"/>
        /// </summary>
        public object[] SelectedObjects
        {
            get
            {
                if (SelectedLink != null)
                    return new object[1] { SelectedLink };

                return SelectedModules.ToArray();
            }
        }

        /// <summary>
        /// Empties list and adds into it the <see cref="SelectedLink"/> if any, otherwise <see cref="SelectedModules"/>
        /// </summary>
        private void FillWithSelectedObjects(
            List<object> list)
        {
            list.Clear();
            if (SelectedLink != null)
                list.Add(SelectedLink);
            else
                list.AddRange(SelectedModules);
        }

        /// <summary>
        /// Resets the selection state and synchronizes the selection with the hierarchy selection if <see cref="CurvyProject.CGSynchronizeSelection"/> is true
        /// </summary>
        public void Clear()
        {
            Reset();
            if (CurvyProject.Instance.CGSynchronizeSelection)
                DTSelection.Clear();
        }

        /// <summary>
        /// Resets the selection state
        /// </summary>
        public void Reset()
        {
            SelectedLink = null;
            SelectedModules.Clear();
        }

        private void SetSelectionTo(
            [NotNull] CGModuleLink link)
        {
            Clear();
            SelectedLink = link;
        }

        public void SetSelectionTo(
            [NotNull] CGModule module) =>
            SetSelectionTo(new[] { module });

        public void SetSelectionTo(
            [NotNull] IEnumerable<CGModule> modules)
        {
            bool modulesSelectionChanged = modules.SequenceEqual(SelectedModules) == false;

            Clear();

            SelectedModules.AddRange(modules);

            if (modulesSelectionChanged && CurvyProject.Instance.CGSynchronizeSelection)
                DTSelection.SetGameObjects(modules.Select(m => m as Component).ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mouseButton">
        ///     0 = left
        ///     1 = right
        ///     2 = middle
        /// </param>
        /// <param name="hoveredLink"></param>
        /// <param name="hoveredModule"></param>
        public bool OnMouseDown(
            int mouseButton,
            [CanBeNull] CGModuleLink hoveredLink,
            [CanBeNull] CGModule hoveredModule,
            bool isControlModifier,
            bool isShiftModifier)
        {
            bool hasSelectionChanged = false;

            //link selection
            if (mouseButton == 0
                && hoveredLink != null
                && SelectedLink != hoveredLink)
            {
                SetSelectionTo(hoveredLink);
                hasSelectionChanged = true;
            }

            //module selection
            if ((mouseButton == 0 || mouseButton == 1)
                && hoveredModule != null)
            {
                if (isControlModifier)
                {
                    List<CGModule> newSelection = new List<CGModule>(SelectedModules);
                    if (SelectedModules.Contains(hoveredModule))
                        newSelection.Remove(hoveredModule);
                    else if (newSelection.Contains(hoveredModule) == false)
                            newSelection.Add(hoveredModule);

                    SetSelectionTo(newSelection);
                    hasSelectionChanged = true;
                }
                else if (isShiftModifier)
                {
                    List<CGModule> newSelection = new List<CGModule>(SelectedModules);
                    if (newSelection.Contains(hoveredModule) == false)
                        newSelection.Add(hoveredModule);
                    SetSelectionTo(newSelection);
                    hasSelectionChanged = true;
                }
                else
                {
                    if (SelectedModules.Contains(hoveredModule) == false)
                    {
                        SetSelectionTo(hoveredModule);
                        hasSelectionChanged = true;
                    }
                }
            }

            return hasSelectionChanged;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mouseButton">
        ///     0 = left
        ///     1 = right
        ///     2 = middle
        /// </param>
        /// <param name="hoveredLink"></param>
        /// <param name="hoveredModule"></param>
        public bool OnMouseUp(
            int mouseButton,
            [CanBeNull] CGModuleLink hoveredLink,
            [CanBeNull] CGModule hoveredModule)
        {
            bool hasSelectionChanged = false;
            //clear selection
            if (mouseButton == 0)
            {
                bool shouldClearSelectedLink = SelectedLink && hoveredLink == null;
                bool shouldClearSelectedModules = SelectedModule && hoveredModule == null;
                if (shouldClearSelectedLink
                    || shouldClearSelectedModules)
                {
                    Clear();
                    hasSelectionChanged = true;
                }
            }

            return hasSelectionChanged;
        }

        public void OnSelectionRectangle(
            [NotNull] [ItemNotNull] List<CGModule> modulesInRectangle,
            bool isControlModifier,
            bool isShiftModifier)
        {
            List<CGModule> oldSelection = SelectedModules;
            List<CGModule> newSelection;

            if (isControlModifier)
            {
                newSelection = new List<CGModule>(oldSelection);
                // If control is pressed, toggle selection for modules in rectangle
                foreach (CGModule module in modulesInRectangle)
                    if (oldSelection.Contains(module))
                        newSelection.Remove(module);
                    else if (newSelection.Contains(module) == false)

                        newSelection.Add(module);
            }
            else if (isShiftModifier)
            {
                // If shift is pressed, add modules in rectangle to selection
                newSelection = new List<CGModule>(oldSelection);
                foreach (CGModule module in modulesInRectangle)
                    if (newSelection.Contains(module) == false)
                        newSelection.Add(module);
            }
            else
                // If no modifier is pressed, replace selection with modules in rectangle
                newSelection = modulesInRectangle;

            SetSelectionTo(
                newSelection
            );
        }
    }
}