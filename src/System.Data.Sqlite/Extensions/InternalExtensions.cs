using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace System.Data.Sqlite
{
    internal static class InternalExtensions
    {
        public static string FormatArgs(this string source, params object[] args)
        {
            return string.Format(source, args);
        }
        public static bool HasElement(this ICollection source)
        {
            return source != null && source.Count > 0;
        }

        public static string ToStringOrEmpty(this object source)
        {
            if (source == null)
                return string.Empty;
            return source.ToString();
        }
        public static MethodInfo GetArraySetValueMethod(this Type type)
        {
            return type.GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
        }
        public static MethodInfo GetArrayGetValueMethod(this Type type)
        {
            return type.GetMethod("GetValue", new Type[] { typeof(int) });
        }

    }
}
