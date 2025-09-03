// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using System.Collections;
using System.Text;
using System;
using System.Reflection;
using FluffyUnderware.DevTools.Extensions;

namespace FluffyUnderware.DevTools
{

    public class DTObjectDump
    {
        private const int INDENTSPACES = 5;
        private string mIndent;
        private StringBuilder mSB;
        private object mObject;

        public DTObjectDump(object o, int indent = 0)
        {
            mIndent = new string(' ', indent);
            mSB = new StringBuilder();
            mObject = o;

            Type T = o.GetType();
            FieldInfo[] fia = T.GetAllFields(false, true);
            if (fia.Length > 0)
                AppendHeader("Fields");
            foreach (FieldInfo fi in fia)
                AppendMember(fi);

            PropertyInfo[] pia = T.GetAllProperties(false, true);
            if (pia.Length > 0)
                AppendHeader("Properties");
            foreach (PropertyInfo pi in pia)
                AppendMember(pi);
        }

        public override string ToString()
            => mSB.ToString();

        private void AppendHeader(string name)
        {
            mSB.AppendLine(mIndent + "<b>---===| " + name + " |===---</b>\n");
        }

        private void AppendMember(MemberInfo info)
        {
            Type type;
            string typeName;
            object value;

            FieldInfo fi = info as FieldInfo;
            if (fi != null)
            {
                type = fi.FieldType;
                typeName = type.Name;
                value = fi.GetValue(mObject);
            }
            else
            {
                PropertyInfo pi = info as PropertyInfo;
                type = pi.PropertyType;
                typeName = type.Name;
                value = pi.GetValue(mObject, null);
            }

            if (value != null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    string tmp = mIndent;
                    int count = 0;
                    IEnumerable ie = value as IEnumerable;
                    if (ie != null)
                    {
                        if (type.GetEnumerableType().BaseType == typeof(ValueType))
                        {
                            foreach (object e in ie)
                                tmp = string.Concat(tmp, string.Format("<b>{0}</b>: {1} ", (count++).ToString(), e.ToString()));
                        }
                        else
                        {
                            if (typeof(IList).IsAssignableFrom(type))
                                typeName = "IList<" + type.GetEnumerableType() + ">";
                            tmp += "\n";
                            foreach (object e in ie)
                                tmp = string.Concat(tmp, string.Format("<b>{0}</b>: {1} ", (count++).ToString(), new DTObjectDump(e, mIndent.Length + INDENTSPACES).ToString()));
                        }
                    }
                    mSB.Append(mIndent);
                    mSB.AppendFormat("(<i>{0}</i>) <b>{1}[{2}]</b> = ", typeName, info.Name, count);
                    mSB.AppendLine(tmp);
                }
                else
                {
                    mSB.Append(mIndent);
                    mSB.AppendFormat("(<i>{0}</i>) <b>{1}</b> = ", typeName, info.Name);
                    mSB.AppendLine(mIndent + value.ToString());
                }
            }
        }
    }

}

