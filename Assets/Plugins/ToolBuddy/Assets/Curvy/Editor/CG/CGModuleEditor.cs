// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class CGModuleEditor<T> : CGModuleEditorBase where T : CGModule
    {
        public new T Target => target as T;
    }
}