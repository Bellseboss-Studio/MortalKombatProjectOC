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
    /// <summary>
    /// Helper class used by InputMesh module
    /// </summary>
    [Serializable]
    public class CGMeshProperties
    {
        [SerializeField]
        private Mesh m_Mesh;

        [SerializeField]
        private Material[] m_Material = new Material[0];

        [SerializeField]
        [VectorEx]
        private Vector3 m_Translation;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Rotation;

        [SerializeField]
        [VectorEx]
        private Vector3 m_Scale = Vector3.one;


        public Mesh Mesh
        {
            get => m_Mesh;
            set
            {
                m_Mesh = value;
                if (m_Mesh && m_Mesh.subMeshCount != m_Material.Length)
                    Array.Resize(
                        ref m_Material,
                        m_Mesh.subMeshCount
                    );
            }
        }

        public Material[] Material
        {
            get => m_Material;
            set => m_Material = value;
        }

        public Vector3 Translation
        {
            get => m_Translation;
            set => m_Translation = value;
        }

        public Vector3 Rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }

        public Vector3 Scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        public Matrix4x4 Matrix => Matrix4x4.TRS(
            Translation,
            Quaternion.Euler(Rotation),
            Scale
        );

        public CGMeshProperties() { }

        public CGMeshProperties(Mesh mesh)
        {
            Mesh = mesh;
            Material = mesh != null
                ? new Material[mesh.subMeshCount]
                : new Material[0];
        }

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        public void OnValidate() =>
            Mesh = m_Mesh;
#endif
    }
}