// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if DREAMTECK_SPLINES == false
using ToolBuddy.Converter.Dreamteck;
using ToolBuddy.Converter.Editor.Common.Messages;
using ToolBuddy.Converter.Editor.Common.Models;
#endif
using UnityEditor;

namespace ToolBuddy.Converter.Editor.Dreamteck
{
    [InitializeOnLoad]
    public static class DependencyValidator
    {
        static DependencyValidator()
        {
#if DREAMTECK_SPLINES == false
            MessageRepository.Instance.AddMessage(
                $"You need Dreamteck Splines to use {AssetInformation.Name}.\nPlease restart Unity once the import completed.",
                MessageType.Error
            );

            //Show message window at each attempted interaction
            ConversionModel.Instance.Changed += () =>
            {
                EditorWindow.GetWindow<MessageWindow>(
                    $"{Common.AssetsCommonInformation.CommonName} Messages",
                    true
                );
            };
#endif
        }
    }
}