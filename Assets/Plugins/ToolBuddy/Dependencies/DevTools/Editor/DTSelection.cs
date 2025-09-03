// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;

namespace FluffyUnderware.DevToolsEditor
{
    public static class DTSelection
    {
        private static GameObject mCurrentGameObject;
        private static int mCurrentGameObjectsCount;

        public static event DT.Callback OnSelectionChange;

        internal static bool MuteEvents { get; set; }

        public static int Count => mCurrentGameObjectsCount;

        internal static void Initialize()
        {
            EditorApplication.update -= checkSelection;
            //TODO listen on Selection.selectionChanged instead?
            EditorApplication.update += checkSelection;

        }

        public static bool HasComponent<T>(bool allowMultiSelection = false) where T : Component
        {
            if (allowMultiSelection)
                return mCurrentGameObjectsCount >= 1 && getSelection<T>().Count > 0;
            else
                return mCurrentGameObjectsCount == 1 && mCurrentGameObject != null && mCurrentGameObject.GetComponent(typeof(T)) != null;
        }

        public static bool HasComponent<T, T1>(bool allowMultiSelection = false) where T : Component where T1 : Component
            => HasComponent<T>(allowMultiSelection) ||
               HasComponent<T1>(allowMultiSelection);

        public static bool HasComponent<T, T1, T2>(bool allowMultiSelection = false)
            where T : Component
            where T1 : Component
            where T2 : Component
            => HasComponent<T>(allowMultiSelection) ||
               HasComponent<T1>(allowMultiSelection) ||
               HasComponent<T2>(allowMultiSelection);

        public static bool HasComponent<T, T1, T2, T3>(bool allowMultiSelection = false)
            where T : Component
            where T1 : Component
            where T2 : Component
            where T3 : Component
            => HasComponent<T>(allowMultiSelection) ||
               HasComponent<T1>(allowMultiSelection) ||
               HasComponent<T2>(allowMultiSelection) ||
               HasComponent<T3>(allowMultiSelection);

        public static bool HasComponent<T, T1, T2, T3, T4>(bool allowMultiSelection = false)
            where T : Component
            where T1 : Component
            where T2 : Component
            where T3 : Component
            where T4 : Component
            => HasComponent<T>(allowMultiSelection) ||
               HasComponent<T1>(allowMultiSelection) ||
               HasComponent<T2>(allowMultiSelection) ||
               HasComponent<T3>(allowMultiSelection) ||
               HasComponent<T4>(allowMultiSelection);

        [CanBeNull]
        public static T GetAs<T>(bool allowMultiSelection = false) where T : Component
        {
            List<T> ar = getSelection<T>();
            if (ar.Count == 1 || (ar.Count > 0 && allowMultiSelection))
                return (T)ar[0];
            else
                return null;
        }

        [NotNull]
        public static List<T> GetAllAs<T>() where T : Component
            => getSelection<T>();

        [CanBeNull]
        public static GameObject GetGameObject(bool allowMultiSelection = false)
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length == 1 || (selectedGameObjects.Length > 1 && allowMultiSelection))
                return Selection.activeGameObject;
            else
                return null;
        }

        public static Vector3 GetPosition()
        {
            Vector3 p = Vector3.zero;
            List<Transform> tt = GetAllAs<Transform>();
            if (tt.Count > 0)
            {
                for (int i = 0; i < tt.Count; i++)
                    p += tt[i].position;

                p /= tt.Count;
            }
            return p;
        }

        [NotNull]
        private static List<T> getSelection<T>() where T : Component
        {
            Object[] S = Selection.GetFiltered(typeof(T), SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable);
            List<T> L = new List<T>();
            foreach (Object O in S)
                if (O != null)
                {
                    if (Selection.activeTransform == ((Component)O).transform)
                        L.Insert(0, (T)O);
                    else
                        L.Add((T)O);
                }
            return L;
        }

        public static void Clear()
        {
            Selection.objects = new Object[0];
            mCurrentGameObject = null;
            mCurrentGameObjectsCount = 0;
            raiseOnSelectionChange();
        }

        public static void Set(params Object[] objects)
        {
            if (objects.Length > 0)
            {
                Selection.objects = objects;
                mCurrentGameObject = Selection.activeGameObject;
                mCurrentGameObjectsCount = Selection.gameObjects.Length;
                raiseOnSelectionChange();
            }
            else
                Clear();
        }

        public static void AddGameObjects(params Component[] objects)
        {
            List<GameObject> gl = new List<GameObject>(objects.Length);
            foreach (Component c in objects)
                if (c != null && c.gameObject != null && !gl.Contains(c.gameObject) && !Selection.Contains(c))
                    gl.Add(c.gameObject);
            if (gl.Count > 0)
            {
                GameObject[] cur = Selection.gameObjects;
                Selection.objects = cur.AddRange(gl.ToArray());
            }
        }

        public static void SetGameObjects(params Component[] objects)
        {
            if (objects.Length > 0)
            {
                List<GameObject> gl = new List<GameObject>(objects.Length);
                foreach (Component c in objects)
                    if (c != null && c.gameObject != null && !gl.Contains(c.gameObject))
                        gl.Add(c.gameObject);
                Selection.objects = gl.ToArray();
                mCurrentGameObject = Selection.activeGameObject;
                mCurrentGameObjectsCount = Selection.gameObjects.Length;
                raiseOnSelectionChange();
            }
            else
                Clear();
        }

        private static void checkSelection()
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            if (!MuteEvents && Selection.activeGameObject != mCurrentGameObject || mCurrentGameObjectsCount != selectedGameObjects.Length)
            {
                mCurrentGameObject = Selection.activeGameObject;
                mCurrentGameObjectsCount = selectedGameObjects.Length;
                raiseOnSelectionChange();
            }
        }

        private static void raiseOnSelectionChange()
        {
            if (OnSelectionChange != null)
                OnSelectionChange();
        }
    }
}
