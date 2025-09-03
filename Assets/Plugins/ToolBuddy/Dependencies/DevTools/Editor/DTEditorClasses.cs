// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor.Extensions;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;

namespace FluffyUnderware.DevToolsEditor
{
    public delegate void DTInspectorNodeEvent(DTInspectorNode e);

    /// <summary>
    /// Base Node class
    /// </summary>
    public class DTInspectorNode : IComparable
    {
        public static bool IsInsideInspector { get; set; }
        public event DTInspectorNodeEvent OnNodeRender;

        public enum RenderAsEnum { Invisible, Section, TabBar, Tab, Field };
        /// <summary>
        /// How to render this node
        /// </summary>
        public RenderAsEnum RenderAs = RenderAsEnum.Field;
        /// <summary>
        /// Gets a list of actions of this node
        /// </summary>
        public List<ActionAttribute> Actions = new List<ActionAttribute>();
        /// <summary>
        /// Parent node
        /// </summary>
        public DTInspectorNode Parent;
        /// <summary>
        /// Child nodes
        /// </summary>
        public List<DTInspectorNode> Items = new List<DTInspectorNode>();
        /// <summary>
        /// Gets the number of child nodes
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets all attributes for this node
        /// </summary>
        public List<Attribute> Attributes;
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether the node is expanded/visible or not
        /// </summary>
        public virtual bool Expanded { get; set; }
        /// <summary>
        /// Whether this node needs a repaint
        /// </summary>
        public virtual bool NeedRepaint { get; set; }
        /// <summary>
        /// Whether child nodes are visible or not
        /// </summary>
        public virtual bool ContentVisible
        {
            get => mContentVisible;
            set => mContentVisible = value;
        }

        /// <summary>
        /// Whether this node is visible or not
        /// </summary>
        public virtual bool Visible
        {
            get => mVisible;
            set => mVisible = value;
        }
        /// <summary>
        /// Whether this node is disabled or not
        /// </summary>
        public virtual bool Disabled { get; set; }
        /// <summary>
        /// The serializedObject currently processed
        /// </summary>
        public SerializedObject serializedObject { get; set; }
        /// <summary>
        /// Label/Icon/Tooltip of this node
        /// </summary>
        public GUIContent GUIContent { get; set; }
        /// <summary>
        /// The absolute Path of this node
        /// </summary>
        public string Path => (Parent) ? Parent.Path + "/" + Name : Name;

        /// <summary>
        /// The nesting Level of this node
        /// </summary>
        public int Level { get; private set; }
        /// <summary>
        /// Sort order
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets a child node by it's index
        /// </summary>
        /// <param name="index">index of the child</param>
        /// <returns>a node or null</returns>
        public DTInspectorNode this[int index] => Items[index];

