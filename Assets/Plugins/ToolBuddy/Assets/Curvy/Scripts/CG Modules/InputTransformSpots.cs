// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Input/Transform Spots",
        ModuleName = "Input Transform Spots",
        Description = "Defines an array of placement spots taken from existing Transforms"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cginputtransformspots")]
    public class InputTransformSpots : CGModule
    {
        [HideInInspector]
        [OutputSlotInfo(typeof(CGSpots))]
        public CGModuleOutputSlot OutSpots = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [ArrayEx]
        [SerializeField]
        private List<TransformSpot> transformSpots = new List<TransformSpot>();

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// The input <see cref="TransformSpots"/>
        /// </summary>
        public List<TransformSpot> TransformSpots
        {
            get => transformSpots;
            set
            {
                if (transformSpots != value)
                {
                    transformSpots = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

        public override void Reset()
        {
            base.Reset();
            TransformSpots.Clear();
        }

        [UsedImplicitly]
        private void Update()
        {
            if (Dirty == false && OutSpots.Data.Length != 0)
                foreach (KeyValuePair<CGSpot, TransformSpot> keyValuePair in outputToInputDictionary)
                {
                    CGSpot cgSpot = keyValuePair.Key;
                    TransformSpot transformSpot = keyValuePair.Value;
                    if (cgSpot.Position != transformSpot.Transform.position)
                    {
                        Dirty = true;
                        return;
                    }
                }
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();

            if (OutSpots.IsLinked)
            {
                outputToInputDictionary.Clear();

                List<CGSpot> spots = TransformSpots.Where(s => s.Transform != null).Select(
                    s =>
                    {
                        CGSpot cgSpot = new CGSpot(
                            s.Index,
                            s.Transform.position,
                            s.Transform.rotation,
                            s.Transform.lossyScale
                        );
                        outputToInputDictionary[cgSpot] = s;
                        return cgSpot;
                    }
                ).ToList();

                OutSpots.SetDataToElement(new CGSpots(spots));
            }

#if UNITY_EDITOR
            if (TransformSpots.Exists(s => s.Transform == null))
                UIMessages.Add("Missing Transform input");
#endif
        }

        #endregion

        #region ### Privates ###

        private readonly Dictionary<CGSpot, TransformSpot> outputToInputDictionary = new Dictionary<CGSpot, TransformSpot>();

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (Application.isPlaying == false)
                Update();
        }

#endif

        #endregion

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            outputToInputDictionary.Clear();
        }
#endif

        /// <summary>
        /// Similar to <see cref="CGSpot"/>, but instead of having a constant position/rotation/scale, it is taken from a Transform
        /// </summary>
        [Serializable]
        public struct TransformSpot : IEquatable<TransformSpot>
        {
           
            /// <summary>
            /// Initializes a new instance of the <see cref="TransformSpot"/> struct.
            /// </summary>
            /// <param name="index">The index of the object to place.</param>
            /// <param name="transform">The Transform from which the spot's position/rotation/scale should be taken.</param>
            public TransformSpot(
                int index,
                Transform transform)
            {
                this.index = index;
                this.transform = transform;
            }


            [SerializeField]
#pragma warning disable 649 //field is modified through InputTransformSpotsEditor, through Unity's serialization API
            private int index;
#pragma warning restore 649

            [SerializeField]
#pragma warning disable 649 //field is modified through InputTransformSpotsEditor, through Unity's serialization API
            private Transform transform;
#pragma warning restore 649

            /// <summary>
            /// The index of the object to place
            /// </summary>
            public int Index => index;

            /// <summary>
            /// The Transform from which the spot's position/rotation/scale should be taken
            /// </summary>
            public Transform Transform => transform;

            public bool Equals(TransformSpot other)
                => index == other.index
                && Equals(
                    transform,
                    other.transform
                );

            public override bool Equals(object obj)
                => obj is TransformSpot other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (index * 397)
                           ^ (transform != null
                               ? transform.GetHashCode()
                               : 0);
                }
            }

            public static bool operator ==(TransformSpot left, TransformSpot right)
                => left.Equals(right);

            public static bool operator !=(TransformSpot left, TransformSpot right)
                => !left.Equals(right);
        }
    }
}