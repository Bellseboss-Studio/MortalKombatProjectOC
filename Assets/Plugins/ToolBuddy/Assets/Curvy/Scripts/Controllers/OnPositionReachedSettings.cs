// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Settings for events to be triggered when the controller reaches a specific position
    /// </summary>
    [Serializable]
    public class OnPositionReachedSettings : ISerializationCallbackReceiver
    {
        public string Name;
        public CurvySplineMoveEvent Event = new CurvySplineMoveEvent();
        public float Position;
        public CurvyPositionMode PositionMode;
        public TriggeringDirections TriggeringDirections;
        public Color GizmoColor;

        #region handling default values

        public OnPositionReachedSettings() =>
            InitializeFieldsWithDefaultValue();

        [SerializeField, HideInInspector]
        private bool initialized;

        /// <summary>
        /// Default values assigned at field initialization or at construction are overriden with default type values when instances of this class are added to a list. This method is used to fix that issue
        /// </summary>
        private void InitializeFieldsWithDefaultValue()
        {
            Name = "My Event";
            PositionMode = CurvyPositionMode.WorldUnits;
            TriggeringDirections = TriggeringDirections.All;
            GizmoColor = new Color(
                0.652f,
                0.652f,
                0.652f
            );
            initialized = true;
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            //when an instance of this class is added to a list via the inspector, its fields are set to their types default values (0, false, ...). I try to detect that via this test, then assign the default values I want
            if (initialized == false)
                InitializeFieldsWithDefaultValue();
        }

        #endregion

        public OnPositionReachedSettings Clone()
            => (OnPositionReachedSettings)MemberwiseClone();
    }
}