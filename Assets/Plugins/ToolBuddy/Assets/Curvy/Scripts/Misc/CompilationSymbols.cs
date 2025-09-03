// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// A class containing all preprocessor define symbols used by Curvy Splines
    /// </summary>
    public static class CompilationSymbols
    {

        /// <summary>
        /// This compiler symbol is defined when Curvy Splines is in the project
        /// </summary>
        public const string CurvySplines = "CURVY_SPLINES";

        /// <summary>
        /// Enables Sanity checks.
        /// Helpful when using Curvy's API, as those checks warn you about invalid usage of the API.
        /// These checks cost some additional CPU time.
        /// </summary>
        /// <seealso cref="CurvyExtraSanityChecks"/>
        public const string CurvySanityChecks = "CURVY_SANITY_CHECKS";

        /// <summary>
        /// Same as <see cref="CurvySanityChecks"/>, but enables checks that are costly CPU wise. It is rarely needed to activate these checks.
        /// </summary>
        public const string CurvyExtraSanityChecks = "CURVY_SANITY_CHECKS_PRIVATE";

        /// <summary>
        /// Shows and logs additional data in several places to ease debugging.
        /// </summary>
        public const string CurvyDebug = "CURVY_DEBUG";

        /// <summary>
        /// Makes the announcements fetcher show all announcements, even if they were already shown.
        /// </summary>
        public const string CurvyShowAllAnnouncements = "CURVY_SHOW_ALL_ANNOUNCEMENTS";

        /// <summary>
        /// Defined by documentation generator to ignore methods inherited from Unity Componenets, such as OnEnable.
        /// </summary>
        public const string DocumentationForceIgnorePrivate = "DOCUMENTATION___FORCE_IGNORE___UNITY";

        /// <summary>
        /// Defined by documentation generator to ignore Curvy's code not meant for documentation.
        /// </summary>
        public const string DocumentationForceIgnoreCurvy = "DOCUMENTATION___FORCE_IGNORE___CURVY";

        /// <summary>
        /// Unity's preprocessor define symbol used to run code in the editor only.
        /// </summary>
        public const string UnityEditor = "UNITY_EDITOR";

        /// <summary>
        /// Scripting symbol for Universal Windows Platform. Additionally, NETFX_CORE is defined when compiling C# files against .NET Core and using .NET scripting backend
        /// </summary>
        public const string UnityWSA = "UNITY_WSA";

        /// <summary>
        /// Scripting symbol for WebGL.
        /// </summary>
        public const string UnityWebGL = "UNITY_WEBGL";

        /// <summary>
        /// Scripting backend for IL2CPP.
        /// </summary>
        public const string EnableIL2Cpp = "ENABLE_IL2CPP";

        /// <summary>
        /// Defined when building scripts against .NET 4.x API compatibility level on Mono and IL2CPP.
        /// </summary>
        public const string Net46 = "NET_4_6";


        //Other symbols related to Unity versions, such as UNITY_2022_2_OR_NEWER
    }
}