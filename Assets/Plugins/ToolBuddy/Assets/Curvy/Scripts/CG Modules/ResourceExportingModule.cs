// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// A CGModule that creates managed resources that can be exported
    /// </summary>
    public abstract class ResourceExportingModule : CGModule
    {
        /// <summary>
        /// Save the created resource(s) to the scene
        /// </summary>
        /// <param name="parent">the parent transform to which the saved resource(s) GameObject(s) will be attached. If null, saved resource(s) GameObject(s) will be at the hierarchy's root</param>
        /// <returns>The created GameObject</returns>
        public GameObject SaveToScene(Transform parent = null)
        {
            List<Component> managedResources;
            GetManagedResources(
                out managedResources,
                out _
            );
            if (managedResources.Count == 0)
                return null;

            GameObject result = new GameObject($"{ModuleName} Exported Resources");
            result.transform.parent = parent;
            for (int i = 0; i < managedResources.Count; i++)
                SaveResourceToScene(
                    managedResources[i],
                    result.transform
                );

            result.transform.position = transform.position;
            result.transform.rotation = transform.rotation;
            result.transform.localScale = transform.localScale;
            return result;
        }

        /// <summary>
        /// Save a specific resource to the scene as a GameObject
        /// </summary>
        /// <returns>The saved GameObject</returns>
        protected abstract GameObject SaveResourceToScene(Component managedResource, Transform newParent);
    }
}