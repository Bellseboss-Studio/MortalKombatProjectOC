// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FluffyUnderware.DevToolsEditor
{
    public static class DTHandles
    {
        public static class SnapSettings
        {
            private static float s_MoveSnapX;
            private static float s_MoveSnapY;
            private static float s_MoveSnapZ;
            private static float s_ScaleSnap;
            private static float s_RotationSnap;
            private static bool s_Initialized;

            private static void Initialize()
            {
                if (!s_Initialized)
                {
                    s_MoveSnapX = EditorPrefs.GetFloat("MoveSnapX", 1f);
                    s_MoveSnapY = EditorPrefs.GetFloat("MoveSnapY", 1f);
                    s_MoveSnapZ = EditorPrefs.GetFloat("MoveSnapZ", 1f);
                    s_ScaleSnap = EditorPrefs.GetFloat("ScaleSnap", 0.1f);
                    s_RotationSnap = EditorPrefs.GetFloat("RotationSnap", 15f);
                    s_Initialized = true;
                }
            }

            public static Vector3 Move =>
                new Vector3(
                    MoveX,
                    MoveY,
                    MoveZ
                );

            public static float MoveX
            {
                get { Initialize(); return s_MoveSnapX; }
            }
            public static float MoveY
            {
                get { Initialize(); return s_MoveSnapY; }
            }
            public static float MoveZ
            {
                get { Initialize(); return s_MoveSnapZ; }
            }
            public static float Rotation
            {
                get { Initialize(); return s_RotationSnap; }
            }
            public static float ScaleSnap
            {
                get { Initialize(); return s_ScaleSnap; }
            }
        }

        public static bool MouseOverSceneView => (SceneView.currentDrawingSceneView != null && Event.current != null) && SceneView.currentDrawingSceneView.position.Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition));

        public static bool SceneViewIsSelected => EditorWindow.focusedWindow == SceneView.currentDrawingSceneView;

        private static readonly Stack<Color> mHandlesColorstack = new Stack<Color>();

        public static void PushHandlesColor(Color col)
        {
            mHandlesColorstack.Push(GUI.color);
            Handles.color = col;
        }

        public static void PopHandlesColor()
        {
            Handles.color = mHandlesColorstack.Pop();
        }

        private static readonly Stack<Matrix4x4> mHandlesMatrixstack = new Stack<Matrix4x4>();

        public static void PushMatrix(Matrix4x4 matrix)
        {
            mHandlesMatrixstack.Push(matrix);
            Handles.matrix = matrix;
        }

        public static void PopMatrix()
        {
            Handles.matrix = mHandlesMatrixstack.Pop();
        }


        public static void LabelIcon(Vector3 position, string text, Color labelColor)
        {
            GUIStyle iconStyle = GUI.skin.button;
            iconStyle.normal.textColor = labelColor;
            Handles.Label(position, text, iconStyle);
        }

        public static bool Button(int id, Vector3 position, Quaternion direction, float size, float pickSize, bool useHandleSize, Handles.CapFunction capFunc, Color hoverCol, out bool isHovering)
        {
            Event current = Event.current;
            isHovering = false;
            if (useHandleSize)
            {
                float s = HandleUtility.GetHandleSize(position);
                size *= s;
                pickSize *= s;
            }
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id)
                    {
                        GUIUtility.hotControl = id;
                        current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if ((current.button == 0 || current.button == 2) && GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        if (HandleUtility.nearestControl == id)
                        {
                            return true;
                        }
                    }
                    break;
                case EventType.MouseMove:
                    if ((HandleUtility.nearestControl == id && current.button == 0) || (GUIUtility.keyboardControl == id && current.button == 2))
                    {
                        HandleUtility.Repaint();
                    }
                    break;
                case EventType.Repaint:
                    {
                        Color color = Handles.color;
                        if (HandleUtility.nearestControl == id && GUI.enabled)
                        {
                            isHovering = true;
                            Handles.color = hoverCol;
                        }

                        capFunc(id, position, Quaternion.identity, size, EventType.Repaint);
                        Handles.color = color;
                        break;
                    }
                case EventType.Layout:
                    if (GUI.enabled)
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, pickSize));
                    }
                    break;
            }
            return false;
        }

        public static bool PositionHandle(Vector3 position, Quaternion rotation, out Vector3 delta)
        {
            Vector3 p = Handles.PositionHandle(position, rotation);
            delta = p - position;
            return delta != Vector3.zero;
        }

        public static bool RotationHandle(Vector3 position, Quaternion rotation, out Quaternion newRotation)
        {
            newRotation = Handles.RotationHandle(rotation, position);
            return (newRotation != rotation);
        }

        private static Vector2 s_StartMousePosition;
        private static Vector2 s_CurrentMousePosition;
        private static float s_StartScale;

        public static bool ScaleSlider(float scale, Vector3 position, Vector3 direction, Quaternion rotation, float length, float size, float snap, out float delta)
        {
            int id = GUIUtility.GetControlID("ScaleSliderHash".GetHashCode(), FocusType.Keyboard);
            float newScale = scale;
            Event current = Event.current;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if ((HandleUtility.nearestControl == id && current.button == 0) || (GUIUtility.keyboardControl == id && current.button == 2))
                    {
                        GUIUtility.keyboardControl = id;
                        GUIUtility.hotControl = id;
                        s_CurrentMousePosition = (s_StartMousePosition = current.mousePosition);
                        s_StartScale = scale;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (current.button == 0 || current.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += current.delta;
                        float num = 1f + HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, position, direction) * 0.1f;
                        num = Handles.SnapValue(num, snap);
                        newScale = s_StartScale * num;
                        GUI.changed = true;
                        current.Use();
                    }
                    break;
                case EventType.Repaint:
                    {
                        Color color = Color.white;
                        if (id == GUIUtility.keyboardControl)
                        {
                            color = Handles.color;
                            Handles.color = Handles.selectedColor;
                        }
                        //float num2 = size;
                        //if (GUIUtility.hotControl == id)
                        //{
                        //    num2 = size * scale / s_StartScale;
                        //}
                        Handles.CubeHandleCap(id, position + direction * length, rotation, HandleUtility.GetHandleSize(position) * size, EventType.Repaint);

                        Handles.DrawLine(position, position + direction * length);
                        if (id == GUIUtility.keyboardControl)
                        {
                            Handles.color = color;
                        }
                        break;
                    }
                case EventType.Layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + direction * length));
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + direction * length, size));
                    break;
            }
            delta = newScale - scale;

            return (scale != newScale);
        }

        public static Quaternion RotationHandle(Quaternion rotation, Vector3 position, float size)
        {
            Color staticColor = new Color(
                0.5f,
                0.5f,
                0.5f,
                0f
            );
            float staticBlend = 0.6f;
            float handleSize = HandleUtility.GetHandleSize(position) * size;
            Color color = Handles.color;
            bool flag = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);
            Handles.color = ((!flag) ? Handles.xAxisColor : Color.Lerp(Handles.xAxisColor, staticColor, staticBlend));
            rotation = Handles.Disc(rotation, position, rotation * Vector3.right, handleSize, true, SnapSettings.Rotation);
            Handles.color = ((!flag) ? Handles.yAxisColor : Color.Lerp(Handles.yAxisColor, staticColor, staticBlend));
            rotation = Handles.Disc(rotation, position, rotation * Vector3.up, handleSize, true, SnapSettings.Rotation);
            Handles.color = ((!flag) ? Handles.zAxisColor : Color.Lerp(Handles.zAxisColor, staticColor, staticBlend));
            rotation = Handles.Disc(rotation, position, rotation * Vector3.forward, handleSize, true, SnapSettings.Rotation);
            if (!flag)
            {
                Handles.color = Handles.centerColor;
                rotation = Handles.Disc(rotation, position, Camera.current.transform.forward, handleSize * 1.1f, false, 0f);
                rotation = Handles.FreeRotateHandle(rotation, position, handleSize);
            }
            Handles.color = color;
            return rotation;
        }

        /// <summary>
        /// Draws a 3D position handle
        /// </summary>
        /// <returns>The updated position</returns>
        /// <seealso cref="GUIUtility.GetControlID(int,FocusType)"/>
        public static Vector3 PositionHandle(int controlId1, int controlId2, int controlId3, Vector3 position, Quaternion rotation, float handleSize, float rectangleToSliderRatio = 0.15f, Color additionalColor = new Color())
        {
            float sliderSize = HandleUtility.GetHandleSize(position) * handleSize;
            float rectangleSize = sliderSize * rectangleToSliderRatio;

            Vector3 snap = SnapSettings.Move;

            Vector3 axis1 = rotation * Vector3.right;
            Vector3 axis2 = rotation * Vector3.up;
            Vector3 axis3 = rotation * Vector3.forward;
            Color axis1Color = new Color(0.9f, 0.3f, 0.1f) + additionalColor;
            Color axis2Color = new Color(0.6f, 0.9f, 0.3f) + additionalColor;
            Color axis3Color = new Color(0.2f, 0.4f, 0.9f) + additionalColor;

            Vector3 updatedPosition = position;

            Handles.color = axis1Color;
            updatedPosition += Handles.Slider(position, axis1, sliderSize, Handles.ArrowHandleCap, snap.x) - position;
            updatedPosition += Handles.Slider2D(controlId1, position, rectangleSize * (axis3 + axis2), Vector3.Cross(axis3, axis2), axis2, axis3, rectangleSize, Handles.RectangleHandleCap, new Vector2(snap.y, snap.z)) - position;

            Handles.color = axis2Color;
            updatedPosition += Handles.Slider(position, axis2, sliderSize, Handles.ArrowHandleCap, snap.y) - position;
            updatedPosition += Handles.Slider2D(controlId2, position, rectangleSize * (axis1 + axis3), Vector3.Cross(axis1, axis3), axis3, axis1, rectangleSize, Handles.RectangleHandleCap, new Vector2(snap.z, snap.x)) - position;

            Handles.color = axis3Color;
            updatedPosition += Handles.Slider(position, axis3, sliderSize, Handles.ArrowHandleCap, snap.z) - position;
            updatedPosition += Handles.Slider2D(controlId3, position, rectangleSize * (axis1 + axis2), Vector3.Cross(axis1, axis2), axis2, axis1, rectangleSize, Handles.RectangleHandleCap, new Vector2(snap.y, snap.x)) - position;

            return updatedPosition;
        }

        /// <summary>
        /// Draws a 2D position handle
        /// </summary>
        /// <param name="size"></param>
        /// <param name="plane">0 for XY, 1 for XZ and 2 for YZ</param>
        /// <param name="controlId"><see cref="GUIUtility.GetControlID(int,FocusType)"/></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <returns>The updated position</returns>
        public static Vector3 PositionHandle2D(int controlId, Vector3 position, Quaternion rotation, float size, int plane = 0)
        {
            float sliderSize = HandleUtility.GetHandleSize(position) * size;
            float rectSize = sliderSize * 0.15f;

            Vector3 snap = SnapSettings.Move;

            Vector3 axis1;
            Vector3 axis2;
            Color axis1Color;
            Color axis2Color;
            Color planeColor;
            {
                Color xColor = new Color(
                    0.9f,
                    0.3f,
                    0.1f
                );
                Color yColor = new Color(
                    0.6f,
                    0.9f,
                    0.3f
                );
                Color zColor = new Color(
                    0.2f,
                    0.4f,
                    0.9f
                );

                switch (plane)
                {
                    //XY
                    case 0:
                        axis1 = rotation * Vector3.right;
                        axis2 = rotation * Vector3.up;
                        axis1Color = xColor;
                        axis2Color = yColor;
                        planeColor = zColor;
                        break;
                    //XZ
                    case 1:
                        axis1 = rotation * Vector3.right;
                        axis2 = rotation * Vector3.forward;
                        axis1Color = xColor;
                        axis2Color = zColor;
                        planeColor = yColor;
                        break;
                    //YZ
                    case 2:
                        axis1 = rotation * Vector3.up;
                        axis2 = rotation * Vector3.forward;
                        axis1Color = yColor;
                        axis2Color = zColor;
                        planeColor = xColor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(plane));
                }
            }

            Vector3 updatedPosition = position;

            Handles.color = axis1Color;
            updatedPosition += Handles.Slider(position, axis1, sliderSize, Handles.ArrowHandleCap, snap.x) - position;

            Handles.color = axis2Color;
            updatedPosition += Handles.Slider(position, axis2, sliderSize, Handles.ArrowHandleCap, snap.y) - position;

            Handles.color = planeColor;
            updatedPosition += Handles.Slider2D(controlId, position, rectSize * (axis1 + axis2), Vector3.Cross(axis1, axis2), axis2, axis1, rectSize, Handles.RectangleHandleCap, new Vector2(snap.x, snap.y)) - position;

            return updatedPosition;
        }

        public static Vector3 TinyHandle2D(int id, Vector3 position, Quaternion rotation, float size, Handles.CapFunction func = null)
            => TinyHandle2D(id, position, rotation * Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, size, func);

        public static Vector3 TinyHandle2D(int id, Transform transform, float size, Handles.CapFunction func = null)
            => TinyHandle2D(id, transform.position, transform.forward, transform.up, transform.right, size, func);

        public static Vector3 TinyHandle2D(int id, Vector3 pos, Vector3 forward, Vector3 up, Vector3 right, float size, Handles.CapFunction func = null)
            => Handles.Slider2D(id, pos, forward, up, right, HandleUtility.GetHandleSize(pos) * size, func, SnapSettings.Move);

        public static void DrawSolidRectangleWithOutline(Vector2 center, Vector2 extends, Color backgroundColor, Color outlineColor)
        {
            DrawSolidRectangleWithOutline(new Rect(center.x - extends.x / 2, center.y - extends.y / 2, extends.x, extends.y), backgroundColor, outlineColor);
        }

        public static void DrawSolidRectangleWithOutline(Rect r, Color backgroundColor, Color outlineColor)
        {
            Vector3[] verts = new Vector3[4]
            {
                new Vector3(
                    r.xMin,
                    r.yMin,
                    0
                ),
                new Vector3(
                    r.xMax,
                    r.yMin,
                    0
                ),
                new Vector3(
                    r.xMax,
                    r.yMax,
                    0
                ),
                new Vector3(
                    r.xMin,
                    r.yMax,
                    0
                )
            };
            Handles.DrawSolidRectangleWithOutline(verts, backgroundColor, outlineColor);
        }

        public static void DrawOutline(Rect r, Color outlineColor)
        {
            DrawSolidRectangleWithOutline(r, new Color(0, 0, 0, 0), outlineColor);
        }

        public static void WireCubeCap(Vector3 position, Vector3 size)
        {
            Vector3 half = size / 2;
            // draw front
            Handles.DrawLine(position + new Vector3(-half.x, -half.y, half.z), position + new Vector3(half.x, -half.y, half.z));
            Handles.DrawLine(position + new Vector3(-half.x, -half.y, half.z), position + new Vector3(-half.x, half.y, half.z));
            Handles.DrawLine(position + new Vector3(half.x, half.y, half.z), position + new Vector3(half.x, -half.y, half.z));
            Handles.DrawLine(position + new Vector3(half.x, half.y, half.z), position + new Vector3(-half.x, half.y, half.z));
            // draw back
            Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(half.x, -half.y, -half.z));
            Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(-half.x, half.y, -half.z));
            Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(half.x, -half.y, -half.z));
            Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(-half.x, half.y, -half.z));
            // draw corners
            Handles.DrawLine(position + new Vector3(-half.x, -half.y, -half.z), position + new Vector3(-half.x, -half.y, half.z));
            Handles.DrawLine(position + new Vector3(half.x, -half.y, -half.z), position + new Vector3(half.x, -half.y, half.z));
            Handles.DrawLine(position + new Vector3(-half.x, half.y, -half.z), position + new Vector3(-half.x, half.y, half.z));
            Handles.DrawLine(position + new Vector3(half.x, half.y, -half.z), position + new Vector3(half.x, half.y, half.z));
        }

        public static void ArrowCap(Vector3 position, Vector3 direction, Color outlineColor, float length, float lineWidth = 0.4f, float headWidth = 1f, float headRatio = 0.3f, bool useHandleSize = true)
        {
            float size = (useHandleSize) ? HandleUtility.GetHandleSize(position) : 1;

            ArrowCap(position, direction, Vector3.up, outlineColor, length, lineWidth, headWidth, headRatio, size);
        }

        public static void ArrowCap(Vector3 position, Vector3 direction, Vector3 upDirection, Color outlineColor, float length, float lineWidth, float headWidth, float headRatio, float sizeMultiplier)
        {
            Vector3[] arrow = new Vector3[8];
            Vector3 start = position;
            length *= sizeMultiplier;
            Vector3 end = position + direction * length;

            Vector3 right = Vector3.Cross(upDirection, direction).normalized;

            float hh = length * headRatio;
            Vector3 rlw2 = right * lineWidth / 2 * sizeMultiplier;
            Vector3 rhw2 = right * headWidth / 2 * sizeMultiplier;
            arrow[0] = start - rlw2;
            arrow[1] = end - direction * hh - rlw2;
            arrow[2] = end - direction * hh - rhw2;
            arrow[3] = end;
            arrow[4] = end - direction * hh + rhw2;
            arrow[5] = end - direction * hh + rlw2;
            arrow[6] = start + rlw2;
            arrow[7] = arrow[0];

            Handles.DrawAAConvexPolygon(arrow[2], arrow[3], arrow[4]);
            Handles.DrawAAConvexPolygon(arrow[0], arrow[1], arrow[5], arrow[6]);
            if (Handles.color != outlineColor)
            {
                Color c = Handles.color;
                Handles.color = outlineColor;
                Handles.DrawAAPolyLine(arrow);
                Handles.color = c;
            }
        }

        public static void BoundsCap(Bounds b)
        {
            WireCubeCap(b.center, b.size);
        }

        public static Vector3 TranslateByPixel(Vector3 position, float x, float y)
            => TranslateByPixel(position, new Vector3(x, y));

        public static Vector3 TranslateByPixel(Vector3 position, Vector3 translateBy)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            if (cam)
                return cam.ScreenToWorldPoint(cam.WorldToScreenPoint(position) + translateBy);
            else
                return position;
        }

        public static Vector3 TranslateAligned(Vector3 position, GUIContent content, TextAlignment alignment = TextAlignment.Center, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;
            float w = style.CalcSize(content).x;
            switch (alignment)
            {
                case TextAlignment.Center:
                    return TranslateByPixel(position, new Vector3(-w / 2, 0, 0));
                case TextAlignment.Right:
                    return TranslateByPixel(position, new Vector3(w / 2, 0, 0));
                default:
                    return position;
            }

        }

        private static bool ContainsStatic(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i].isStatic)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
