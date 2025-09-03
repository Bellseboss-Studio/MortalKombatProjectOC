// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEditor;

//todo should this namespace be merged with others? For example to move this class to the Controllers folder/namespace.
namespace ToolBuddy.Converter.Editor.Common.Messages
{
    [InitializeOnLoad]
    public static class MessageWindowController
    {
        static MessageWindowController()
        {
            MessageRepository.Instance.MessageAdded += () =>
            {
                MessageWindow instance = EditorWindow.GetWindow<MessageWindow>(
                    $"{AssetsCommonInformation.CommonName} Messages",
                    false
                );
                instance.Repaint();
            };
        }
    }
}