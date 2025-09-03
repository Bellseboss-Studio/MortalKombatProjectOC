// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        /// <summary>
        /// The module which title is being edited. Can be null if no title editing is in progress
        /// </summary>
        [CanBeNull]
        private CGModule titleEditingModule;

        private void StartTitleEditing(
            [NotNull] CGModule module)
        {
            titleEditingModule = module;
        }

        private void ExitTitleEditing()
        {
            titleEditingModule = null;
        }

        private bool ShouldExitTitleEditing(
            Event @event)
        {
            if (titleEditingModule == null)
                return false;

            bool isExitKeyEvent = (@event.isKey && (@event.keyCode == KeyCode.Escape || @event.keyCode == KeyCode.Return));
            bool isModuleChanged = CanvasSelection.SelectedModule != titleEditingModule;
            return isExitKeyEvent || isModuleChanged;
        }
    }
}