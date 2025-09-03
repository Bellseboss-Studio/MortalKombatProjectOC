// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;
using System;
using FluffyUnderware.DevTools.Extensions;

namespace FluffyUnderware.DevToolsEditor
{
    public interface IDTClipboardHandler
    {
        void Copy(object value);
        object Paste(object data);
        bool CanPasteFrom(Type clipboardType);
    }

    public class DTVector2Clipboard : IDTClipboardHandler
    {

        public void Copy(object value)
        {
            DT.ClipBoardSet(value);
            EditorGUIUtility.systemCopyBuffer = value.GetType().Name;
        }

        public object Paste(object data)
        {
            if (data is Vector2)
                return (Vector2)data;
            else if (data is Vector3)
                return new Vector2(((Vector3)data).x, ((Vector3)data).y);
            else if (data is Vector4)
                return new Vector2(((Vector4)data).x, ((Vector4)data).y);

            return Vector2.zero;
        }

        public bool CanPasteFrom(Type clipboardType)
            => (clipboardType.Matches(typeof(Vector2), typeof(Vector3), typeof(Vector4)));
    }

    public class DTVector3Clipboard : IDTClipboardHandler
    {

        public void Copy(object value)
        {
            DT.ClipBoardSet(value);
            EditorGUIUtility.systemCopyBuffer = value.GetType().Name;
        }

        public object Paste(object data)
        {
            if (data is Vector3)
                return (Vector3)data;
            else if (data is Vector4)
                return new Vector3(((Vector4)data).x, ((Vector4)data).y, ((Vector4)data).z);

            return Vector2.zero;
        }

        public bool CanPasteFrom(Type clipboardType)
            => (clipboardType.Matches(typeof(Vector3), typeof(Vector4)));
    }

    public class DTAnimationCurveClipboard : IDTClipboardHandler
    {
        public void Copy(object value)
        {
            DT.ClipBoardSet(value);
        }

        public object Paste(object data)
        {
            AnimationCurve clp=(AnimationCurve)data;
            AnimationCurve curve= new AnimationCurve(clp.keys);
            curve.preWrapMode = clp.preWrapMode;
            curve.postWrapMode = clp.postWrapMode;
            return curve;
        }

        public bool CanPasteFrom(Type clipboardType)
            => clipboardType == typeof(AnimationCurve);
    }
    
    
}
