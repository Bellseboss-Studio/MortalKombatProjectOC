using System;
using System.Collections.Generic;
using ToolBuddy.Pooling;
using ToolBuddy.Pooling.Collections;
using UnityEngine;


namespace FluffyUnderware.Curvy.ThirdParty.LibTessDotNet
{
    public static class LibTessVector3Extension
    {
        public static Vec3 Vec3(this Vector3 v) =>
            new Vec3 { X = v.x, Y = v.y, Z = v.z };

        public static ContourVertex ContourVertex(this Vector3 v)
        {
            var r = new ContourVertex();
            r.Position = v.Vec3();
            return r;
        }

    }

    [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
    public static class LibTessV3Extension
    {
        public static Vector3 Vector3(this Vec3 v) =>
            new Vector3(
                v.X,
                v.Y,
                v.Z
            );
    }




    public static class UnityLibTessUtility
    {
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public static ContourVertex[] ToContourVertex(Vector3[] v, bool zeroZ = false)
        {
            var res = new ContourVertex[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                res[i].Position.X = v[i].x;
                res[i].Position.Y = v[i].y;
                res[i].Position.Z = (zeroZ) ? 0 : v[i].z;
            }
            return res;
        }

        public static ContourVertex[] ToContourVertex(SubArray<Vector3> v, bool zeroZ = false)
        {
            int count = v.Count;
            Vector3[] vArray = v.Array;
            ContourVertex[] res = new ContourVertex[count];
            for (int i = 0; i < count; i++)
            {
                res[i].Position.X = vArray[i].x;
                res[i].Position.Y = vArray[i].y;
                res[i].Position.Z = (zeroZ) ? 0 : vArray[i].z;
            }
            return res;
        }

        public static void FromContourVertex(ContourVertex[] v, SubArray<Vector3> output)
        {
            var count = output.Count;
            var array = output.Array;

            for (int i = 0; i < count; i++)
            {
                array[i].x = v[i].Position.X;
                array[i].y = v[i].Position.Y;
                array[i].z = v[i].Position.Z;
            }
        }

        public static SubArray<Vector3> ContourVerticesToPositions(ContourVertex[] v)
        {
            var result = ArrayPoolsProvider.GetPool<Vector3>().Allocate(v.Length);
            var res = result.Array;
            for (int i = 0; i < result.Count; i++)
            {
                res[i].x = v[i].Position.X;
                res[i].y = v[i].Position.Y;
                res[i].z = v[i].Position.Z;
            }

            return result;
        }

        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public static void SetFromContourVertex(ref Vector3[] v3Array, ref ContourVertex[] cvArray)
        {
            Array.Resize(ref v3Array, cvArray.Length);
            for (int i = 0; i < v3Array.Length; i++)
            {
                v3Array[i].x = cvArray[i].Position.X;
                v3Array[i].y = cvArray[i].Position.Y;
                v3Array[i].z = cvArray[i].Position.Z;
            }
        }

        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public static void SetToContourVertex(ref ContourVertex[] cvArray, ref Vector3[] v3Array)
        {
            Array.Resize(ref cvArray, v3Array.Length);
            for (int i = 0; i < cvArray.Length; i++)
            {
                cvArray[i].Position.X = v3Array[i].x;
                cvArray[i].Position.Y = v3Array[i].y;
                cvArray[i].Position.Z = v3Array[i].z;
            }
        }

    }
}