// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator.Modules;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    /// <summary>
    /// Modifies the Mix Curve of the Variable Mix Shapes in example scene 27
    /// </summary>
    public class E27_MixingAnimator : MonoBehaviour
    {
        public ModifierVariableMixShapes VariableMixShapes;

        [UsedImplicitly]
        private void Update()
        {
            Keyframe[] mixCurveKeys = VariableMixShapes.MixCurve.keys;
            mixCurveKeys[1].value = Mathf.Sin(Time.time);
            VariableMixShapes.MixCurve.keys = mixCurveKeys;
            VariableMixShapes.Dirty = true;
        }
    }
}