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
    [CustomEditor(typeof(Note))]
    public class NoteEditor : CGModuleEditor<Note>
    {
        /*
        // Skip Label
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.UpdateIfDirtyOrScript();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Text"), new GUIContent(""));
            serializedObject.ApplyModifiedProperties();
                

        }
         */
    }
}