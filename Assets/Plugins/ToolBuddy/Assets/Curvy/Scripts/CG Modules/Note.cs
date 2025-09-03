// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Note",
        ModuleName = "Note",
        Description = "Creates a note"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgnote")]
    public class Note : CGModule, INoProcessing
    {
        [SerializeField, TextArea(
             3,
             10
         )]
        private string m_Note;

        public string NoteText
        {
            get => m_Note;
            set => m_Note = value;
        }

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            Properties.LabelWidth = 50;
        }

        public override void Reset()
        {
            base.Reset();
            m_Note = null;
        }

#endif

        #endregion
    }
}