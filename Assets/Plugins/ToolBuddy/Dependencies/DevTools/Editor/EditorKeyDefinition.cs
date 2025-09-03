// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using FluffyUnderware.DevToolsEditor.Extensions;


namespace FluffyUnderware.DevToolsEditor
{
    [Serializable]
    public class EditorKeyBinding : IComparable
    {
        public static bool BindingsEnabled = true;

        public enum MouseButtonEnum
        {
            None = -1,
            Click = 0,
            RClick = 1,
            MClick = 2,
        }

        public string Name;
        public string Description;
        public KeyCode Key;
        public bool Shift;
        public bool Control;
        public bool Alt;
        public bool Command;
        public MouseButtonEnum MouseButton;

        public bool ShowInPrefs = true;
        
        public EditorKeyBinding(string name, string description, KeyCode defKey = KeyCode.None, bool defShift = false, bool defControl = false, bool defAlt = false, bool defCommand = false, MouseButtonEnum defMouseButton = MouseButtonEnum.None)
        {
            Set(name, description, defKey, defShift, defControl, defAlt, defCommand, defMouseButton);
        }

        /// <summary>
        /// Creates a binding from it's serialized string from the user preferences
        /// </summary>
        public EditorKeyBinding(string bindingString)
        {
            Set(bindingString);
        }

        /// <summary>
        /// Set the binding from it's serialized string from the user preferences
        /// </summary>
        public void Set(string bindingString)
        {
            string[] s = bindingString.Split(',');
            if (s.Length < 6) return;
            Name = s[0];
            Key = (KeyCode)Enum.Parse(typeof(KeyCode), s[1]);
            Shift = bool.Parse(s[2]);
            Control = bool.Parse(s[3]);
            Alt = bool.Parse(s[4]);
            Command = bool.Parse(s[5]);
            MouseButton = (MouseButtonEnum)Enum.Parse(typeof(MouseButtonEnum), s[6]);
        }

        public void Set(string name, string description, KeyCode defKey = KeyCode.None, bool defShift = false, bool defControl = false, bool defAlt = false, bool defCommand = false, MouseButtonEnum defMouseButton = MouseButtonEnum.None)
        {
            Name = name;
            Description = description;
            Key = defKey;
            Shift = defShift;
            Control = defControl;
            Alt = defAlt;
            Command = defCommand;
            MouseButton = defMouseButton;
        }
       

        public bool IsTriggered(Event e=null)
        {
            
            if (!BindingsEnabled)
                return false;
            
            if (e == null)
                e = Event.current;
            if ((e.type == EventType.MouseDown && e.button == (int)MouseButton) ||
                (e.type == EventType.KeyDown && Key != KeyCode.None && e.keyCode == Key))
                return (e.shift == Shift &&
                        e.control == Control &&
                        e.alt == Alt &&
                        e.command == Command);
            else
                return false;
        }

        public bool OnPreferencesGUI()
        {
            EditorGUI.BeginChangeCheck();
            Key = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent(Name), Key);
            
            EditorGUILayoutExtension.Horizontal(
                () =>
                {
                    GUILayout.FlexibleSpace();
                    Shift = GUILayout.Toggle(Shift, "Shift");
                    Control = GUILayout.Toggle(Control, "Ctrl");
                    Alt = GUILayout.Toggle(Alt, "Alt");
                    Command = GUILayout.Toggle(Command, "Cmd");
                }
            );

            EditorGUILayoutExtension.Horizontal(
                () =>
                {
                    GUILayout.FlexibleSpace();
                    MouseButton = (MouseButtonEnum)EditorGUILayout.EnumPopup(MouseButton);
                }
            );

            return EditorGUI.EndChangeCheck();
        }

        public string ToTooltipString()
        {
            if (Key == KeyCode.None && MouseButton==MouseButtonEnum.None)
                return string.Empty;

            List<string> s = new List<string>();
            if (Control)
                s.Add("Ctrl");
            if (Alt)
                s.Add("Alt");
            if (Shift)
                s.Add("Shift");
            if (Command)
                s.Add("Cmd");
            
            string res = string.Join("-", s.ToArray());
            if (res.Length > 0)
                res += "-";

            if (Key != KeyCode.None)
                return "(" + res + Key.ToString() + ")";
            else
                return "(" + res + MouseButton.ToString() + ")";
        }

        public string ToPrefsString()
            => Name + "," + ((int)Key).ToString() + "," + Shift.ToString() + "," + Control.ToString() + "," + Alt.ToString() + "," + Command.ToString() + "," + ((int)MouseButton).ToString();

        public int CompareTo(object obj)
            => Name.CompareTo(((EditorKeyBinding)obj).Name);

        public override bool Equals(object obj)
        {
            EditorKeyBinding o = (EditorKeyBinding)obj;
            return (Name == o.Name &&
                    Key == o.Key &&
                    Shift == o.Shift &&
                    Control == o.Control &&
                    Alt == o.Alt &&
                    Command == o.Command &&
                    MouseButton == o.MouseButton);
        }
        public override int GetHashCode()
            => Name.GetHashCode() ^ Key.GetHashCode() ^ Shift.GetHashCode() ^ Control.GetHashCode() ^ Alt.GetHashCode() ^ Command.GetHashCode() ^ MouseButton.GetHashCode();
    }
}
