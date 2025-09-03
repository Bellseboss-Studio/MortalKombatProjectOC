// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FluffyUnderware.DevTools.Extensions;
using System;

namespace FluffyUnderware.DevToolsEditor
{
    [JetBrains.Annotations.UsedImplicitly]
    [Obsolete("Now that SceneView has a OnSceneGUI method, there is seemingly no need to use DTSceneView which implements its own OnSceneGUI. It might even conflict with SceneView.OnSceneGUI")]
    public class DTSceneView : SceneView
    {
        #region ### Public Properties ###

        public bool In2DMode
        {
            get => in2DMode;
            set => in2DMode = value;
        }

        public SceneViewState State
        {
            get => mStateField.GetValue(this) as SceneViewState;
            set => mStateField.SetValue(this, value);
        }

        #endregion

        #region ### Privates Fields ###

        private FieldInfo mStateField;

        #endregion

        #region ### Unity Callbacks ###
        #if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void OnEnable()
        {
            base.OnEnable();
            getInternals();
            duringSceneGui += onScene;
        }

        public override void OnDisable()
        {
            duringSceneGui -= onScene;
            base.OnDisable();
        }

        #endif
        #endregion

        #region ### Public Methods ###

        protected new virtual void OnSceneGUI()
        {
        }

        #endregion

        #region ### Privates & Internals ###
        #if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        private void onScene(SceneView view)
        {
            if (EditorApplication.isCompiling)
            {
                duringSceneGui -= onScene;
                Close();
                GUIUtility.ExitGUI();
            }
            if (view == this)
                OnSceneGUI();
        }

        private void getInternals()
        {
            mStateField = GetType().FieldByName("m_SceneViewState", true, true);
        }



        #endif
        #endregion

    }
}
