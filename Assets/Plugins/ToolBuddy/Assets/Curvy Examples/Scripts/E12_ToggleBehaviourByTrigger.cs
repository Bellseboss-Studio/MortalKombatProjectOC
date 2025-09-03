// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E12_ToggleBehaviourByTrigger : MonoBehaviour
    {
        public Behaviour UIElement;

        [UsedImplicitly]
        private void OnTriggerEnter()
        {
            if (UIElement)
                UIElement.enabled = !UIElement.enabled;
        }
    }
}