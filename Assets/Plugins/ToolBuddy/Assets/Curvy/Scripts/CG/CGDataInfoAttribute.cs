// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools.Extensions;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Additional properties for CGData based classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CGDataInfoAttribute : Attribute
    {
        public readonly Color Color;

        public CGDataInfoAttribute(Color color) =>
            Color = color;

        public CGDataInfoAttribute(float r, float g, float b, float a = 1) =>
            Color = new Color(
                r,
                g,
                b,
                a
            );

        public CGDataInfoAttribute(string htmlColor) =>
            Color = htmlColor.ColorFromHtml();
    }
}