// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools.Extensions;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    public partial class CurvyGenerator
    {
#if UNITY_EDITOR

        #region ### Serialized Fields ###

        [Tooltip("Show Debug Output?")]
        [SerializeField]
        private bool m_ShowDebug;

        [SerializeField]
        [HideInInspector]
        private float viewportZoom = 1.0f;

        [SerializeField]
        [HideInInspector]
        private Vector2 viewportScroll;

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Gets or sets whether to show debug outputs
        /// </summary>
        public bool ShowDebug
        {
            get => m_ShowDebug;
            set => m_ShowDebug = value;
        }

        /// <summary>
        /// The zoom of the Generator window's viewport.
        /// </summary>
        public float ViewportZoom
        {
            get => viewportZoom;
            set
            {
                if (value != viewportZoom)
                {
                    viewportZoom = value;
                    gameObject.MarkParentSceneAsDirty();
                }
            }
        }

        /// <summary>
        /// The scroll position of the Generator window's viewport.
        /// </summary>
        public Vector2 ViewportScroll
        {
            get => viewportScroll;
            set
            {
                if (value != viewportScroll)
                {
                    viewportScroll = value;
                    gameObject.MarkParentSceneAsDirty();
                }
            }
        }

        #endregion

#else
        [System.Obsolete("This property is editor only")]
        public bool ShowDebug
        {
            get => false;
            set { }
        }
#endif
    }
}