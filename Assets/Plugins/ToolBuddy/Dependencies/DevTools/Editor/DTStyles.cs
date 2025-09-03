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
    public static class DTStyles
    {

        public static GUIStyle BackdropHtmlLabel
        {
            get
            {
                if (mBackdropHtmlLabel == null)
                {
                    mBackdropHtmlLabel = new GUIStyle();//GUI.skin.GetStyle("HelpBox"));
                    mBackdropHtmlLabel.alignment = TextAnchor.MiddleCenter;
                    Texture2D bgTex = new Texture2D(
                        1,
                        1
                    );
                    bgTex.SetPixel(0, 0, new Color(1, 1, 1, 0.3f));
                    bgTex.Apply();
                    bgTex.hideFlags = HideFlags.DontSave;
                    mBackdropHtmlLabel.padding = new RectOffset(1, 2, 0, 0);
                    mBackdropHtmlLabel.normal.background = bgTex;
                    mBackdropHtmlLabel.richText = true;
                }
                return mBackdropHtmlLabel;
            }
        }

        private static GUIStyle mBackdropHtmlLabel;

        private static GUIStyle mTBButton;
        public static GUIStyle TBButton
        {
            get
            {
                if (mTBButton == null)
                { 
                    mTBButton = new GUIStyle(GUI.skin.button);
                    mTBButton.alignment = TextAnchor.MiddleLeft;
                    mTBButton.padding.top = 4;
                    mTBButton.padding.bottom = 2;
                }
            
                return mTBButton;
            }
        }

        private static GUIStyle mTBBackground;
        public static GUIStyle TBBackground
        {
            get
            {
                if (mTBBackground == null)
                {
                    mTBBackground = new GUIStyle(GUI.skin.textArea);
                }
                return mTBBackground;
            }
        }

        private static GUIStyle mHtmlHelpBox;
        public static GUIStyle HtmlHelpBox
        {
            get
            {
                if (mHtmlHelpBox == null)
                {
                    mHtmlHelpBox= new GUIStyle(GUI.skin.GetStyle("HelpBox"));
                    mHtmlHelpBox.richText = true;
                    return mHtmlHelpBox;
                }
                return mHtmlHelpBox;
            }
        }

        private static GUIStyle mHtmlLabel;
        public static GUIStyle HtmlLabel
        {
            get
            {
                if (mHtmlLabel == null)
                {
                    mHtmlLabel = new GUIStyle(GUI.skin.label);
                    mHtmlLabel.richText = true;
                    return mHtmlLabel;
                }
                return mHtmlLabel;
            }
        }

        private static GUIStyle mHtmlLabelAlignTop;

        public static GUIStyle HtmlLabelAlignTop
        {
            get
            {
                if (mHtmlLabelAlignTop == null)
                {
                    mHtmlLabelAlignTop = new GUIStyle(GUI.skin.label);
                    mHtmlLabelAlignTop.richText = true;
                    mHtmlLabelAlignTop.alignment = TextAnchor.UpperLeft;
                    mHtmlLabelAlignTop.wordWrap = true;
                    return mHtmlLabelAlignTop;
                }
                return mHtmlLabelAlignTop;
            }
        }

        private static GUIStyle mHtmlLinkLabel;
        public static GUIStyle HtmlLinkLabel
        {
            get
            {
                if (mHtmlLinkLabel == null)
                {
                    mHtmlLinkLabel = new GUIStyle(EditorStyles.label);
                    mHtmlLinkLabel.richText = true;
                    int h = (int)mHtmlLinkLabel.lineHeight;
                    mHtmlLinkLabel.normal.background = new Texture2D(1, h);
                    Color lineCol = new Color(
                        .44f,
                        .57f,
                        .79f
                    );
                    for (int i = 0; i < h; i++)
                        mHtmlLinkLabel.normal.background.SetPixel(0, i, (i==3) ? lineCol : new Color(0,0,0,0));
                    
                    mHtmlLinkLabel.normal.background.Apply();
                    
                    mHtmlLinkLabel.hover.background = mHtmlLinkLabel.normal.background;
                    mHtmlLinkLabel.hover.textColor = new Color(.44f, .57f, .79f);
                    return mHtmlLinkLabel;
                }
                return mHtmlLinkLabel;
            }
        }
    }
}
