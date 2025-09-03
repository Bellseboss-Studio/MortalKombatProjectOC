// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.DevToolsEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PrefabPool), true)]
    public class PrefabPoolEditor : DTEditor<PrefabPool>
    {
        protected override void OnCustomInspectorGUIBefore()
        {
            PoolManagerEditor.showBar(Target);
        }


    }
}
