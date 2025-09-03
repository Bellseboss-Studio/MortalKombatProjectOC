// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Diagnostics;
using System.Text;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace FluffyUnderware.DevTools
{
    public static class DTUtility
    {
        /// <summary>
        /// The base Url for DT components' documentation
        /// </summary>
        public const string HelpUrlBase = "https://curvyeditor.com/doclink/";

#if UNITY_EDITOR
        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        private static MethodInfo mGetBuiltinExtraResourcesMethod;
#endif

        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        public static Material GetDefaultMaterial()
        {
#if UNITY_EDITOR
            if (mGetBuiltinExtraResourcesMethod == null)
            {
                BindingFlags bfs = BindingFlags.NonPublic | BindingFlags.Static;
                mGetBuiltinExtraResourcesMethod = typeof(EditorGUIUtility).GetMethod("GetBuiltinExtraResource", bfs);
            }

            Material result;
            if (mGetBuiltinExtraResourcesMethod == null)
            {
                result = null;
                Debug.LogError("Couldn't find method GetBuiltinExtraResource in type UnityEditor.EditorGUIUtility");
            }
            else
                result = (Material)mGetBuiltinExtraResourcesMethod.Invoke(null, new object[] { typeof(Material), "Default-Diffuse.mat" });

            return result;
#else
            return null;
#endif
        }


        public static bool IsEditorStateChange
        {
            get
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                    return true;
                else
#endif
                    return false;
            }
        }


#if UNITY_EDITOR
        private const string INDENT_STRING = "    ";
        public static string FormatJson(string str)
        {
            int indent = 0;
            bool quoted = false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        int index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
#endif

        /// <summary>
        /// Much like HandleUtility.GetHandleSize(), but works for gizmos
        /// </summary>
        /// <param name="position"> handle position in world space</param>
        public static float GetHandleSize(Vector3 position)
        {
            Camera camera = Camera.current;

            if (camera)
            {
                Transform cameraTransform = camera.transform;

                Vector3 localDirection;
                localDirection.x = 0;
                localDirection.y = 0;
                localDirection.z = 1;
                Vector3 cameraZDirection = cameraTransform.TransformDirection(localDirection);

                localDirection.x = 1;
                localDirection.y = 0;
                localDirection.z = 0;
                Vector3 cameraXDirection = cameraTransform.TransformDirection(localDirection);


                return GetHandleSize(Gizmos.matrix.MultiplyPoint3x4(position), camera, camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f, cameraTransform.position, cameraZDirection, cameraXDirection);
            }

            return 20f;
        }

        /// <summary>
        /// Much like HandleUtility.GetHandleSize(), but works for gizmos
        /// </summary>
        /// <param name="position"> handle position in world space</param>
        /// <param name="camera"> the camera through which the object is displayed</param>
        /// <param name="cameraCenterWidth"> camera.pixelWidth * 0.5f</param>
        /// <param name="cameraCenterHeight"> camera.pixelHeight * 0.5f</param>
        /// <param name="cameraPosition"> camera.transform.position</param>
        /// <param name="cameraZDirection"> camera.transform.forward</param>
        /// <param name="cameraXDirection"> camera.transform.right</param>
        public static float GetHandleSize(Vector3 position, Camera camera, float cameraCenterWidth, float cameraCenterHeight, Vector3 cameraPosition, Vector3 cameraZDirection, Vector3 cameraXDirection)
        {
            //inlined version of Vector3.Dot(positionMinusCameraPosition, cameraZDirection)
            float z = (position.x - cameraPosition.x) * cameraZDirection.x +
                      (position.y - cameraPosition.y) * cameraZDirection.y +
                      (position.z - cameraPosition.z) * cameraZDirection.z;

            //OPTIM
            //If you reaaaally need those extra milliseconds, use this
            //return z * 0.15f;
            //This will give a good enough result. The problem with this method is that the handle size is unsatisfying when transitioning between Perspective and Orthogonal view, and when the scene view window is too narrow to the point that it changes the FOV of the scene view.

            Vector3 b;
            {
                Vector3 camPosPlusDirB;
                camPosPlusDirB.x = cameraPosition.x + cameraZDirection.x * z + cameraXDirection.x;
                camPosPlusDirB.y = cameraPosition.y + cameraZDirection.y * z + cameraXDirection.y;
                camPosPlusDirB.z = cameraPosition.z + cameraZDirection.z * z + cameraXDirection.z;
                b = camera.WorldToScreenPoint(camPosPlusDirB, Camera.MonoOrStereoscopicEye.Mono);
            }

            float aMinusBX = cameraCenterWidth - b.x;
            float aMinusBY = cameraCenterHeight - b.y;
            return 80f / (float)Math.Sqrt(aMinusBX * aMinusBX +
                                          aMinusBY * aMinusBY);
        }

        public static void SetPlayerPrefs<T>(string key, T value)
        {
            Type tt = typeof(T);
            if (tt.IsEnum)
            {
                PlayerPrefs.SetInt(key, Convert.ToInt32(Enum.Parse(typeof(T), value.ToString()) as Enum));
            }
            else if (tt.IsArray)
            {
                throw new NotImplementedException();
            }
            else if (tt.Matches(typeof(int), typeof(Int32)))
                PlayerPrefs.SetInt(key, (value as int?).Value);
            else if (tt == typeof(string))
                PlayerPrefs.SetString(key, (value as string));
            else if (tt == typeof(float))
                PlayerPrefs.SetFloat(key, (value as float?).Value);
            else if (tt == typeof(bool))
                PlayerPrefs.SetInt(key, ((value as bool?).Value == true) ? 1 : 0);
            else if (tt == typeof(Color))
                PlayerPrefs.SetString(key, (value as Color?).Value.ToHtml());
            else
                Debug.LogError("[DevTools.SetEditorPrefs] Unsupported datatype: " + tt.Name);
        }

        public static T GetPlayerPrefs<T>(string key, T defaultValue)
        {
            if (PlayerPrefs.HasKey(key))
            {
                Type tt = typeof(T);
                try
                {
                    if (tt.IsEnum || tt.Matches(typeof(int), typeof(Int32)))
                    {
                        return (T)(object)PlayerPrefs.GetInt(key, (int)(object)defaultValue);
                    }
                    else if (tt.IsArray)
                    {
                        throw new NotImplementedException();
                    }
                    else if (tt == typeof(string))
                        return (T)(object)PlayerPrefs.GetString(key, defaultValue.ToString());
                    else if (tt == typeof(float))
                        return (T)(object)PlayerPrefs.GetFloat(key, (float)(object)defaultValue);
                    else if (tt == typeof(bool))
                        return (T)(object)(PlayerPrefs.GetInt(key, ((bool)(object)defaultValue == true) ? 1 : 0) == 1);
                    else if (tt == typeof(Color))
                        return (T)(object)PlayerPrefs.GetString(key, ((Color)(object)defaultValue).ToHtml()).ColorFromHtml();
                    else
                        Debug.LogError("[DevTools.SetEditorPrefs] Unsupported datatype: " + tt.Name);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return defaultValue;
                }
            }


            return defaultValue;
        }

        public static float RandomSign()
            => UnityEngine.Random.Range(0, 2) * 2 - 1;

        public static string GetHelpUrl(object forClass)
            => (forClass == null) ? string.Empty : GetHelpUrl(forClass.GetType());

        public static string GetHelpUrl(Type classType)
        {
            if (classType != null)
            {
                object[] attribs = classType.GetCustomAttributes(typeof(HelpURLAttribute), true);
                if (attribs.Length > 0)
                    return (((HelpURLAttribute)attribs[0]).URL);
            }
            return string.Empty;
        }

        public static Vector3 GetCenterPosition(Vector3 fallback, params Vector3[] vectors)
        {
            if (vectors.Length == 0)
                return fallback;
            Vector3 v = vectors[0];
            for (int i = 1; i < vectors.Length; i++)
                v += vectors[i];
            return (v / vectors.Length);
        }

        public static T CreateGameObject<T>(Transform parent, string name) where T : MonoBehaviour
        {
            GameObject go = new GameObject(name);
            go.transform.parent = parent;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            T cmp = go.AddComponent<T>();
            return cmp;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns true if the object's prefab instance status allows for deletion
        /// </summary>
        /// <param name="object">The input object to test</param>
        /// <param name="errorMessage"> Is set to empty string if method returns true, otherwise it is set to a message error, similar to the one display by Unity, explaining the issue.</param>
        /// <see cref="PrefabInstanceStatus"/>
        public static bool DoesPrefabStatusAllowDeletion(this Object @object, out string errorMessage)
        {
            PrefabInstanceStatus prefabInstanceStatus = default;
            bool isDeletionAllowed = @object is GameObject == false || DoesPrefabStatusAllowDeletion((GameObject)@object, out prefabInstanceStatus);
            if (isDeletionAllowed == false)
            {
                errorMessage = $"You initiated an operation that leads to the deletion of the Game Object '{@object.name}', which is part of a Prefab instance.\n\n";
                if (prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
                    errorMessage += "The Prefab asset is missing. You have to unpack the Prefab instance to be able to execute this operation";
                else
                    errorMessage += $"Children of a Prefab instance cannot be deleted or moved, and components cannot be reordered. \n\nYou have to open the Prefab in Prefab Mode to restructure the Prefab Asset itself, or unpack the Prefab instance to remove its Prefab connection.";
            }
            else
            {
                errorMessage = string.Empty;
            }

            return isDeletionAllowed;
        }

        /// <summary>
        /// Returns true if the object's prefab instance status allows for deletion
        /// </summary>
        /// <param name="gameObject">The input object to test</param>
        /// <param name="prefabInstanceStatus"> The prefab instance status of the gameObject</param>
        public static bool DoesPrefabStatusAllowDeletion(this GameObject gameObject, out PrefabInstanceStatus prefabInstanceStatus)
        {
            prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            return prefabInstanceStatus != PrefabInstanceStatus.Connected || PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject);
        }
#endif

        /// <summary>
        /// Returns whether the code is executed in edit mode or not
        /// </summary>
        public static bool IsInEditMode
        {
            get
            {
                bool result;
#if UNITY_EDITOR
                result = !Application.isPlaying;
#else
                result = false;
#endif
                return result;
            }
        }
    }

    public static class DTTime
    {
        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        private static float _EditorDeltaTime;
        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        private static float _EditorLastTime;

        /// <summary>
        /// Expressed in seconds of real time.
        /// </summary>
        public static double TimeSinceStartup
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return EditorApplication.timeSinceStartup;
#endif

                return Time.realtimeSinceStartupAsDouble;
            }
        }

        [UsedImplicitly] [Obsolete("Seems to me that this is not working properly. Probably because InitializeEditorTime and UpdateEditorTime are never called. Fix this before using it")]
        public static float deltaTime => (Application.isPlaying) ? Time.deltaTime : _EditorDeltaTime;

        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        public static void InitializeEditorTime()
        {
            _EditorLastTime = Time.realtimeSinceStartup;
            _EditorDeltaTime = 0;
        }

        [UsedImplicitly] [Obsolete("Will get removed since it is not used by Curvy, and needs maintenance to be compatible with Unity's Enter Play Mode Settings")]
        public static void UpdateEditorTime()
        {
            float cur = Time.realtimeSinceStartup;
            float timeDelta = (cur - _EditorLastTime);

            _EditorDeltaTime = timeDelta;
            _EditorLastTime = cur;

            /*
            if (frameDelta > 20 || timeDelta > 1)
            {
                _EditorLastFrame = Time.frameCount;
                _EditorLastTime = cur;
                _EditorDeltaTime = 0;
            }
            else if (frameDelta > 0)
            {
                _EditorDeltaTime = timeDelta / frameDelta;
                _EditorLastTime = cur;
                _EditorLastFrame = Time.frameCount;
            }*/
        }


    }

    public class TimeMeasure : Ring<long>
    {
        public System.Diagnostics.Stopwatch mWatch = new Stopwatch();

        public TimeMeasure(int size) : base(size)
        { }

        public void Start()
        {
            mWatch.Start();
        }

        public void Stop()
        {
            mWatch.Stop();
            Add(mWatch.ElapsedTicks);
            mWatch.Reset();
        }

        public void Pause()
        {
            mWatch.Stop();
        }

        public double LastTicks => this[Count - 1];

        public double LastMS => LastTicks / (double)TimeSpan.TicksPerMillisecond;

        public double AverageMS
        {
            get
            {
                long d = 0;
                for (int i = 0; i < Count; i++)
                    d += this[i];

                return DTMath.FixNaN((d / (double)TimeSpan.TicksPerMillisecond) / Count);
            }
        }

        public double MinimumMS
        {
            get
            {
                long d = long.MaxValue;
                for (int i = 0; i < Count; i++)
                    d = Math.Min(d, this[i]);

                return DTMath.FixNaN(d / (double)TimeSpan.TicksPerMillisecond);
            }
        }

        public double MaximumMS
        {
            get
            {
                long d = long.MinValue;
                for (int i = 0; i < Count; i++)
                    d = Math.Max(d, this[i]);

                return DTMath.FixNaN(d / (double)TimeSpan.TicksPerMillisecond);
            }
        }

        public double AverageTicks
        {
            get
            {
                long d = 0;
                for (int i = 0; i < Count; i++)
                    d += this[i];
                return d / (double)Count;
            }
        }

        public double MinimumTicks
        {
            get
            {
                long d = long.MaxValue;
                for (int i = 0; i < Count; i++)
                    d = Math.Min(d, this[i]);
                return d;
            }
        }

        public double MaximumTicks
        {
            get
            {
                long d = 0;
                for (int i = 0; i < Count; i++)
                    d = Math.Max(d, this[i]);
                return d;
            }
        }
    }

    public static class DTMath
    {
        public static Vector3 ParallelTransportFrame(Vector3 up, Vector3 tan0, Vector3 tan1)
        {
            Vector3 A = Vector3.Cross(tan0, tan1);
            if (tan0 == -tan1) //Result is undefined in this case
            {
                Debug.LogWarning("[DevTools] ParallelTransportFrame's result is undefined for cases where tan0 == -tan1");
            }
            float a = Mathf.Atan2(A.magnitude, Vector3.Dot(tan0, tan1));
            return Quaternion.AngleAxis(Mathf.Rad2Deg * a, A) * up;
        }

        public static Vector3 LeftTan(ref Vector3 tan, ref Vector3 up)
            => Vector3.Cross(tan, up);

        public static Vector3 RightTan(ref Vector3 tan, ref Vector3 up)
            => Vector3.Cross(up, tan);

        /// <summary>
        /// Much like Mathf.Repeat(), but DTMath.Repeat(v,v) returns v instead of 0
        /// </summary>
        public static float Repeat(float t, float length)
            => (t == length) ? t : t - Mathf.Floor(t / length) * length;

        public static double FixNaN(double v)
        {
            if (double.IsNaN(v))
                v = 0;
            return v;
        }

        public static float FixNaN(float v)
        {
            if (float.IsNaN(v))
                v = 0;
            return v;
        }

        public static Vector2 FixNaN(Vector2 v)
        {
            if (float.IsNaN(v.x))
            {
                v.x = 0;
            }
            if (float.IsNaN(v.y))
            {
                v.y = 0;
            }
            return v;
        }

        /// <summary>
        /// Fixes NaN for Vector3
        /// </summary>
        /// <param name="v">a Vector3</param>
        /// <returns>the "cleaned up" vector</returns>
        public static Vector3 FixNaN(Vector3 v)
        {
            if (float.IsNaN(v.x))
            {
                v.x = 0;
            }
            if (float.IsNaN(v.y))
            {
                v.y = 0;
            }
            if (float.IsNaN(v.z))
            {
                v.z = 0;
            }
            return v;
        }

        /// <summary>
        /// Maps a value from a source range to a destination range
        /// </summary>
        /// <param name="min">min destination value</param>
        /// <param name="max">max destination value</param>
        /// <param name="value">current source value</param>
        /// <param name="vMin">min source value</param>
        /// <param name="vMax">max source value</param>
        /// <returns></returns>
        public static float MapValue(float min, float max, float value, float vMin = -1, float vMax = 1)
            => min + (max - min) * (value - vMin) / (vMax - vMin);

        public static float SnapPrecision(float value, int decimals)
            => (decimals >= 0) ? (float)Math.Round(value, decimals) : value;

        public static Vector2 SnapPrecision(Vector2 value, int decimals)
        {
            if (decimals < 0)
                return value;

            value.Set(SnapPrecision(value.x, decimals), SnapPrecision(value.y, decimals));
            return value;
        }

        public static Vector3 SnapPrecision(Vector3 value, int decimals)
        {
            if (decimals < 0)
                return value;
            value.Set(SnapPrecision(value.x, decimals), SnapPrecision(value.y, decimals), SnapPrecision(value.z, decimals));
            return value;
        }

        /// <summary>
        /// Gets the squared distance to the nearest point on a line
        /// </summary>
        /// <param name="l1">Line P1</param>
        /// <param name="l2">Line P2</param>
        /// <param name="p">a Point</param>
        /// <param name="frag">fragment on the line (0..1) of the nearest point</param>
        /// <returns>sqrMagnitude</returns>
        public static float LinePointDistanceSqr(Vector3 l1, Vector3 l2, Vector3 p, out float frag)
        {
            Vector3 v = l2;
            v.x -= l1.x;
            v.y -= l1.y;
            v.z -= l1.z;

            Vector3 w = p;
            w.x -= l1.x;
            w.y -= l1.y;
            w.z -= l1.z;

            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0)
            {
                frag = 0;
                return w.sqrMagnitude;
            }
            float c2 = Vector3.Dot(v, v);
            if (c2 <= c1)
            {
                frag = 1;

                Vector3 pMinusl2 = p;
                pMinusl2.x -= l2.x;
                pMinusl2.y -= l2.y;
                pMinusl2.z -= l2.z;

                return pMinusl2.sqrMagnitude;
            }
            frag = c1 / c2;

            Vector3 fragMulv = v;
            fragMulv.x *= frag;
            fragMulv.y *= frag;
            fragMulv.z *= frag;

            Vector3 pb = l1;
            pb.x += fragMulv.x;
            pb.y += fragMulv.y;
            pb.z += fragMulv.z;

            Vector3 pMinuspb = p;
            pMinuspb.x -= pb.x;
            pMinuspb.y -= pb.y;
            pMinuspb.z -= pb.z;

            return pMinuspb.sqrMagnitude;
        }

        /// <summary>
        /// Collide a ray (point + direction) against a line segment and return the hit point
        /// </summary>
        /// <param name="r0">Ray position</param>
        /// <param name="dir">Ray direction</param>
        /// <param name="l1">Line P1</param>
        /// <param name="l2">Line P2</param>
        /// <param name="hit">Collision Point</param>
        /// <param name="frag">fragment on the line (0..1) of the collision point</param>
        /// <returns>true if collision occurs</returns>
        public static bool RayLineSegmentIntersection(Vector2 r0, Vector2 dir, Vector2 l1, Vector2 l2, out Vector2 hit, out float frag)
        {
            Vector2 s2 = l2 - l1;
            float t;
            frag = (-dir.y * (r0.x - l1.x) + dir.x * (r0.y - l1.y)) / (-s2.x * dir.y + dir.x * s2.y);
            t = (s2.x * (r0.y - l1.y) - s2.y * (r0.x - l1.x)) / (-s2.x * dir.y + dir.x * s2.y);

            if (frag >= 0 && frag <= 1 && t > 0)
            {
                hit = new Vector2(r0.x + (t * dir.x), r0.y + (t * dir.y));
                return true;
            }
            hit = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Calculates the intersection line segment between 2 lines (not segments).
        /// </summary>
        /// <returns>false if no solution can be found</returns>
        public static bool ShortestIntersectionLine(Vector3 line1A, Vector3 line1B,
            Vector3 line2A, Vector3 line2B, out Vector3 resultSegmentA, out Vector3 resultSegmentB)
        {
            // Algorithm is ported from the C algorithm of 
            // Paul Bourke at http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
            resultSegmentA = Vector3.zero;
            resultSegmentB = Vector3.zero;

            Vector3 p1 = line1A;
            Vector3 p2 = line1B;
            Vector3 p3 = line2A;
            Vector3 p4 = line2B;
            Vector3 p13 = p1 - p3;
            Vector3 p43 = p4 - p3;

            if (p43.sqrMagnitude < Mathf.Epsilon)
            {
                return false;
            }
            Vector3 p21 = p2 - p1;
            if (p21.sqrMagnitude < Mathf.Epsilon)
            {
                return false;
            }

            double d1343 = p13.x * (double)p43.x + (double)p13.y * p43.y + (double)p13.z * p43.z;
            double d4321 = p43.x * (double)p21.x + (double)p43.y * p21.y + (double)p43.z * p21.z;
            double d1321 = p13.x * (double)p21.x + (double)p13.y * p21.y + (double)p13.z * p21.z;
            double d4343 = p43.x * (double)p43.x + (double)p43.y * p43.y + (double)p43.z * p43.z;
            double d2121 = p21.x * (double)p21.x + (double)p21.y * p21.y + (double)p21.z * p21.z;

            double denom = d2121 * d4343 - d4321 * d4321;
            if (Math.Abs(denom) < double.Epsilon)
            {
                return false;
            }
            double numer = d1343 * d4321 - d1321 * d4343;

            double mua = numer / denom;
            double mub = (d1343 + d4321 * (mua)) / d4343;
            resultSegmentA = new Vector3((float)(p1.x + mua * p21.x), (float)(p1.y + mua * p21.y), (float)(p1.z + mua * p21.z));
            resultSegmentB = new Vector3((float)(p3.x + mub * p43.x), (float)(p3.y + mub * p43.y), (float)(p3.z + mub * p43.z));
            return true;
        }

        /// <summary>
        /// Calculates the intersection between two line segments
        /// </summary>
        /// <returns>false if no solution can be found</returns>
        public static bool LineLineIntersection(Vector3 line1A, Vector3 line1B, Vector3 line2A, Vector3 line2B, out Vector3 hitPoint)
        {
            Vector3 resB;
            if (ShortestIntersectionLine(line1A, line1B, line2A, line2B, out hitPoint, out resB))
            {
                if ((resB - hitPoint).sqrMagnitude <= Mathf.Epsilon * Mathf.Epsilon)
                    return true;
            }
            return false;
        }

        public static bool LineLineIntersect(Vector2 line1A, Vector2 line1B, Vector2 line2A, Vector2 line2B, out Vector2 hitPoint, bool segmentOnly = true)
        {
            hitPoint = Vector2.zero;
            // Denominator for ua and ub are the same, so store this calculation
            double d =
               (line2B.y - line2A.y) * (line1B.x - line1A.x)
               -
               (line2B.x - line2A.x) * (line1B.y - line1A.y);

            //n_a and n_b are calculated as seperate values for readability
            double n_a =
               (line2B.x - line2A.x) * (line1A.y - line2A.y)
               -
               (line2B.y - line2A.y) * (line1A.x - line2A.x);

            double n_b =
               (line1B.x - line1A.x) * (line1A.y - line2A.y)
               -
               (line1B.y - line1A.y) * (line1A.x - line2A.x);

            // Make sure there is not a division by zero - this also indicates that
            // the lines are parallel.  
            // If n_a and n_b were both equal to zero the lines would be on top of each 
            // other (coincidental).  This check is not done because it is not 
            // necessary for this implementation (the parallel check accounts for this).
            if (d == 0)
                return false;

            // Calculate the intermediate fractional point that the lines potentially intersect.
            double ua = n_a / d;
            double ub = n_b / d;

            // The fractional point will be between 0 and 1 inclusive if the lines
            // intersect.  If the fractional calculation is larger than 1 or smaller
            // than 0 the lines would need to be longer to intersect.
            if (!segmentOnly || (ua >= 0d && ua <= 1d && ub >= 0d && ub <= 1d))
            {
                hitPoint.Set((float)(line1A.x + (ua * (line1B.x - line1A.x))), (float)(line1A.y + (ua * (line1B.y - line1A.y))));
                return true;
            }
            return false;
        }

        public static bool PointInsideTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 p, out float ac, out float ab, bool edgesAllowed)
        {
            Vector3 v0 = C - A;
            Vector3 v1 = B - A;
            Vector3 v2 = p - A;
            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            ac = (dot11 * dot02 - dot01 * dot12) * invDenom;
            ab = (dot00 * dot12 - dot01 * dot02) * invDenom;
            if (edgesAllowed)
                return ac >= 0 && ab >= 0 && (ac + ab < 1);
            else
                return ac > 0 && ab > 0 && (ac + ab < 1);
        }
    }

    /// <summary>
    /// Extended UnityEvent
    /// </summary>
    [Serializable]
    public class UnityEventEx<T0> : UnityEvent<T0>
    {
        private object mCallerList;
        private MethodInfo mCallsCount;
        private int mCount = -1;

        /// <summary>
        /// Removes and adds a listener, ensuring it's only bound once
        /// </summary>
        /// <param name="call"></param>
        public void AddListenerOnce(UnityAction<T0> call)
        {
            RemoveListener(call);
            AddListener(call);
            CheckForListeners();
        }

        /// <summary>
        /// Whether the event has any listeners at all
        /// </summary>
        /// <returns></returns>
        public bool HasListeners()
        {
            if (mCallsCount == null)
            {
                FieldInfo fi = typeof(UnityEventBase).FieldByName("m_Calls", false, true);
                if (fi != null)
                {
                    mCallerList = fi.GetValue(this);
                    if (mCallerList != null)
                        mCallsCount = mCallerList.GetType().PropertyByName("Count").GetGetMethod();
                }
            }
            if (mCount == -1)
            {
                if (mCallerList != null && mCallsCount != null)
                    mCount = (int)mCallsCount.Invoke(mCallerList, null); //.GetValue(mCallerList, null);
                mCount += GetPersistentEventCount();
            }
            return (mCount > 0);
        }

        /// <summary>
        /// Force HasListeners() to recheck for dynamically bound events
        /// </summary>
        public void CheckForListeners()
        {
            mCount = -1;
        }
    }

    /// <summary>
    /// Same functionality as various UnityEngine.Debug methods
    /// </summary>
    /// <remarks>Used to distinct between temporarly debug output and regular package output</remarks>
    public static class DTLog
    {
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        public static void Log(object message, [CanBeNull] Object context)
        {
            Debug.Log(message, context);
        }

        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        public static void LogError(object message, [CanBeNull] Object context)
        {
            Debug.LogError(message, context);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, format, args);
        }

        public static void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }

        public static void LogException(Exception exception, [CanBeNull] Object context)
        {
            Debug.LogException(exception, context);
        }

        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }

        public static void LogFormat(Object context, string format, params object[] args)
        {
            Debug.LogFormat(context, format, args);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public static void LogWarning(object message, [CanBeNull] Object context)
        {
            Debug.LogWarning(message, context);
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }

        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            Debug.LogWarningFormat(context, format, args);
        }
    }

}
