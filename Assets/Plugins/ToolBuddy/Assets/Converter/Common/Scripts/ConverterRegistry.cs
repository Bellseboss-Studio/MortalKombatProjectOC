// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;

namespace ToolBuddy.Converter.Common
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class ConverterRegistry
    {
        private static readonly List<string> registeredConverters = new List<string>();

        public static List<string> RegisteredConverters() =>
            registeredConverters;

        public static void RegisterConverter(
            string converterId)
        {
            if (!registeredConverters.Contains(converterId))
                registeredConverters.Add(converterId);
        }
    }
}