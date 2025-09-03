// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor.Extensions;

namespace FluffyUnderware.DevToolsEditor
{
    public class DTStatusbar
    {
        private string mSourceIdent;
        private string mStatusBarMessage;
        private MessageType mStatusBarMessageType;
        private float mStatusBarMessageDelay;
        private double mStatusBarMessageLastTime;

        private GUIStyle mMessageStyle;
        public GUIStyle MessageStyle
        {
            get
            {
                if (mMessageStyle == null)
                {
                    mMessageStyle = GetStyle();
                }
                return mMessageStyle;
            }
            set => mMessageStyle = value;
        }

        public bool Visible = true;


        protected virtual GUIStyle GetStyle()
        {
            GUIStyle st= new GUIStyle(GUI.skin.GetStyle("HelpBox"));
            st.richText = true;
            return st;
        }

        protected virtual void GetColors()
        {
            switch (mStatusBarMessageType)
            {
                case MessageType.Error:
                    GUI.contentColor = Color.white;
                    GUI.backgroundColor = Color.red.SkinAwareColor();
                    break;
                case MessageType.Warning:
                    GUI.contentColor = Color.white;
                    GUI.backgroundColor = Color.yellow.SkinAwareColor();
                    break;
                case MessageType.Info:
                    GUI.contentColor = Color.yellow;
                    break;
                default:
                    GUI.contentColor = Color.white;
                    break;
            }

            
        }

        public void Clear(string source="")
        {
            if (source == mSourceIdent || string.IsNullOrEmpty(mSourceIdent))
            {
                mStatusBarMessage = "";
                mStatusBarMessageType = MessageType.None;
                mStatusBarMessageDelay = 0;
                mStatusBarMessageLastTime = EditorApplication.timeSinceStartup;
                mSourceIdent = "";
            }

        }

        public void Set(string text, string source="",float delay = 0)
        {
            SetMessage(text, source,MessageType.None, delay);
        }

        public void SetError(string text, string source = "", float delay = 0)
        {
            SetMessage(text,source, MessageType.Error, delay);
        }

        public void SetInfo(string text, string source = "", float delay = 0)
        {
            SetMessage(text, source,MessageType.Info, delay);
        }

        public void SetWarning(string text, string source = "", float delay = 0)
        {
            SetMessage(text, source,MessageType.Warning, delay);
        }

        public void SetMessage(string msg = "", string source="",MessageType type = MessageType.None, float delay = 0)
        {
                mStatusBarMessage = msg;
                mStatusBarMessageType = type;
                mStatusBarMessageDelay = delay;
                mStatusBarMessageLastTime = EditorApplication.timeSinceStartup;
                mSourceIdent = source;
        }

        /// <summary>
        /// Renders the StatusBar
        /// </summary>
        /// <param name="renderingZone">the rect to use</param>
        /// <param name="style">(Optional) A style to override the default style</param>
        /// <param name="fit">Allow the <paramref name="renderingZone"/> to be modified to fit the text to render</param>
        /// <returns>true if you should do a repaint (i.e. a color fade is in progress)</returns>
        public bool Render(Rect renderingZone, GUIStyle style=null, bool fit=false)
        {
            if (!Visible || string.IsNullOrEmpty(mStatusBarMessage))
                return false;
            
            Color c = GUI.contentColor;
            //Color bc = GUI.backgroundColor;
            GetColors();
            GUI.contentColor = GUI.contentColor.Fade(mStatusBarMessageLastTime, mStatusBarMessageDelay, 1);
            bool repaint = (EditorApplication.timeSinceStartup-mStatusBarMessageDelay<mStatusBarMessageLastTime);
            if (fit)
            {
                Vector2 calcSize = EditorStyles.label.CalcSize(new GUIContent(mStatusBarMessage));
                float w=calcSize.x + 20;
                renderingZone.x += (renderingZone.width - w) / 2;
                renderingZone.width = w;
                float h = calcSize.y;
                renderingZone.y += renderingZone.height - h;
                renderingZone.height = h;
            }

            EditorGUI.LabelField(renderingZone, mStatusBarMessage, style==null ? MessageStyle:style);
            GUI.contentColor = c;
            //GUI.backgroundColor = bc;
            return repaint;
        }
    }


}
