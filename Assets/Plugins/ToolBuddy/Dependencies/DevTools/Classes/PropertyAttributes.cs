// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

namespace FluffyUnderware.DevTools
{
    [JetBrains.Annotations.UsedImplicitly] [Obsolete]
    public enum DTMessageType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public enum AttributeOptionsFlags : int
    {
        None = 0,
        Compact = 1,
        Clipboard = 128,
        Zero = 256,
        One = 512,
        Negate = 1024,
        Full = 1920,
        FullCompact = 1921
    }

    public class DTPropertyAttribute : PropertyAttribute
    {
        
        public string Label;
        public string Tooltip;
        public string Color;
        public AttributeOptionsFlags Options;
        public int Precision = -1;

        public DTPropertyAttribute(string label="", string tooltip = "")
        {
            Label = label;
            Tooltip = tooltip;
        }
    }

    public class LabelAttribute : DTPropertyAttribute
    {
        public LabelAttribute() : base() { }
        public LabelAttribute(string label, string tooltip = "") : base(label, tooltip) { }
    }

    public class ToggleButtonAttribute : DTPropertyAttribute
    {
        public ToggleButtonAttribute(string label = "", string tooltip = "") : base(label, tooltip)
        {
        }
    }

    public class LayerAttribute : DTPropertyAttribute
    {
        public LayerAttribute(string label = "", string tooltip = "")
            : base(label, tooltip)
        {
        }
    }

    public class TagAttribute : DTPropertyAttribute
    {
        public TagAttribute(string label = "", string tooltip = "")
            : base(label, tooltip)
        {
        }
    }

    public class VectorExAttribute : DTPropertyAttribute
    {
        public VectorExAttribute(string label = "", string tooltip = "") : base(label, tooltip)
        {
            Options = AttributeOptionsFlags.Full;
        }
    }

    public class AnimationCurveExAttribute : DTPropertyAttribute
    {
        public AnimationCurveExAttribute(string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            Options = AttributeOptionsFlags.Clipboard;
        }
    }

    public class MinAttribute : DTPropertyAttribute
    {
        public float MinValue;
        public string MinFieldOrPropertyName;

        public MinAttribute(float value, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinValue = value;
        }

        public MinAttribute(string fieldOrProperty, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinFieldOrPropertyName = fieldOrProperty;
        }
    }

    public class PositiveAttribute : MinAttribute
    {
        public PositiveAttribute() : base(0) { }
    }

    public class MaxAttribute : DTPropertyAttribute
    {
        public float MaxValue;
        public string MaxFieldOrPropertyName;

        public MaxAttribute(float value, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MaxValue = value;
        }

        public MaxAttribute(string fieldOrProperty, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MaxFieldOrPropertyName = fieldOrProperty;
        }
    }

    public class RangeExAttribute : DTPropertyAttribute
    {
        public float MinValue;
        public string MinFieldOrPropertyName;
        public float MaxValue;
        public string MaxFieldOrPropertyName;
        public bool Slider=true;

        public RangeExAttribute(float minValue, float maxValue, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public RangeExAttribute(string minFieldOrProperty, float maxValue, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinFieldOrPropertyName = minFieldOrProperty;
            MaxValue = maxValue;
        }

        public RangeExAttribute(float minValue, string  maxFieldOrProperty, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinValue = minValue;
            MaxFieldOrPropertyName = maxFieldOrProperty;
        }

        public RangeExAttribute(string minFieldOrProperty, string maxFieldOrProperty, string label = "", string tooltip = "")
            : base(label, tooltip)
        {
            MinFieldOrPropertyName = minFieldOrProperty;
            MaxFieldOrPropertyName = maxFieldOrProperty;
        }

    }

    public class MinMaxAttribute : DTPropertyAttribute
    {
        public readonly string MaxValueField;
        public float Min;
        public string MinBoundFieldOrPropertyName;
        public float Max;
        public string MaxBoundFieldOrPropertyName;

        public MinMaxAttribute(string maxValueField, string label="", string tooltip="") : base(label, tooltip) 
        {
            MaxValueField = maxValueField;
            Min = 0;
            Max = 1;
        }
    }

    public class EnumFlagAttribute : DTPropertyAttribute
    {
        public EnumFlagAttribute(string label = "", string tooltip = "") : base(label,tooltip) { }
    }

    public class ObjectSelectorAttribute : DTPropertyAttribute
    {
        public ObjectSelectorAttribute(string label = "", string tooltip = "") : base(label, tooltip)
        { }
    }

    public class PathSelectorAttribute : DTPropertyAttribute
    {
        public enum DialogMode
        {
            OpenFile,
            OpenFolder,
            CreateFile
        }

        public readonly DialogMode Mode;
        public string Title;
        public string Directory;
        public string Extension;
        public string DefaultName;

        public PathSelectorAttribute(DialogMode mode=DialogMode.OpenFile)
        {
            Mode = mode;
            Directory = Application.dataPath;
        }
    }

    public class EnumSelectionGridAttribute : DTPropertyAttribute
    {
        public EnumSelectionGridAttribute(string label="",string tooltip="") : base(label,tooltip) { }
    }

    public class DTRegionAttribute : DTPropertyAttribute
    {
        public bool RegionIsOptional;
        public string RegionOptionsPropertyName;
        public bool UseSlider=true;
    }

    public class FloatRegionAttribute : DTRegionAttribute { }
    public class IntRegionAttribute : DTRegionAttribute { }

    public struct RegionOptions<T>
    {
        public string LabelFrom;
        public string LabelTo;
        public string OptionalTooltip;
        public DTValueClamping ClampFrom;
        public DTValueClamping ClampTo;
        public T FromMin;
        public T FromMax;
        public T ToMin;
        public T ToMax;

        public static RegionOptions<T> Default =>
            new RegionOptions<T>
            {
            OptionalTooltip = "Range",
            LabelFrom= "From",
            LabelTo= "To",
            ClampFrom = DTValueClamping.None,
            ClampTo = DTValueClamping.None
        };

        public static RegionOptions<T> MinMax(T min, T max) =>
            new RegionOptions<T>
            {
                LabelFrom = "From",
                LabelTo = "To",
                ClampFrom = DTValueClamping.Range,
                ClampTo = DTValueClamping.Range,
                FromMin=min,
                FromMax=max,
                ToMin=min,
                ToMax=max
            };
    }

    public enum DTValueClamping
    {
        None = 0,
        Min = 1,
        Max = 2,
        Range = 3
    }

    
}
