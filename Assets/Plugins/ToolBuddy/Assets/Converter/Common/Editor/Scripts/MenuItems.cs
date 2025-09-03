// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using ToolBuddy.Converter.Editor.Common.Messages;
using ToolBuddy.Converter.Editor.Common.Models;
using UnityEditor;
using UnityEngine;

namespace ToolBuddy.Converter.Editor.Common
{
    /// <summary>
    /// Provides menu items for converting splines within the Unity Editor.
    /// </summary>
    public static class MenuItems
    {
        private const string PublisherItemsPath = "Tools/" + AssetsCommonInformation.Publisher;
        private const string AssetItemPath = PublisherItemsPath + "/" + AssetsCommonInformation.CommonName + "/";

        /// <summary>
        /// Converts all Unity spline containers found in the scene to Curvy Splines.
        /// </summary>
        [MenuItem(
            AssetItemPath + "Set Convert All",
            priority = 0
        )]
        public static void SetConvertAll() =>
            ConversionModel.Instance.ConversionSource = ConversionSource.AllSplines;


        /// <summary>
        /// Converts the Unity spline containers selected in the scene to Curvy Splines.
        /// </summary>
        [MenuItem(
            AssetItemPath + "Set Convert Selected",
            priority = 1
        )]
        public static void SetConvertSelected() =>
            ConversionModel.Instance.ConversionSource = ConversionSource.SelectedSplines;

        /// <summary>
        ///  Converts no Unity spline containers to Curvy Splines.
        ///  </summary>
        [MenuItem(
            AssetItemPath + "Set Convert None",
            priority = 2
        )]
        public static void SetConvertNone() =>
            ConversionModel.Instance.ConversionSource = ConversionSource.None;

        [MenuItem(
            "Window/" + AssetsCommonInformation.CommonName + "/" + "Messages"
        )]
        public static void OpenMessagesWindow() =>
            EditorWindow.GetWindow<MessageWindow>(
                $"{AssetsCommonInformation.CommonName} Messages",
                true
            );

        [MenuItem(
            PublisherItemsPath + "/Publisher Page"
        )]
        public static void OpenPublisherPage() =>
            Application.OpenURL(AssetsCommonInformation.PublisherURL);
    }
}