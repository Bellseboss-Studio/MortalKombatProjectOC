// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.DevToolsEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : DTEditor<PoolManager>
    {

        protected override void OnReadNodes()
        {
            Node.AddSection("Type Pools", typePoolsGUI);
        }


        private void typePoolsGUI(DTInspectorNode node)
        {

#pragma warning disable CS0618
            foreach (KeyValuePair<Type, IPool> kv in Target.TypePools)
#pragma warning restore CS0618
            {
                showBar(kv.Value);
            }
        }


        public static void showBar(IPool pool)
        {
            Rect r = EditorGUILayout.GetControlRect(false, GUILayout.Height(20));
            float hi = Mathf.Max(pool.Count, pool.Settings.MaximumCount);
            Color c = Color.green;
            if (pool.Count < pool.Settings.MinimumCount)
                c = Color.yellow;
            else if (pool.Count > pool.Settings.MaximumCount)
                c = Color.red;
            DTGUI.PushContentColor(c);
            string s = pool.Identifier;
            if (!string.IsNullOrEmpty(s))
            {
                int i = s.IndexOf(",", StringComparison.Ordinal);
                if (i > 0)
                    s = pool.Identifier.Substring(0, i);
            }
            EditorGUI.ProgressBar(r, pool.Count / hi, s + ":" + pool.Count.ToString());
            DTGUI.PopContentColor();
        }
    }
}
