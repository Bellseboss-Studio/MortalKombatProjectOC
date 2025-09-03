// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// GameObject data (Bounds + Object)
    /// </summary>
    [CGDataInfo("#FFF59D")]
    public class CGGameObject : CGBounds
    {
        public GameObject Object;
        public Vector3 Translate;
        public Vector3 Rotate;
        public Vector3 Scale = Vector3.one;

        public Matrix4x4 Matrix => Matrix4x4.TRS(
            Translate,
            Quaternion.Euler(Rotate),
            Scale
        );

        public CGGameObject() { }

        public CGGameObject(CGGameObjectProperties properties) : this(
            properties.Object,
            properties.Translation,
            properties.Rotation,
            properties.Scale
        ) { }

        public CGGameObject(GameObject obj) : this(
            obj,
            Vector3.zero,
            Vector3.zero,
            Vector3.one
        ) { }

        public CGGameObject(GameObject obj, Vector3 translate, Vector3 rotate, Vector3 scale)
        {
            Object = obj;
            Translate = translate;
            Rotate = rotate;
            Scale = scale;
            if (Object)
                Name = Object.name;
        }

        public CGGameObject(CGGameObject source) : base(source)
        {
            Object = source.Object;
            Translate = source.Translate;
            Rotate = source.Rotate;
            Scale = source.Scale;
        }

        public override T Clone<T>()
            => new CGGameObject(this) as T;

        public override void RecalculateBounds()
        {
            if (Object == null)
                mBounds = new Bounds();
            else
            {
                Renderer[] renderer = Object.GetComponentsInChildren<Renderer>(true);
                Collider[] collider = Object.GetComponentsInChildren<Collider>(true);
                Bounds bounds;
                if (renderer.Length > 0)
                {
                    bounds = renderer[0].bounds;
                    for (int i = 1; i < renderer.Length; i++)
                        bounds.Encapsulate(renderer[i].bounds);
                    for (int i = 0; i < collider.Length; i++)
                        bounds.Encapsulate(collider[i].bounds);
                }
                else if (collider.Length > 0)
                {
                    bounds = collider[0].bounds;
                    for (int i = 1; i < collider.Length; i++)
                        bounds.Encapsulate(collider[i].bounds);
                }
                else
                    bounds = new Bounds();

                Vector3 rotationlessBoundsSize = Quaternion.Inverse(Object.transform.localRotation) * bounds.size;
                bounds.size = new Vector3(
                    rotationlessBoundsSize.x * Scale.x,
                    rotationlessBoundsSize.y * Scale.y,
                    rotationlessBoundsSize.z * Scale.z
                );

                mBounds = bounds;
            }
        }
    }
}