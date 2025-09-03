// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;

namespace FluffyUnderware.DevTools
{
    #region ### Interfaces ###
    /// <summary>
    /// Interface for attributes that change group parsing options
    /// </summary>
    public interface IDTGroupParsingAttribute
    {
        string Path { get; }
    }
    /// <summary>
    /// Interface for attributes that change field parsing options
    /// </summary>
    public interface IDTFieldParsingAttribute { }
    /// <summary>
    /// Interface for attributes that change group rendering options
    /// </summary>
    public interface IDTGroupRenderAttribute { }
    /// <summary>
    /// Interface for attributes that change field rendering options
    /// </summary>
    public interface IDTFieldRenderAttribute { }

    #endregion


    #region ### Base Attributes ###
    //OPTIM: all the attributes bellow (and maybe DTVersionAttribute too) seem to be used only in editor code. If so, and there overhead is big, make their compilation conditional to gain performance in the build applications

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DTAttribute : Attribute, IComparable
    {
        public int TypeSort { get; protected set; }
        public int Sort = 100;
        public bool ShowBelowProperty;
        public int Space;

        public virtual int CompareTo(object obj)
        {
            DTAttribute other = (DTAttribute)obj;
            int v = ShowBelowProperty.CompareTo(other.ShowBelowProperty);
            if (v == 0)
            {
                int ts = TypeSort.CompareTo(other.TypeSort);
                if (ts == 0)
                    return Sort.CompareTo(other.Sort);
                else
                    return ts;
            }
            else
                return v;
        }

        public DTAttribute(int sortOrder, bool showBelow = false)
        {
            TypeSort = sortOrder;
            ShowBelowProperty = showBelow;
        }
    }

    /// <summary>
    /// Explicitely add the next field to a specific group
    /// </summary>
    public class GroupAttribute : DTAttribute, IDTGroupParsingAttribute, IDTGroupRenderAttribute
    {
        public string Path
        {
            get => mPath;
            protected set
            {
                PathIsAbsolute = !string.IsNullOrEmpty(value) && value.StartsWith("@");
                if (PathIsAbsolute)
                {
                    mPath = value.Substring(1);
                    if (string.IsNullOrEmpty(mPath))
                        mPath = null;
                }
                else
                    mPath = value;
            }
        }
        public bool PathIsAbsolute { get; private set; }

        public bool Expanded = true;
        public bool Invisible;
        public string Label = null;
        public string Tooltip = null;
        public string HelpURL = null;

        private string mPath;

        public GroupAttribute(string pathAndName)
            : base(15)
        {
            Path = pathAndName;
        }
    }

    public class ActionAttribute : DTAttribute
    {
        public enum ActionEnum { Show, Hide, Enable, Disable, ShowInfo, ShowWarning, ShowError, Callback }
        public enum ActionPositionEnum { Above, Below }
        public ActionEnum Action = ActionEnum.Callback;
        public ActionPositionEnum Position = ActionPositionEnum.Below;
        public object ActionData;

        private MethodInfo mCallback;

        protected ActionAttribute(string actionData, ActionEnum action = ActionEnum.Callback)
            : base(50)
        {
            ActionData = actionData;
            Action = action;
        }

        public void Callback(object classInstance)
        {
            string methodName = ActionData as string;
            if (!string.IsNullOrEmpty(methodName))
            {
                if (mCallback == null)
                    mCallback = classInstance.GetType().MethodByName(methodName, true, true);

                if (mCallback != null)
                    mCallback.Invoke(classInstance, null);
                else
                    Debug.LogWarningFormat("[DevTools] Unable to find method '{0}' at class '{1}' !", methodName, classInstance.GetType().Name);
            }
        }
    }

    public class ConditionalAttribute : ActionAttribute
    {
        public enum OperatorEnum { AND, OR };
        public class Condition
        {
            public string FieldName;
            public FieldInfo FieldInfo;
            public PropertyInfo PropertyInfo;
            public object CompareTo;
            public bool CompareFalse;
            public OperatorEnum Operator;
            public MethodInfo MethodInfo;
            public string MethodName;
        }

        public Condition[] Conditions;


        protected ConditionalAttribute(string fieldOrProperty, object compareTo, bool compareFalse = false)
            : base(null, ActionEnum.Show)
        {
            TypeSort = 55;
            Conditions = new Condition[1]
            {
                new Condition
                {
                    FieldName=fieldOrProperty,
                    CompareTo=compareTo,
                    CompareFalse=compareFalse
                }
            };
        }

