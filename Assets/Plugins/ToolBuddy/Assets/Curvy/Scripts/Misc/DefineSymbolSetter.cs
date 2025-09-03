// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build;
#endif

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Makes sure the define symbols are updated to include the ones from Curvy Splines.
    /// </summary>
    [InitializeOnLoad]
    internal class DefineSymbolSetter
    {
        static DefineSymbolSetter()
        {
            string[] curvySplineDefineSymbols =
            {
                CompilationSymbols.CurvySplines,
                $"{CompilationSymbols.CurvySplines}_{AssetInformation.Version.Replace('.', '_')}"
            };

#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget target = 
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#else
            BuildTargetGroup target = 
                EditorUserBuildSettings.selectedBuildTargetGroup;
#endif

            IEnumerable<string> existingSymbols =
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.GetScriptingDefineSymbols(target)
#else
                PlayerSettings.GetScriptingDefineSymbolsForGroup(target)
#endif
                    .Split(';')
                    .Select(s => s.Trim())
                    .ToList();

            IEnumerable<string> symbolsExceptCurvy =
                existingSymbols.Where(s => s.StartsWith(CompilationSymbols.CurvySplines) == false);

            IEnumerable<string> symbolsIncludingCurvy = symbolsExceptCurvy.Concat(curvySplineDefineSymbols).ToList();

            bool areSymbolsDifferent = new HashSet<string>(existingSymbols).SetEquals(symbolsIncludingCurvy) == false;

            if (areSymbolsDifferent)
            {
#if UNITY_6000_0_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
#endif
                    target,
                    string.Join(
                        ";",
                        symbolsIncludingCurvy
                    )
                );

                // Log the symbol modifications
                string buildTargetName =
#if UNITY_6000_0_OR_NEWER
                    target.TargetName;
#else
                    target.ToString();
#endif
                foreach (string symbol in existingSymbols.Except(symbolsIncludingCurvy))
                    DTLog.Log($"[Curvy] Define symbol {symbol} removed from the {buildTargetName} build target.");

                foreach (string symbol in symbolsIncludingCurvy.Except(existingSymbols))
                    DTLog.Log($"[Curvy] Define symbol {symbol} added to the {buildTargetName} build target.");
            }
        }
    }
}

#endif