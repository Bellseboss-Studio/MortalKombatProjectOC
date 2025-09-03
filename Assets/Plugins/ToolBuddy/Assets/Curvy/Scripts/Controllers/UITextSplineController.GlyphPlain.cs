// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools.Extensions;
using UnityEngine;

namespace FluffyUnderware.Curvy.Controllers
{
    public partial class UITextSplineController
    {
        protected class GlyphPlain : IGlyph
        {
            public Vector3[] V = new Vector3[4];
            public Rect Rect;
            public Vector3 Center => Rect.center;

            public void Load(ref Vector3[] verts, int index)
            {
                V[0] = verts[index];
                V[1] = verts[index + 1];
                V[2] = verts[index + 2];
                V[3] = verts[index + 3];

                calcRect();
            }

            public void calcRect() =>
                Rect = new Rect(
                    V[0].x,
                    V[2].y,
                    V[2].x - V[0].x,
                    V[0].y - V[2].y
                );

            public void Save(ref Vector3[] verts, int index)
            {
                verts[index] = V[0];
                verts[index + 1] = V[1];
                verts[index + 2] = V[2];
                verts[index + 3] = V[3];
            }

            public void Transpose(Vector3 v)
            {
                for (int i = 0; i < 4; i++)
                    V[i] += v;
            }

            public void Rotate(Quaternion rotation)
            {
                for (int i = 0; i < 4; i++)
                    V[i] = V[i].RotateAround(
                        Center,
                        rotation
                    );
            }
        }
    }
}