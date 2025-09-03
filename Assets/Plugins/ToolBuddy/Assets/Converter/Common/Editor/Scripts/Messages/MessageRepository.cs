using System;
using System.Collections.Generic;
using UnityEditor;

// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace ToolBuddy.Converter.Editor.Common.Messages
{
    public class MessageRepository
    {
        private static MessageRepository instance;

        private readonly Queue<(string, MessageType)> messages = new Queue<(string, MessageType)>();

        public static MessageRepository Instance =>
            instance ??= new MessageRepository();

        public IEnumerable<(string, MessageType)> Messages =>
            messages;

        public event Action MessageAdded = delegate { };

        public void Clear() =>
            messages.Clear();

        public void AddMessage(
            string message,
            MessageType messageType)
        {
            //maintain a maximum of 999 messages
            if (messages.Count >= 999)
                messages.Dequeue();

            messages.Enqueue(($"[{DateTime.Now:HH:mm:ss}] {message}", messageType));
            MessageAdded();
        }
    }
}