// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    public abstract partial class CGModule
    {
        private class InformationProvider
        {
            [NotNull]
            private readonly CGModule module;

            private ModuleInfoAttribute moduleInformation;

            public InformationProvider([NotNull] CGModule module) =>
                this.module = module;

            [CanBeNull]
            public ModuleInfoAttribute Information
            {
                get
                {
                    if (moduleInformation == null)
                        moduleInformation = GetInformation();
                    return moduleInformation;
                }
            }

            [CanBeNull]
            private ModuleInfoAttribute GetInformation()
            {
                object[] inf = module.GetType().GetCustomAttributes(
                    typeof(ModuleInfoAttribute),
                    true
                );
                return inf.Length > 0
                    ? (ModuleInfoAttribute)inf[0]
                    : null;
            }
        }

        [UsedImplicitly]
        [Obsolete]
        [CanBeNull]
        public ModuleInfoAttribute Info => informationProvider.Information;

        [UsedImplicitly]
        [Obsolete]
        internal ModuleInfoAttribute getInfo()
        {
            object[] inf = GetType().GetCustomAttributes(
                typeof(ModuleInfoAttribute),
                true
            );
            return inf.Length > 0
                ? (ModuleInfoAttribute)inf[0]
                : null;
        }
    }
}