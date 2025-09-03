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
using System.Collections.Generic;

namespace FluffyUnderware.DevToolsEditor
{

    public abstract class DTToolbarItem : IComparable
    {
        /// <summary>
        /// If set, disables event processing for all other items.
        /// </summary>
        public static DTToolbarItem FocusedItem { get; set; }
        public Texture2D Icon
        {
            get
            {
                if (mIcon == null && !string.IsNullOrEmpty(mIconPackedString) && Project.Resource != null)
                    mIcon = Project.Resource.LoadPacked(mIconPackedString);
                return mIcon;
            }
            set
            {
                if (mIcon != value)
                    mIcon = value;
            }
        }

        public string Label { get; set; }
        public string Tooltip { get; set; }
        public DTProject Project { get; set; }
        public int Order { get; set; }

        public static DTToolbarStatus _StatusBar = new DTToolbarStatus();

        public List<EditorKeyBinding> KeyBindings => mKeyBindings;

        public GUIStyle Style => DTStyles.TBButton;

        public GUIContent Content
        {
            get
            {
                string keyTT = (KeyBindings.Count > 0) ? KeyBindings[0].ToTooltipString() : string.Empty;

                string tooltip;
                {
                    if (string.IsNullOrEmpty(Tooltip))
                        tooltip = keyTT;
                    else if (string.IsNullOrEmpty(keyTT))
                        tooltip = Tooltip;
                    else
                        tooltip = $"{Tooltip} {keyTT}";
                }

                switch (Project.ToolbarMode)
                {
                    case DTToolbarMode.Icon:
                        if (Icon != null)
                            return new GUIContent(Icon, tooltip);
                        else // fallback to "Text"
                            return new GUIContent(Label, tooltip);
                    case DTToolbarMode.Text:
                        return new GUIContent(Label, tooltip);
                    default:
                        return new GUIContent(Label, Icon, tooltip);
                }
            }
        }

        public bool Visible
        {
            get => mVisible;
            set
            {
                if (mVisible != value)
                    mVisible = value;
            }
        }

        public bool Enabled
        {
            get => mEnabled;
            set
            {
                if (mEnabled != value)
                    mEnabled = value;
            }
        }

        public virtual bool ShowClientArea
        {
            get => mShowClientArea;
            set
            {
                if (mShowClientArea != value)
                    mShowClientArea = value;
            }
        }

        public bool IsMouseOver => mItemRect.Contains(DTGUI.MousePosition);

        public virtual string StatusBarInfo => string.Empty;

        private bool mVisible = true;
        private bool mEnabled = true;
        private bool mShowClientArea;
        private readonly string mIconPackedString;
        private Texture2D mIcon;
        private readonly List<EditorKeyBinding> mKeyBindings = new List<EditorKeyBinding>();

        private static DTToolbarItem _lastClickedItem;

        internal Rect mItemRect;
        internal List<Rect> mBackgroundRects = new List<Rect>();

        protected DTToolbarItem()
        {
            object[] attribs = GetType().GetCustomAttributes(typeof(ToolbarItemAttribute), true);
            if (attribs.Length > 0)
            {
                ToolbarItemAttribute a = (ToolbarItemAttribute)attribs[0];
                Project = DT.Project(a.Project);
                Project.ToolbarItems.Add(this);
                Label = a.Label;
                Tooltip = a.Tooltip;
                if (!string.IsNullOrEmpty(a.Icon))
                {
                    mIconPackedString = (!EditorGUIUtility.isProSkin && !string.IsNullOrEmpty(a.IconLightSkin)) ? a.IconLightSkin : a.Icon;
                }
                Order = a.Order;
                OnSelectionChange();
            }
        }


        #region ### Events ###

        public virtual void OnClick()
        {
            if (_lastClickedItem != null && _lastClickedItem != this)
                _lastClickedItem.OnOtherItemClicked(this);
            _lastClickedItem = this;
        }

        public virtual void OnSelectionChange()
        {
        }

        public virtual void OnOtherItemClicked(DTToolbarItem other)
        {
        }


        #endregion

        /// <summary>
        /// Renders the item (button etc.) itself inside a Handles.BeginGUI()/Handles.EndGUI() block
        /// </summary>
        /// <param name="r">area to use</param>
        public virtual void Render(Rect r)
        {
        }

        /// <summary>
        /// Renders an item's client area (if ShowClientArea==true) inside a Handles.BeginGUI()/Handles.EndGUI() block
        /// </summary>
        /// <param name="r">initial Rect with x/y set to starting position</param>
        public virtual void RenderClientArea(Rect r)
        {
        }

