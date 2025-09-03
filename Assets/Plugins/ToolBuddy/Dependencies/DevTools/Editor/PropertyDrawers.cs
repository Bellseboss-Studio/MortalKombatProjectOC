// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using UnityEditor;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor.Extensions;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using MinAttribute = FluffyUnderware.DevTools.MinAttribute;

namespace FluffyUnderware.DevToolsEditor
{
    public class DTPropertyDrawer<T> : PropertyDrawer where T : DTPropertyAttribute
    {

        /// <summary>
        /// Gets the attribute
        /// </summary>
        protected T Attribute => attribute as T;

        /// <summary>
        /// Gets some common attribute options
        /// </summary>
        protected AttributeOptionsFlags Options => Attribute.Options;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!string.IsNullOrEmpty(Attribute.Label))
                label.text = Attribute.Label;
            label.tooltip = Attribute.Tooltip;
        }

        protected void PushCustomColors()
        {
            if (!string.IsNullOrEmpty(Attribute.Color))
                DTGUI.PushColor(Attribute.Color.ColorFromHtml());
        }

        protected void PopCustomColors()
        {
            if (!string.IsNullOrEmpty(Attribute.Color))
                DTGUI.PopColor();
        }

        protected MemberInfo GetMemberInfo(SerializedProperty property, string fieldOrPropertyName, out object parent)
        {
            parent = property.GetParentObject();
            Type parentType = parent.GetType();
            FieldInfo fi = parentType.FieldByName(fieldOrPropertyName, true, true);
            if (fi != null)
                return fi;
            else
            {
                PropertyInfo pi = parentType.PropertyByName(fieldOrPropertyName, true, true);
                if (pi != null)
                    return pi;
                else
                {
                    DTLog.LogError(parentType.Name + ": Field or Property '" + fieldOrPropertyName + "' not found!", property.serializedObject.targetObject);
                    return null;
                }
            }
        }

        /// <summary>
        /// Get a value from a field or property of the serialized object by it's name
        /// </summary>
        /// <typeparam name="U">datatype to retrieve</typeparam>
        /// <param name="property">a serialized property from the object in question</param>
        /// <param name="fieldOrPropertyName">name of the field or property</param>
        /// <returns>the value of the field or property</returns>
        protected U GetMemberValue<U>(SerializedProperty property, string fieldOrPropertyName)
        {
            object parent;
            object o = null;
            MemberInfo mi = GetMemberInfo(property, fieldOrPropertyName, out parent);
            if (mi != null)
            {
                if (mi is FieldInfo)
                    o = ((FieldInfo)mi).GetValue(parent);
                else
                    o = ((PropertyInfo)mi).GetValue(parent, null);
            }

            Type oT = o.GetType();
            Type rT = typeof(U);
            if (oT == rT)
                return (U)o;
            else if (oT == typeof(int) && rT == typeof(float))
                return (U)Convert.ChangeType(o, rT, System.Globalization.CultureInfo.InvariantCulture);
            else
                return default;
        }

        protected object GetParent(SerializedProperty prop)
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');
            foreach (string element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        private object GetValue(object source, string name)
        {
            if (source == null)
                return null;
            Type type = source.GetType();
            FieldInfo f = type.FieldByName(name, true, true);
            if (f == null)
            {
                PropertyInfo p = type.PropertyByName(name, true, true);
                if (p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        private object GetValue(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue(source, name) as IEnumerable;
            IEnumerator enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }

        protected U GetPropertySourceField<U>(SerializedProperty property)
            => (U)fieldInfo.GetValue(GetParent(property));
        /*
            var instance = property.serializedObject.targetObject;
            var t = instance.GetType();
            FieldInfo fi = null;
            while (fi == null && t != null)
            {
                fi = t.GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi == null)
                    t = t.BaseType;
            }
            return (fi != null) ? (U)fi.GetValue(instance) : default(U);
            */
    }

    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelAttributePropertyDrawer : DTPropertyDrawer<LabelAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            EditorGUI.PropertyField(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(ToggleButtonAttribute))]
    public class ToggleButtonAttributePropertyDrawer : DTPropertyDrawer<ToggleButtonAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            if (property.propertyType != SerializedPropertyType.Boolean)
                DTLog.LogError("[DevTools] - [ToggleButton] only valid for boolean fields!", property.serializedObject.targetObject);
            else
            {
                EditorGUI.BeginChangeCheck();
                GUIStyle st = GUI.skin.button;

                bool v = GUI.Toggle(position, property.boolValue, label, st);
                if (EditorGUI.EndChangeCheck())
                    property.boolValue = v;
            }
        }
    }

    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerPropertyDrawer : DTPropertyDrawer<LayerAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            property.intValue = EditorGUI.LayerField(position, property.intValue);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagPropertyDrawer : DTPropertyDrawer<TagAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            property.stringValue = EditorGUI.TagField(position, property.stringValue);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(VectorExAttribute))]
    public class VectorExPropertyDrawer : DTPropertyDrawer<VectorExAttribute>
    {
        private static readonly GUIContent CopyButtonGuiContent = new GUIContent(
            "C",
            "Copy"
        );
        private static readonly GUIContent PasteButtonGuiContent = new GUIContent(
            "P",
            "Past"
        );
        private static readonly GUIContent SetZeroButtonGuiContent = new GUIContent(
            "0",
            "Set to zero"
        );
        private static readonly GUIContent SetOneButtonGuiContent = new GUIContent(
            "1",
            "Set to one"
        );
        private static readonly GUIContent NegateButtonGuiContent = new GUIContent(
            "~",
            "Negate"
        );

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h;
            if (Options.HasFlag(AttributeOptionsFlags.Compact) || EditorGUIUtility.wideMode)
                h = base.GetPropertyHeight(property, label);
            else
                h = base.GetPropertyHeight(property, label) * 2;
            return h;
        }


        public static void DoGUI(Rect position, SerializedProperty property, DTPropertyAttribute attribute, GUIContent label, AttributeOptionsFlags flags, VectorExPropertyDrawer drawer = null)
        {
            if (property.propertyType != SerializedPropertyType.Vector2 && property.propertyType != SerializedPropertyType.Vector3 && property.propertyType != SerializedPropertyType.Quaternion)
                DTLog.LogError("[DevTools] - [VectorEx] only valid for Vector and Quaternion fields!", property.serializedObject.targetObject);
            else
            {

                int buttons = 0;
                if (flags.HasFlag(AttributeOptionsFlags.Clipboard))
                    buttons += 2;
                if (flags.HasFlag(AttributeOptionsFlags.One))
                    buttons++;
                if (flags.HasFlag(AttributeOptionsFlags.Zero))
                    buttons++;
                if (flags.HasFlag(AttributeOptionsFlags.Negate))
                    buttons++;

                int buttonWidth = 21;
                int interButtonSpace = 1;
                int buttonsWidth = buttons * buttonWidth + Math.Max(0, buttons - 1) * interButtonSpace;

                Rect buttonsRectangle = new Rect();
                buttonsRectangle.x = position.x + position.width - buttonsWidth;
                buttonsRectangle.y = position.y;
                buttonsRectangle.width = buttonWidth;
                buttonsRectangle.height = EditorGUIUtility.singleLineHeight;

                Rect r = position.WithoutLabel();
                r.width -= buttonsWidth;

                if (flags.HasFlag(AttributeOptionsFlags.Compact) || EditorGUIUtility.wideMode)
                {
                    if (drawer != null)
                        drawer.PushCustomColors();
                    EditorGUI.PrefixLabel(position, label);

                    if (property.propertyType == SerializedPropertyType.Vector2)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector2 v = DTGUI.CompactVector2Field(r, property.vector2Value);
                        if (EditorGUI.EndChangeCheck())
                            property.vector2Value = DT._UseSnapValuePrecision ? DTMath.SnapPrecision(v, attribute.Precision) : v;
                    }
                    else if (property.propertyType == SerializedPropertyType.Vector3)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 v = DTGUI.CompactVector3Field(r, property.vector3Value);
                        if (EditorGUI.EndChangeCheck())
                            property.vector3Value = DT._UseSnapValuePrecision ? DTMath.SnapPrecision(v, attribute.Precision) : v;
                    }
                    else if (property.propertyType == SerializedPropertyType.Quaternion)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 v = DTGUI.CompactVector3Field(r, DT._UseSnapValuePrecision ? DTMath.SnapPrecision(property.quaternionValue.eulerAngles, attribute.Precision) : property.quaternionValue.eulerAngles);
                        if (EditorGUI.EndChangeCheck())
                            property.quaternionValue = Quaternion.Euler(v);
                    }
                    if (drawer != null)
                        drawer.PopCustomColors();

                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    if (drawer != null)
                        drawer.PushCustomColors();

