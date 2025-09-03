// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;

namespace FluffyUnderware.DevTools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DTVersionAttribute : Attribute
    {
        public readonly string Version;

        public DTVersionAttribute(string version)
        {
            Version = version;
        }
    }
}