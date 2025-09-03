// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Generator;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// EventArgs for CurvyCGEvent events
    /// </summary>
    public class CurvyCGEventArgs : EventArgs
    {
        /// <summary>
        /// the component raising the event
        /// </summary>
        public readonly MonoBehaviour Sender;

        /// <summary>
        /// The related CurvyGenerator
        /// </summary>
        public readonly CurvyGenerator Generator;

        /// <summary>
        /// The related CGModule
        /// </summary>
        public readonly CGModule Module;

        public CurvyCGEventArgs(CGModule module)
        {
            Sender = module;
            Generator = module.Generator;
            Module = module;

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(Sender != null);
#endif
        }

        public CurvyCGEventArgs(CurvyGenerator generator, CGModule module)
        {
            Sender = generator;
            Generator = generator;
            Module = module;

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(Sender != null);
#endif
        }
    }
}