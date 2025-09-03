// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using FluffyUnderware.DevToolsEditor.Extensions;
using FluffyUnderware.DevTools;
using System;
using JetBrains.Annotations;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace FluffyUnderware.DevToolsEditor
{
    public class DTEditor<T> : Editor where T : Object
    {
        #region ### Public Properties ###

        /// <summary>
        /// Target Script
        /// </summary>
        [CanBeNull]
        public virtual T Target => (target != null) ? target as T : null;

        /// <summary>
        /// Whether the target is currently selected or not
        /// </summary>
        public bool TargetIsActive
        {
            get
            {
                if (target is MonoBehaviour)
                    return (target != null && ((MonoBehaviour)target).transform == Selection.activeTransform);
                else
                    return true;
            }
        }

        /// <summary>
        /// Whether the target is part of a prefab
        /// </summary>
        /// <remarks>Has issues with the new prefab system. See the comments in the code that assigns IsPrefab's value to know more about this</remarks>
        // TODO Has issues with the new prefab system. See the comments in the code that assigns IsPrefab's value to know more about this
        [UsedImplicitly] [Obsolete("Will get removed in the next major version. Use Unity's PrefabUtility to know if this.Target is part of a prefab")]
        public bool IsPrefab { get; private set; }

        public bool IsInsideInspector { get; private set; }


        /// <summary>
        /// The Root node of all inspector fields
        /// </summary>
        public DTGroupNode Node => mRootNode;

        /// <summary>
        /// The renderer used to render inspector fields
        /// </summary>
        public IDTInspectorNodeRenderer NodeRenderer
        {
            get => mNodeRenderer;
            set => mNodeRenderer = value;
        }
        /// <summary>
        /// Whether the inspector needs a repaint
        /// </summary>
        public bool NeedRepaint { get; set; }

        #endregion


        private DTGroupNode mRootNode;
        private IDTInspectorNodeRenderer mNodeRenderer = new DTInspectorNodeDefaultRenderer();
        private bool mEnterChildren;

        #region ### Public Methods ###

        /// <summary>
        /// Called when UndoRedo occured
        /// </summary>
        public virtual void OnUndoRedo()
        {
        }

        public override void OnInspectorGUI()
        {
            RenderGUI(false);
        }

        public void RenderGUI(bool embedded = false)
        {
            if (Target == null)
                return;
            serializedObject.UpdateIfRequiredOrScript();

            if (Node.Count == 0)
                ReadNodes();

            NeedRepaint = false;

            if (mRootNode)
            {
                DTInspectorNode.IsInsideInspector = !embedded;
                IsInsideInspector = !embedded;
                OnCustomInspectorGUIBefore();
                renderNode(mRootNode);
                OnCustomInspectorGUI();
            }
            if (serializedObject.ApplyModifiedProperties())
                OnModified();

            GUI.SetNextControlName("");

            if (NeedRepaint)
                Repaint();
        }

        /// <summary>
        /// builds node tree and process parsing attributes
        /// </summary>
        public void ReadNodes()
        {
            DTGroupNode._serializedObject = serializedObject;
            SerializedProperty iterator = serializedObject.GetIterator();
            mRootNode.Clear();
            mEnterChildren = true;

            DTGroupNode baseNode = mRootNode;
            DTGroupNode parentNode = baseNode;
            Stack<string> propertyPathStack = new Stack<string>();
            Stack<DTGroupNode> baseNodeStack = new Stack<DTGroupNode>();
            bool resetParent = false;

            while (iterator.NextVisible(mEnterChildren))
            {
                mEnterChildren = false;
                if (iterator.name != "m_Script" && iterator.name != "InspectorFoldout")
                {
                    // handle baseNode resetting (AsGroup etc...)
                    while (propertyPathStack.Count > 0 && !iterator.propertyPath.StartsWith(propertyPathStack.Peek()))
                    {
                        propertyPathStack.Pop();
                        baseNode = baseNodeStack.Pop();
                        parentNode = baseNode;
                    }


                    DTFieldNode fieldNode = new DTFieldNode(iterator);

                    // get group parsing attributes 
                    List<Attribute> groupParseAttribs = iterator.GetAttributes(typeof(IDTGroupParsingAttribute));
                    groupParseAttribs.Sort();
                    // get field parsing attributes
                    List<Attribute> parsingAttributes = iterator.GetAttributes(typeof(IDTFieldParsingAttribute));

                    foreach (IDTGroupParsingAttribute ga in groupParseAttribs)
                    {

                        if (ga is TabAttribute)
                        {
                            TabAttribute tabA = (TabAttribute)ga;
                            parentNode = baseNode.EnsurePath(tabA.Path, false);

                            if (!string.IsNullOrEmpty(tabA.TabBarName) && !string.IsNullOrEmpty(tabA.TabName))
                            {
                                if (!parentNode[tabA.TabBarName])
                                    parentNode = (DTGroupNode)parentNode.Add(new DTGroupNode(tabA.TabBarName, null, DTInspectorNode.RenderAsEnum.TabBar));
                                else
                                    parentNode = (DTGroupNode)parentNode[tabA.TabBarName];
                                if (!parentNode[tabA.TabName])
                                    parentNode = (DTGroupNode)parentNode.Add(new DTGroupNode(tabA.TabName, iterator, DTInspectorNode.RenderAsEnum.Tab));
                                else
                                    parentNode = (DTGroupNode)parentNode[tabA.TabName];
                                if (tabA.Sort != 100)
                                    parentNode.SortOrder = tabA.Sort;
                            }
                            else
                                DTLog.LogWarningFormat(Target, "[DevTools] Skipping [Tab] on '{0}' because Path is missing TabBar or Tab!", iterator.propertyPath);

                        }
                        else if (ga is SectionAttribute)
                        {
                            SectionAttribute sectionA = (SectionAttribute)ga;
                            parentNode = createGroup(baseNode, sectionA.Path, iterator);
                            if (sectionA.Sort != 100)
                                parentNode.SortOrder = sectionA.Sort;
                        }
                        else if (ga is AsGroupAttribute)
                        {
                            AsGroupAttribute asGroupA = (AsGroupAttribute)ga;
                            propertyPathStack.Push(fieldNode.SerializedPropertyPath);
                            baseNodeStack.Push(baseNode);
                            parentNode = createGroup((asGroupA.PathIsAbsolute) ? baseNode : parentNode, (asGroupA.Path == null) ? fieldNode.Name : asGroupA.Path + "/" + fieldNode.Name, iterator);
                            baseNode = parentNode;

                        }
                        else if (ga is GroupAttribute)
                        {
                            GroupAttribute groupA = (GroupAttribute)ga;
                            parentNode = createGroup(baseNode, groupA.Path, iterator);
                            if (groupA.Sort != 100)
                                parentNode.SortOrder = groupA.Sort;
                            resetParent = true;
                        }
                    }

                    foreach (IDTFieldParsingAttribute pa in parsingAttributes)
                    {
                        if (pa is Hide)
                        {
                            fieldNode.Visible = false;
                            fieldNode.ContentVisible = false;
                            mEnterChildren = false;
                        }
                        else if (pa is AsGroupAttribute || pa is Inline)
                        {
                            fieldNode.Visible = false;
                            fieldNode.ContentVisible = false;
                            mEnterChildren = true;

                        }
                        else if (pa is ArrayExAttribute)
                        {
                            ArrayExAttribute arrayA = (ArrayExAttribute)pa;
                            fieldNode.ArrayEx = new ReorderableList(serializedObject, iterator, arrayA.Draggable, arrayA.ShowHeader, arrayA.ShowAdd, arrayA.ShowDelete);
                            SetupArrayEx(fieldNode, arrayA);
                        }
                        else if (pa is SortOrderAttribute)
                        {
                            fieldNode.SortOrder = ((SortOrderAttribute)pa).Sort;
                        }

                    }

                    parentNode.Add(fieldNode);
                    if (resetParent)
                    {
                        parentNode = parentNode.Parent as DTGroupNode;
                        resetParent = false;
                    }
                }
            }
            OnReadNodes();
            Node.Sort();
        }

        #endregion


        #region ### Protected Methods (override to change inspector appearance or behaviour) ###

        protected virtual void OnEnable()
        {
            if (mRootNode == null)
                mRootNode = new DTGroupNode("Root");
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
#pragma warning disable 618
            if (target != null)
            {
                PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(target);
                //BUG in the new prefab system, an instantiated prefab will have IsPrefab == true, while the documentation of the PrefabUtility.GetPrefabAssetType says otherwise. But is fixing this worth it knowing that IsPrefab is not used in Curvy when using the new prefab system?
                IsPrefab = prefabAssetType == PrefabAssetType.Regular || prefabAssetType == PrefabAssetType.Variant;
            }
            else
                IsPrefab = false;
#pragma warning restore 618
        }

        protected virtual void OnDisable()
        {
            if (mRootNode != null)
                mRootNode.Clear();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        protected virtual void OnReadNodes()
        {
        }

        protected virtual void OnSceneGUI()
        {
        }

        protected virtual void OnModified()
        {
        }

        // <summary>
        /// Add custom GUI code here, rendered before the default inspector
        /// </summary>
        protected virtual void OnCustomInspectorGUIBefore()
        {
        }

        /// <summary>
        /// Add custom GUI code here, rendered after the default inspector
        /// </summary>
        protected virtual void OnCustomInspectorGUI()
        {
        }

        /// <summary>
        /// Called to initialize a ReorderableList. Override to add custom behaviour
        /// </summary>
        /// <param name="node">field node</param>
        /// <param name="attribute">ArrayEx attribute of the field</param>
        protected virtual void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            // Defaults
            if (attribute.ShowHeader)
            {
                // TODO DESIGN make these callbacks virtual protected, and make all the inheriting members override the method instead of assigning a different method to the delegates 

                node.ArrayEx.drawHeaderCallback = (Rect r) =>
                {
                    EditorGUI.LabelField(r, node.GUIContent);
                    if (attribute.DropTarget)
                    {
                        Event ev = Event.current;
                        if (ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform)
                        {
                            Type fieldType = node.serializedProperty.GetFieldType();
                            //Allowed is never true in the currently used cases in Curvy Splines. This might just be removed and it should change nothing
                            bool allowed = DragAndDrop.objectReferences.Length > 0 && DTEditorUtility.DragDropTypeMatch(fieldType);
                            bool isMouseOver = r.Contains(ev.mousePosition);

                            if (ev.type == EventType.DragUpdated)
                            {
                                if (isMouseOver)
                                    DragAndDrop.visualMode = allowed ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                            }
                            else if (ev.type == EventType.DragPerform)
                            {
                                if (isMouseOver && allowed)
                                {
                                    Object[] objs = DTEditorUtility.DragDropGetObjectsOfType(fieldType);

                                    foreach (Object o in objs)
                                    {
                                        int idx = node.serializedProperty.arraySize;
                                        node.serializedProperty.InsertArrayElementAtIndex(idx);
                                        node.serializedProperty.GetArrayElementAtIndex(idx).objectReferenceValue = o;
                                    }

                                    node.serializedObject.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                };
            }

            node.ArrayEx.drawElementCallback = (Rect r, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty e = node.ArrayEx.serializedProperty.GetArrayElementAtIndex(index);
                if (e != null)
                    EditorGUI.PropertyField(r, e);
            };
        }

        #endregion


        /// <summary>
        /// Renders the node tree and process rendering attributes
        /// </summary>
        /// <param name="node"></param>
        private void renderNode(DTInspectorNode node)
        {
            if (serializedObject == null)
                return;
            bool guistate = GUI.enabled;
            DTInspectorNode item;
            for (int i = 0; i < node.Items.Count; i++)
            {
                item = node[i];
                item.serializedObject = serializedObject;

                if (item.Disabled)
                    GUI.enabled = false;
                if (item is DTFieldNode)
                {

                    DTFieldNode field = (DTFieldNode)item;

                    field.serializedProperty = serializedObject.FindProperty(field.SerializedPropertyPath);
                    if (field.serializedProperty == null)
                        return;
                    field.Calculate();

                    if (field.Visible)
                    {
                        foreach (ActionAttribute act in item.Actions)
                            if (act.Position == ActionAttribute.ActionPositionEnum.Above)
                                NodeRenderer.RenderAction(item, act, this, Target);

                        if (field.ArrayEx != null)
                        {
                            field.ArrayEx.serializedProperty = field.serializedProperty;
                            field.ArrayEx.DoLayoutList();
                        }
                        else
                        {
                            NodeRenderer.RenderField(field);
                            field.raiseOnRender();
                        }

                        foreach (ActionAttribute act in item.Actions)
                            if (act.Position == ActionAttribute.ActionPositionEnum.Below)
                                NodeRenderer.RenderAction(item, act, this, Target);
                    }

                }
                else if (item is DTGroupNode)
                {
                    DTGroupNode group = (DTGroupNode)item;
                    group.Calculate();

                    if (group.Visible)
                    {
                        foreach (ActionAttribute act in item.Actions)
                            if (act.Position == ActionAttribute.ActionPositionEnum.Above)
                                NodeRenderer.RenderAction(item, act, this, Target);

                        if (group.Disabled)
                            GUI.enabled = false;

                        switch (item.RenderAs)
                        {
                            case DTInspectorNode.RenderAsEnum.Section:
                                NodeRenderer.RenderSectionHeader(group);
                                try
                                {
                                    if (group.ContentVisible)
                                    {
                                        renderNode(group);
                                        group.raiseOnRender();
                                    }
                                }
                                finally
                                {
                                    NodeRenderer.RenderSectionFooter(group);
                                }
                                break;
                            case DTInspectorNode.RenderAsEnum.TabBar:
                                NodeRenderer.RenderTabBarHeader(
                                    group,
                                    group.MaxItemsPerRow == -1
                                        ? group.Items.Count
                                        : group.MaxItemsPerRow
                                );
                                try
                                {
                                    if (group.SelectedIndex > -1)
                                    {
                                        renderNode(group[group.SelectedIndex]);
                                        group[group.SelectedIndex].raiseOnRender();
                                    }
                                }
                                finally
                                {
                                    NodeRenderer.RenderTabBarFooter(group);
                                }

                                break;
                            default:
                                if (group.ContentVisible)
                                    renderNode(group);
                                break;
                        }

                        foreach (ActionAttribute act in item.Actions)
                            if (act.Position == ActionAttribute.ActionPositionEnum.Below)
                                NodeRenderer.RenderAction(item, act, this, Target);

                    }

                }
                GUI.enabled = guistate;
                if (item.NeedRepaint)
                {
                    item.NeedRepaint = false;
                    NeedRepaint = true;
                }
            }
        }

        /// <summary>
        /// Creates a parent group for a field, creating the path if neccessary
        /// </summary>
        /// <param name="baseNode">"root" the path is applied to</param>
        /// <param name="path">node path</param>
        /// <param name="forProperty">field property the parent node is for</param>
        /// <returns></returns>
        private DTGroupNode createGroup(DTGroupNode baseNode, string path, SerializedProperty forProperty)
        {
            DTGroupNode node = baseNode.EnsurePath(path, false, forProperty);
            return node;
        }



    }

}
