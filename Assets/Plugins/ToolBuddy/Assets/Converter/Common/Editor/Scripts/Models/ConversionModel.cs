// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Linq;
using ToolBuddy.Converter.Common;
#if CURVY_SPLINES
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
#endif

namespace ToolBuddy.Converter.Editor.Common.Models
{
    public class ConversionModel : Singleton<ConversionModel>
    {
        private const string ConversionSourceKey =
            AssetsCommonInformation.Publisher + "." + AssetsCommonInformation.CommonName + ".ConversionSource";

        private const string ConversionTypeKey =
            AssetsCommonInformation.Publisher + "." + AssetsCommonInformation.CommonName + ".ConversionType";

        private ConversionSource conversionSource;
        private string conversionType;

        public ConversionSource ConversionSource
        {
            get => conversionSource;
            set
            {
                if (conversionSource != value)
                {
                    conversionSource = value;
#if CURVY_SPLINES
                    DT.SetEditorPrefs(
                        ConversionSourceKey,
                        value
                    );
#endif
                    OnChanged();
                }
            }
        }

        public string ConversionType
        {
            get => conversionType;
            set
            {
                if (conversionType != value)
                {
                    conversionType = value;
#if CURVY_SPLINES
                    DT.SetEditorPrefs(
                        ConversionTypeKey,
                        value
                    );
#endif
                    OnChanged();
                }
            }
        }

        public ConversionModel()
        {
#if CURVY_SPLINES
            conversionSource = DT.GetEditorPrefs(
                ConversionSourceKey,
                ConversionSource.None
            );

            string defaultConverter = ConverterRegistry.RegisteredConverters().FirstOrDefault();
            if (defaultConverter == null)
                defaultConverter = "Unspecified";

            conversionType = DT.GetEditorPrefs(
                ConversionTypeKey,
                defaultConverter
            );

            if (ConverterRegistry.RegisteredConverters().Contains(conversionType) == false
                && conversionType != defaultConverter)
            {
                DTLog.LogError(
                    $"[Converter] Conversion type '{conversionType}' not found in the available choices. Changing it to '{defaultConverter}'"
                );

                ConversionType = defaultConverter;
            }
#endif
        }

        public event Action Changed;

        private void OnChanged() =>
            Changed?.Invoke();
    }
}