// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using Object = UnityEngine.Object;
using System;
using System.Collections;



namespace FluffyUnderware.DevToolsEditor.Extensions
{

    public static class ColorEditorExt
    {

        public static Color SkinAwareColor(this Color c, bool subtle = false)
        {
            if (EditorGUIUtility.isProSkin)
                return c;
            if (subtle)
                return Color.Lerp(c, Color.white,
                    0.9f);

            if (c == Color.white)
                return Color.black;
            if (c == Color.black)
                return Color.white;

            float brightness = c.Brightness();
            if (brightness > 0.9)
                return Color.Lerp(c, Color.black, 0.7f);

            return Color.Lerp(c, Color.black, 0.2f);
        }

        public static float Brightness(this Color c)
            => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        public static Color Fade(this Color c, double startTime, double stayTime, float fadeSpeed)
        {
            double d = EditorApplication.timeSinceStartup - startTime;
            if (stayTime == 0 || d < stayTime)
                return c;
            else
                return new Color(c.r, c.g, c.b, 1 - Mathf.Clamp01((float)(d - stayTime) / Mathf.Max(0, 0001f, fadeSpeed)));
        }
    }

    public static class SerializePropertyExt
    {

        public static GUIContent GetContent(this SerializedProperty property, string label, string tooltip)
        {
            if (string.IsNullOrEmpty(label))
                label = property.displayName;
            return new GUIContent(label, tooltip);
        }

        public static List<Attribute> GetAttributes(this SerializedProperty property, params Type[] ofType)
        {
            List<Attribute> res = new List<Attribute>();
            Type objType = property.GetObjectType();
            if (objType != null)
            {
                FieldInfo fi = objType.FieldByName(property.name, true, true);

                if (fi != null)
                {
                    object[] attribs = fi.GetCustomAttributes(true);
                    foreach (Attribute a in attribs)
                    {

                        if (ofType.Length == 0)
                            res.Add(a);
                        else
                        {
                            Type at = a.GetType();
                            foreach (Type t in ofType)
                                if (t.IsAssignableFrom(at))
                                {
                                    res.Add(a);
                                    break;
                                }
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Gets the objec type the property is a children of.
        /// </summary>
        public static Type GetObjectType(this SerializedProperty property)
        {
            Type baseType = property.serializedObject.targetObject.GetType();
            if (property.depth > 0)
            {
                string[] path = property.Path();
                FieldInfo fi;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    fi = baseType.GetField(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi == null)
                        return null;
                    baseType = fi.FieldType;
                }
            }
            return baseType;
        }

        /// <summary>
        /// Gets the field type
        /// </summary>
        public static Type GetFieldType(this SerializedProperty property)
        {
            Type parentType = GetObjectType(property);
            FieldInfo fi = parentType.FieldByName(property.name, true, true);
            if (fi != null)
            {
                Type tt = fi.FieldType;
                if (tt.IsGenericType && tt.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return tt.GetGenericArguments()[0];
                }
                if (fi.FieldType.IsArray)
                    return fi.FieldType.GetElementType();
                else
                    return fi.FieldType;
            }
            else
                return null;
        }

        public static string[] Path(this SerializedProperty property)
            => property.propertyPath.Split('.');

        public static SerializedProperty Parent(this SerializedProperty property)
        {
            string[] path = property.propertyPath.Split('.');
            if (path.Length > 1)
                return property.serializedObject.FindProperty(path[path.Length - 2]);
            else
                return property;
        }

        public static object GetParentObject(this SerializedProperty prop)
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');
            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = getValue(obj, elementName, index);
                }
                else
                {
                    obj = getValue(obj, element);
                }
            }
            return obj;
        }

        private static object getValue(object source, string name)
        {
            if (source == null)
                return null;
            Type type = source.GetType();
            FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        private static object getValue(object source, string name, int index)
        {
            IEnumerable enumerable = getValue(source, name) as IEnumerable;
            IEnumerator enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }


        public static SerializedProperty Root(this SerializedProperty property)
        {
            string[] path = property.propertyPath.Split('.');
            if (path.Length > 0)
                return property.serializedObject.FindProperty(path[0]);
            else
                return property;
        }
    }

    public static class RectEditorExt
    {
        public static Rect ShiftXBy(this Rect r, float x)
        {
            r.x += x;
            r.width -= x;
            return new Rect(r);
        }

        public static Rect WithoutLabel(this Rect r)
            => r.ShiftXBy(EditorGUIUtility.labelWidth);
    }






}
