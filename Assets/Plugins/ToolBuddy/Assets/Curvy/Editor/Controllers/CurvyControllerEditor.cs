// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Controllers
{
    public class CurvyControllerEditor<T> : CurvyEditorBase<T> where T : CurvyController
    {
        protected override void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            base.OnDisable();
            if (Application.isPlaying == false)
                if (Target)
                    Target.Stop();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state) =>
            OnStateChanged();

        private void OnStateChanged()
        {
            if (Application.isPlaying == false)
                Target.Stop();
        }

        protected override void OnReadNodes()
        {
            DTGroupNode node = Node.AddSection(
                "Preview",
                ShowPreviewButtons
            );
            node.Expanded = false;
            node.SortOrder = 5000;
        }


        /// <summary>
        /// Show the preview buttons
        /// </summary>
        protected void ShowPreviewButtons(DTInspectorNode node) =>
            GUILayoutExtension.Horizontal(
                () =>
                {
                    GUI.enabled = !Application.isPlaying;

                    bool isPlayingOrPaused = Target.PlayState == CurvyController.CurvyControllerState.Playing
                                             || Target.PlayState == CurvyController.CurvyControllerState.Paused;

                    //TODO it would be nice to have two different icons, one for Play and one for Pause
                    if (GUILayout.Toggle(
                            isPlayingOrPaused,
                            new GUIContent(
                                CurvyStyles.TexPlay,
                                "Play/Pause in Editor"
                            ),
                            GUI.skin.button
                        )
                        != isPlayingOrPaused)
                        switch (Target.PlayState)
                        {
                            case CurvyController.CurvyControllerState.Paused:
                            case CurvyController.CurvyControllerState.Stopped:
                                Target.Play();
                                break;
                            case CurvyController.CurvyControllerState.Playing:
                                Target.Pause();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                    if (GUILayout.Button(
                            new GUIContent(
                                CurvyStyles.TexStop,
                                "Stop/Reset"
                            )
                        ))
                        Target.Stop();
                    GUI.enabled = true;
                }
            );
    }
}