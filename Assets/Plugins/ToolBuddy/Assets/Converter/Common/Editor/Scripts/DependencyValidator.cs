// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES == false
using ToolBuddy.Converter.Editor.Common.Messages;
using ToolBuddy.Converter.Editor.Common.Models;
#endif
using UnityEditor;

namespace ToolBuddy.Converter.Editor.Common
{
    [InitializeOnLoad]
    public static class DependencyValidator
    {
        static DependencyValidator()
        {
#if CURVY_SPLINES == false
            MessageRepository.Instance.AddMessage(
                "You need Curvy Splines version 8.9.1 or above to use Converter. You can get it using the button below.\nPlease restart Unity once the import completed.",
                MessageType.Error
            );

            //Show message window at each attempted interaction
            ConversionModel.Instance.Changed += () =>
            {
                EditorWindow.GetWindow<MessageWindow>(
                    $"{AssetsCommonInformation.CommonName} Messages",
                    true
                );
            };
#endif
        }
    }
}