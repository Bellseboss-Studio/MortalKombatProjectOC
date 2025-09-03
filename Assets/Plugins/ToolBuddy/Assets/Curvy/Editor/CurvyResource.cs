// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    /// <summary>
    /// Class for loading image resources
    /// </summary>
    public class CurvyResource : DTResource
    {
        private static CurvyResource _Instance;

        public static CurvyResource Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new CurvyResource();
                return _Instance;
            }
        }

        public CurvyResource()
        {
            ResourceDLL = FindResourceDLL("CurvyEditorIcons");
            ResourceNamespace = ""; //Assets.Curvy.Editor.Resources.";
        }

        private const string fallbackPackedString = "missing,16,16";

        public static Texture2D Load(string packedString)
        {
            Texture2D tex = Instance.LoadPacked(packedString);
            if (tex == null)
            {
                DTLog.LogError("Loading texture from packed string failed: " + packedString);
                return Instance.LoadPacked(fallbackPackedString);
            }

            return tex;
        }
    }
}