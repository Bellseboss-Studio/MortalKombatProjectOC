// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Controllers
{
    public partial class UITextSplineController
    {
        protected interface IGlyph
        {
            Vector3 Center { get; }
            void Transpose(Vector3 v);
            void Rotate(Quaternion rotation);
        }
    }
}