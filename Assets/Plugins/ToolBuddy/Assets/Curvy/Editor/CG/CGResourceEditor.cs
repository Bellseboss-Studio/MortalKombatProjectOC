// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class CGResourceEditor
    {
        protected Component Resource { get; private set; }

        public CGResourceEditor() { }

        public CGResourceEditor(Component resource) =>
            Resource = resource;

        /// <summary>
        /// Resource GUI
        /// </summary>
        /// <returns>true if changes were made</returns>
        public virtual bool OnGUI()
            => false;

        public static implicit operator bool(CGResourceEditor a)
            => !ReferenceEquals(
                a,
                null
            );
    }
}