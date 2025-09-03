// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor
{
    public partial class CurvyProject
    {
        /// <summary>
        /// The class used by Unity 2018.3 and newer to provide Curvy's preferences window
        /// </summary>
        public class CurvySettingsProvider : SettingsProvider
        {
            public CurvySettingsProvider(SettingsScope scopes, IEnumerable<string> keywords = null)
                : base(
                    GetPreferencesPath(),
                    scopes,
                    keywords
                ) { }

            public override void OnGUI(string searchContext) =>
                PreferencesGUI();

            /// <summary>
            /// The settings path for Curvy's Settings
            /// </summary>
            public static string GetPreferencesPath()
                => "Preferences/" + SettingsEntryName;
        }
    }
}