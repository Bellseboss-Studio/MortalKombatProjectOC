// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Globalization;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    public abstract partial class CGModule
    {
        [UsedImplicitly]
        [Obsolete]
        public int SetUniqueIdINTERNAL()
        {
            int id = 0;
            while (Generator.Modules.Exists(
                       m => ReferenceEquals(
                                m,
                                this
                            )
                            == false
                            && m.UniqueID == id
                   ))
                id++;
            identifier.ID = id;
            return identifier.ID;
        }

        private class Identifier
        {
            [NotNull]
            private readonly CGModule module;

            // In order to reduce per frame allocations, we cache the string version
            [CanBeNull]
            private string cachedStringID;

            public int ID
            {
                get => module.m_UniqueID;
                set
                {
                    module.m_UniqueID = value;
                    cachedStringID = null;
                }
            }

            [NotNull]
            public string StringID
            {
                get
                {
                    if (cachedStringID == null)
                        cachedStringID = ID.ToString(CultureInfo.InvariantCulture);
                    return cachedStringID;
                }
            }

            public Identifier([NotNull] CGModule module) =>
                this.module = module;

            public void Reset() =>
                cachedStringID = null;
        }
    }
}