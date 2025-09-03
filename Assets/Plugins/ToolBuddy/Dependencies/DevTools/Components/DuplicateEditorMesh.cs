// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using System.Collections;


namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// Add this script to a GameObject with a MeshFilter to ensure it will be properly duplicated in the editor!
    /// </summary>
    /// <remarks>On Duplicating, Awake() checks if the sharedMesh is already used in the scene. If yes, a new mesh will be created to ensure that each sharedMesh is unique</remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [JetBrains.Annotations.UsedImplicitly] [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
    public abstract class DuplicateEditorMesh : DTVersionedMonoBehaviour
    {
        private MeshFilter mFilter;

        public MeshFilter Filter
        {
            get
            {
                if (ReferenceEquals(mFilter, null))
                    mFilter = GetComponent<MeshFilter>();
                return mFilter;
            }
        }
    }
}
