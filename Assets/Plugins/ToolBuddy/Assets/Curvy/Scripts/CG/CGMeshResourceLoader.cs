// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Mesh resource loader class
    /// </summary>
    public class CGMeshResourceLoader : ICGResourceLoader
    {
        [EnvironmentAgnosticInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        protected static void InitializeOnLoad() =>
            CGResourceHandler.RegisterResourceLoader(
                "Mesh",
                new CGMeshResourceLoader()
            );

        public Component Create(CGModule cgModule, string context)
            => cgModule.Generator.PoolManager.GetComponentPool<CGMeshResource>().Pop();

        public void Destroy(CGModule cgModule, Component obj, string context, bool kill)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(obj.GetComponent<CGMeshResource>() != null);
#endif
            if (obj != null)
            {
                if (kill)
                    obj.gameObject.Destroy(
                        false,
                        false
                    );
                else
                {
                    obj.StripComponents(
                        typeof(CGMeshResource),
                        typeof(MeshFilter),
                        typeof(MeshRenderer)
                    );
                    cgModule.Generator.PoolManager.GetComponentPool<CGMeshResource>().Push(obj);
                }
            }
        }
    }
}