// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using ToolBuddy.Converter.Editor.Common.Models;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace ToolBuddy.Converter.Editor.Common
{
    public static class Shortcuts
    {
        //set default shortcuts based on qwerty and action names
        private const string ShortcutCatergoryPrefix =
            AssetsCommonInformation.Publisher + "/" + AssetsCommonInformation.CommonName + "/";

        [Shortcut(
            ShortcutCatergoryPrefix + "Toggle Convert All",
            null,
            KeyCode.E,
            ShortcutModifiers.Control
        )]
        [UsedImplicitly]
        private static void ToggleConvertAll(
            ShortcutArguments args)
        {
            ConversionModel model = ConversionModel.Instance;
            model.ConversionSource = model.ConversionSource == ConversionSource.AllSplines
                ? ConversionSource.None
                : ConversionSource.AllSplines;
        }

        [Shortcut(
            ShortcutCatergoryPrefix + "Toggle Convert Selected",
            null,
            KeyCode.T,
            ShortcutModifiers.Control
        )]
        [UsedImplicitly]
        private static void ToggleConvertSelected(
            ShortcutArguments args)
        {
            ConversionModel model = ConversionModel.Instance;
            model.ConversionSource = model.ConversionSource == ConversionSource.SelectedSplines
                ? ConversionSource.None
                : ConversionSource.SelectedSplines;
        }

        [Shortcut(
            ShortcutCatergoryPrefix + "Set Convert All"
        )]
        [UsedImplicitly]
        private static void SetConvertAll(
            ShortcutArguments args) =>
            MenuItems.SetConvertAll();

        [Shortcut(
            ShortcutCatergoryPrefix + "Set Convert Selected"
        )]
        [UsedImplicitly]
        private static void SetConvertSelected(
            ShortcutArguments args) =>
            MenuItems.SetConvertSelected();

        [Shortcut(
            ShortcutCatergoryPrefix + "Set Convert None"
        )]
        [UsedImplicitly]
        private static void SetConvertNone(
            ShortcutArguments args) =>
            MenuItems.SetConvertNone();
    }
}