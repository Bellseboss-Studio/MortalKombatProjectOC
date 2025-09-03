// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using System.Collections;
using FluffyUnderware.DevTools.Extensions;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// A MonoBehaviour with a version number, useful to handle upgrades if needed
    /// </summary>
    public abstract class DTVersionedMonoBehaviour : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string m_Version;

        /// <summary>
        /// Gets the version of this component
        /// </summary>
        public string Version
        {
            get => m_Version;
            protected set => m_Version = value;
        }

        /// <summary>
        /// Similar to Behaviour.isActiveAndEnabled, except it is not bugged.
        /// The problem with Behaviour.isActiveAndEnabled is that its value stays true in between OnDisable and OnEnable when entering play mode with Domain Reload being true. This invalid value is then possibly used in OnValidate, which creates bugs. More details here: https://forum.unity.com/threads/about-compatibility-with-enter-play-mode-options-domain-reload.1369845/#post-8761450
        /// </summary>
        protected bool IsActiveAndEnabled { get; private set; }

        protected virtual void OnEnable()
        {
            IsActiveAndEnabled = true;
            ResetOnEnable();
        }

        /// <summary>
        /// Resets all fields that need to be reset when OnEnable is called.
        /// </summary>
        /// <remarks>
        /// When setting the Scene Reload option to false in Unity, the values of fields from the previous session will persist into the new session, potentially leading to unexpected behavior.
        /// To avoid this, it is recommended to reset the fields in the OnEnable method of the component.
        /// More information here: https://docs.unity3d.com/2023.1/Documentation/Manual/SceneReloading.html
        /// </remarks>
        protected virtual void ResetOnEnable()
        {

        }

        protected virtual void OnDisable()
        {
            IsActiveAndEnabled = false;
        }

        protected virtual void OnValidate()
        {
            //Is called in the editor only.Source: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        /// <summary>
        /// Destroys the gameobject
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        [Obsolete("Use ObjectExt.Destroy(...) instead")]
        public void Destroy()
        {
            gameObject.Destroy(false, true);
        }
    }
}
