// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using System.Collections;
using Object = UnityEngine.Object;

namespace FluffyUnderware.DevToolsEditor
{
    public static class DTEditorUtility
    {
        public static Camera ActiveCamera => (SceneView.currentDrawingSceneView) ? SceneView.currentDrawingSceneView.camera : Camera.current;

        public static bool DragDropTypeMatch(Type type)
        {
            foreach (Object go in DragAndDrop.objectReferences)
            {
                if (go is GameObject)
                {
                    if (type.IsSubclassOf(typeof(Component)) == false)
                        return false;
                    if (((GameObject)go).GetComponent(type) == null)
                        return false;
                }

                else if (go.GetType() != type)
                    return false;
            }

            return true;
        }

        public static bool DragDropTypeMatch<T>() where T : Object
            => DragDropTypeMatch(typeof(T));

        public static Object[] DragDropGetObjectsOfType(Type type)
        {
            if (type.IsArrayOrList())
                return new Object[0];
            List<Object> res = new List<Object>();
            foreach (Object o in DragAndDrop.objectReferences)
            {
                if (o.GetType() == type)
                    res.Add(o);
                else if (o is GameObject)
                {
                    Component f = ((GameObject)o).GetComponent(type);
                    if (f != null)
                        res.Add(f);
                }
            }
            return res.ToArray();
        }

        public static T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            ProjectWindowUtil.CreateAsset(asset, "New " + typeof(T).Name + ".asset");
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static void ToggleSceneViewGizmos(bool gizmosOn)
        {
            ToggleSceneViewGizmos(gizmosOn, new string[0]);
        }

        public static void ToggleSceneViewGizmos(bool gizmosOn, params string[] scriptClasses)
        {
            int val = gizmosOn ? 1 : 0;
            List<string> classes = new List<string>(scriptClasses);
            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            Type type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                MethodInfo getAnnotations = type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
                if (getAnnotations != null)
                {
                    MethodInfo setGizmoEnabled = type.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                    if (setGizmoEnabled != null)
                    {
                        MethodInfo setIconEnabled = type.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
                        if (setIconEnabled != null)
                        {
                            object annotations = getAnnotations.Invoke(null, null);
                            foreach (object annotation in (IEnumerable)annotations)
                            {
                                Type annotationType = annotation.GetType();
                                FieldInfo classIdField = annotationType.GetField("classID", BindingFlags.Public | BindingFlags.Instance);
                                FieldInfo scriptClassField = annotationType.GetField("scriptClass", BindingFlags.Public | BindingFlags.Instance);
                                if (classIdField != null && scriptClassField != null)
                                {
                                    int classId = (int)classIdField.GetValue(annotation);
                                    string scriptClass = (string)scriptClassField.GetValue(annotation);
                                    if (classes.Count == 0 || classes.Contains(scriptClass))
                                    {
                                        setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                                        setIconEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                                    }
                                }
                            }
                        }
                        else
                            Debug.LogError("Couldn't find method SetIconEnabled in type " + type.FullName);
                    }
                    else
                        Debug.LogError("Couldn't find method SetGizmoEnabled in type " + type.FullName);
                }
                else
                    Debug.LogError("Couldn't find method GetAnnotations in type " + type.FullName);
            }
            else
                Debug.LogError("Couldn't find type UnityEditor.AnnotationUtility in assembly " + asm.FullName);
        }

        public static List<T> LoadPrefabsContaining<T>(string path) where T : Component
        {
            List<T> res = new List<T>();

            string[] gos = AssetDatabase.FindAssets("t:gameobject", new string[] { path });
            foreach (string id in gos)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));
                T cmp = go.GetComponent<T>();
                if (cmp != null)
                    res.Add(cmp);
            }
            return res;
        }

    }
}
