// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEditor;
using System.Collections.Generic;
using System;
using FluffyUnderware.DevTools;


namespace FluffyUnderware.DevToolsEditor
{
    public abstract class DTProject : IComparable, IDisposable
    {
        public string Identifier { get; private set; }
        public string Version { get; private set; }
        public DTResource Resource { get; protected set; }

        #region Toolbar settings

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Assumed to be always true")]
        public bool ShowToolbarInAllSceneViews = false;

        private static DTToolbarOrientation mTBOrientation = DTToolbarOrientation.Left;
        public DTToolbarOrientation ToolbarOrientation
        {
            get => mTBOrientation;
            set
            {
                if (mTBOrientation != value)
                {
                    mTBOrientation = value;
                    if (!string.IsNullOrEmpty(Identifier))
                    {
                        SetEditorPrefs("ToolbarOrientation", ToolbarOrientation);
                    }
                    else
                        DTLog.LogError("[DevTools] Project " + GetType().Name + "missing identifier!");
                    DTToolbar.RecalcItemSize = true;
                }
            }
        }

        private static DTToolbarMode mTBMode = DTToolbarMode.Full;
        public DTToolbarMode ToolbarMode
        {
            get => mTBMode;
            set
            {
                if (mTBMode != value)
                {
                    mTBMode = value;
                    if (!string.IsNullOrEmpty(Identifier))
                    {
                        SetEditorPrefs("ToolbarMode", ToolbarMode);
                    }
                    else
                        DTLog.LogError("[DevTools] Project " + GetType().Name + "missing identifier!");
                    DTToolbar.RecalcItemSize = true;
                }
            }
        }

        public DTToolbarMargins ToolbarMargins { get; protected set; } = new DTToolbarMargins();
        public bool ShowMargins { get; protected set; }

        #endregion

        private readonly Dictionary<string, EditorKeyBinding> AdditionalKeyBindings = new Dictionary<string, EditorKeyBinding>();
        internal List<DTToolbarItem> ToolbarItems = new List<DTToolbarItem>();

        protected DTProject(string identifier, string version)
        {
            Identifier = identifier;
            Version = version;
            LoadPreferences();
        }

        internal void Clear()
        {
            ToolbarItems.Clear();
            AdditionalKeyBindings.Clear();
        }

        public virtual void ResetPreferences()
        {
        }
       
        public virtual void LoadPreferences() 
        {
            if (!string.IsNullOrEmpty(Identifier))
            {
                // Upgrade?
                string ver=GetEditorPrefs("Version", Version);
                if (string.Compare(ver, Version)==-1)// (ver != Version)
                {
                    UpgradePreferences(ver);
                    SetEditorPrefs("Version", Version);
                }
                 
                mTBMode=GetEditorPrefs("ToolbarMode", ToolbarMode);
                mTBOrientation=GetEditorPrefs("ToolbarOrientation", ToolbarOrientation);
                ToolbarMargins.LoadFromPreferences();
                ShowMargins = GetEditorPrefs("ToolbarShowMargins", ShowMargins);

            } else
                DTLog.LogError("[DevTools] Project "+GetType().Name+"missing identifier!");
        }

        public virtual void SavePreferences()
        {
            if (!string.IsNullOrEmpty(Identifier))
            {
                SetEditorPrefs("Version",Version);
                SetEditorPrefs("ToolbarMode", ToolbarMode);
                SetEditorPrefs("ToolbarOrientation", ToolbarOrientation);
                SetEditorPrefs("ToolbarShowMargins", ShowMargins);
                ToolbarMargins.SaveToPreferences();
            } else
                DTLog.LogError("[DevTools] Project " + GetType().Name + "missing identifier!");
        }

        public List<EditorKeyBinding> GetProjectBindings()
        {
            List<EditorKeyBinding> res = new List<EditorKeyBinding>();
            foreach (DTToolbarItem item in ToolbarItems)
                foreach (EditorKeyBinding binding in item.KeyBindings)
                    if (binding.ShowInPrefs)
                        res.Add(binding);
            res.AddRange(AdditionalKeyBindings.Values);
            res.Sort();
            return res;
        }

