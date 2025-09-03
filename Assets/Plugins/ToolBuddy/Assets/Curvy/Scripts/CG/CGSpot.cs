// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    #region ### CGModule related ###

    /// <summary>
    /// Spots are used to place objects (like meshes or Game Objects) A spot is defined by spacial coordinates (similar to Transform) and the index of the object to place
    /// </summary>
    [Serializable]
    public struct CGSpot : IEquatable<CGSpot>
    {
        [SerializeField]
        [Label("Index")]
        private int m_Index;

        [SerializeField]
        [VectorEx(
            "Position",
            Options = AttributeOptionsFlags.Compact,
            Precision = 4
        )]
        private Vector3 m_Position;

        [SerializeField]
        [VectorEx(
            "Rotation",
            Options = AttributeOptionsFlags.Compact,
            Precision = 4
        )]
        private Quaternion m_Rotation;

        [SerializeField]
        [VectorEx(
            "Scale",
            Options = AttributeOptionsFlags.Compact,
            Precision = 4
        )]
        private Vector3 m_Scale;

        /// <summary>
        /// The index of the object to place
        /// </summary>
        public int Index => m_Index;

        /// <summary>
        /// Gets or sets the position
        /// </summary>
        public Vector3 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        /// <summary>
        /// Gets or sets the rotation
        /// </summary>
        public Quaternion Rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }

        /// <summary>
        /// Gets or sets the scale
        /// </summary>
        public Vector3 Scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        /// <summary>
        /// Gets a TRS matrix using Position, Rotation, Scale
        /// </summary>
        public Matrix4x4 Matrix => Matrix4x4.TRS(
            m_Position,
            m_Rotation,
            m_Scale
        );

        public CGSpot(int index) : this(
            index,
            Vector3.zero,
            Quaternion.identity,
            Vector3.one
        ) { }

        public CGSpot(int index, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            m_Index = index;
            m_Position = position;
            m_Rotation = rotation;
            m_Scale = scale;
        }

        /// <summary>
        /// Sets a transform to match Position, Rotation, Scale in local space
        /// </summary>
        /// <param name="transform"></param>
        public void ToTransform(Transform transform)
        {
            transform.localPosition = Position;
            transform.localRotation = Rotation;
            transform.localScale = Scale;
        }

        public bool Equals(CGSpot other)
            => m_Index == other.m_Index
               && m_Position.Equals(other.m_Position)
               && m_Rotation.Equals(other.m_Rotation)
               && m_Scale.Equals(other.m_Scale);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(
                    null,
                    obj
                ))
                return false;
            return obj is CGSpot && Equals((CGSpot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = m_Index;
                hashCode = (hashCode * 397) ^ m_Position.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Scale.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CGSpot left, CGSpot right)
            => left.Equals(right);

        public static bool operator !=(CGSpot left, CGSpot right)
            => !left.Equals(right);
    }

    #endregion

    #region ### Spline rasterization related ###

    #endregion
}