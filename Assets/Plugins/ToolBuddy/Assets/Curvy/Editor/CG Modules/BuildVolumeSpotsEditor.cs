// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using FluffyUnderware.DevToolsEditor.Extensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildVolumeSpots))]
    public class BuildVolumeSpotsEditor : CGModuleEditor<BuildVolumeSpots>
    {
        private ReorderableList mGroupItemsList;
        private CGBoundsGroup mCurrentGroup;

        private DTGroupNode distributionGroupNode;
        private DTGroupNode rotationGroupNode;
        private DTGroupNode translationGroupNode;
        private DTGroupNode scaleGroupNode;

        private readonly GUIContent uniformScalingLabel = new GUIContent("Scale");
        private readonly GUIContent itemsNumberLabel = new GUIContent("    Items #");
        private readonly GUIContent emptyLabel1 = new GUIContent("");
        private readonly GUIContent emptyLabel2 = new GUIContent("");
        private readonly GUIContent emptyLabel3 = new GUIContent("");

        protected override void OnEnable()
        {
            base.OnEnable();
            HasDebugVisuals = true;
        }

        public override void OnModuleSceneDebugGUI()
        {
            CGSpots data = Target.SimulatedSpots;
            if (data)
            {
                Handles.matrix = Target.Generator.transform.localToWorldMatrix;
                for (int i = 0; i < data.Spots.Count; i++)
                {
                    Quaternion Q = data.Spots.Array[i].Rotation
                    * Quaternion.Euler(
                        -90,
                        0,
                        0
                    );
                    Handles.ArrowHandleCap(
                        0,
                        data.Spots.Array[i].Position,
                        Q,
                        2,
                        EventType.Repaint
                    );

                    Handles.Label(
                        data.Spots.Array[i].Position,
                        data.Spots.Array[i].Index.ToString(),
                        EditorStyles.whiteBoldLabel
                    );
                }

                Handles.matrix = Matrix4x4.identity;
            }
        }

        protected override void OnReadNodes()
        {
            base.OnReadNodes();
            ensureGroupTabs();

            //Used to subdivide nicely the diplayed fields within groups
            distributionGroupNode = new DTGroupNode("Distribution");

            rotationGroupNode = new DTGroupNode("Rotation");
            rotationGroupNode.Expanded = false;
            translationGroupNode = new DTGroupNode("Translation");
            translationGroupNode.Expanded = false;
            scaleGroupNode = new DTGroupNode("Scale");
            scaleGroupNode.Expanded = false;
        }

        private void ensureGroupTabs()
        {
            DTGroupNode tabbar = Node.FindTabBarAt("Default");
            for (int i = 0; i < Target.GroupCount; i++)
            {
                string tabName = string.Format(
                    "{0}:{1}",
                    i,
                    Target.Groups[i].Name
                );
                if (tabbar.Count <= i + 2)
                    tabbar.AddTab(
                        tabName,
                        OnRenderTab
                    );
                else
                {
                    tabbar[i + 2].Name = tabName;
                    tabbar[i + 2].GUIContent.text = tabName;
                }
            }

            for (int i = tabbar.Count - 1; i > Target.GroupCount + 1; i--)
                tabbar[i].Delete();
        }

        private void OnRenderTab(DTInspectorNode node)
        {
            int grpIdx = node.Index - 2;

            if (grpIdx >= 0 && grpIdx < Target.GroupCount)
            {
                SerializedProperty pGroup = serializedObject.FindProperty(
                    string.Format(
                        "m_Groups.Array.data[{0}]",
                        grpIdx
                    )
                );
                if (pGroup != null)
                {
                    CGBoundsGroup boundsGroup = Target.Groups[grpIdx];
                    SerializedProperty pItems = pGroup.FindPropertyRelative("m_Items");
                    if (pItems != null)
                    {
                        if (mCurrentGroup != null && mCurrentGroup != boundsGroup)
                            mGroupItemsList = null;
                        if (mGroupItemsList == null)
                        {
                            mCurrentGroup = boundsGroup;
                            mGroupItemsList = new ReorderableList(
                                pItems.serializedObject,
                                pItems
                            );
                            mGroupItemsList.draggable = true;
                            mGroupItemsList.drawHeaderCallback = Rect =>
                            {
                                EditorGUI.LabelField(
                                    Rect,
                                    "Items"
                                );
                            };
                            mGroupItemsList.drawElementCallback = (rect, index, isActive, isFocused) =>
                            {
                                #region ---

                                SerializedProperty prop = pItems.FindPropertyRelative(
                                    string.Format(
                                        "Array.data[{0}]",
                                        index
                                    )
                                );
                                SerializedProperty pIndex = prop.FindPropertyRelative("Index");

                                rect.height = EditorGUIUtility.singleLineHeight;
                                EditorGUI.LabelField(
                                    new Rect(
                                        rect.x,
                                        rect.y,
                                        30,
                                        rect.height
                                    ),
                                    "#" + index + ":"
                                );

                                GUIContent[] boundNames = Target.BoundsNames;
                                if (boundNames.Length == 0)
                                    pIndex.intValue = EditorGUI.IntField(
                                        GetSelectorDrawArea(rect),
                                        "",
                                        pIndex.intValue
                                    );
                                else
                                {
                                    int[] targetBoundsIndices;
                                    {
                                        targetBoundsIndices = new int[boundNames.Length];
                                        for (int i = 0; i < targetBoundsIndices.Length; i++)
                                            targetBoundsIndices[i] = i;
                                    }

                                    EditorGUI.IntPopup(
                                        GetSelectorDrawArea(rect),
                                        pIndex,
                                        boundNames,
                                        targetBoundsIndices,
                                        emptyLabel1
                                    );
                                }

                                if (boundsGroup.RandomizeItems
                                    && index >= boundsGroup.FirstRepeating
                                    && index <= boundsGroup.LastRepeating)
                                    EditorGUI.PropertyField(
                                        GetWeightDrawArea(rect),
                                        prop.FindPropertyRelative("m_Weight"),
                                        emptyLabel2
                                    );

                                #endregion
                            };

                            mGroupItemsList.onAddCallback = l =>
                            {
                                boundsGroup.Items.Insert(
                                    Mathf.Clamp(
                                        l.index + 1,
                                        0,
                                        boundsGroup.ItemCount
                                    ),
                                    new CGBoundsGroupItem()
                                );
                                boundsGroup.LastRepeating++;
                                Target.Dirty = true;
                                EditorUtility.SetDirty(Target);
                            };
                            mGroupItemsList.onRemoveCallback = l =>
                            {
                                boundsGroup.Items.RemoveAt(l.index);
                                boundsGroup.LastRepeating--;
                                Target.Dirty = true;
                                EditorUtility.SetDirty(Target);
                            };
                        }

                        mGroupItemsList.DoLayoutList();

                        NeedRepaint |= RenderGroup(
                            distributionGroupNode,
                            () =>
                            {
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RandomizeItems"));
                                if (boundsGroup.RandomizeItems)
                                    EditorGUILayout.PropertyField(
                                        pGroup.FindPropertyRelative("m_RepeatingItems"),
                                        itemsNumberLabel
                                    );
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_KeepTogether"));
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_SpaceBefore"));
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_SpaceAfter"));
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_CrossBase"));
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_IgnoreModuleCrossBase"));
                            }
                        );

                        NeedRepaint |= RenderGroup(
                            translationGroupNode,
                            () =>
                            {
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RelativeTranslation"));
                                EditorGUILayout.PropertyField(
                                    pGroup.FindPropertyRelative("m_TranslationX")
                                ); //, XTranslationContent);
                                EditorGUILayout.PropertyField(
                                    pGroup.FindPropertyRelative("m_TranslationY")
                                ); //, YTranslationContent);
                                EditorGUILayout.PropertyField(
                                    pGroup.FindPropertyRelative("m_TranslationZ")
                                ); //, ZTranslationContent);
                            }
                        );

                        NeedRepaint |= RenderGroup(
                            rotationGroupNode,
                            () =>
                            {
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RotationMode"));
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RotationX")); //, XRotationContent);
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RotationY")); //, YRotationContent);
                                EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_RotationZ")); //, ZRotationContent);
                            }
                        );

                        NeedRepaint |= RenderGroup(
                            scaleGroupNode,
                            () =>
                            {
                                EditorGUILayout.PropertyField(
                                    pGroup.FindPropertyRelative("m_UniformScaling")
                                ); //, XScaleContent);
                                if (boundsGroup.UniformScaling)
                                {
                                    EditorGUILayout.PropertyField(
                                        pGroup.FindPropertyRelative("m_ScaleX"),
                                        uniformScalingLabel
                                    );
                                }
                                else
                                {
                                    EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_ScaleX")); //, XScaleContent);
                                    EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_ScaleY")); //, YScaleContent);
                                    EditorGUILayout.PropertyField(pGroup.FindPropertyRelative("m_ScaleZ")); //, ZScaleContent);
                                }
                            }
                        );
                    }
                }
            }
        }

        private bool RenderGroup(DTGroupNode groupNode, Action action)
        {
            GUILayout.Space(10);

            Rect controlRect = EditorGUILayout.GetControlRect(
                false,
                16
            );
            bool toggleState = groupNode.Expanded;
            DTInspectorNodeDefaultRenderer.RenderHeader(
                controlRect,
                0,
                String.Empty,
                groupNode.GUIContent,
                ref toggleState
            );
            groupNode.Expanded = toggleState;

            EditorGUILayoutExtension.FadeGroup(
                isVisible =>
                {
                    //todo Why is isVisible not used?

                    //BUG if indentation is activated, mouse detection on the FloatRegion parameters gets fucked up
                    //EditorGUI.indentLevel = EditorGUI.indentLevel + 1;

                    if (groupNode.ContentVisible)
                        action();

                    //BUG if indentation is activated, mouse detection on the FloatRegion parameters gets fucked up
                    //EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
                },
                groupNode.ExpandedFaded
            );

            return groupNode.NeedRepaint;
        }

        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );

            switch (node.Name)
            {
                case "m_Groups":
                    node.ArrayEx.drawElementCallback = OnGroupElementGUI;
                    node.ArrayEx.onAddCallback = l =>
                    {
                        //TODO unify this code with the one in BuildVolumeSpot.AddGroup()

                        //Creates a group
                        CGBoundsGroup cgBoundsGroup = new CGBoundsGroup("Group");

                        ////Adds the first item from input module if any.
                        //if (Target.InBounds)
                        //{
                        //    List<CGGameObject> inGameObjects = Target.InBounds.GetAllData<CGGameObject>();
                        //    if (inGameObjects.Count != 0)
                        //    {
                        //        CGBoundsGroupItem cgBoundsGroupItem = new CGBoundsGroupItem();
                        //        cgBoundsGroup.Items.Add(cgBoundsGroupItem);
                        //    }
                        //}

                        //Always add an input
                        cgBoundsGroup.Items.Add(new CGBoundsGroupItem());

                        //Adds the group
                        Target.Groups.Insert(
                            Mathf.Clamp(
                                l.index + 1,
                                0,
                                Target.GroupCount
                            ),
                            cgBoundsGroup
                        );

                        Target.LastRepeating++;
                        EditorUtility.SetDirty(Target);
                        ensureGroupTabs();
                    };
                    node.ArrayEx.onRemoveCallback = l =>
                    {
                        mGroupItemsList = null;
                        Target.Groups.RemoveAt(l.index);
                        Target.LastRepeating--;
                        EditorUtility.SetDirty(Target);

                        //node[1+l.index].Delete();
                        ensureGroupTabs();
                        GUIUtility.ExitGUI();
                    };
                    node.ArrayEx.onReorderCallback = l =>
                    {
                        ensureGroupTabs();
                        GUIUtility.ExitGUI();
                    };
                    break;
            }
        }

        private void OnGroupElementGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            bool fix = index < Target.FirstRepeating || index > Target.LastRepeating;

            if (fix)
                DTHandles.DrawSolidRectangleWithOutline(
                    rect,
                    new Color(
                        0,
                        0,
                        0.5f,
                        0.2f
                    ),
                    new Color(
                        0,
                        0,
                        0,
                        0
                    )
                );

            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "m_Groups.Array.data[{0}]",
                    index
                )
            );
            if (prop != null)
            {
                SerializedProperty pName = prop.FindPropertyRelative("m_Name");
                SerializedProperty pRepeatingOrder = serializedObject.FindProperty("m_RepeatingOrder");

                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(
                    new Rect(
                        rect.x,
                        rect.y,
                        30,
                        rect.height
                    ),
                    "#" + index + ":"
                );

                EditorGUI.BeginChangeCheck();
                pName.stringValue = EditorGUI.TextField(
                    GetSelectorDrawArea(rect),
                    "",
                    pName.stringValue
                );
                if (EditorGUI.EndChangeCheck())
                {
                    DTGroupNode tab = Node.FindTabBarAt("Default");
                    if (tab && tab.Count > index + 2)
                    {
                        tab[index + 2].Name = string.Format(
                            "{0}:{1}",
                            index,
                            pName.stringValue
                        );
                        tab[index + 2].GUIContent.text = string.Format(
                            "{0}:{1}",
                            index,
                            pName.stringValue
                        );
                    }
                }

                if (!fix && pRepeatingOrder.intValue == (int)CurvyRepeatingOrderEnum.Random)
                    EditorGUI.PropertyField(
                        GetWeightDrawArea(rect),
                        prop.FindPropertyRelative("m_Weight"),
                        emptyLabel3
                    );
            }
        }

        protected override void OnCustomInspectorGUIBefore()
        {
            base.OnCustomInspectorGUIBefore();
            EditorGUILayout.HelpBox(
                "Spots: " + Target.Count,
                MessageType.Info
            );
        }

        private static Rect GetSelectorDrawArea(Rect rect)
        {
            Rect itemSelectorDrawArea = new Rect(rect);
            itemSelectorDrawArea.x += 30;
            itemSelectorDrawArea.y += 1;
            itemSelectorDrawArea.width = (rect.width / 2) - 50;
            return itemSelectorDrawArea;
        }

        private static Rect GetWeightDrawArea(Rect rect)
        {
            Rect weightDrawArea = new Rect(rect);
            weightDrawArea.x += (rect.width / 2) - 10;
            weightDrawArea.y += 1;
            weightDrawArea.width = rect.width / 2;
            return weightDrawArea;
        }
    }
}