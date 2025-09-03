// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using ToolBuddy.Converter.Editor.Common.Models;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolBuddy.Converter.Editor.Common.ToolbarElements
{
    [EditorToolbarElement(
        nameof(ConvertSelectedToggle),
        typeof(SceneView)
    )]
    public class ConvertSelectedToggle : ToolbarToggle
    {
        public ConvertSelectedToggle()
        {
            text = "Convert Selected";
            tooltip = "Convert selected splines";
            style.unityTextAlign = TextAnchor.MiddleCenter;

            this.RegisterValueChangedCallback(OnValueChanged);

            ReadValueFromModel();
            ConversionModel.Instance.Changed += ReadValueFromModel;
        }

        ~ConvertSelectedToggle()
        {
            ConversionModel.Instance.Changed -= ReadValueFromModel;
        }

        private void OnValueChanged(
            ChangeEvent<bool> evt)
        {
            ConversionModel model = ConversionModel.Instance;
            model.ConversionSource = evt.newValue
                ? ConversionSource.SelectedSplines
                : ConversionSource.None;
        }

        private void ReadValueFromModel()
        {
            SetValueWithoutNotify(ConversionModel.Instance.ConversionSource == ConversionSource.SelectedSplines);
        }
    }
}