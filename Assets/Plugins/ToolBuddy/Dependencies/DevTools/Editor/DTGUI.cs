// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FluffyUnderware.DevToolsEditor
{
    public static class DTGUI
    {
        private static readonly Stack<Color> _BGColorStack = new Stack<Color>();
        private static readonly Stack<Color> _ColorStack = new Stack<Color>();
        private static readonly Stack<Color> _ContentColorStack = new Stack<Color>();
        

        #region --- Colors ---

        public static void PushColor(Color col)
        {
            _ColorStack.Push(GUI.color);
            GUI.color = col;
        }

        public static void PopColor() 
        {
            GUI.color = _ColorStack.Pop();
        }

        public static void PushBackgroundColor(Color col)
        {
            _BGColorStack.Push(GUI.color);
            GUI.color = col;
        }

        public static void PopBackgroundColor()
        {
            GUI.color = _BGColorStack.Pop();
        }

        public static void PushContentColor(Color col)
        {
            _ContentColorStack.Push(GUI.contentColor);
            GUI.contentColor = col;
        }

        public static void PopContentColor()
        {
            GUI.contentColor = _ContentColorStack.Pop();
        }

       

        #endregion
        
        #region --- Events ---

        public static bool IsClick => (Event.current!=null && Event.current.type == EventType.MouseDown && Event.current.button == 0);

        public static bool IsContextClick => (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 1);

        public static bool IsLayout => Event.current != null && Event.current.type == EventType.Layout;
        public static bool IsRepaint => Event.current != null && Event.current.type == EventType.Repaint;

        public static Vector2 MousePosition => (Event.current != null) ? Event.current.mousePosition : Vector2.zero;

        public static void UseEvent(int hashcode, Event e)
        {
            int controlID = GUIUtility.GetControlID(hashcode, FocusType.Passive);
            GUIUtility.hotControl = controlID;
            e.Use();
            GUIUtility.hotControl = 0;

        }

        #endregion

        #region --- Controls ---

        public static void HtmlHelpBox(string message, MessageType type = MessageType.None)
        {
            GUIContent c;
            switch (type)
            {
                case MessageType.Info:
                    c = EditorGUIUtility.IconContent("console.infoicon");
                    break;
                case MessageType.Warning:
                    c = EditorGUIUtility.IconContent("console.warningicon");
                    break;
                case MessageType.Error:
                    c = EditorGUIUtility.IconContent("console.erroricon");
                    break;
                default:
                    c = new GUIContent();
                    break;
            }
            c.text=message;
            GUILayout.Label(c, DTStyles.HtmlHelpBox);
        }

        public static Vector2 CompactVector2Field(Rect position, Vector2 value)
        {
            float labelWidth = 13f;

            float num2 = (position.width - 2) / 2f;
            Rect position2 = new Rect(position);
            position2.width = num2;
            float labelWidth2 = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = 0;
            value.x = EditorGUI.FloatField(position2, "X", value.x);
            position2.x += num2 + 2f;
            value.y = EditorGUI.FloatField(position2, "Y", value.y);
            
            EditorGUIUtility.labelWidth = labelWidth2;
            EditorGUI.indentLevel = indentLevel;

            return value;
        }

        public static Vector3 CompactVector3Field(Rect position, Vector3 value)
        {
            float labelWidth = 13f;
            float num2 = (position.width - 4) / 3f;
            Rect position2 = new Rect(position);
            position2.width = num2;
            float labelWidth2 = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = 0;
            value.x = EditorGUI.FloatField(position2, "X", value.x);
            position2.x += num2 + 2f;
            value.y = EditorGUI.FloatField(position2, "Y", value.y);
            position2.x += num2 + 2f;
            value.z = EditorGUI.FloatField(position2, "Z", value.z);

            EditorGUIUtility.labelWidth = labelWidth2;
            EditorGUI.indentLevel = indentLevel;

            return value;
        }

        public static bool LinkButton(Rect position, GUIContent content, ref bool repaint)
        {
            if (position.Contains(MousePosition))
                repaint = true;
            return GUI.Button(position, content, DTStyles.HtmlLinkLabel);
        }

        #endregion

        public static float LabelWidth(string label)
            => LabelWidth(new GUIContent(label));

        public static float LabelWidth(GUIContent label)
            => EditorStyles.label.CalcSize(label).x;

        public static Rect FromTo(this Vector2 start, Vector2 end)
        {
            Rect result = new Rect(
                start.x,
                start.y,
                end.x - start.x,
                end.y - start.y
            );
            if (result.width < 0f)
            {
                result.x += result.width;
                result.width = -result.width;
            }
            if (result.height < 0f)
            {
                result.y += result.height;
                result.height = -result.height;
            }
            return result;
        }
    }

}
