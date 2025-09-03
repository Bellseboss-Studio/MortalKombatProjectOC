// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Generator;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// Represents a clipboard for Curvy Generator modules, providing cut, copy and paste functionality.
    /// </summary>
    public class CGClipboard
    {
        /// <summary>
        /// Specifies the clipboard operation mode.
        /// </summary>
        public enum ClipboardMode
        {
            /// <summary>
            /// Cut mode: removes the modules from the source generator after pasting.
            /// </summary>
            Cut,

            /// <summary>
            /// Copy mode: retains the modules in the source generator after pasting.
            /// </summary>
            Copy
        }

        public ClipboardMode Mode = ClipboardMode.Copy;

        [NotNull]
        public readonly List<CGModule> Modules = new List<CGModule>();

        public bool Empty
            => Modules.Count == 0;

        [CanBeNull]
        [UsedImplicitly]
        [Obsolete]
        public CurvyGenerator ParentGenerator
            => Modules.Count > 0
                ? Modules[0].Generator
                : null;

        /// <summary>
        /// Cuts the specified modules, placing them into the clipboard.
        /// </summary>
        /// <param name="modules">The modules to cut.</param>
        public void CutModules([NotNull] [ItemNotNull] IList<CGModule> modules)
        {
            Mode = ClipboardMode.Cut;
            CopyInternal(modules);
        }

        /// <summary>
        /// Copies the specified modules, placing them into the clipboard.
        /// </summary>
        /// <param name="modules">The modules to copy.</param>
        public void CopyModules([NotNull] [ItemNotNull] IList<CGModule> modules)
        {
            Mode = ClipboardMode.Copy;
            CopyInternal(modules);
        }

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        public void Clear()
            => Modules.Clear();

        /// <summary>
        /// Paste all Clipboard modules to the specified target generator.
        /// </summary>
        /// <param name="target">The generator to paste the modules to.</param>
        /// <param name="positionOffset">Canvas offset to use.</param>
        /// <returns>A list of the new modules created.</returns>
        public List<CGModule> PasteModules([NotNull] CurvyGenerator target, Vector2 positionOffset)
        {
            List<CGModule> newModules = CGEditorUtility.CopyModules(
                Modules,
                target,
                positionOffset
            );

            if (Mode == ClipboardMode.Cut)
            {
                foreach (CGModule module in Modules)
                    module.Generator.DeleteModule(module);

                Clear();
            }

            return newModules;
        }

        private void CopyInternal([NotNull] [ItemNotNull] IList<CGModule> modules)
        {
            Modules.Clear();
            Modules.AddRange(modules);
        }
    }
}