        protected ConditionalAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2)
            : base(null, ActionEnum.Show)
        {
            TypeSort = 55;
            Conditions = new Condition[2]
            {
                new Condition
                {
                    FieldName=fieldOrProperty,
                    CompareTo=compareTo,
                    CompareFalse=compareFalse
                },
                new Condition
                {
                    FieldName=fieldOrProperty2,
                    CompareTo=compareTo2,
                    CompareFalse=compareFalse2,
                    Operator=op
                }
            };
        }

        protected ConditionalAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2,
                                    string fieldOrProperty3, object compareTo3, bool compareFalse3)
            : base(null, ActionEnum.Show)
        {
            TypeSort = 55;
            Conditions = new Condition[3]
            {
                new Condition
                {
                    FieldName=fieldOrProperty,
                    CompareTo=compareTo,
                    CompareFalse=compareFalse
                },
                new Condition
                {
                    FieldName=fieldOrProperty2,
                    CompareTo=compareTo2,
                    CompareFalse=compareFalse2,
                    Operator=op
                },
                new Condition
                {
                    FieldName=fieldOrProperty3,
                    CompareTo=compareTo3,
                    CompareFalse=compareFalse3,
                    Operator=op
                }
            };
        }

        protected ConditionalAttribute(string methodToQuery)
            : base(null, ActionEnum.Show)
        {
            TypeSort = 55;
            Conditions = new Condition[1]
            {
                new Condition
                {
                    MethodName=methodToQuery,
                    CompareTo=null
                }
            };
        }


        public virtual bool ConditionMet(object classInstance)
        {
            bool result = evaluate(Conditions[0], classInstance);

            for (int i = 1; i < Conditions.Length; i++)
            {
                Condition c = Conditions[i];
                switch (c.Operator)
                {
                    case OperatorEnum.AND:
                        result = result && evaluate(c, classInstance);
                        break;
                    case OperatorEnum.OR:
                        result = result || evaluate(c, classInstance);
                        break;
                }
            }
            return result;


        }

        private bool evaluate(Condition cond, object classInstance)
        {
            if (!string.IsNullOrEmpty(cond.MethodName))
            {
                if (cond.MethodInfo == null)
                    cond.MethodInfo = classInstance.GetType().MethodByName(cond.MethodName, true, true);
                if (cond.MethodInfo != null)
                {
                    if (cond.CompareFalse)
                        return !(bool)cond.MethodInfo.Invoke(classInstance, null);
                    else
                        return (bool)cond.MethodInfo.Invoke(classInstance, null);
                }
                else
                {
                    Debug.LogWarningFormat("[DevTools] Unable to find method '{0}' at class '{1}' !", cond.MethodName, classInstance.GetType().Name);
                    return cond.CompareFalse;
                }
            }
            else
            {
                if (cond.FieldInfo == null)
                {
                    cond.FieldInfo = classInstance.GetType().FieldByName(cond.FieldName, true, true);
                    if (cond.FieldInfo == null)
                    {
                        cond.PropertyInfo = classInstance.GetType().PropertyByName(cond.FieldName, true, true);
                    }
                }
                object value = null;
                if (cond.FieldInfo != null)
                    value = cond.FieldInfo.GetValue(classInstance);
                else if (cond.PropertyInfo != null)
                    value = cond.PropertyInfo.GetValue(classInstance, null);

                if (value == null)
                {
                    return (cond.CompareTo == null && !cond.CompareFalse);
                }
                else
                    return value.Equals(cond.CompareTo) == !cond.CompareFalse;
            }
        }

    }

    #endregion

    #region ### Attributes handling Nesting/Parsing ###

    public class SortOrderAttribute : DTAttribute, IDTFieldParsingAttribute
    {
        public SortOrderAttribute(int sort = 100) : base(0)
        {
            Sort = sort;
        }
    }

    /// <summary>
    /// Show children inside a section. Start the path with "@" to break out of any previous sections
    /// </summary>
    public class AsGroupAttribute : GroupAttribute, IDTFieldParsingAttribute, IDTFieldRenderAttribute
    {

        public AsGroupAttribute(string pathAndName = null)
            : base(pathAndName)
        {
            TypeSort = 10;
        }
    }

    /// <summary>
    /// Show all subsequent fields inside a section rendered as a tab
    /// </summary>
    public class TabAttribute : GroupAttribute
    {
        public readonly string TabName;
        public readonly string TabBarName;

        public TabAttribute(string pathAndName)
            : base("")
        {
            TypeSort = 10;
            string p;
            split(pathAndName, out p, out TabBarName, out TabName);
            Path = p;
        }

        private static bool split(string pathAndName, out string path, out string tabBar, out string tabname)
        {
            string[] p = pathAndName.Split('/');
            path = string.Empty;
            tabBar = string.Empty;
            tabname = pathAndName;
            if (p.Length == 0)
                return false;
            else if (p.Length == 1)
            {
                tabname = p[0];
                tabBar = "Default";
                return true;
            }
            else
            {
                tabname = p[p.Length - 1];
                tabBar = p[p.Length - 2];
                path = string.Join("/", p, 0, p.Length - 2);
                return true;
            }

        }
    }

    /// <summary>
    /// Show all subsequent fields inside a section.
    /// </summary>
    public class SectionAttribute : GroupAttribute
    {
        public bool Fixed;

        public SectionAttribute(string name, bool expanded = true, bool fix = false, int sort = 100)
            : base(name)
        {
            Expanded = expanded;
            TypeSort = 10;
            Sort = sort;
            Fixed = fix;
        }
    }

    /// <summary>
    /// Ends any section
    /// </summary>
    public class NoSectionAttribute : SectionAttribute
    {
        public NoSectionAttribute() : base("")
        {
            TypeSort = 10;
        }
    }



    /// <summary>
    /// Hides Field and doesn't enter children
    /// </summary>
    public class Hide : DTAttribute, IDTFieldParsingAttribute, IDTFieldRenderAttribute
    {
        public Hide() : base(20) { }
    }

    /// <summary>
    /// Hides Field, but enter children
    /// </summary>
    public class Inline : DTAttribute, IDTFieldParsingAttribute, IDTFieldRenderAttribute
    {
        public Inline() : base(21) { }
    }





    #endregion

    #region ### Attributes handling visual representation ###

    public class ArrayExAttribute : DTAttribute, IDTFieldParsingAttribute
    {
        public bool Draggable = true;
        public bool ShowHeader = true;
        public bool ShowAdd = true;
        public bool ShowDelete = true;
        public bool DropTarget = true;

        public ArrayExAttribute()
            : base(35)
        {
        }
    }

    #endregion

    #region ### Attributes handling actions and dynamic evaluation ###



    public class FieldActionAttribute : ActionAttribute, IDTFieldRenderAttribute
    {
        public FieldActionAttribute(string actionData, ActionEnum action = ActionEnum.Callback)
            : base(actionData, action) { }

    }

    public class GroupActionAttribute : ActionAttribute, IDTGroupRenderAttribute
    {
        public GroupActionAttribute(string actionData, ActionEnum action = ActionEnum.Callback)
            : base(actionData, action) { }
    }



    public class FieldConditionAttribute : ConditionalAttribute, IDTFieldRenderAttribute
    {
        public FieldConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse = false, ActionEnum action = ActionEnum.Show, object actionData = null, ActionPositionEnum position = ActionPositionEnum.Below)
            : base(fieldOrProperty, compareTo, compareFalse)
        {
            Action = action;
            ActionData = actionData;
            Position = position;
        }


        public FieldConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2)
            : base(fieldOrProperty, compareTo, compareFalse, op, fieldOrProperty2, compareTo2, compareFalse2) { }


        public FieldConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2,
                                    string fieldOrProperty3, object compareTo3, bool compareFalse3)
            : base(fieldOrProperty, compareTo, compareFalse, op, fieldOrProperty2, compareTo2, compareFalse2, fieldOrProperty3, compareTo3, compareFalse3) { }

        public FieldConditionAttribute(string methodToQuery)
            : base(methodToQuery) { }

    }
    public class GroupConditionAttribute : ConditionalAttribute, IDTGroupRenderAttribute
    {
        public GroupConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse = false)
            : base(fieldOrProperty, compareTo, compareFalse) { }


        public GroupConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2)
            : base(fieldOrProperty, compareTo, compareFalse, op, fieldOrProperty2, compareTo2, compareFalse2) { }


        public GroupConditionAttribute(string fieldOrProperty, object compareTo, bool compareFalse, OperatorEnum op,
                                    string fieldOrProperty2, object compareTo2, bool compareFalse2,
                                    string fieldOrProperty3, object compareTo3, bool compareFalse3)
            : base(fieldOrProperty, compareTo, compareFalse, op, fieldOrProperty2, compareTo2, compareFalse2, fieldOrProperty3, compareTo3, compareFalse3) { }

        public GroupConditionAttribute(string methodToQuery)
            : base(methodToQuery) { }
    }

    #endregion

}
