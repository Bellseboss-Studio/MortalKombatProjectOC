// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Base class for TRS Modules
    /// </summary>
    public abstract class TRSModuleBase : CGModule
    {
        #region ### Serialized Fields ###

        [SerializeField]
        [VectorEx]
        private Vector3 m_Transpose;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Rotation;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Scale = Vector3.one;

        #endregion

        #region ### Public Properties ###

        public Vector3 Transpose
        {
            get => m_Transpose;
            set
            {
                if (m_Transpose != value)
                {
                    m_Transpose = value;
                    Dirty = true;
                }
            }
        }

        public Vector3 Rotation
        {
            get => m_Rotation;
            set
            {
                if (m_Rotation != value)
                {
                    m_Rotation = value;
                    Dirty = true;
                }
            }
        }

        public Vector3 Scale
        {
            get => m_Scale;
            set
            {
                if (m_Scale != value)
                {
                    m_Scale = value;
                    Dirty = true;
                }
            }
        }

        public Matrix4x4 Matrix => Matrix4x4.TRS(
            Transpose,
            Quaternion.Euler(Rotation),
            Scale
        );

        #endregion

        #region ### Private Fields & Properties ###

        #endregion

        protected Matrix4x4 ApplyTrsOnShape([NotNull] CGShape shape)
        {
            Matrix4x4 mat = Matrix;
            Matrix4x4 scaleLessMatrix = Matrix4x4.TRS(
                Transpose,
                Quaternion.Euler(Rotation),
                Vector3.one
            );
            for (int i = 0; i < shape.Count; i++)
            {
                shape.Positions.Array[i] = mat.MultiplyPoint3x4(shape.Positions.Array[i]);
                shape.Normals.Array[i] = scaleLessMatrix.MultiplyVector(shape.Normals.Array[i]);
            }

            if (Scale != Vector3.one)
                shape.Recalculate();

            return scaleLessMatrix;
        }

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            Properties.LabelWidth = 50;
        }

        public override void Reset()
        {
            base.Reset();
            Transpose = Vector3.zero;
            Rotation = Vector3.zero;
            Scale = Vector3.one;
        }

#endif

        #endregion
    }
}