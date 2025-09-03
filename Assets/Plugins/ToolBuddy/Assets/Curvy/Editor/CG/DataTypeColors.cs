// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// Provides the colors associated with each <see cref="CGData"/>. The color is provided by the <see cref="CGDataInfoAttribute"/> associated to the <see cref="CGData"/> class.
    /// </summary>
    public class DataTypeColors
    {
        [CanBeNull]
        private static DataTypeColors instance;

        [NotNull]
        public static DataTypeColors Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataTypeColors();
                return instance;
            }
        }

        [NotNull]
        private readonly Dictionary<Type, Color> colors = new Dictionary<Type, Color>();

        public DataTypeColors()
        {
            IEnumerable<Type> loadedTypes = TypeCache.GetTypesDerivedFrom(typeof(CGData));
            foreach (Type type in loadedTypes)
            {
                object[] ai = type.GetCustomAttributes(
                    typeof(CGDataInfoAttribute),
                    true
                );
                if (ai.Length > 0)
                    colors.Add(
                        type,
                        ((CGDataInfoAttribute)ai[0]).Color
                    );
            }
        }

        /// <summary>
        /// Returns the color associated with a given type, otherwise returns white.
        /// </summary>
        /// <param name="type">An array that should contain only one type inheriting from <see cref="CGData"/>, and decorated with the <see cref="CGDataInfoAttribute"/> attribute. Otherwise, the result will be white.</param>
        /// <returns></returns>
        public Color GetTypeColor(
            [NotNull] Type type)
        {
            Color result;
            
                bool foundColor = colors.TryGetValue(
                    type,
                    out result
                );

                if (foundColor == false)
                {
                    DTLog.LogWarning($"[Curvy] No color found for CGData type{type}");
                    result = Color.white;
                }
            

            return result;
        }
    }
}