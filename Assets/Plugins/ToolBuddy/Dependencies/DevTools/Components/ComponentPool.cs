// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
#if CURVY_SANITY_CHECKS_PRIVATE
using UnityEngine.Assertions;
#endif
using UnityEngine.SceneManagement;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// A pool for UnityEngine.ComponentPool instances
    /// </summary>
    [HelpURL(DTUtility.HelpUrlBase + "dtcomponentpool")]
    public class ComponentPool : UnityObjectPool<Component>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private string m_Identifier;

        /// <summary>
        /// Due to bad design decisions, Identifier is used to store the type of the pooled objects. And the setter does nothing
        /// </summary>
        //todo dissociate Identifier with type, and then potentially move Identifier in base class
        public override string Identifier
        {
            get
            {
#if CURVY_SANITY_CHECKS_PRIVATE
                Assert.IsNotNull(m_Identifier);
#endif
                return m_Identifier;
            }
            set
            {
                throw new InvalidOperationException("Component pool's identifier should always indicate the pooled type's assembly qualified name");
                /*Here is why:
                In the Initialize method, m_Identifier is set as a fully qualified type name.
                The Type getter uses m_Identifier as a fully qualified type name.
                If needed, add a field that contains the pooled type name, and use it instead of Identifier when you need to find the right pool for the right component type*/
            }
        }

        /// <summary>
        /// The type of the pooled objects
        /// </summary>
        public Type Type
        {
            get
            {
                Type type = Type.GetType(Identifier);
                if (type == null)
                    DTLog.LogWarning("[DevTools] ComponentPool's Type is an unknown type " + m_Identifier, this);
                return type;
            }
        }


        public void Initialize(Type type, PoolSettings settings)
        {
            string typeAssemblyQualifiedName = type.AssemblyQualifiedName;
            if (typeAssemblyQualifiedName == null)
                throw new InvalidOperationException();

            m_Identifier = typeAssemblyQualifiedName;
            //todo design: once you fix the problematic Identifier setter, you could include the Identifier and the above line in UnityObjectPool
            Initialize(settings);
        }

        protected override Component CreateObject()
        {
            Type componentType = Type;
            if (componentType == null)
                throw new InvalidOperationException($"[DevTools] ComponentPool {m_Identifier} could not create component because the associated type is null");

            GameObject go = new GameObject();
            ConfigureCreatedGameObject(go, Identifier);
            return go.AddComponent(componentType);
        }

        protected override GameObject GetItemGameObject(Component item)
            => item.gameObject;

        #region Obsolete

        [JetBrains.Annotations.UsedImplicitly] [Obsolete]
        public void OnSceneLoaded(Scene scn, LoadSceneMode mode)
        {

        }

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use other Pop method instead")]
        public T Pop<T>(Transform parent) where T : Component
            => Pop(parent) as T;

        #endregion

        #region ISerializationCallbackReceiver
        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (Type.GetType(m_Identifier) == null)
            {

                //Handles cases where the component is migrated to another assembly (for example if using Unity's Assembly Definitions feature

                const char separator = ',';
                const string separatorString = ",";
                string[] splittedAssemblyQualifiedName = m_Identifier.Split(separator);
                if (splittedAssemblyQualifiedName.Length >= 5)
                {
                    string typeName = String.Join(separatorString, splittedAssemblyQualifiedName.SubArray(0, splittedAssemblyQualifiedName.Length - 4));
                    //As you can see with this example: 
                    //"FluffyUnderware.Curvy.CurvySplineSegment, ToolBuddy.Curvy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
                    //the 4 last elements do not contain the type name. Keep in mind that a type name can include a ',' like  Dictionary<int, List<double>>

#if UNITY_EDITOR
                    UnityEditor.TypeCache.TypeCollection knownTypes = UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(System.Object));
#else

                    Type[] knownTypes = TypeExt.GetLoadedTypes();

#endif
                    Type type = knownTypes.FirstOrDefault(t => t.FullName == typeName);
                    if (type != null)
                    {
                        m_Identifier = type.AssemblyQualifiedName;
#if CURVY_SANITY_CHECKS_PRIVATE
                        Assert.IsNotNull(m_Identifier);
#endif
                    }
                }
            }
        }
        #endregion
    }
}
