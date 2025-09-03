// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace FluffyUnderware.Curvy.Controllers
{
    public partial class UITextSplineController
    {
        protected class GlyphQuad : IGlyph
        {
            public UIVertex[] V = new UIVertex[4];
            public Rect Rect;
            public Vector3 Center => Rect.center;

            public void Load(List<UIVertex> verts, int index)
            {
                V[0] = verts[index];
                V[1] = verts[index + 1];
                V[2] = verts[index + 2];
                V[3] = verts[index + 3];

                calcRect();
            }

            public void LoadTris(List<UIVertex> verts, int index)
            {
                V[0] = verts[index];
                V[1] = verts[index + 1];
                V[2] = verts[index + 2];
                V[3] = verts[index + 4];
                calcRect();
            }

            public void calcRect() =>
                Rect = new Rect(
                    V[0].position.x,
                    V[2].position.y,
                    V[2].position.x - V[0].position.x,
                    V[0].position.y - V[2].position.y
                );

            public void Save(List<UIVertex> verts, int index)
            {
                verts[index] = V[0];
                verts[index + 1] = V[1];
                verts[index + 2] = V[2];
                verts[index + 3] = V[3];
            }

            public void Save(VertexHelper vh) =>
                vh.AddUIVertexQuad(V);

            public void Transpose(Vector3 v)
            {
                for (int i = 0; i < 4; i++)
                    V[i].position += v;
            }

            public void Rotate(Quaternion rotation)
            {
                for (int i = 0; i < 4; i++)
                    V[i].position = V[i].position.RotateAround(
                        Center,
                        rotation
                    );
            }
        }
    }
}