        /// <summary>
        /// Regular OnSceneGUI call. Place preview etc. here
        /// </summary>
        public virtual void OnSceneGUI()
        {
        }

        /// <summary>
        /// Returns the size of the item
        /// </summary>
        /// <returns>the size the item needs</returns>
        public virtual Vector2 GetItemSize()
            => Style.CalcSize(Content);

        public virtual void HandleEvents(Event e)
        {
            if (KeyBindings.Count > 0 && KeyBindings[0].IsTriggered(e))
                OnClick();
        }



        #region ### GUI Orientation Helpers ###

        /// <summary>
        /// Changes the rect to properly place an GUI control with a certain width and height.
        /// </summary>
        /// <remarks>Call this before the first GUI control, but after Background()</remarks>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected void SetElementSize(ref Rect r, float width, float height)
        {
            r.width = width;
            r.height = height;
            switch (Project.ToolbarOrientation)
            {
                case DTToolbarOrientation.Right:
                    r.x -= r.width;
                    break;
                case DTToolbarOrientation.Bottom:
                    r.y -= r.height;
                    break;
            }


            //SmallLineAdjust(ref r,height);
            Rect scene = SceneView.currentDrawingSceneView.position;

            if (r.xMax > scene.width)
                r.xMin = scene.width - r.width;
            if (r.yMax > scene.height)
                r.yMin = scene.height - r.height;
        }

        /// <summary>
        /// Draws a background, taking only position from the rect
        /// </summary>
        /// <param name="r"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="guiStyle"></param>
        protected void Background(Rect r, float width, float height, GUIStyle guiStyle = null)
        {
            mBackgroundRects.Add(r);
            if (guiStyle == null)
                guiStyle = DTStyles.TBBackground;
            r.y -= 1;
            r.x -= 1;
            r.width = width + 2;
            r.height = height + 2;
            switch (Project.ToolbarOrientation)
            {
                case DTToolbarOrientation.Right:
                    r.x -= width;
                    break;
                case DTToolbarOrientation.Bottom:
                    r.y -= height;
                    break;
            }


            GUI.Box(r, "", guiStyle);

        }

        /// <summary>
        /// Advance the rect to a new line, respecting Toolbar Orientation
        /// </summary>
        protected void Advance(ref Rect r) { Advance(ref r, r.height); }
        /// <summary>
        /// Advance the rect to a new line, respecting Toolbar Orientation
        /// </summary>
        protected void Advance(ref Rect r, float lastLineHeight)
        {
            switch (Project.ToolbarOrientation)
            {
                case DTToolbarOrientation.Bottom:
                    r.y -= lastLineHeight + 5;
                    break;
                case DTToolbarOrientation.Left:
                    r.x += r.width + 2;
                    break;
                case DTToolbarOrientation.Right:
                    r.x -= r.width + 2;
                    break;
                default: // Top
                    r.y += lastLineHeight + 5;
                    break;
            }
        }

        /// <summary>
        /// Advance the rect to a new line below (or above) the existing rect, depending on Toolbar Orientation
        /// </summary>
        protected void AdvanceBelow(ref Rect r) { AdvanceBelow(ref r, r.height); }
        /// <summary>
        /// Advance the rect to a new line below (or above) the existing rect, depending on Toolbar Orientation
        /// </summary>
        protected void AdvanceBelow(ref Rect r, float lastLineHeight)
        {
            switch (Project.ToolbarOrientation)
            {
                case DTToolbarOrientation.Bottom:
                    r.y -= lastLineHeight;
                    break;
                default:
                    r.y += lastLineHeight;
                    break;
            }
        }

#endregion

        public int CompareTo(object obj)
        {
            DTToolbarItem other = (DTToolbarItem)obj;

            if (Order == other.Order)
                return Label.CompareTo(other.Label);
            else
                return Order.CompareTo(other.Order);
        }

        public static implicit operator bool(DTToolbarItem a)
            => !ReferenceEquals(a, null);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ToolbarItemAttribute : Attribute
    {
        public readonly string Project;
        public readonly int Order;
        public readonly string Label;
        public readonly string Tooltip;
        public readonly string Icon;
        public readonly string IconLightSkin;

        public ToolbarItemAttribute(int order, string project, string label, string tooltip = "", string icon = "", string iconLight = "") : this(project, label, tooltip, icon, iconLight, order)
        {
        }

        public ToolbarItemAttribute(string project, string label, string tooltip = "", string icon = "", string iconLight = "", int order = 0)
        {
            Project = project;
            Label = label;
            Tooltip = tooltip;
            Icon = icon;
            IconLightSkin = iconLight;
            Order = order;
        }
    }

}
