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
        nameof(ConvertAllToggle),
        typeof(SceneView)
    )]
    public class ConvertAllToggle : ToolbarToggle
    {
        private bool isSwitching;

        public ConvertAllToggle()
        {
            text = "Convert All";
            tooltip = "Convert all splines";
            style.unityTextAlign = TextAnchor.MiddleCenter;

            this.RegisterValueChangedCallback(OnValueChanged);

            ReadValueFromModel();
            ConversionModel.Instance.Changed += ReadValueFromModel;
        }

        ~ConvertAllToggle() =>
            ConversionModel.Instance.Changed -= ReadValueFromModel;

        private void OnValueChanged(
            ChangeEvent<bool> evt)
        {
            ConversionModel model = ConversionModel.Instance;
            model.ConversionSource = evt.newValue
                ? ConversionSource.AllSplines
                : ConversionSource.None;
        }

        private void ReadValueFromModel()
        {
            SetValueWithoutNotify(ConversionModel.Instance.ConversionSource == ConversionSource.AllSplines);
        }
    }
}