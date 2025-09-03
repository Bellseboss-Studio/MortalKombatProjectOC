// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using System.Reflection;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.DevToolsEditor
{
    [InitializeOnLoad]
    public static class DT
    {
        private const char editorPreferencesArraySeparator = ';';

        public const string VERSION = "1.0.3";
        /// <summary>
        /// Global override for float precision rounding (e.g. DTPropertyAttribute.Precision)
        /// </summary>
        public static bool _UseSnapValuePrecision;


        public delegate void Callback();

        public static List<DTProject> Projects => mProjects.Values.ToList();

        public static DTProject Project(string identifier)
        {
            DTProject prj;
            if (!mProjects.TryGetValue(identifier, out prj))
            {
                LoadProjects();
                if (!mProjects.TryGetValue(identifier, out prj))
                    DTLog.LogError("[DevTools] Unable to find project '" + identifier + "' !");
            }

            return prj;
        }

        private static object mClipboardData;
        private static readonly Dictionary<Type, IDTClipboardHandler> mClipboardHandlers =
            new Dictionary<Type, IDTClipboardHandler>();
        private static readonly Dictionary<string, DTProject> mProjects = new Dictionary<string, DTProject>();

        private static bool _compiling;

        static DT()
        {
            EditorApplication.update += delayedInitialize;
            EditorApplication.update -= compileCheck;
            EditorApplication.update += compileCheck;

        }

        public static void Clear()
        {
            foreach (DTProject prj in Projects)
                prj.Clear();
            Projects.Clear();
        }

        public static void ReInitialize(bool loadProjects = true)
        {
            if (loadProjects)
                LoadProjects();

            DTToolbar.Initialize();
            HandleProjectsKeyBindings();
        }

        private static void delayedInitialize()
        {
            EditorApplication.update -= delayedInitialize;
            LoadPreferences();
            DTSelection.Initialize();
            bool loadProjects = Projects.Count == 0;//do not reload projects if already loaded
            ReInitialize(loadProjects);
            // Register Clipboard-Handlers
            RegisterClipboardType(typeof(Vector2), new DTVector2Clipboard());
            RegisterClipboardType(typeof(Vector3), new DTVector3Clipboard());
            RegisterClipboardType(typeof(AnimationCurve), new DTAnimationCurveClipboard());
        }

        private static void compileCheck()
        {
            if (!_compiling)
            {
                _compiling = EditorApplication.isCompiling;
                if (_compiling)
                    Clear();
            }
            else
                _compiling = EditorApplication.isCompiling;
        }

        #region ### Clipboard ###
        public static void RegisterClipboardType(Type dataType, IDTClipboardHandler handler, bool overwriteExisting = false)
        {
            if (mClipboardHandlers.ContainsKey(dataType))
            {
                if (overwriteExisting)
                    mClipboardHandlers[dataType] = handler;
            }
            else
                mClipboardHandlers.Add(dataType, handler);
        }

        public static void ClipboardCopy(object data)
        {
            Type dataType = data.GetType();
            IDTClipboardHandler handler;
            if (mClipboardHandlers.TryGetValue(dataType, out handler))
            {
                handler.Copy(data);
            }
            else
                DTLog.LogError("[DevTools] No ClipboardHandler for data type '" + data.GetType().Name + "' found!");
        }

        public static T ClipboardPaste<T>()
        {
            IDTClipboardHandler handler;
            if (mClipboardData != null && mClipboardHandlers.TryGetValue(typeof(T), out handler))
            {
                if (handler.CanPasteFrom(mClipboardData.GetType()))
                {
                    return (T)handler.Paste(mClipboardData);
                }
            }
            return default;
        }

        public static bool ClipboardCanPasteTo<T>()
        {
            IDTClipboardHandler handler;
            if (mClipboardData != null && mClipboardHandlers.TryGetValue(typeof(T), out handler))
            {
                return handler.CanPasteFrom(mClipboardData.GetType());
            }
            return false;
        }

        internal static void ClipBoardSet(object data)
        {
            mClipboardData = data;
        }

        #endregion

        #region ### Project Management ###

        private static void LoadProjects()
        {
            mProjects.ForEach(pair=>pair.Value.Dispose());
            mProjects.Clear();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(typeof(DTProject));
            foreach (Type t in types)
            {
                DTProject newProject = (DTProject)Activator.CreateInstance(t);
                mProjects.Add(newProject.Identifier, newProject);
            }
        }

        private static void HandleProjectsKeyBindings()
        {
            foreach (DTProject prj in Projects)
                if (!prj.CheckKeyBindingNamesAreUnique())
                    return;
                else
                    prj.LoadKeyBindingRemappings();
        }

        #endregion

        #region ### EditorPrefs-Helpers ###

        private static void LoadPreferences()
        {
            // Upgrade?
            string ver = GetEditorPrefs("FluffyUnderware.DevTools.Version", VERSION);
            if (ver != VERSION)
            {
                UpgradeDevTools(ver);
                SavePreferences();
            }
        }

        private static void SavePreferences()
        {
            SetEditorPrefs("FluffyUnderware.DevTools.Version", VERSION);
        }

        /// <summary>
        /// Add code to handle upgrading (delete old pref-keys etc...) here
        /// </summary>
        /// <param name="oldVersion">the version stored in the EditorPrefs</param>
        private static void UpgradeDevTools(string oldVersion)
        {
            DTLog.Log("[DevTools] Upgrading settings from " + oldVersion + " to " + VERSION);
        }

        public static void SetEditorPrefs<T>(string key, T value)
        {
            Type tt = typeof(T);
            if (tt.IsEnum)
            {
                EditorPrefs.SetInt(key, Convert.ToInt32(Enum.Parse(typeof(T), value.ToString()) as Enum));
            }
            else if (tt.IsArray)
            {
                IList list = (IList)value;
                string[] array = new string[list.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = list[i].ToString();
                    if(array[i].Contains(editorPreferencesArraySeparator))
                        throw new ArgumentException(String.Format("value should not have any element containing a {0} character",editorPreferencesArraySeparator));
                }
                SetEditorPrefs(key, String.Join(editorPreferencesArraySeparator.ToString(), array));
            }
            else if (tt.Matches(typeof(int), typeof(Int32)))
                EditorPrefs.SetInt(key, (value as int?).Value);
            else if (tt == typeof(string))
                EditorPrefs.SetString(key, (value as string));
            else if (tt == typeof(float))
                EditorPrefs.SetFloat(key, (value as float?).Value);
            else if (tt == typeof(bool))
                EditorPrefs.SetBool(key, (value as bool?).Value);
            else if (tt == typeof(Color))
                EditorPrefs.SetString(key, (value as Color?).Value.ToHtml());
            else
                DTLog.LogError("[DevTools] SetEditorPrefs: Unsupported datatype: " + tt.Name);
        }

        public static T GetEditorPrefs<T>(string key, T defaultValue)
        {
            if (EditorPrefs.HasKey(key))
            {
                Type tt = typeof(T);
                try
                {
                    if (tt.IsEnum || tt.Matches(typeof(int), typeof(Int32)))
                    {
                        return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
                    }
                    else if (tt.IsArray)
                    {
                        throw new NotImplementedException();
                    }
                    else if (tt == typeof(string))
                        return (T)(object)EditorPrefs.GetString(key, defaultValue.ToString());
                    else if (tt == typeof(float))
                        return (T)(object)EditorPrefs.GetFloat(key, (float)(object)defaultValue);
                    else if (tt == typeof(bool))
                        return (T)(object)EditorPrefs.GetBool(key, (bool)(object)defaultValue);
                    else if (tt == typeof(Color))
                        return (T)(object)EditorPrefs.GetString(key, ((Color)(object)defaultValue).ToHtml()).ColorFromHtml();
                    else
                        DTLog.LogError("[DevTools] SetEditorPrefs: Unsupported datatype: " + tt.Name);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public static string[] GetEditorPrefs(string key)
        {
            string arrayString = GetEditorPrefs(key, String.Empty);
            return String.IsNullOrEmpty(arrayString) ? new string[0] : arrayString.Split(editorPreferencesArraySeparator);
        }

        #endregion

        #region ### Utilities ###

        /// <summary>
        /// Opens Unity's preferences window
        /// </summary>
        public static void OpenPreferencesWindow()
        {
            SettingsService.OpenUserPreferences();
        }

        /// <summary>
        /// Opens Unity's preferences window at a specific path. The path should be the same than the one of the associated <see cref="SettingsProvider"/>
        /// </summary>
        public static void OpenPreferencesWindow(string settingsPath)
        {
            SettingsService.OpenUserPreferences(settingsPath);
        }

        #endregion
    }
}
