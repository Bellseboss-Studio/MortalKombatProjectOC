// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    #region ### CG related ###

    //[CustomPropertyDrawer(typeof(CGSpot))]
    public class CGSpotPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            base.OnGUI(
                position,
                property,
                label
            );
        //  property.f
    }

    #endregion
}