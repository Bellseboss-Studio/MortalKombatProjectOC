// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildShapeExtrusion))]
    public class BuildShapeExtrusionEditor : CGModuleEditor<BuildShapeExtrusion>
    {
        private bool mEditCross;
        private bool mShowEditButton;

        public override void OnModuleDebugGUI()
        {
            BuildShapeExtrusion.Statistics statistics = Target.ExtrusionStatistics;
            EditorGUILayout.LabelField("Samples Path/Cross: " + statistics.PathSampleCount + "/" + statistics.CrossSampleCount);
            EditorGUILayout.LabelField("Cross Sample Groups: " + statistics.MaterialGroupsCount);
        }

        [UsedImplicitly]
        private void CBEditCrossButton()
        {
            if (DTGUI.IsLayout)
                mShowEditButton = Target.IsConfigured
                                  && Target.InCross.SourceSlot().ExternalInput != null
                                  && Target.InCross.SourceSlot().ExternalInput.SupportsIPE;

            if (mShowEditButton)
            {
                EditorGUI.BeginChangeCheck();
                mEditCross = GUILayout.Toggle(
                    mEditCross,
                    "Edit Cross",
                    EditorStyles.miniButton
                );
                if (EditorGUI.EndChangeCheck())
                {
                    if (mEditCross)
                        CGGraph.SetIPE(
                            Target.Cross,
                            this
                        );
                    else
                        CGGraph.SetIPE();
                }
            }
        }

        /// <summary>
        /// Called for the IPE initiator to get the TRS values for the target
        /// </summary>
        internal override void OnIPEGetTRS(out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            if (Target.OutVolume.Data.Length == 0)
            {
                position = default;
                rotation = default;
            }
            else
            {
                CGVolume volume = (CGVolume)Target.OutVolume.Data[0];

                if (volume.Positions.Array.Length == 0)
                {
                    position = default;
                    rotation = default;
                }
                else
                {
                    position = volume.Positions.Array[0];
                    rotation = Quaternion.LookRotation(
                        volume.Directions.Array[0],
                        volume.Normals.Array[0]
                    );
                }
            }

            Vector2 scaleVector = (Target as ScalingModule).GetScale(0);
            scale = new Vector3(
                scaleVector.x,
                scaleVector.y,
                1
            );
        }
    }
}