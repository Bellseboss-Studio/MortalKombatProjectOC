// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using ToolBuddy.Converter.Editor.Common.ToolbarElements;
using UnityEditor;
using UnityEditor.Overlays;

namespace ToolBuddy.Converter.Editor.Common.Overlays
{
    [Overlay(
        typeof(EditorWindow),
        AssetsCommonInformation.CommonName,
        true
    )]
    public class OneWayConversionOverlay : ToolbarOverlay
    {
        private OneWayConversionOverlay() : base(
            nameof(ConversionTypeField),
            nameof(ConvertAllToggle),
            nameof(ConvertSelectedToggle)
        ) { }
    }
}