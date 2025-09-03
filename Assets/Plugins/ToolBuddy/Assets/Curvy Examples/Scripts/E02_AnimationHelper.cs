// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    /// <summary>
    /// A class that makes some Animation methods available to Unity Events
    /// </summary>
    public class E02_AnimationHelper : MonoBehaviour
    {
        public void Play(Animation animation) =>
            animation.Play();

        public void RewindThenPlay(Animation animation)
        {
            animation.Rewind();
            animation.Play();
        }
    }
}