        /// <summary>
        /// Gets a child node by it's name
        /// </summary>
        /// <param name="name">name of the child</param>
        /// <returns>a node or null</returns>
        public DTInspectorNode this[string name]
        {
            get
            {
                return Items.Find(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public int Index { get; protected set; }


        private bool mContentVisible = true;
        private bool mVisible = true;


        protected DTInspectorNode(string name)
        {
            SortOrder = 100;
            Name = name;
        }

        public virtual int CompareTo(object obj)
        {
            DTInspectorNode other = (DTInspectorNode)obj;
            int c = SortOrder.CompareTo(other.SortOrder);
            return (c != 0) ? c : Index.CompareTo(other.Index);
        }
        /// <summary>
        /// Clear everything the node contains
        /// </summary>
        public virtual void Clear()
        {
            for (int i = 0; i < Items.Count; i++)
                Items[i].Clear();
            Items.Clear();
        }

        public virtual DTInspectorNode Add(DTInspectorNode node)
        {
            if (node != null)
            {
                Items.Add(node);
                node.Index = Items.IndexOf(node);
                node.Parent = this;
                node.Level = node.Parent.Level + 1;
            }
            return node;
        }

        public virtual void Calculate(bool firstRun = false) { }

        public void Delete()
        {
            foreach (DTInspectorNode it in Items)
                it.Delete();
            if (Parent)
            {
                Parent.Items.Remove(this);
                Parent.Sort(); // to reset Index
            }
            Parent = null;
        }

        /// <summary>
        /// Get a certain attribute
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <returns>the attribute or null</returns>
        public T GetAttribute<T>() where T : Attribute
        {
            return Attributes.Find(x => x.GetType().IsAssignableFrom(typeof(T))) as T;
        }

        /// <summary>
        /// Gets all attributes
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <returns>all attributes</returns>
        public T[] GetAllAttributes<T>() where T : Attribute
        {
            List<Attribute> res = Attributes.FindAll(x => x.GetType().IsAssignableFrom(typeof(T)));
            T[] r = new T[res.Count];
            for (int i = 0; i < res.Count; i++)
                r[i] = (T)res[i];
            return r;
        }

        public static implicit operator bool(DTInspectorNode a)
            => !ReferenceEquals(a, null);

        /// <summary>
        /// Search for a node at a specific path
        /// </summary>
        /// <typeparam name="T">Type of the node in question</typeparam>
        /// <param name="name">Path and Name of the node in question</param>
        /// <param name="node">The search result or null</param>
        /// <returns>true if found</returns>
        public bool FindNodeAt<T>(string pathAndName, out T node) where T : DTInspectorNode
        {
            string[] p = pathAndName.Split('/');
            node = null;
            DTInspectorNode N = this;
            for (int i = 0; i < p.Length; i++)
            {
                N = N[p[i]];
                if (N == null)
                    return false;
            }
            node = N as T;
            return node != null;
        }

        /// <summary>
        /// Search all child nodes for a node of the given type with a certain name
        /// </summary>
        /// <typeparam name="T">Type of the node in question</typeparam>
        /// <param name="name">Name of the node in question</param>
        /// <param name="node">The search result or null</param>
        /// <returns>true if found</returns>
        public bool FindNode<T>(string name, out T node) where T : DTInspectorNode
        {
            node = this[name] as T;
            if (node != null)
                return true;
            else
            {
                foreach (DTInspectorNode it in Items)
                    if (it.FindNode(name, out node))
                        return true;
            }
            return false;
        }
        /// <summary>
        /// Sort child nodes
        /// </summary>
        public void Sort()
        {
            Items.Sort();

            foreach (DTInspectorNode it in Items)
            {
                it.Index = Items.IndexOf(it);
                if (it is DTGroupNode)
                    it.Sort();
            }
        }

        internal void raiseOnRender()
        {
            if (OnNodeRender != null)
                OnNodeRender(this);
        }

    }

    /// <summary>
    /// Field Node class
    /// </summary>
    public class DTFieldNode : DTInspectorNode
    {
        public string SerializedPropertyPath;
        public SerializedProperty serializedProperty { get; set; }
        public bool IncludeChildren { get; set; }
        public ReorderableList ArrayEx { get; set; }
        public string Tooltip { get; set; }
        public string HelpURL { get; set; }

        public DTFieldNode(SerializedProperty property) : base(property.name)
        {
            RenderAs = RenderAsEnum.Field;
            SerializedPropertyPath = property.propertyPath;
            Attributes = property.GetAttributes(typeof(IDTFieldRenderAttribute));
            Attributes.Sort();
            GUIContent = new GUIContent(ObjectNames.NicifyVariableName(Name));
            Calculate(true);
        }

        public override void Calculate(bool firstRun = false)
        {
            IncludeChildren = true;
            Visible = true;
            Disabled = false;
            Actions.Clear();
            foreach (IDTFieldRenderAttribute a in Attributes)
            {
                if (a is Hide || a is AsGroupAttribute)
                {
                    Visible = false;
                    ContentVisible = false;
                    IncludeChildren = false;
                }
                else if (a is Inline)
                {
                    Visible = false;
                    IncludeChildren = true;
                }

                else if (serializedProperty != null)
                {
                    if (a is FieldConditionAttribute)
                    {
                        FieldConditionAttribute condA = (FieldConditionAttribute)a;
                        bool met = condA.ConditionMet(serializedProperty.serializedObject.targetObject);

                        switch (condA.Action)
                        {
                            case ActionAttribute.ActionEnum.Show:
                                Visible = met;
                                break;
                            case ActionAttribute.ActionEnum.Hide:
                                Visible = !met;
                                break;
                            case ActionAttribute.ActionEnum.Enable:
                                Disabled = !met;
                                break;
                            case ActionAttribute.ActionEnum.Disable:
                                Disabled = met;
                                break;
                            default:
                                if (met)
                                    Actions.Add(condA);
                                break;
                        }

                    }
                    else if (a is FieldActionAttribute)
                    {
                        Actions.Add((FieldActionAttribute)a);
                    }
                }
            }

        }



    }

    /// <summary>
    /// Group node class
    /// </summary>
    public class DTGroupNode : DTInspectorNode
    {

        internal static SerializedObject _serializedObject;

        public override bool Expanded
        {
            get => mState.target;
            set
            {

                if (mState.target != value)
                {
                    mState.target = value;
                    if (serializedObject != null)
                        DTPersistentState.SetBool(serializedObject.targetObject.GetInstanceID().ToString() + Path, value);
                    NeedRepaint = true;
                }
            }
        }

        public float ExpandedFaded => mState.faded;

        public override bool ContentVisible
        {
            get => base.ContentVisible && ExpandedFaded != 0;
            set => base.ContentVisible = value;
        }

        public override bool NeedRepaint => mState.isAnimating;

        public string HelpURL { get; set; }
        public bool Fixed { get; set; }




        #region --- Tab Bar Properties ---

        public int SelectedIndex
        {
            get => Count > 0 ? Mathf.Clamp(mSelectedItem, 0, Count - 1) : -1;
            set => mSelectedItem = Count > 0 ? Mathf.Clamp(value, 0, Count - 1) : -1;
        }

        public DTInspectorNode SelectedItem
        {
            get => (SelectedIndex != -1) ? Items[SelectedIndex] : null;
            set => SelectedIndex = (value == null) ? -1 : value.Index;
        }

        public int MaxItemsPerRow { get; set; }

        #endregion

        private readonly AnimBool mState = new AnimBool(true);
        private int mSelectedItem = -1;

        public DTGroupNode(string name, SerializedProperty forProperty = null, RenderAsEnum renderAs = RenderAsEnum.Section) : base(name)
        {
            RenderAs = renderAs;
            MaxItemsPerRow = -1;
            if (forProperty != null)
            {
                Attributes = forProperty.GetAttributes(typeof(IDTGroupRenderAttribute));
                Attributes.Sort();
                Calculate(true);
            }
            else
            {
                Attributes = new List<Attribute>();
                GUIContent = new GUIContent(ObjectNames.NicifyVariableName(Name));
            }

        }

        public override void Calculate(bool firstRun = false)
        {
            Actions.Clear();
            foreach (IDTGroupRenderAttribute a in Attributes)
            {
                if (a is GroupAttribute)
                {
                    GroupAttribute groupA = (GroupAttribute)a;
                    if (groupA.Invisible)
                        RenderAs = RenderAsEnum.Invisible;
                    if (firstRun)
                    {
                        mState.value = groupA.Expanded;
                        getAdditionalGroupParams(groupA);
                    }
                }
                else if (serializedObject != null)
                {
                    if (a is GroupConditionAttribute)
                    {
                        GroupConditionAttribute condA = (GroupConditionAttribute)a;
                        bool met = condA.ConditionMet(serializedObject.targetObject);

                        switch (condA.Action)
                        {
                            case ActionAttribute.ActionEnum.Show:
                                Visible = met;
                                break;
                            case ActionAttribute.ActionEnum.Hide:
                                Visible = !met;
                                break;
                            case ActionAttribute.ActionEnum.Enable:
                                Disabled = !met;
                                break;
                            case ActionAttribute.ActionEnum.Disable:
                                Disabled = met;
                                break;
                            default:
                                if (met)
                                    Actions.Add(condA);
                                break;
                        }

                    }
                    else if (a is GroupActionAttribute)
                    {
                        Actions.Add((GroupActionAttribute)a);
                    }
                }
            }
        }

        private void getAdditionalGroupParams(GroupAttribute a)
        {
            if (a == null)
            {
                GUIContent = new GUIContent(ObjectNames.NicifyVariableName(Name));
            }
            else
            {
                GUIContent = new GUIContent(a.Label ?? ObjectNames.NicifyVariableName(Name), a.Tooltip);
                HelpURL = a.HelpURL;
            }
        }

        public DTGroupNode EnsurePath(string path, bool includesName = false, SerializedProperty forProperty = null)
        {
            DTGroupNode node = this;
            if (!string.IsNullOrEmpty(path))
            {
                string[] p = path.Split('/');
                int hi = (includesName) ? p.Length - 1 : p.Length;
                for (int i = 0; i < hi; i++)
                {
                    DTGroupNode sub = node[p[i]] as DTGroupNode;
                    if (sub == null)
                    {
                        if (forProperty != null && i == hi - 1)
                            sub = new DTGroupNode(p[i], forProperty);
                        else
                            sub = new DTGroupNode(p[i]);
                        node.Add(sub);
                    }
                    node = sub;
                }
            }
            return node;
        }

        public GUIContent[] GetItemsGUIContent()
        {
            GUIContent[] res = new GUIContent[Count];
            for (int i = 0; i < Count; i++)
                res[i] = this[i].GUIContent;
            return res;
        }

        public DTGroupNode FindTabBarAt(string nameAndPath)
        {
            DTGroupNode node;
            if (FindNodeAt(nameAndPath, out node))
                if (node.RenderAs == RenderAsEnum.TabBar)
                    return node;
            return null;
        }

        public override DTInspectorNode Add(DTInspectorNode node)
        {
            node = base.Add(node);
            DTGroupNode grp = node as DTGroupNode;
            if (grp != null && _serializedObject != null && _serializedObject.targetObject != null)
                grp.mState.value = DTPersistentState.GetBool(_serializedObject.targetObject.GetInstanceID().ToString() + grp.Path, grp.mState.value);

            return node;
        }

        public DTGroupNode AddSection(string name, DTInspectorNodeEvent func)
        {
            DTInspectorNode item = this[name];
            if (item)
            {
                if (item.RenderAs != RenderAsEnum.Section)
                    return null;
            }
            else
            {
                item = new DTGroupNode(name, null, RenderAsEnum.Section);
                Add(item);
                item.OnNodeRender += func;
            }
            return item as DTGroupNode;
        }

        public DTGroupNode AddTab(string name, DTInspectorNodeEvent func)
        {
            if (RenderAs == RenderAsEnum.TabBar)
            {
                DTInspectorNode item = this[name];
                if (item)
                {
                    if (item.RenderAs != RenderAsEnum.Tab)
                        return null;
                }
                else
                {
                    item = new DTGroupNode(name, null, RenderAsEnum.Tab);
                    Add(item);
                    item.OnNodeRender += func;
                }
                return item as DTGroupNode;
            }
            return null;
        }


    }



    public static class DTPersistentState
    {
        private static readonly Dictionary<string, bool> _states = new Dictionary<string, bool>();


        public static bool GetBool(string ident, bool defaultState = true)
        {

            bool res;
            if (!_states.TryGetValue(ident, out res))
            {
                res = defaultState;
                _states.Add(ident, defaultState);
            }
            return res;
        }

        public static void SetBool(string ident, bool state)
        {
            if (!_states.ContainsKey(ident))
                _states.Add(ident, state);
            else
                _states[ident] = state;
        }

    }

    #region ### Renderer ###

    public interface IDTInspectorNodeRenderer
    {
        void RenderTabBarHeader(DTGroupNode node, int maxItemsPerRow);
        void RenderTabBarFooter(DTGroupNode node);
        void RenderSectionHeader(DTGroupNode node);
        void RenderSectionFooter(DTGroupNode node);
        void RenderField(DTFieldNode node);
        void RenderAction(DTInspectorNode node, ActionAttribute action, System.Object editorObject, System.Object targetObject);
    }

    public class DTInspectorNodeDefaultRenderer : IDTInspectorNodeRenderer
    {


        public static Texture HelpIcon
        {
            get
            {
                if (mHelpIcon == null)
                    mHelpIcon = (Texture)EditorGUIUtility.Load("icons/_Help.png");
                return mHelpIcon;
            }
        }

        private static Texture mHelpIcon;
        public static GUIStyle BoldFoldout
        {
            get
            {
                if (mBoldFoldout == null)
                {
                    mBoldFoldout = new GUIStyle(EditorStyles.foldout);
                    mBoldFoldout.fontStyle = FontStyle.Bold;
                    mBoldFoldout.margin.top += 2;
                    mBoldFoldout.margin.bottom += 4;

                }
                return mBoldFoldout;
            }
        }

        private static GUIStyle mBoldFoldout;

        public static GUIStyle TabbarButton
        {
            get
            {
                if (mTabbarButton == null)
                {
                    mTabbarButton = new GUIStyle(EditorStyles.toolbarButton);
                    mTabbarButton.alignment = TextAnchor.MiddleCenter;

                    GUIStyle skinButton = GUI.skin.button;
                    RectOffset skinButtonPadding = skinButton.padding;
                    RectOffset skinButtonMargin = skinButton.margin;
                    RectOffset skinButtonBorder = skinButton.border;
                    RectOffset skinButtonOverflow = skinButton.overflow;

                    mTabbarButton.padding = new RectOffset(skinButtonPadding.left, skinButtonPadding.right, skinButtonPadding.top, skinButtonPadding.bottom);
                    mTabbarButton.margin = new RectOffset(skinButtonMargin.left, skinButtonMargin.right, skinButtonMargin.top, skinButtonMargin.bottom);
                    mTabbarButton.border = new RectOffset(skinButtonBorder.left, skinButtonBorder.right, skinButtonBorder.top, skinButtonBorder.bottom);
                    mTabbarButton.overflow = new RectOffset(skinButtonOverflow.left, skinButtonOverflow.right, skinButtonOverflow.top, skinButtonOverflow.bottom);
                }

                return mTabbarButton;
            }
        }

        private static GUIStyle mTabbarButton;

        public virtual void RenderAction(DTInspectorNode node, ActionAttribute action, System.Object editorObject, System.Object targetObject)
        {
            switch (action.Action)
            {
                case ActionAttribute.ActionEnum.ShowInfo:
                    EditorGUILayout.HelpBox(action.ActionData as string, MessageType.Info);
                    return;
                case ActionAttribute.ActionEnum.ShowWarning:
                    EditorGUILayout.HelpBox(action.ActionData as string, MessageType.Warning);
                    return;
                case ActionAttribute.ActionEnum.ShowError:
                    EditorGUILayout.HelpBox(action.ActionData as string, MessageType.Error);
                    return;
                case ActionAttribute.ActionEnum.Callback:
                    action.Callback(editorObject);
                    return;
            }
        }

        public virtual void RenderField(DTFieldNode node)
        {
            EditorGUILayout.PropertyField(node.serializedProperty, node.IncludeChildren);
        }

        public virtual void RenderSectionHeader(DTGroupNode node)
        {
            GUILayout.Space(10);

            string helpUrl = node.HelpURL;

            Rect controlRect = EditorGUILayout.GetControlRect(false, 16);
            int xOffset = (node.Level <= 1 && DTInspectorNode.IsInsideInspector) ? 12 : 0;
            controlRect.x -= xOffset;

            bool toggleState = node.Expanded;
            int indentLevel = RenderHeader(controlRect, xOffset, helpUrl, node.GUIContent, ref toggleState);

            node.Expanded = toggleState;
            EditorGUILayout.BeginFadeGroup(node.ExpandedFaded);
            EditorGUI.indentLevel = (node.Level <= 1) ? indentLevel : indentLevel + 1;
        }

#pragma warning disable 162
#pragma warning disable 429
        private static readonly Color SectionHeaderBackgroundColor = new Color(
            1f,
            1f,
            1f
        );
#pragma warning restore 429
#pragma warning restore 162
        public static int RenderHeader(Rect controlRect, int xOffset, string helpUrl, GUIContent toggleGuiContent, ref bool toggleState)
        {
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = Mathf.Max(0, indentLevel - 1);
            controlRect = EditorGUI.IndentedRect(controlRect);
            DTGUI.PushColor(SectionHeaderBackgroundColor);
            const int additionalHeight = 4;
            Rect boxRectangle = new Rect(
                controlRect.x,
                controlRect.y - 2,
                controlRect.width + xOffset + 1,
                controlRect.height + additionalHeight
            );
            GUI.Box(boxRectangle, "", GUI.skin.box);
            DTHandles.DrawOutline(boxRectangle, Color.black);
            DTGUI.PopColor();

            Rect expandClickableArea = new Rect(controlRect);
            bool hasHelp = !string.IsNullOrEmpty(helpUrl);
            if (hasHelp)
                expandClickableArea.width -= 12;

            toggleState = GUI.Toggle(expandClickableArea, toggleState, toggleGuiContent, BoldFoldout);
            if (hasHelp)
            {
                if (GUI.Button(new Rect(boxRectangle.xMax - 20, boxRectangle.y + 3, 16, 16)
                    , new GUIContent(HelpIcon, "Help"), new GUIStyle()))
                    Application.OpenURL(helpUrl);
            }

            GUILayout.Space(additionalHeight);
            return indentLevel;
        }

        public virtual void RenderSectionFooter(DTGroupNode node)
        {
            if (node.Level > 1)
                EditorGUI.indentLevel--;
            EditorGUILayout.EndFadeGroup();
        }

        public virtual void RenderTabBarHeader(DTGroupNode node, int maxItemsPerRow)
        {
            GUILayout.Space(4);

            int s = GUILayout.SelectionGrid(node.SelectedIndex, node.GetItemsGUIContent(), Mathf.Min(node.Count, maxItemsPerRow), TabbarButton);
            if (s != node.SelectedIndex)
            {
                node.SelectedIndex = s;
                GUIUtility.keyboardControl = 0;
            }
            Color c = GUI.color;
            if (!EditorGUIUtility.isProSkin)
                GUI.color = new Color(c.r, c.g, c.b, 0.5f);

            GUILayout.BeginVertical(GUI.skin.box);
            GUI.color = c;
        }

        public virtual void RenderTabBarFooter(DTGroupNode node)
        {
            GUILayout.EndVertical();
        }

    }

    #endregion
}
