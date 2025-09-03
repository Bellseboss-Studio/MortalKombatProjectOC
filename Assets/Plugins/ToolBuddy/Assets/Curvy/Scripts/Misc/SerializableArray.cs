// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Utils
{
    /// <summary>
    /// A workaround to the Unity Json's class not being able to serialize top level arrays.
    /// Including such arrays in another object avoids the issue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SerializableArray<T>
    {
        public T[] Array;
    }
}