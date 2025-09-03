// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InputMesh))]
    public class InputMeshEditor : CGModuleEditor<InputMesh>
    {
        private int selectedIndex;

        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );
            node.ArrayEx.drawElementCallback = OnMeshGUI;
            node.ArrayEx.onSelectCallback = l => { selectedIndex = l.index; };
            node.ArrayEx.onAddCallback = l =>
            {
                Target.Meshes.Insert(
                    Mathf.Clamp(
                        l.index + 1,
                        0,
                        Target.Meshes.Count
                    ),
                    new CGMeshProperties()
                );
                EditorUtility.SetDirty(Target);
                Target.Dirty = true;
            };
        }


        private void OnMeshGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "m_Meshes.Array.data[{0}]",
                    index
                )
            );
            if (prop != null)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += 1;
                rect.width -= 100;
                SerializedProperty mshProp = prop.FindPropertyRelative("m_Mesh");
                mshProp.objectReferenceValue = EditorGUI.ObjectField(
                    rect,
                    mshProp.objectReferenceValue,
                    typeof(Mesh),
                    false
                );

                rect.x += rect.width;
                EditorGUI.LabelField(
                    rect,
                    getFormattedMeshInfo(mshProp.objectReferenceValue as Mesh),
                    DTStyles.HtmlLabel
                );
            }
        }

        private void OnPropertiesGUI()
        {
            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "m_Meshes.Array.data[{0}]",
                    selectedIndex
                )
            );
            if (prop != null)
            {
                SerializedProperty matProp = prop.FindPropertyRelative("m_Material");
                if (matProp != null)
                {
                    ReorderableList l = new ReorderableList(
                        serializedObject,
                        matProp,
                        true,
                        true,
                        false,
                        false
                    );
                    l.drawHeaderCallback = rect =>
                    {
                        GUI.Label(
                            rect,
                            "Materials for " + Target.Meshes[selectedIndex].Mesh.name
                        );
                    };
                    l.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        rect.height = EditorGUIUtility.singleLineHeight;
                        SerializedProperty pMat = prop.FindPropertyRelative(
                            string.Format(
                                "m_Material.Array.data[{0}]",
                                index
                            )
                        );
                        pMat.objectReferenceValue = EditorGUI.ObjectField(
                            rect,
                            pMat.objectReferenceValue,
                            typeof(Material),
                            false
                        );
                    };
                    l.DoLayoutList();
                }

                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Translation"));
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Rotation"));
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Scale"));
            }
        }

        protected override void OnCustomInspectorGUI()
        {
            base.OnCustomInspectorGUI();
            if (Target && selectedIndex < Target.Meshes.Count && Target.Meshes[selectedIndex].Mesh)
            {
                GUILayout.Space(5);
                bool open = true;
                CurvyGUI.Foldout(
                    ref open,
                    "Properties"
                );
                OnPropertiesGUI();
            }
        }

        private string getFormattedMeshInfo(Mesh mesh)
        {
            if (mesh)
            {
                string has = "<color=#008000>";
                string hasnt = "<color=#800000>";
                string close = "</color>";
                //OPTIM this code calls mesh's properties, just to get their length. Those properties do copy an array, every frame! A solution might be to store their length (or the boolean) in CGMeshProperties
                string norm = mesh.normals.Length > 0
                    ? has
                    : hasnt;
                string tan = mesh.tangents.Length > 0
                    ? has
                    : hasnt;
                string uv = mesh.uv.Length > 0
                    ? has
                    : hasnt;
                string uv2 = mesh.uv2.Length > 0
                    ? has
                    : hasnt;
                return string.Format(
                    "{1}Nor{0} {2}Tan{0} {3}UV{0} {4}UV2{0}",
                    close,
                    norm,
                    tan,
                    uv,
                    uv2
                );
            }

            return "";
        }
    }
}