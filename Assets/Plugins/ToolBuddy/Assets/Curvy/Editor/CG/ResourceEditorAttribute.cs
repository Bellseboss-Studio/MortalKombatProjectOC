// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.CurvyEditor.Generator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceEditorAttribute : Attribute
    {
        public readonly string ResourceName;

        public ResourceEditorAttribute(string resName) =>
            ResourceName = resName;
    }
}