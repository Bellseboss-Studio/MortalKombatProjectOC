// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using ToolBuddy.Converter.Common.Listeners;
using ToolBuddy.Converter.Editor.Common.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;


namespace ToolBuddy.Converter.Editor.Common.Controllers
{
    /// <summary>
    /// Base conversion controller for converting splines from one type to another.
    /// </summary>
    /// <typeparam name="T">The type of spline to convert.</typeparam>
    public abstract class BaseConversionController<T> where T : Component
    {
        [NotNull]
        private readonly string converterId;

        private readonly BaseSplineListener<T> splineListener;

        private bool IsActive =>
            ConversionModel.Instance.ConversionType == converterId;

        protected BaseConversionController(
            BaseSplineListener<T> splineListener,
            [NotNull] string converterId)
        {
            this.converterId = converterId;
            this.splineListener = splineListener;
            onFirstEditorUpdate = OnFirstEditorUpdate;

            EditorApplication.update += onFirstEditorUpdate;
        }

        #region Delayed initialization

        private bool isInitialized;
        private EditorApplication.CallbackFunction onFirstEditorUpdate;

        private void OnFirstEditorUpdate()
        {
            //initialization is delayed until all the RequestProcessors from all the assemblies are loaded, so that the ConversionModel can validate the conversion type while being aware of all the valid conversion types
            if (isInitialized == false)
                Initialize();

            EditorApplication.update -= onFirstEditorUpdate;
        }

        private void Initialize()
        {
            ListenOnEvents();

            if (TryUpdateListenerRegistry())
                foreach (T registeredSpline in splineListener.RegisteredSplines)
                    ConvertSpline(registeredSpline);

            isInitialized = true;
        }

        #endregion

        /// <summary>
        /// Converts the spline given spline to the target spline type.
        /// </summary>
        protected abstract void ConvertSpline(
            T spline);

        /// <summary>
        /// Adds a new component of type <typeparamref name="TComponent"/> to a child object of the specified parent.
        /// </summary>
        /// <typeparam name="TComponent">The type of component to add.</typeparam>
        /// <param name="parent">The parent GameObject to which the child will be added.</param>
        /// <param name="childName">The name of the child GameObject.</param>
        /// <returns>The newly added component.</returns>
        /// <remarks>Handles undoing</remarks>
        protected static TComponent AddChildComponent<TComponent>(
            [NotNull] GameObject parent,
            [NotNull] string childName) where TComponent : Component
        {
            //todo unify the undo operations
            GameObject newObject = GameObjectExt.UndoableCreateGameObject(
                childName
            );
            newObject.transform.UndoableSetParent(
                parent.transform,
                false,
                "Set Object Parent"
            );
            return newObject.UndoableAddComponent<TComponent>();
        }

        /// <summary>
        /// Gets the splines to convert based on the specified conversion source.
        /// </summary>
        /// <param name="conversionSource">The conversion source.</param>
        /// <returns>The splines to convert.</returns>
        protected virtual IEnumerable<T> GetSplinesToConvert(
            ConversionSource conversionSource)
        {
            return conversionSource switch
            {
                ConversionSource.AllSplines => ObjectExt.FindObjectsOfTypeInCurrentStage<T>(
                    false,
                    false
                ),
                ConversionSource.SelectedSplines => DTSelection.GetAllAs<T>(),
                ConversionSource.None => Enumerable.Empty<T>(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(conversionSource),
                    conversionSource,
                    null
                )
            };
        }

        #region Event listeners

        private void ListenOnEvents()
        {
            splineListener.SplineChanged += OnSplineEvent;
            //update when model updates
            ConversionModel.Instance.Changed += OnNonSplineEvent;

            //update when scene opened
            //might be unnecessary because hierarchyChanged is called when scene is opened based on the doc
            EditorSceneManager.sceneOpened += (
                    _,
                    _) =>
                OnNonSplineEvent();

            //to handle first scene opening, when unity boots up (runtime's InitializeOnLoadMethod executes too early, before the scene is loaded)
            EditorSceneManager.activeSceneChangedInEditMode += (
                    _,
                    _) =>
                OnNonSplineEvent();

            //update when selection changes
            Selection.selectionChanged += OnNonSplineEvent;

            //update when enter/exit playmode or recompile
            EditorApplication.playModeStateChanged += _ => OnNonSplineEvent();

            //clear deleted splines from listened splines registry
            EditorApplication.hierarchyChanged += OnNonSplineEvent;
        }

        private void OnNonSplineEvent()
        {
            if (IsActive)
            {
                //TODO DESIGN: is this needed? Not important task.
                splineListener.UnregisterDeletedSplines();

                if (TryUpdateListenerRegistry())
                    foreach (T registeredSpline in splineListener.RegisteredSplines)
                        ConvertSpline(registeredSpline);
            }
            else
                splineListener.UnregisterAll();
        }

        private void OnSplineEvent(
            [NotNull] T spline)
        {
            if (IsActive)
                ConvertSpline(spline);
            else
                splineListener.UnregisterAll();
        }

        private bool TryUpdateListenerRegistry()
        {
            if (IsActive == false)
                return false;

            IEnumerable<T> splinesToConvert = GetSplinesToConvert(ConversionModel.Instance.ConversionSource);

            bool isListenerRegistryUpToDate =
                ((HashSet<T>)splineListener.RegisteredSplines)
                .SetEquals(splinesToConvert);

            if (isListenerRegistryUpToDate == false)
                UpdateListenerRegistry(splinesToConvert);

            return isListenerRegistryUpToDate == false;
        }

        private void UpdateListenerRegistry(
            IEnumerable<T> splinesToConvert)
        {
            splineListener.UnregisterAll();
            foreach (T spline in splinesToConvert)
                splineListener.Register(spline);
        }

        #endregion
    }
}
#endif