// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common
{
    public class Singleton<T> where T : new()
    {
        private static T instance;

        [NotNull]
        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = new T();
                return instance;
            }
        }
    }
}