        public EditorKeyBinding RegisterKeyBinding(EditorKeyBinding binding)
        {
            EditorKeyBinding b;
            if (AdditionalKeyBindings.TryGetValue(binding.Name,out b))
                return b;
            else {
                setKeyBindingFromPrefs(binding,false);
                AdditionalKeyBindings.Add(binding.Name, binding);
                return binding;
            }
        }

        /// <summary>
        ///  For all EditorKeyBindings registered to ToolbarItems of this project, check if Name is unique
        /// </summary>
        /// <returns></returns>
        internal bool CheckKeyBindingNamesAreUnique()
        {
            
            Dictionary<string, object> dict = new Dictionary<string, object>();
            object exist;
             foreach (DTToolbarItem item in ToolbarItems)
                foreach (EditorKeyBinding binding in item.KeyBindings)
                {
                    if (dict.TryGetValue(binding.Name, out exist)){
                        DTLog.LogError(string.Format("[DevTools] KeyBindings need unique names! ({0} and {1}",item.GetType().Name,exist.GetType().Name));
                        return false;
                    } else
                        dict.Add(binding.Name,item);
                }
            
            return true;
        }

        private void setKeyBindingFromPrefs(EditorKeyBinding binding, bool removePrefsIfEqual=true)
        {
            string newBindString = GetEditorPrefs(binding.Name, "");
            if (!string.IsNullOrEmpty(newBindString) ){
                // Same binding? Then remove prefs setting
                if (removePrefsIfEqual && newBindString == binding.ToPrefsString())
                    DeleteEditorPrefs(binding.Name);
                else // load and set new binding
                    binding.Set(newBindString);
            }
        }

        /// <summary>
        /// For all EditorKeyBindings registered to ToolbarItems of this project, manage remappings stored in EditorPrefs
        /// </summary>
        internal void LoadKeyBindingRemappings()
        {
            // Toolbars
            foreach (DTToolbarItem item in ToolbarItems)
                for (int i=0;i<item.KeyBindings.Count;i++)
                    setKeyBindingFromPrefs(item.KeyBindings[i]);
        }

        protected virtual void UpgradePreferences(string oldVersion)
        {
            DTLog.Log(string.Format("[{0}] Upgrading project settings from {1} to {2}", Identifier, oldVersion, Version));
        }

        public void SetEditorPrefs<T>(string key, T value)
        {
            DT.SetEditorPrefs(Identifier + "." + key, value);
        }

        public T GetEditorPrefs<T>(string key, T defaultValue)
            => DT.GetEditorPrefs(Identifier + "." + key, defaultValue);

        public string[] GetEditorPrefs(string key)
            => DT.GetEditorPrefs(Identifier + "." + key);

        public bool HasEditorPrefs(string key)
            => EditorPrefs.HasKey(Identifier + "." + key);

        public void DeleteEditorPrefs(string key)
        {
            EditorPrefs.DeleteKey(Identifier + "." + key);
        }

        public T FindItem<T>() where T:DTToolbarItem
        {
            Type t = typeof(T);
            for (int i = 0; i < ToolbarItems.Count; i++)
                if (ToolbarItems[i].GetType() == t)
                    return (T)ToolbarItems[i];
            return null;
        }

        internal void SetRadioGroupState(DTToolbarRadioButton active)
        {
            for (int i = 0; i < ToolbarItems.Count; i++)
            {
                DTToolbarRadioButton rad = ToolbarItems[i] as DTToolbarRadioButton;
                if (rad && rad.RadioGroup.Equals(active.RadioGroup, StringComparison.CurrentCultureIgnoreCase))
                    rad.mOn = (rad == active);
            }
        }

        public int CompareTo(object obj)
        {
            DTProject other = (DTProject)obj;
            return Identifier.CompareTo(other.Identifier);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
