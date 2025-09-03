// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Defines properties for game objects used within the Curvy Generator.
    /// This class allows for specifying transformations (translation, rotation, scale)
    /// to be applied to a game object when it is utilized by a generator module.
    /// </summary>
    [Serializable]
    public class CGGameObjectProperties
    {
        [SerializeField]
        [CanBeNull]
        private GameObject m_Object;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Translation;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Rotation;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Scale = Vector3.one;

        /// <summary>
        /// Gets or sets the game object.
        /// </summary>
        [CanBeNull]
        public GameObject Object
        {
            get => m_Object;
            set => m_Object = value;
        }

        /// <summary>
        /// Gets or sets the translation vector to apply to the game object.
        /// </summary>
        public Vector3 Translation
        {
            get => m_Translation;
            set => m_Translation = value;
        }

        /// <summary>
        /// Gets or sets the rotation vector (Euler angles) to apply to the game object.
        /// </summary>
        public Vector3 Rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }

        /// <summary>
        /// Gets or sets the scale vector to apply to the game object.
        /// </summary>
        public Vector3 Scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        /// <summary>
        /// Gets the transformation matrix combining translation, rotation, and scale.
        /// </summary>
        public Matrix4x4 Matrix => Matrix4x4.TRS(
            Translation,
            Quaternion.Euler(Rotation),
            Scale
        );

        public CGGameObjectProperties() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CGGameObjectProperties"/> class
        /// with the specified game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        public CGGameObjectProperties(GameObject gameObject) =>
            Object = gameObject;
    }
}
