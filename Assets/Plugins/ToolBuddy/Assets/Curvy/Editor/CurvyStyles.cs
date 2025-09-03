// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Text;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevToolsEditor.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public static class CurvyStyles
    {
        public static GUIStyle GizmoText
        {
            get
            {
                if (gizmoText == null)
                {
                    gizmoText = new GUIStyle();
                    gizmoText.fixedHeight = 15;
                    gizmoText.alignment = TextAnchor.MiddleCenter;
                    //gizmoText.fixedWidth = label.Length * 10;
                    //gizmoText.padding = new RectOffset(0,0,0,0);
                    //gizmoText.margin = new RectOffset(0,0,0,0);
                    //gizmoText.border = new RectOffset(0,0,0,0);
                    //gizmoText.overflow = new RectOffset(0,0,0,0);
                    //gizmoText.contentOffset = Vector2.zero;
                    Texture2D backgroundTexture = new Texture2D(
                        1,
                        1
                    );
                    backgroundTexture.SetPixel(
                        0,
                        0,
                        new Color(
                            1,
                            1,
                            1,
                            0.3f
                        )
                    );
                    backgroundTexture.Apply();
                    backgroundTexture.hideFlags = HideFlags.DontSave;
                    gizmoText.normal.background = backgroundTexture;
                }

                return gizmoText;
            }
        }

        private static GUIStyle gizmoText;

        public static GUIStyle ControllerCustomEventStyle
        {
            get
            {
                if (controllerCustomEventStyle == null)
                {
                    controllerCustomEventStyle = new GUIStyle();
                    controllerCustomEventStyle.fixedHeight = 15;
                    controllerCustomEventStyle.alignment = TextAnchor.MiddleCenter;
                    //gizmoText.fixedWidth = label.Length * 10;
                    //gizmoText.padding = new RectOffset(0,0,0,0);
                    //gizmoText.margin = new RectOffset(0,0,0,0);
                    //gizmoText.border = new RectOffset(0,0,0,0);
                    //gizmoText.overflow = new RectOffset(0,0,0,0);
                    //gizmoText.contentOffset = Vector2.zero;
                    Texture2D backgroundTexture = new Texture2D(
                        1,
                        1
                    );
                    backgroundTexture.SetPixel(
                        0,
                        0,
                        new Color(
                            1,
                            1,
                            1,
                            0.3f
                        )
                    );
                    backgroundTexture.Apply();
                    backgroundTexture.hideFlags = HideFlags.DontSave;
                    controllerCustomEventStyle.normal.background = backgroundTexture;
                }

                return controllerCustomEventStyle;
            }
        }

        private static GUIStyle controllerCustomEventStyle;

        #region ### Buttons ###

        public static GUIStyle BorderlessButton
        {
            get
            {
                if (mBorderlessButton == null)
                {
                    mBorderlessButton = new GUIStyle(GUI.skin.label);
                    mBorderlessButton.padding = new RectOffset(
                        -1,
                        3,
                        -1,
                        -1
                    );
                    mBorderlessButton.imagePosition = ImagePosition.ImageOnly;
                }

                return mBorderlessButton;
            }
        }

        private static GUIStyle mBorderlessButton;

        public static GUIStyle SmallButton
        {
            get
            {
                if (mSmallButton == null)
                {
                    mSmallButton = new GUIStyle(EditorStyles.miniButton);
                    mSmallButton.margin = new RectOffset(
                        0,
                        0,
                        0,
                        0
                    );
                    mSmallButton.padding = new RectOffset(
                        1,
                        1,
                        -1,
                        -1
                    );
                    //mSmallButton.imagePosition = ImagePosition.ImageOnly;
                }

                return mSmallButton;
            }
        }

        private static GUIStyle mSmallButton;

        public static GUIStyle ImageButton
        {
            get
            {
                if (mImageButton == null)
                {
                    mImageButton = new GUIStyle(GUI.skin.button);
                    mImageButton.padding = new RectOffset(
                        -1,
                        -1,
                        -1,
                        -1
                    );
                    mImageButton.imagePosition = ImagePosition.ImageOnly;
                }

                return mImageButton;
            }
        }

        private static GUIStyle mImageButton;

        #endregion

        #region ### Misc ###

        public static GUIStyle Foldout
        {
            get
            {
                if (mFoldout == null)
                {
                    mFoldout = new GUIStyle(EditorStyles.foldout);
                    mFoldout.fontStyle = FontStyle.Bold;
                    mFoldout.margin.top += 2;
                    mFoldout.margin.bottom += 4;
                }

                return mFoldout;
            }
        }

        private static GUIStyle mFoldout;

        public static GUIStyle HelpBox
        {
            get
            {
                if (mHelpBox == null)
                {
                    mHelpBox = new GUIStyle(GUI.skin.GetStyle("HelpBox"));
                    mHelpBox.richText = true;
                }

                return mHelpBox;
            }
        }

        private static GUIStyle mHelpBox;

        public static GUIStyle Toolbar
        {
            get
            {
                if (mToolbar == null)
                {
                    mToolbar = new GUIStyle(EditorStyles.toolbar);
                    mToolbar.fixedHeight = 0;
                    mToolbar.padding = new RectOffset(
                        6,
                        6,
                        4,
                        4
                    );
                }

                return mToolbar;
            }
        }

        public static GUIStyle mToolbar;

        public static GUIStyle ModuleHighlight
        {
            get
            {
                if (moduleHighlight == null)
                {
                    moduleHighlight = new GUIStyle();
                    moduleHighlight.normal.background = EditorGUIUtility.whiteTexture;
                    //moduleHighlight.border = new RectOffset(
                    //    1,
                    //    1,
                    //    1,
                    //    1
                    //);
                    //moduleHighlight.padding = new RectOffset(
                    //    1,
                    //    1,
                    //    1,
                    //    1
                    //);
                    //moduleHighlight.margin = new RectOffset(
                    //    1,
                    //    1,
                    //    1,
                    //    1
                    //);
                }

                return moduleHighlight;
            }
        }

        private static GUIStyle moduleHighlight;

        //public static GUIStyle RoundRectangle
        //{
        //    get
        //    {
        //        if (mRoundRectangle == null)
        //        {
        //            mRoundRectangle = new GUIStyle();
        //            mRoundRectangle.normal.background = CurvyResource.Load("roundrectangle,16,16");
        //            mRoundRectangle.border = new RectOffset(
        //                3,
        //                3,
        //                3,
        //                3

        //            );
        //            //mRoundRectangle.overflow = new RectOffset(
        //            //    1,
        //            //    0,
        //            //    0,
        //            //    1
        //            //);
        //        }

        //        return mRoundRectangle;
        //    }
        //}

        //private static GUIStyle mRoundRectangle;

        //public static GUIStyle ToolbarItem
        //{
        //    get
        //    {
        //        if (mToolbarItem == null)
        //        {
        //            mToolbarItem = new GUIStyle(GUI.skin.button);
        //            mToolbarItem.alignment = TextAnchor.MiddleLeft;
        //            mToolbarItem.padding.top = 4;
        //            mToolbarItem.padding.bottom = 2;
        //        }

        //        return mToolbarItem;
        //    }
        //}

        //private static GUIStyle mToolbarItem;

        #endregion

        #region ### CG Colors ###

        public static Color IOnRequestProcessingTitleColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return new Color(
                        0.2f,
                        0.7f,
                        0.2f
                    );

                return new Color(
                    0.1f,
                    0.5f,
                    0.1f
                );
            }
        }

        public static Color IOnRequestProcessingSlotColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return new Color(
                        0.2f,
                        0.7f,
                        0.2f
                    );

                return new Color(
                    0.1f,
                    0.5f,
                    0.1f
                );
            }
        }

        #endregion

        #region ### CG Module Window ###

        private static GUIStyle moduleLOD1LabelStyle;

        public static GUIStyle GetModuleLOD1LabelStyle(
            bool isSmallModule)
        {
            if (moduleLOD1LabelStyle == null)
            {
                moduleLOD1LabelStyle = new GUIStyle(GUI.skin.label);
                moduleLOD1LabelStyle.alignment = TextAnchor.MiddleCenter;
                moduleLOD1LabelStyle.richText = true;
                moduleLOD1LabelStyle.wordWrap = true;
                moduleLOD1LabelStyle.margin = new RectOffset(
                    0,
                    0,
                    0,
                    0
                );

                int horizontalPadding = CGGraph.ConnectorWidth;
                int verticalPadding = 4;
                moduleLOD1LabelStyle.padding = new RectOffset(
                    horizontalPadding,
                    horizontalPadding,
                    verticalPadding,
                    verticalPadding
                );
            }

            moduleLOD1LabelStyle.fontSize = isSmallModule
                ? 26
                : 38;

            return moduleLOD1LabelStyle;
        }


        public static int ModuleWindowTitleHeight = 26;

        public static GUIStyle ModuleWindow
        {
            get
            {
                if (mModuleWindow == null)
                {
                    mModuleWindow = new GUIStyle(GUI.skin.window);
//                    mModuleWindow.normal.background = TexModuleWindow;
//                    mModuleWindow.onNormal.background = TexModuleWindow1;
//                    mModuleWindow.border = new RectOffset(
//                        10,
//                        12,
//                        24,
//                        13
//                    );
//                    mModuleWindow.padding = new RectOffset(
//                        0,
//                        0,
//                        24,
//                        6
//                    );
//                    mModuleWindow.contentOffset = new Vector2(
//                        0,
//                        -18
//                    );
//#pragma warning disable 162
//                    mModuleWindow.overflow = new RectOffset(
//                        10,
//                        11,
//                        8,
//                        11
//                    );
//#pragma warning restore 162
                    mModuleWindow.richText = true;

                    //make the window look the same regardless of whether it is selected or not
                    mModuleWindow.onNormal = mModuleWindow.onActive;
                }

                return mModuleWindow;
            }
        }

        private static GUIStyle mModuleWindow;

        public static GUIStyle ModuleWindowSlotBackground
        {
            get
            {
                if (mModuleWindowSlotBackground == null)
                {
                    mModuleWindowSlotBackground = new GUIStyle(GUI.skin.box);
                    mModuleWindowSlotBackground.padding = new RectOffset(
                        1,
                        1,
                        1,
                        1
                    );
                    mModuleWindowSlotBackground.margin = new RectOffset(
                        1,
                        1,
                        0,
                        0
                    );
                }

                return mModuleWindowSlotBackground;
            }
        }

        private static GUIStyle mModuleWindowSlotBackground;

        public static GUIStyle ModuleWindowBackground
        {
            get
            {
                if (mModuleWindowBackground == null)
                {
                    mModuleWindowBackground = new GUIStyle(GUI.skin.box);
                    mModuleWindowBackground.padding = new RectOffset(
                        1,
                        1,
                        1,
                        1
                    );
                    mModuleWindowBackground.margin = new RectOffset(
                        1,
                        1,
                        5,
                        0
                    );
                }

                return mModuleWindowBackground;
            }
        }

        private static GUIStyle mModuleWindowBackground;


        public static Texture2D HelpTexture
        {
            get
            {
                if (mHelpTexture == null)
                    // mHelpTexture=(Texture2D)EditorGUIUtility.Load("icons/_Help.png");
                    // mHelpTexture = CurvyResource.Load("help12,12,12");
                    mHelpTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "help12",
                            12,
                            12
                        )
                    );
                return mHelpTexture;
            }
        }

        private static Texture2D mHelpTexture;

        public static Texture2D EditTexture
        {
            get
            {
                if (mEditTexture == null)
                    // mEditTexture = CurvyResource.Load("editsmall,12,12");
                    mEditTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "editsmall",
                            12,
                            12
                        )
                    );
                return mEditTexture;
            }
        }

        private static Texture2D mEditTexture;


        public static GUIStyle GlowBox
        {
            get
            {
                if (mGlowBox == null)
                {
                    mGlowBox = new GUIStyle();
                    mGlowBox.normal.background = CurvyResource.Load("glowbox,26,26");
                    mGlowBox.border = new RectOffset(
                        11,
                        11,
                        11,
                        11
                    );
                    mGlowBox.overflow = new RectOffset(
                        1,
                        0,
                        0,
                        1
                    );
                }

                return mGlowBox;
            }
        }

        private static GUIStyle mGlowBox;


        public static GUIStyle ShowDetailsButton
        {
            get
            {
                if (showDetailsButton == null)
                {
                    showDetailsButton = new GUIStyle(EditorStyles.toolbarButton);
                    showDetailsButton.margin.left = 5;
                    showDetailsButton.margin.right = 1;
                }

                return showDetailsButton;
            }
        }

        private static GUIStyle showDetailsButton;


        public static GUIStyle ModuleRenameStyle
        {
            get
            {
                if (moduleRenameStyle == null)
                {
                    moduleRenameStyle = new GUIStyle(GUI.skin.textField);
                    moduleRenameStyle.alignment = TextAnchor.MiddleCenter;
                }

                return moduleRenameStyle;
            }
        }

        private static GUIStyle moduleRenameStyle;


        #endregion

        #region ### CG Slots ###

        private static GUIStyle mSlot;

        public static GUIStyle Slot
        {
            get
            {
                if (mSlot == null)
                {
                    mSlot = new GUIStyle();
                    mSlot.normal.background = EditorGUIUtility.whiteTexture;
                    mSlot.fixedHeight = 17;
                    mSlot.fixedWidth = 17;
                    mSlot.normal.textColor = new Color(
                        0,
                        0,
                        0,
                        0.6f
                    );
                    mSlot.alignment = TextAnchor.MiddleCenter;
                    mSlot.contentOffset = new Vector2(
                        -1f,
                        -1f
                    );
                }

                return mSlot;
            }
        }

        //private static GUIStyle optionalSlot;

        //public static GUIStyle OptionalSlot
        //{
        //    get
        //    {
        //        if (optionalSlot == null)
        //        {
        //            optionalSlot = new GUIStyle();

        //            optionalSlot.normal.background = MakeDashedTexture(Color.white, new Color(1, 1, 1, .55f), 2);
        //            optionalSlot.fixedHeight = 17;
        //            optionalSlot.fixedWidth = 17;
        //            optionalSlot.normal.textColor = new Color(
        //                0,
        //                0,
        //                0,
        //                0.6f
        //            );
        //            optionalSlot.alignment = TextAnchor.MiddleCenter;
        //            optionalSlot.contentOffset = new Vector2(
        //                -1f,
        //                -1f
        //            );
        //        }

        //        return optionalSlot;
        //    }
        //}

        //private static Texture2D MakeDashedTexture(Color color1, Color color2, int repeat)
        //{
        //    int width = repeat;
        //    int height = width;
        //    Texture2D texture = new Texture2D(width, height);
        //    Color[] pixels = new Color[width * height];

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            bool isOddLine = y % 2 == 1;
        //            int oddLineModificator = isOddLine
        //                ? 1
        //                : 0;
        //            bool colorSelector = (x + oddLineModificator) % 2 == 0;
        //            pixels[y * width + x] = colorSelector ? color1 : color2;
        //        }
        //    }


        //    texture.SetPixels(pixels);
        //    texture.filterMode = FilterMode.Point;
        //    texture.wrapMode = TextureWrapMode.Clamp;
        //    texture.Apply();


        //    return texture;
        //}

        private static GUIStyle slotLable;

        public static GUIStyle GetSlotLabelStyle(
            [NotNull] CGModuleSlot slot,
            bool isSlotDisabled = false)
        {
            if (slotLable == null)
            {
                slotLable = new GUIStyle();
                slotLable.fixedHeight = 18;
                slotLable.padding.bottom = 3;
                slotLable.margin.left = 2;
                slotLable.margin.right = 2;
            }

            ConfigureSlotLabelStyle(
                slot,
                isSlotDisabled,
                slotLable
            );

            return slotLable;
        }

        private static void ConfigureSlotLabelStyle(
            [NotNull] CGModuleSlot slot,
            bool isSlotDisabled,
            [NotNull] GUIStyle style)
        {
            CGModuleInputSlot inputSlot = slot as CGModuleInputSlot;
            bool isSlotOptional = inputSlot != null && inputSlot.InputInfo != null && inputSlot.InputInfo.Optional;


            // Linked Slots => Bold
            // Optional => Italic
            if (slot.IsLinked)
                style.fontStyle = isSlotOptional
                    ? FontStyle.BoldAndItalic
                    : FontStyle.Bold;
            else
                style.fontStyle = isSlotOptional
                    ? FontStyle.Italic
                    : FontStyle.Normal;

            // OnRequestProcessing => Green
            // Disabled (incompatible with link drag) => Gray
            if (isSlotDisabled)
                style.normal.textColor = Color.gray;
            else if (slot.IsOnRequest)
                style.normal.textColor = IOnRequestProcessingSlotColor.SkinAwareColor();
            else
                style.normal.textColor = new Color(
                    1,
                    1,
                    1,
                    0.6f
                ).SkinAwareColor();

            style.alignment = inputSlot
                ? TextAnchor.MiddleLeft
                : TextAnchor.MiddleRight;
        }

        #endregion

        #region ### Textures ###

        public static string GetTextureFilename(
            string name,
            int width,
            int height,
            string darkskinPostfix = "_dark",
            string ligthskinPostfix = "_light")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append(
                EditorGUIUtility.isProSkin
                    ? darkskinPostfix
                    : ligthskinPostfix
            );
            sb.Append(",");
            sb.Append(width);
            sb.Append(",");
            sb.Append(height);

            string filename = sb.ToString();
            return filename;
        }

        //public static Texture2D TexModuleWindow
        //{
        //    get
        //    {
        //        if (mTexModuleWindow == null)
        //            mTexModuleWindow = CurvyResource.Load(
        //                EditorGUIUtility.isProSkin
        //                    ? "cgwindowdark,64,64"
        //                    : "cgwindowlight,64,64"
        //            );

        //        return mTexModuleWindow;
        //    }
        //}

        //private static Texture2D mTexModuleWindow;

        //public static Texture2D TexModuleWindow1
        //{
        //    get
        //    {
        //        if (mTexModuleWindow1 == null)
        //            mTexModuleWindow1 = CurvyResource.Load(
        //                EditorGUIUtility.isProSkin
        //                    ? "cgwindowdark1,64,64"
        //                    : "cgwindowlight1,64,64"
        //            );

        //        return mTexModuleWindow1;
        //    }
        //}

        //private static Texture2D mTexModuleWindow1;

        public static Texture2D TexGridSnap
        {
            get
            {
                if (mTexGridSnap == null)
                    mTexGridSnap = CurvyResource.Load(
                        GetTextureFilename(
                            "cggridstep",
                            16,
                            16
                        )
                    );

                return mTexGridSnap;
            }
        }

        private static Texture2D mTexGridSnap;

        public static Texture2D TexPlay
        {
            get
            {
                if (mTexPlay == null)
                    mTexPlay = CurvyResource.Load("play,24,24");

                return mTexPlay;
            }
        }

        private static Texture2D mTexPlay;

        public static Texture2D TexStop
        {
            get
            {
                if (mTexStop == null)
                    mTexStop = CurvyResource.Load("stop,24,24");

                return mTexStop;
            }
        }

        private static Texture2D mTexStop;

        public static Texture2D TexLogoBig
        {
            get
            {
                if (mTexLogoBig == null)
                    mTexLogoBig = CurvyResource.Load(
                        GetTextureFilename(
                            "curvylogo",
                            436,
                            160
                        )
                    );

                return mTexLogoBig;
            }
        }

        private static Texture2D mTexLogoBig;

        public static Texture2D TexLogoSmall
        {
            get
            {
                if (mTexLogoSmall == null)
                    mTexLogoSmall = CurvyResource.Load(
                        GetTextureFilename(
                            "curvylogo_small",
                            178,
                            124
                        )
                    );

                return mTexLogoSmall;
            }
        }

        private static Texture2D mTexLogoSmall;

        public static Texture2D TexConnection
        {
            get
            {
                if (mTexConnection == null)
                    mTexConnection = CurvyResource.Load(
                        GetTextureFilename(
                            "connection",
                            24,
                            24
                        )
                    );

                return mTexConnection;
            }
        }

        private static Texture2D mTexConnection;

        public static Texture2D TexConnectionPos
        {
            get
            {
                if (mTexConnectionPos == null)
                    mTexConnectionPos = CurvyResource.Load(
                        GetTextureFilename(
                            "connectionpos",
                            24,
                            24
                        )
                    );

                return mTexConnectionPos;
            }
        }

        private static Texture2D mTexConnectionPos;

        public static Texture2D TexConnectionRot
        {
            get
            {
                if (mTexConnectionRot == null)
                    mTexConnectionRot = CurvyResource.Load(
                        GetTextureFilename(
                            "connectionrot",
                            24,
                            24
                        )
                    );

                return mTexConnectionRot;
            }
        }

        private static Texture2D mTexConnectionRot;

        public static Texture2D TexConnectionFull
        {
            get
            {
                if (mTexConnectionFull == null)
                    mTexConnectionFull = CurvyResource.Load(
                        GetTextureFilename(
                            "connectionfull",
                            24,
                            24
                        )
                    );

                return mTexConnectionFull;
            }
        }

        private static Texture2D mTexConnectionFull;

        public static Texture2D HierarchyConnectionTexture
        {
            get
            {
                if (mHierarchyConnectionTexture == null)
                    mHierarchyConnectionTexture = CurvyResource.Load("connectionsmall,12,12");

                return mHierarchyConnectionTexture;
            }
        }

        private static Texture2D mHierarchyConnectionTexture;

        public static Texture2D HierarchyAnchorTexture
        {
            get
            {
                if (hierarchyAnchorTexture == null)
                    hierarchyAnchorTexture = CurvyResource.Load("anchorsmall,12,12");
                return hierarchyAnchorTexture;
            }
        }

        private static Texture2D hierarchyAnchorTexture;

        public static Texture2D RndSeedTexture
        {
            get
            {
                if (mRndSeedTexture == null)
                    mRndSeedTexture = CurvyResource.Load("rndseed,12,12");

                return mRndSeedTexture;
            }
        }

        private static Texture2D mRndSeedTexture;

        public static Texture2D DeleteTexture
        {
            get
            {
                if (mDeleteTexture == null)
                    mDeleteTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "delete16",
                            16,
                            16
                        )
                    );

                return mDeleteTexture;
            }
        }

        private static Texture2D mDeleteTexture;

        public static Texture2D DeleteBTexture
        {
            get
            {
                if (deleteBTexture == null)
                    deleteBTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "deleteB16",
                            16,
                            16
                        )
                    );

                return deleteBTexture;
            }
        }

        private static Texture2D deleteBTexture;

        public static Texture2D SaveResourcesTexture
        {
            get
            {
                if (saveResourcesTexture == null)
                    saveResourcesTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "save_resources",
                            16,
                            16
                        )
                    );

                return saveResourcesTexture;
            }
        }

        private static Texture2D saveResourcesTexture;

        public static Texture2D RefreshTexture
        {
            get
            {
                if (mRefreshTexture == null)
                    mRefreshTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "reload",
                            16,
                            16
                        )
                    );

                return mRefreshTexture;
            }
        }

        private static Texture2D mRefreshTexture;

        public static Texture2D ReorderTexture
        {
            get
            {
                if (mReorderTexture == null)
                    mReorderTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "reorder",
                            16,
                            16
                        )
                    );

                return mReorderTexture;
            }
        }

        private static Texture2D mReorderTexture;

        public static Texture2D CGAutoFoldTexture
        {
            get
            {
                if (mCGAutoFoldTexture == null)
                    mCGAutoFoldTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "autofold",
                            16,
                            16
                        )
                    );

                return mCGAutoFoldTexture;
            }
        }

        private static Texture2D mCGAutoFoldTexture;

        public static Texture2D AddTemplateTexture
        {
            get
            {
                if (mAddTemplateTexture == null)
                    mAddTemplateTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "addCGTemplate",
                            16,
                            16
                        )
                    );

                return mAddTemplateTexture;
            }
        }

        private static Texture2D mAddTemplateTexture;

        public static Texture2D DebugTexture
        {
            get
            {
                if (mDebugTexture == null)
                    mDebugTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "debug",
                            16,
                            16
                        )
                    );

                return mDebugTexture;
            }
        }

        private static Texture2D mDebugTexture;

        public static Texture2D DebugSceneViewTexture => EditorGUIUtility.isProSkin
            ? EditorGUIUtility.IconContent("d_UnityEditor.SceneView").image as Texture2D
            : EditorGUIUtility.IconContent("UnityEditor.SceneView").image as Texture2D;

        public static Texture2D LineTexture
        {
            get
            {
                if (mLineTexture == null)
                {
                    mLineTexture = new Texture2D(
                        1,
                        2
                    );
                    Color c = Color.white; //.SkinAwareColor();
                    Color ca = new Color(
                        c.r,
                        c.g,
                        c.b,
                        0
                    );
                    mLineTexture.SetPixels(new[] { ca, c });
                    mLineTexture.Apply();
                    mLineTexture.hideFlags = HideFlags.DontSave;
                }

                return mLineTexture;
            }
        }

        private static Texture2D mLineTexture;

        public static Texture2D RequestLineTexture
        {
            get
            {
                if (mRequestLineTexture == null)
                {
                    mRequestLineTexture = new Texture2D(
                        2,
                        2
                    );
                    Color c = Color.white;
                    Color ca = new Color(
                        c.r,
                        c.g,
                        c.b,
                        0
                    );
                    mRequestLineTexture.SetPixels(new[] { ca, Color.black, c, Color.black });
                    mRequestLineTexture.Apply();
                    mRequestLineTexture.hideFlags = HideFlags.DontSave;
                }

                return mRequestLineTexture;
            }
        }

        private static Texture2D mRequestLineTexture;

        public static Texture2D InspectorTexture =>
            EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image as Texture2D;

        public static Texture2D ExpandTexture
        {
            get
            {
                if (mExpandTexture == null)
                    mExpandTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "expand16",
                            16,
                            16
                        )
                    );

                return mExpandTexture;
            }
        }

        private static Texture2D mExpandTexture;

        public static Texture2D SynchronizeTexture
        {
            get
            {
                if (mSynchronizeTexture == null)
                    mSynchronizeTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "synchronize",
                            16,
                            16
                        )
                    );

                return mSynchronizeTexture;
            }
        }

        private static Texture2D mSynchronizeTexture;

        public static Texture2D CollapseTexture
        {
            get
            {
                if (mCollapseTexture == null)
                    mCollapseTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "collapse16",
                            16,
                            16
                        )
                    );

                return mCollapseTexture;
            }
        }

        private static Texture2D mCollapseTexture;

        public static Texture2D OpenGraphTexture
        {
            get
            {
                if (mOpenGraphTexture == null)
                    mOpenGraphTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "opengraph",
                            24,
                            24
                        )
                    );

                return mOpenGraphTexture;
            }
        }

        private static Texture2D mOpenGraphTexture;

        public static Texture2D DeleteSmallTexture
        {
            get
            {
                if (mDeleteSmallTexture == null)
                    mDeleteSmallTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "deletesmall",
                            12,
                            12
                        )
                    );

                return mDeleteSmallTexture;
            }
        }

        private static Texture2D mDeleteSmallTexture;

        public static Texture2D ClearSmallTexture
        {
            get
            {
                if (mClearSmallTexture == null)
                    mClearSmallTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "clearsmall",
                            12,
                            12
                        )
                    );

                return mClearSmallTexture;
            }
        }

        private static Texture2D mClearSmallTexture;

        public static Texture2D SelectTexture
        {
            get
            {
                if (mSelectTexture == null)
                    mSelectTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "selectsmall",
                            12,
                            12
                        )
                    );

                return mSelectTexture;
            }
        }

        private static Texture2D mSelectTexture;

        public static Texture2D AddSmallTexture
        {
            get
            {
                if (mAddSmallTexture == null)
                    mAddSmallTexture = CurvyResource.Load(
                        GetTextureFilename(
                            "addsmall",
                            12,
                            12
                        )
                    );

                return mAddSmallTexture;
            }
        }

        private static Texture2D mAddSmallTexture;

        #region --- Toolbar Icons ---

        public static Texture2D IconPrefs
        {
            get
            {
                if (mIconPrefs == null)
                    mIconPrefs = CurvyResource.Load("prefs,24,24");

                return mIconPrefs;
            }
        }

        private static Texture2D mIconPrefs;

        public static Texture2D IconAbout
        {
            get
            {
                if (mIconAbout == null)
                    mIconAbout = CurvyResource.Load("about,24,24");

                return mIconAbout;
            }
        }

        private static Texture2D mIconAbout;

        public static Texture2D IconAsmdef
        {
            get
            {
                if (mIconAsmdef == null)
                    mIconAsmdef = CurvyResource.Load("asmdef,24,24");

                return mIconAsmdef;
            }
        }

        private static Texture2D mIconAsmdef;

        public static Texture2D IconHelp
        {
            get
            {
                if (mIconHelp == null)
                    mIconHelp = CurvyResource.Load("help,24,24");

                return mIconHelp;
            }
        }

        private static Texture2D mIconHelp;

        public static Texture2D IconWWW
        {
            get
            {
                if (mIconWWW == null)
                    mIconWWW = CurvyResource.Load("web,24,24");

                return mIconWWW;
            }
        }

        private static Texture2D mIconWWW;

        public static Texture2D IconBugReporter
        {
            get
            {
                if (mIconBugReporter == null)
                    mIconBugReporter = CurvyResource.Load("bugreport,24,24");

                return mIconBugReporter;
            }
        }

        private static Texture2D mIconBugReporter;

        public static Texture2D IconNewShape
        {
            get
            {
                if (mIconNewShape == null)
                    mIconNewShape = CurvyResource.Load("shapewizard,24,24");

                return mIconNewShape;
            }
        }

        private static Texture2D mIconNewShape;

        public static Texture2D IconNewGroup
        {
            get
            {
                if (mIconNewGroup == null)
                    mIconNewGroup = CurvyResource.Load("group,24,24");

                return mIconNewGroup;
            }
        }

        private static Texture2D mIconNewGroup;

        public static Texture2D IconNewCG
        {
            get
            {
                if (mIconNewCG == null)
                    mIconNewCG = CurvyResource.Load(
                        GetTextureFilename(
                            "opengraph",
                            24,
                            24
                        )
                    );

                return mIconNewCG;
            }
        }

        private static Texture2D mIconNewCG;


        public static Texture2D IconCP
        {
            get
            {
                if (mIconCP == null)
                    mIconCP = CurvyResource.Load("singlecp,24,24");

                return mIconCP;
            }
        }

        private static Texture2D mIconCP;

        public static Texture2D IconCPOff
        {
            get
            {
                if (mIconCPOff == null)
                    mIconCPOff = CurvyResource.Load("singlecp_off,24,24");

                return mIconCPOff;
            }
        }

        private static Texture2D mIconCPOff;

        public static Texture2D IconRaycast
        {
            get
            {
                if (mIconRaycast == null)
                    mIconRaycast = CurvyResource.Load("raycast,24,24");

                return mIconRaycast;
            }
        }

        private static Texture2D mIconRaycast;

        public static Texture2D IconRaycastOff
        {
            get
            {
                if (mIconRaycastOff == null)
                    mIconRaycastOff = CurvyResource.Load("raycast_off,24,24");

                return mIconRaycastOff;
            }
        }

        private static Texture2D mIconRaycastOff;

        public static Texture2D IconSubdivide
        {
            get
            {
                if (mIconSubdivide == null)
                    mIconSubdivide = CurvyResource.Load("subdivide,24,24");

                return mIconSubdivide;
            }
        }

        private static Texture2D mIconSubdivide;

        public static Texture2D IconSimplify
        {
            get
            {
                if (mIconSimplify == null)
                    mIconSimplify = CurvyResource.Load("simplify,24,24");

                return mIconSimplify;
            }
        }

        private static Texture2D mIconSimplify;

        public static Texture2D IconEqualize
        {
            get
            {
                if (mIconEqualize == null)
                    mIconEqualize = CurvyResource.Load("equalize,24,24");

                return mIconEqualize;
            }
        }

        private static Texture2D mIconEqualize;

        public static Texture2D IconMeshExport
        {
            get
            {
                if (mIconMeshExport == null)
                    mIconMeshExport = CurvyResource.Load("exportmesh,24,24");

                return mIconMeshExport;
            }
        }

        private static Texture2D mIconMeshExport;

        public static Texture2D IconSyncFromHierarchy
        {
            get
            {
                if (mIconSyncFromHierarchy == null)
                    mIconSyncFromHierarchy = CurvyResource.Load("syncfromhierarchy,24,24");

                return mIconSyncFromHierarchy;
            }
        }

        private static Texture2D mIconSyncFromHierarchy;

        public static Texture2D IconSelectContainingConnections
        {
            get
            {
                if (mIconSelectContainingConnections == null)
                    mIconSelectContainingConnections = CurvyResource.Load("containingcon,24,24");

                return mIconSelectContainingConnections;
            }
        }

        private static Texture2D mIconSelectContainingConnections;

        public static Texture2D IconAxisXY
        {
            get
            {
                if (mIconAxisXY == null)
                    mIconAxisXY = CurvyResource.Load("axisxy,24,24");

                return mIconAxisXY;
            }
        }

        private static Texture2D mIconAxisXY;

        public static Texture2D IconAxisXZ
        {
            get
            {
                if (mIconAxisXZ == null)
                    mIconAxisXZ = CurvyResource.Load("axisxz,24,24");

                return mIconAxisXZ;
            }
        }

        private static Texture2D mIconAxisXZ;

        public static Texture2D IconAxisYZ
        {
            get
            {
                if (mIconAxisYZ == null)
                    mIconAxisYZ = CurvyResource.Load("axisyz,24,24");

                return mIconAxisYZ;
            }
        }

        private static Texture2D mIconAxisYZ;

        public static Texture2D IconView
        {
            get
            {
                if (iconView == null)
                    iconView = CurvyResource.Load("view,24,24");

                return iconView;
            }
        }

        private static Texture2D iconView;

        #endregion

        #endregion
    }
}