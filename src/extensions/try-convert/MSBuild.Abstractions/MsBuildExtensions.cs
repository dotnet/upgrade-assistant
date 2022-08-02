using System;
using System.Reflection;
using System.Xml;

using Microsoft.Build.Construction;

namespace MSBuild.Abstractions
{
    public static class MsBuildExtensions
    {
        public static XmlElement GetXml(this ProjectItemElement projectItem)
        {
            return (XmlElement)GetPropertyValue(projectItem, "XmlElement");

            static object GetPropertyValue(object obj, string propertyName)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException(nameof(obj));
                }

                var objType = obj.GetType();
                var propInfo = GetPropertyInfo(objType, propertyName);

                var propertyValue = propInfo.GetValue(obj, null);
                if (propertyValue == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(propertyName),
                      string.Format("Null value for property {0} in type {1}", propertyName, objType.FullName));
                }
                return propertyValue;
            }

            static PropertyInfo GetPropertyInfo(Type type, string propertyName)
            {
                PropertyInfo? propInfo;
                Type? baseType = type;
                do
                {
                    propInfo = type.GetProperty(propertyName,
                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    baseType = baseType.BaseType;
                }
                while (propInfo == null && baseType != null);

                if (propInfo == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(propertyName),
                      string.Format("Couldn't find property {0} in type {1}", propertyName, type.FullName));
                }
                return propInfo;
            }
        }
    }
}