#pragma warning disable 162
                    position.y -= 2;
#pragma warning restore 162

                    EditorGUI.PropertyField(position, property, label);
                    if (drawer != null)
                        drawer.PopCustomColors();
                    if (EditorGUI.EndChangeCheck() && DT._UseSnapValuePrecision)
                    {
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = DTMath.SnapPrecision(property.vector2Value, attribute.Precision);
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            property.vector3Value = DTMath.SnapPrecision(property.vector3Value, attribute.Precision);
                    }
                }

                if (flags.HasFlag(AttributeOptionsFlags.Clipboard))
                {
                    if (GUI.Button(buttonsRectangle, CopyButtonGuiContent, EditorStyles.miniButton))
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            DT.ClipboardCopy(property.vector2Value);
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            DT.ClipboardCopy(property.vector3Value);
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                    // Can Paste?
                    if (property.propertyType == SerializedPropertyType.Vector2)
                        GUI.enabled = DT.ClipboardCanPasteTo<Vector2>();
                    else if (property.propertyType == SerializedPropertyType.Vector3)
                        GUI.enabled = DT.ClipboardCanPasteTo<Vector3>();

                    if (GUI.Button(buttonsRectangle, PasteButtonGuiContent, EditorStyles.miniButton))
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = DT.ClipboardPaste<Vector2>();
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            property.vector3Value = DT.ClipboardPaste<Vector3>();
                    GUI.enabled = true;
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                }
                if (flags.HasFlag(AttributeOptionsFlags.Zero))
                {
                    if (GUI.Button(buttonsRectangle, SetZeroButtonGuiContent, EditorStyles.miniButton))
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = Vector2.zero;
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            property.vector3Value = Vector3.zero;
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                }
                if (flags.HasFlag(AttributeOptionsFlags.One))
                {
                    if (GUI.Button(buttonsRectangle, SetOneButtonGuiContent, EditorStyles.miniButton))
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value = Vector2.one;
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            property.vector3Value = Vector3.one;
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                }
                if (flags.HasFlag(AttributeOptionsFlags.Negate))
                {
                    if (GUI.Button(buttonsRectangle, NegateButtonGuiContent, EditorStyles.miniButton))
                        if (property.propertyType == SerializedPropertyType.Vector2)
                            property.vector2Value *= -1;
                        else if (property.propertyType == SerializedPropertyType.Vector3)
                            property.vector3Value *= -1;
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                }

            }

        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            DoGUI(position, property, Attribute, label, Options, this);
        }
    }

    [CustomPropertyDrawer(typeof(AnimationCurveExAttribute))]
    public class AnimationCurveExPropertyDrawer : DTPropertyDrawer<AnimationCurveExAttribute>
    {

        public override void OnGUI(Rect propertyRectangle, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(propertyRectangle, property, label);
            if (property.propertyType != SerializedPropertyType.AnimationCurve)
                DTLog.LogError("[DevTools] - [AnimationCurvEx] only valid for AnimationCurve fields!", property.serializedObject.targetObject);
            else
            {
                int buttons = 0;
                if (Options.HasFlag(AttributeOptionsFlags.Clipboard))
                    buttons += 2;

                int buttonWidth = 21;
                int interButtonSpace = 1;
                int buttonsWidth = buttons * buttonWidth + Math.Max(0, buttons - 1) * interButtonSpace;

                Rect buttonsRectangle = new Rect();
                buttonsRectangle.x = propertyRectangle.x + propertyRectangle.width - buttonsWidth;
                buttonsRectangle.y = propertyRectangle.y;
                buttonsRectangle.width = buttonWidth;
                buttonsRectangle.height = EditorGUIUtility.singleLineHeight;

                propertyRectangle.width -= buttonsWidth;

                EditorGUI.PropertyField(propertyRectangle, property, label);
                // Buttons
                if (Options.HasFlag(AttributeOptionsFlags.Clipboard))
                {
                    if (GUI.Button(buttonsRectangle, "C", EditorStyles.miniButton))
                        DT.ClipboardCopy(property.animationCurveValue);
                    buttonsRectangle.x += buttonWidth + interButtonSpace;
                    GUI.enabled = DT.ClipboardCanPasteTo<AnimationCurve>();
                    if (GUI.Button(buttonsRectangle, "P", EditorStyles.miniButton))
                        property.animationCurveValue = DT.ClipboardPaste<AnimationCurve>();
                    GUI.enabled = true;
                }
            }
        }
    }



    [CustomPropertyDrawer(typeof(RangeExAttribute))]
    public class RangeExPropertyDrawer : DTPropertyDrawer<RangeExAttribute>
    {
        private float minV;
        private float maxV;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);


            minV = Attribute.MinValue;
            maxV = Attribute.MaxValue;

            if (!string.IsNullOrEmpty(Attribute.MinFieldOrPropertyName))
            {
                minV = GetMemberValue<float>(property, Attribute.MinFieldOrPropertyName);
            }

            if (!string.IsNullOrEmpty(Attribute.MaxFieldOrPropertyName))
            {
                maxV = GetMemberValue<float>(property, Attribute.MaxFieldOrPropertyName);
            }

            if (minV > maxV)
            {
                (maxV, minV) = (minV, maxV);
            }

            // GUI
            PushCustomColors();
            EditorGUI.BeginChangeCheck();
            if (Attribute.Slider)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        EditorGUI.Slider(position, property, minV, maxV, label);
                        break;
                    case SerializedPropertyType.Integer:
                        EditorGUI.IntSlider(position, property, (int)minV, (int)maxV, label);
                        break;
                }

            }
            else
                EditorGUI.PropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                // Clamp
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        if (DT._UseSnapValuePrecision && Attribute.Precision > -1)
                            property.floatValue = DTMath.SnapPrecision(property.floatValue, Attribute.Precision);
                        if (property.floatValue < minV || property.floatValue > maxV)
                            property.floatValue = Mathf.Clamp(property.floatValue, minV, maxV);
                        break;
                    case SerializedPropertyType.Integer:
                        if (property.intValue < minV || property.intValue > maxV)
                            property.intValue = Mathf.Clamp(property.intValue, (int)minV, (int)maxV);
                        break;
                }
            }
            PopCustomColors();
            label.tooltip = "";
        }
    }

    [CustomPropertyDrawer(typeof(MinMaxAttribute))]
    public class MinMaxPropertyDrawer : DTPropertyDrawer<MinMaxAttribute>
    {
        private float lBound;
        private float uBound;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            lBound = Attribute.Min;
            uBound = Attribute.Max;
            // Get maxV serialized
            string[] path = property.Path();
            path[path.Length - 1] = Attribute.MaxValueField;
            SerializedProperty pMaxV = property.serializedObject.FindProperty(string.Join(".", path));

            if (!string.IsNullOrEmpty(Attribute.MinBoundFieldOrPropertyName))
            {
                lBound = GetMemberValue<float>(property, Attribute.MinBoundFieldOrPropertyName);
            }

            if (!string.IsNullOrEmpty(Attribute.MaxBoundFieldOrPropertyName))
            {
                uBound = GetMemberValue<float>(property, Attribute.MaxBoundFieldOrPropertyName);
            }

            if (lBound > uBound)
            {
                (uBound, lBound) = (lBound, uBound);
            }

            float minV = 0;
            float maxV = 0;

            // GUI
            EditorGUI.BeginChangeCheck();


            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    minV = property.floatValue;
                    maxV = pMaxV.floatValue;
                    EditorGUI.MinMaxSlider(position, label, ref minV, ref maxV, lBound, uBound);
                    break;
                case SerializedPropertyType.Integer:
                    minV = property.intValue;
                    maxV = pMaxV.intValue;
                    EditorGUI.MinMaxSlider(position, label, ref minV, ref maxV, lBound, uBound);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Clamp
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        if (DT._UseSnapValuePrecision && Attribute.Precision > -1)
                            property.floatValue = Mathf.Max(lBound, DTMath.SnapPrecision(minV, Attribute.Precision));
                        pMaxV.floatValue = Mathf.Min(uBound, DTMath.SnapPrecision(maxV, Attribute.Precision));
                        break;
                    case SerializedPropertyType.Integer:
                        property.intValue = (int)minV;
                        pMaxV.intValue = (int)maxV;
                        break;
                }

            }
            label.tooltip = "";
        }

    }

    [CustomPropertyDrawer(typeof(MinAttribute), true)]
    public class MinPropertyDrawer : DTPropertyDrawer<MinAttribute>
    {
        protected float minV;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            // GUI
            EditorGUI.PropertyField(position, property, label);


            minV = Attribute.MinValue;

            if (!string.IsNullOrEmpty(Attribute.MinFieldOrPropertyName))
            {
                minV = GetMemberValue<float>(property, Attribute.MinFieldOrPropertyName);
            }

            // Clamp
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (DT._UseSnapValuePrecision && Attribute.Precision > -1)
                        property.floatValue = DTMath.SnapPrecision(property.floatValue, Attribute.Precision);
                    if (property.floatValue < minV)
                        property.floatValue = minV;
                    break;
                case SerializedPropertyType.Integer:
                    if (property.intValue < minV)
                        property.intValue = (int)minV;
                    break;
            }
            label.tooltip = "";
        }

    }

    [CustomPropertyDrawer(typeof(MaxAttribute))]
    public class MaxPropertyDrawer : DTPropertyDrawer<MaxAttribute>
    {
        protected float maxV;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            // GUI
            EditorGUI.PropertyField(position, property, label);


            maxV = Attribute.MaxValue;

            if (!string.IsNullOrEmpty(Attribute.MaxFieldOrPropertyName))
            {
                maxV = GetMemberValue<float>(property, Attribute.MaxFieldOrPropertyName);
            }

            // Clamp and snap
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (DT._UseSnapValuePrecision && Attribute.Precision > -1)
                        property.floatValue = DTMath.SnapPrecision(property.floatValue, Attribute.Precision);
                    if (property.floatValue > maxV)
                        property.floatValue = maxV;

                    break;
                case SerializedPropertyType.Integer:
                    if (property.intValue > maxV)
                        property.intValue = (int)maxV;
                    break;
            }
            label.tooltip = "";
        }

    }

    [CustomPropertyDrawer(typeof(PathSelectorAttribute))]
    public class PathSelectorDrawer : DTPropertyDrawer<PathSelectorAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            if (property.propertyType == SerializedPropertyType.String)
            {
                position.width -= 20;
                property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
                position.xMin = position.xMax;
                position.width = 20;
                if (GUI.Button(position, "..", EditorStyles.miniButton))
                {
                    string v = string.Empty;
                    switch (Attribute.Mode)
                    {
                        case PathSelectorAttribute.DialogMode.OpenFile:
                            v = EditorUtility.OpenFilePanel(Attribute.Title, Attribute.Directory, Attribute.Extension);
                            break;
                        case PathSelectorAttribute.DialogMode.OpenFolder:
                            v = EditorUtility.OpenFolderPanel(Attribute.Title, Attribute.Directory, Attribute.DefaultName);
                            break;
                        case PathSelectorAttribute.DialogMode.CreateFile:
                            v = EditorUtility.SaveFilePanel(Attribute.Title, Attribute.Directory, Attribute.DefaultName, Attribute.Extension);
                            break;
                    }
                    if (!string.IsNullOrEmpty(v))
                        property.stringValue = v;
                }
            }
            else
                DTLog.LogError("[DevTools] PropertyAttribute [FileSelector] only works on string fields!", property.serializedObject.targetObject);
        }
    }

    [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
    public class EnumFlagDrawer : DTPropertyDrawer<EnumFlagAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            Enum targetEnum = GetPropertySourceField<Enum>(property);

            EditorGUI.BeginProperty(position, label, property);
            Enum enumNew = EditorGUI.EnumFlagsField(position, label, targetEnum);
            property.intValue = (int)Convert.ChangeType(enumNew, targetEnum.GetType(), System.Globalization.CultureInfo.InvariantCulture);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(EnumSelectionGridAttribute))]
    public class EnumSelectionGridDrawer : DTPropertyDrawer<EnumSelectionGridAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                string[] names = property.enumDisplayNames;

                EditorGUI.PrefixLabel(position, label);
                position = position.WithoutLabel();
                EditorGUI.BeginChangeCheck();
                int v = GUI.SelectionGrid(position, property.enumValueIndex, names, names.Length);
                if (EditorGUI.EndChangeCheck())
                    property.enumValueIndex = v;
            }
            else
                DTLog.LogError("[DevTools] PropertyAttribute [SelectionGrid] only works on Enum fields!", property.serializedObject.targetObject);
        }
    }

    [CustomPropertyDrawer(typeof(ObjectSelectorAttribute))]
    public class ObjectSelectorDrawer : DTPropertyDrawer<ObjectSelectorAttribute>
    {
    }

    [CustomPropertyDrawer(typeof(FloatRegionAttribute))]
    public class FloatRegionPropertyDrawer : DTPropertyDrawer<FloatRegionAttribute>
    {
        private PropertyInfo mPI;
        private object mObject;
        private SerializedProperty ppFrom;
        private SerializedProperty ppTo;
        private SerializedProperty ppSimpleValue;
        private RegionOptions<float> mOptions;

        private bool compact => ((EditorGUIUtility.wideMode && Options == AttributeOptionsFlags.None) ||
                                 ((Options & AttributeOptionsFlags.Compact) == AttributeOptionsFlags.Compact ||
                                  (Options & AttributeOptionsFlags.FullCompact) == AttributeOptionsFlags.FullCompact
                                 )
            );

        private bool minmax => (Attribute.UseSlider && !ppSimpleValue.boolValue &&
                                mOptions.ClampFrom == DTValueClamping.Range && mOptions.ClampTo == DTValueClamping.Range &&
                                mOptions.FromMin == mOptions.ToMin && mOptions.FromMax == mOptions.ToMax);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            float h = base.GetPropertyHeight(property, label);
            if (!compact)
            {
                if (!Attribute.RegionIsOptional || !ppSimpleValue.boolValue || minmax)
                    h = (h + EditorGUIUtility.standardVerticalSpacing) * 2;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            Init(property);
            Rect r = new Rect(position);
            r.height = EditorGUIUtility.singleLineHeight;

            if (minmax)
            {
                doMinmax(r, label, Attribute.RegionIsOptional);
            }
            else if (compact)
            {
                doCompact(r, label, new GUIContent(mOptions.LabelTo), Attribute.RegionIsOptional);
            }
            else
            {
                doRegular(r, label);
            }

            label.tooltip = "";
        }

        private void doMinmax(Rect r, GUIContent label, bool showOptional)
        {
            float l = ppFrom.floatValue;
            float u = ppTo.floatValue;

            Rect r2 = new Rect(r);

            if (showOptional)
                r2.width -= 16;

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(r2, label, ref l, ref u, mOptions.FromMin, mOptions.FromMax);
            if (EditorGUI.EndChangeCheck())
            {
                ppFrom.floatValue = l;
                validateFrom();
                ppTo.floatValue = u;
                validateTo();
            }
            if (showOptional)
            {
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(r.xMax - 16, r.y, 16, r.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);
            }
            if (!compact)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                doCompact(r, new GUIContent(mOptions.LabelFrom), new GUIContent(mOptions.LabelTo), false);
            }
        }

        private void doCompact(Rect r, GUIContent label, GUIContent labelTo, bool showOptional, bool fullLine = false)
        {
            float lw = EditorGUIUtility.labelWidth;
            float fw = EditorGUIUtility.fieldWidth;
            Rect r2 = new Rect(r);
            bool showTo = !showOptional || !ppSimpleValue.boolValue;
            float showToLabelWidth = DTGUI.LabelWidth(labelTo);

            float chkWidth = showOptional ? 16 : 0;
            if (fullLine)
            {
                EditorGUIUtility.labelWidth = DTGUI.LabelWidth(label);
                lw = EditorGUIUtility.labelWidth;
            }

            r2.width = (r.width - lw - chkWidth - showToLabelWidth) / 2;

            // From
            r.width = lw + r2.width;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r, ppFrom, label);
            if (EditorGUI.EndChangeCheck())
                validateFrom();

            r.x += r.width;

            // "To" may be optional
            if (showOptional)
            {
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(r.x, r.y, 16, r.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);
                r.x += 16;
            }
            // Show "To"?

            if (showTo)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = showToLabelWidth;
                r.width = r2.width + showToLabelWidth;
                ppTo.floatValue = EditorGUI.FloatField(r, labelTo, ppTo.floatValue);
                if (EditorGUI.EndChangeCheck())
                    validateTo();

            }

            EditorGUIUtility.labelWidth = lw;
            EditorGUIUtility.fieldWidth = fw;
        }

        private void doRegular(Rect r, GUIContent label)
        {
            // From
            Rect rF = new Rect(r);
            if (Attribute.RegionIsOptional)
                rF.width -= 16;
            EditorGUI.BeginProperty(rF, label, ppFrom);
            EditorGUI.BeginChangeCheck();
            if (Attribute.UseSlider && mOptions.ClampFrom == DTValueClamping.Range)
                ppFrom.floatValue = EditorGUI.Slider(rF, label, ppFrom.floatValue, mOptions.FromMin, mOptions.FromMax);
            else
                ppFrom.floatValue = EditorGUI.FloatField(rF, label, ppFrom.floatValue);
            if (EditorGUI.EndChangeCheck())
                validateFrom();
            EditorGUI.EndProperty();
            // Perhaps To is optional
            if (Attribute.RegionIsOptional)
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(rF.x + rF.width + 1, rF.y, 16, rF.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);

            // To
            if (!Attribute.RegionIsOptional || !ppSimpleValue.boolValue)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                label.text = mOptions.LabelTo;
                EditorGUI.BeginProperty(r, label, ppTo);
                EditorGUI.BeginChangeCheck();
                if (Attribute.UseSlider && mOptions.ClampTo == DTValueClamping.Range)
                    ppTo.floatValue = EditorGUI.Slider(r, label, ppTo.floatValue, mOptions.ToMin, mOptions.ToMax);
                else
                    ppTo.floatValue = EditorGUI.FloatField(r, label, ppTo.floatValue);
                if (EditorGUI.EndChangeCheck())
                    validateTo();
                EditorGUI.EndProperty();
            }
        }

        private void validateFrom()
        {
            ppFrom.floatValue = DTMath.SnapPrecision(ppFrom.floatValue, Attribute.Precision);
            switch (mOptions.ClampFrom)
            {
                case DTValueClamping.Min:
                    ppFrom.floatValue = Mathf.Max(mOptions.FromMin, ppFrom.floatValue);
                    break;
                case DTValueClamping.Max:
                    ppFrom.floatValue = Mathf.Min(mOptions.FromMax, ppFrom.floatValue);
                    break;
                case DTValueClamping.Range:
                    ppFrom.floatValue = Mathf.Clamp(ppFrom.floatValue, mOptions.FromMin, mOptions.FromMax);
                    if (minmax)
                        ppFrom.floatValue = Mathf.Min(ppFrom.floatValue, ppTo.floatValue);
                    break;
            }
        }

        private void validateTo()
        {
            ppTo.floatValue = DTMath.SnapPrecision(ppTo.floatValue, Attribute.Precision);
            switch (mOptions.ClampTo)
            {
                case DTValueClamping.Min:
                    ppTo.floatValue = Mathf.Max(mOptions.ToMin, ppTo.floatValue);
                    break;
                case DTValueClamping.Max:
                    ppTo.floatValue = Mathf.Min(mOptions.ToMax, ppTo.floatValue);
                    break;
                case DTValueClamping.Range:
                    ppTo.floatValue = Mathf.Clamp(ppTo.floatValue, mOptions.ToMin, mOptions.ToMax);
                    if (minmax)
                        ppTo.floatValue = Mathf.Max(ppTo.floatValue, ppFrom.floatValue);
                    break;
            }
        }

        private void Init(SerializedProperty property)
        {
            try
            {
                mObject = property.GetParentObject();
                if (mPI == null && !string.IsNullOrEmpty(Attribute.RegionOptionsPropertyName))
                    mPI = mObject.GetType().PropertyByName(Attribute.RegionOptionsPropertyName, true, true);

                if (mPI != null)
                    mOptions = (RegionOptions<float>)mPI.GetValue(mObject, null);
                else
                    mOptions = RegionOptions<float>.Default;
            }
            catch (Exception e)
            {
                DTLog.LogError("[DevTools] FloatRegionPropertyDrawer: Unable to find property '" + Attribute.RegionOptionsPropertyName + "'! (" + e.ToString() + ")", property.serializedObject.targetObject);
            }
            ppFrom = property.FindPropertyRelative("From");
            ppTo = property.FindPropertyRelative("To");
            ppSimpleValue = property.FindPropertyRelative("SimpleValue");
            if (minmax)
                ppSimpleValue.boolValue = false;

        }
    }

    [CustomPropertyDrawer(typeof(IntRegionAttribute))]
    public class IntRegionPropertyDrawer : DTPropertyDrawer<IntRegionAttribute>
    {
        private PropertyInfo mPI;
        private object mObject;
        private SerializedProperty ppFrom;
        private SerializedProperty ppTo;
        private SerializedProperty ppSimpleValue;
        private RegionOptions<int> mOptions;

        private bool compact => (EditorGUIUtility.wideMode ||
                                 ((Options & AttributeOptionsFlags.Compact) == AttributeOptionsFlags.Compact ||
                                  (Options & AttributeOptionsFlags.FullCompact) == AttributeOptionsFlags.FullCompact
                                 )
            );

        private bool minmax => (Attribute.UseSlider && !Attribute.RegionIsOptional &&
                                mOptions.ClampFrom == DTValueClamping.Range && mOptions.ClampTo == DTValueClamping.Range &&
                                mOptions.FromMin == mOptions.ToMin && mOptions.FromMax == mOptions.ToMax);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            float h = base.GetPropertyHeight(property, label);
            if (!compact)
            {
                if (!Attribute.RegionIsOptional || !ppSimpleValue.boolValue || minmax)
                    h = (h + EditorGUIUtility.standardVerticalSpacing) * 2;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            Init(property);
            Rect r = new Rect(position);
            r.height = EditorGUIUtility.singleLineHeight;

            if (minmax)
            {
                doMinmax(r, label, Attribute.RegionIsOptional);
            }
            else if (compact)
            {
                doCompact(r, label, new GUIContent(mOptions.LabelTo), Attribute.RegionIsOptional);
            }
            else
            {
                doRegular(r, label);
            }

            label.tooltip = "";
        }

        private void doMinmax(Rect r, GUIContent label, bool showOptional)
        {
            float l = ppFrom.intValue;
            float u = ppTo.intValue;

            Rect r2 = new Rect(r);

            if (showOptional)
                r2.width -= 16;

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(r2, label, ref l, ref u, mOptions.FromMin, mOptions.FromMax);
            if (EditorGUI.EndChangeCheck())
            {
                ppFrom.intValue = (int)l;
                validateFrom();
                ppTo.intValue = (int)u;
                validateTo();
            }
            if (showOptional)
            {
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(r.xMax - 16, r.y, 16, r.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);
            }
            if (!compact)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                doCompact(r, new GUIContent(mOptions.LabelFrom), new GUIContent(mOptions.LabelTo), false);
            }
        }

        private void doCompact(Rect r, GUIContent label, GUIContent labelTo, bool showOptional, bool fullLine = false)
        {
            float lw = EditorGUIUtility.labelWidth;
            float fw = EditorGUIUtility.fieldWidth;
            Rect r2 = new Rect(r);
            bool showTo = !showOptional || !ppSimpleValue.boolValue;
            float showToLabelWidth = DTGUI.LabelWidth(labelTo);

            float chkWidth = showOptional ? 16 : 0;
            if (fullLine)
            {
                EditorGUIUtility.labelWidth = DTGUI.LabelWidth(label);
                lw = EditorGUIUtility.labelWidth;
            }

            r2.width = (r.width - lw - chkWidth - showToLabelWidth) / 2;

            // From
            r.width = lw + r2.width;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r, ppFrom, label);
            if (EditorGUI.EndChangeCheck())
                validateFrom();

            r.x += r.width;

            // "To" may be optional
            if (showOptional)
            {
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(r.x, r.y, 16, r.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);
                r.x += 16;
            }
            // Show "To"?

            if (showTo)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = showToLabelWidth;
                r.width = r2.width + showToLabelWidth;
                ppTo.intValue = EditorGUI.IntField(r, labelTo, ppTo.intValue);
                if (EditorGUI.EndChangeCheck())
                    validateTo();

            }

            EditorGUIUtility.labelWidth = lw;
            EditorGUIUtility.fieldWidth = fw;
        }

        private void doRegular(Rect r, GUIContent label)
        {
            // From
            Rect rF = new Rect(r);
            if (Attribute.RegionIsOptional)
                rF.width -= 16;
            EditorGUI.BeginProperty(rF, label, ppFrom);
            EditorGUI.BeginChangeCheck();
            if (Attribute.UseSlider && mOptions.ClampFrom == DTValueClamping.Range)
                ppFrom.intValue = EditorGUI.IntSlider(rF, label, ppFrom.intValue, mOptions.FromMin, mOptions.FromMax);
            else
                ppFrom.intValue = EditorGUI.IntField(rF, label, ppFrom.intValue);
            if (EditorGUI.EndChangeCheck())
                validateFrom();
            EditorGUI.EndProperty();
            // Perhaps To is optional
            if (Attribute.RegionIsOptional)
                ppSimpleValue.boolValue = !EditorGUI.Toggle(new Rect(rF.x + rF.width, rF.y, 16, rF.height), new GUIContent("", mOptions.OptionalTooltip), !ppSimpleValue.boolValue);

            // To
            if (!Attribute.RegionIsOptional || !ppSimpleValue.boolValue)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                label.text = mOptions.LabelTo;
                EditorGUI.BeginProperty(r, label, ppTo);
                EditorGUI.BeginChangeCheck();
                if (Attribute.UseSlider && mOptions.ClampTo == DTValueClamping.Range)
                    ppTo.intValue = EditorGUI.IntSlider(r, label, ppTo.intValue, mOptions.ToMin, mOptions.ToMax);
                else
                    ppTo.intValue = EditorGUI.IntField(r, label, ppTo.intValue);
                if (EditorGUI.EndChangeCheck())
                    validateTo();
                EditorGUI.EndProperty();
            }
        }

        private void validateFrom()
        {
            switch (mOptions.ClampFrom)
            {
                case DTValueClamping.Min:
                    ppFrom.intValue = Mathf.Max(mOptions.FromMin, ppFrom.intValue);
                    break;
                case DTValueClamping.Max:
                    ppFrom.intValue = Mathf.Min(mOptions.FromMax, ppFrom.intValue);
                    break;
                case DTValueClamping.Range:
                    ppFrom.intValue = Mathf.Clamp(ppFrom.intValue, mOptions.FromMin, mOptions.FromMax);
                    if (minmax)
                        ppFrom.intValue = Mathf.Min(ppFrom.intValue, ppTo.intValue);
                    break;
            }
        }

        private void validateTo()
        {
            switch (mOptions.ClampTo)
            {
                case DTValueClamping.Min:
                    ppTo.intValue = Mathf.Max(mOptions.ToMin, ppTo.intValue);
                    break;
                case DTValueClamping.Max:
                    ppTo.intValue = Mathf.Min(mOptions.ToMax, ppTo.intValue);
                    break;
                case DTValueClamping.Range:
                    ppTo.intValue = Mathf.Clamp(ppTo.intValue, mOptions.ToMin, mOptions.ToMax);
                    if (minmax)
                        ppTo.intValue = Mathf.Max(ppTo.intValue, ppFrom.intValue);
                    break;
            }
        }

        private void Init(SerializedProperty property)
        {
            try
            {
                mObject = property.GetParentObject();
                if (mPI == null && !string.IsNullOrEmpty(Attribute.RegionOptionsPropertyName))
                    mPI = mObject.GetType().PropertyByName(Attribute.RegionOptionsPropertyName, true, true);

                if (mPI != null)
                    mOptions = (RegionOptions<int>)mPI.GetValue(mObject, null);
                else
                    mOptions = RegionOptions<int>.Default;
            }
            catch (Exception e)
            {
                DTLog.LogError("[DevTools] IntRegionPropertyDrawer: Unable to find property '" + Attribute.RegionOptionsPropertyName + "'! (" + e.ToString() + ")", property.serializedObject.targetObject);
            }
            ppFrom = property.FindPropertyRelative("From");
            ppTo = property.FindPropertyRelative("To");
            ppSimpleValue = property.FindPropertyRelative("SimpleValue");

        }
    }

}
