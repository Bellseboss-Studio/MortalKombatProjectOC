// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using FluffyUnderware.DevToolsEditor.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// Provides extension methods for the <see cref="CGModule"/> class.
    /// </summary>
    public static class CGModuleEditorExtension
    {
        /// <summary>
        /// Retrieves a formatted, rich text, title for a given CGModule.
        /// </summary>
        [NotNull]
        public static string GetTitle(
            [NotNull] this CGModule module,
            int maxCharactersLength = 30)
        {
#if CURVY_DEBUG
            return module.UniqueID + ":" + module.ModuleName;

#endif
            string truncatedModuleName;
            {
                if (module.ModuleName.Length > maxCharactersLength)
                    truncatedModuleName = module.ModuleName.Substring(
                                             0,
                                             maxCharactersLength - 3
                                         )
                                         + "...";
                else
                    truncatedModuleName = module.ModuleName;
            }

            string title;
            if (!module.IsConfigured)
                title = string.Format(
                    "<color={0}>{1}</color>",
                    new Color(
                        1,
                        0.2f,
                        0.2f
                    ).SkinAwareColor().ToHtml(),
                    truncatedModuleName
                );
            else if (module is IOnRequestProcessing)
                title = string.Format(
                    "<color={0}>{1}</color>",
                    CurvyStyles.IOnRequestProcessingTitleColor.SkinAwareColor().ToHtml(),
                    truncatedModuleName
                );
            else
                title = truncatedModuleName;

            return title;
        }

        public static bool GetExpectedExpansionState(
            [NotNull] this CGModule module,
            [CanBeNull] CGModule selectedModule) =>
            DTGUI.IsLayout && CurvyProject.Instance.CGAutoModuleDetails
                ? module == selectedModule
                : module.Properties.Expanded.target;

        public static void UpdateSlotDimensions(
            [NotNull] this CGModule module)
        {
            int i = 0;
            float slotDropZoneHeight = 18;

            while (module.Input.Count > i || module.Output.Count > i)
            {
                float y = CurvyStyles.ModuleWindowTitleHeight + (slotDropZoneHeight * i);

                if (module.Input.Count > i)
                {
                    CGModuleInputSlot slot = module.Input[i];

                    float labelWidth = GetDropZoneWidth(slot);

                    slot.DropZone = new Rect(
                        0,
                        y,
                        labelWidth,
                        slotDropZoneHeight
                    );
                    slot.Origin = new Vector2(
                        module.Properties.Dimensions.xMin,
                        module.Properties.Dimensions.yMin + y + (slotDropZoneHeight / 2)
                    );
                }

                if (module.Output.Count > i)
                {
                    CGModuleOutputSlot slot = module.Output[i];

                    float labelWidth = GetDropZoneWidth(slot);

                    slot.DropZone = new Rect(
                        module.Properties.Dimensions.width - labelWidth,
                        y,
                        labelWidth,
                        slotDropZoneHeight
                    );
                    slot.Origin = new Vector2(
                        module.Properties.Dimensions.xMax,
                        module.Properties.Dimensions.yMin + y + (slotDropZoneHeight / 2)
                    );
                }

                i++;
            }
        }

        public static void DrawRefreshHighlight(
            [NotNull] this CGModule module,
            float highlightDuration,
            int highlightSize,
            double timeSinceLastUpdate)
        {
            float alpha = Mathf.SmoothStep(
                1,
                0,
                (float)timeSinceLastUpdate / highlightDuration
            );
            DTGUI.PushBackgroundColor(
                new Color(
                    0,
                    1,
                    0,
                    alpha
                )
            );
            GUI.Box(
                module.Properties.Dimensions.ScaleBy(highlightSize),
                "",
                CurvyStyles.GlowBox
            );
            DTGUI.PopBackgroundColor();
        }

        /// <summary>
        /// Returns the bounding box of the given modules, or an empty rectangle if the list is empty
        /// </summary>
        public static Rect GetModulesBoundingBox(
            [NotNull] [ItemNotNull] this List<CGModule> modules)
        {
            if (modules == null)
                throw new ArgumentNullException(nameof(modules));

            if (modules.Count == 0)
                return new Rect();

            Vector2 min = new Vector2(
                float.MaxValue,
                float.MaxValue
            );
            Vector2 max = new Vector2(
                float.MinValue,
                float.MinValue
            );

            foreach (CGModule module in modules)
            {
                min = Vector2.Min(
                    min,
                    module.Properties.Dimensions.min
                );
                max = Vector2.Max(
                    max,
                    module.Properties.Dimensions.max
                );
            }

            return new Rect(
                min,
                max - min
            );
        }

        #region DropZone width

        private static GUIContent cachedGuiContent = new GUIContent();

        private static float GetDropZoneWidth(
            [NotNull] CGModuleSlot slot) =>
            CGGraph.ConnectorWidth + GetSlotLabelSize(slot).x;

        private static Vector2 GetSlotLabelSize(
            [NotNull] CGModuleSlot slot)
        {
            cachedGuiContent.text = slot.GetLabelText();
            return CurvyStyles.GetSlotLabelStyle(slot).CalcSize(cachedGuiContent);
        }

        #endregion
    }
}