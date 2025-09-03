// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Examples
{
    public class E12_MDJunctionControl : CurvyMetadataBase
    {
        public bool UseJunction;

        public void Toggle() =>
            UseJunction = !UseJunction;
    }
}