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
    /// <summary>
    /// Helper class used by various Curvy Generator modules
    /// </summary>
    [Serializable]
    //Design: get rid of CGMaterialSettingsEx, use CGMaterialSettings instead
    public class CGMaterialSettingsEx : CGMaterialSettings
    {
        [UsedImplicitly]
        [Obsolete("This field is not used anymore, will get remove in a future update")]
        public int MaterialID;
    }
}