// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using ToolBuddy.Converter.Common;
using ToolBuddy.Converter.Editor.Common.Models;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;// do not delete, needed for Unity 2021
using UnityEngine.UIElements;
#if CURVY_SPLINES
using FluffyUnderware.DevTools;
#endif

namespace ToolBuddy.Converter.Editor.Common.ToolbarElements
{
    [EditorToolbarElement(
        nameof(ConversionTypeField),
        typeof(SceneView)
    )]
    public class ConversionTypeField : PopupField<string>
    {
        public ConversionTypeField() : base(
            ConverterRegistry.RegisteredConverters(),
            0 //gets overriden in ReadValueFromModel
        )
        {
            tooltip = "The type of spline conversion";

            this.RegisterValueChangedCallback(OnValueChanged);
            ReadValueFromModel();
            ConversionModel.Instance.Changed += ReadValueFromModel;
        }

        ~ConversionTypeField() =>
            ConversionModel.Instance.Changed -= ReadValueFromModel;

        private void OnValueChanged(
            ChangeEvent<string> evt)
        {
            ConversionModel model = ConversionModel.Instance;
            model.ConversionType = evt.newValue;
            UpdateDisplayedValue();
        }


        private void ReadValueFromModel()
        {
            int choiceIndex = choices.IndexOf(ConversionModel.Instance.ConversionType);
            if (choiceIndex >= 0)
            {
                SetValueWithoutNotify(choices[choiceIndex]);
                UpdateDisplayedValue();
            }
#if CURVY_SPLINES
            else
                DTLog.LogError(
                    $"[Converter] Conversion type '{ConversionModel.Instance.ConversionType}' not found in the available choices."
                );
#endif
        }

        private void UpdateDisplayedValue()
        {
            TextElement valueText = GetValueTextElement();

            if (valueText != null)
                valueText.text = value;
        }

        [CanBeNull]
        private TextElement GetValueTextElement()
        {
            TextElement valueText;
            if (hierarchy.childCount > 0 && hierarchy[0].childCount > 0)
                valueText = hierarchy[0][0] as TextElement;
            else
            {
                valueText = null;

#if CURVY_SANITY_CHECKS && CURVY_SPLINES
                DTLog.LogWarning("[Converter] TextElement not found in ConversionTypeSelector.");
#endif
            }

            return valueText;
        }
    }
}