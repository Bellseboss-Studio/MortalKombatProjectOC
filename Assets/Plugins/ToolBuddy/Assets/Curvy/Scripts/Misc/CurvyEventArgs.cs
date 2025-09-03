// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;
using UnityEngine.Assertions;
#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif

namespace FluffyUnderware.Curvy
{
    public class CurvyEventArgs : EventArgs
    {
        /// <summary>
        /// The component raising the event
        /// </summary>
        public readonly MonoBehaviour Sender;

        /// <summary>
        /// Custom data
        /// </summary>
        public readonly object Data;

        public CurvyEventArgs(MonoBehaviour sender, object data)
        {
            Sender = sender;
            Data = data;

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                ReferenceEquals(
                    null,
                    Sender
                )
                == false
            );
#endif
        }
    }
}