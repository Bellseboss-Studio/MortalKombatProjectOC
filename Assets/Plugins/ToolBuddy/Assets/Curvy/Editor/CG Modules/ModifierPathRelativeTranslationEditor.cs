// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator.Modules;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ModifierPathRelativeTranslation))]
    public class ModifierPathRelativeTranslationEditor : CGModuleEditor<ModifierPathRelativeTranslation>
    {
        // Scene View GUI - Called only if the module is initialized and configured
        //public override void OnModuleSceneGUI() {}

        // Scene View Debug GUI - Called only when Show Debug Visuals is activated
        //public override void OnModuleSceneDebugGUI() {}

        // Inspector Debug GUI - Called only when Show Debug Values is activated
        //public override void OnModuleDebugGUI() {}
    }
}