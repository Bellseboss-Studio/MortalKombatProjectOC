// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public static class CGResourceEditorHandler
    {
        private static readonly Dictionary<string, Type> Editors = new Dictionary<string, Type>();

        internal static CGResourceEditor GetEditor(string resourceName, Component resource)
        {
            if (Editors.Count == 0)
                loadEditors();
            if (Editors.ContainsKey(resourceName))
                return (CGResourceEditor)Activator.CreateInstance(
                    Editors[resourceName],
                    (object)resource
                );
            return null;
        }

        private static void loadEditors()
        {
            Editors.Clear();
            TypeCache.TypeCollection types = TypeCache.GetTypesWithAttribute<ResourceEditorAttribute>();
            foreach (Type T in types)
            {
                object[] at = T.GetCustomAttributes(
                    typeof(ResourceEditorAttribute),
                    true
                );
                Editors.Add(
                    ((ResourceEditorAttribute)at[0]).ResourceName,
                    T
                );
            }
        }
    }
}