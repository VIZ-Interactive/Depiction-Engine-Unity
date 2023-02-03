// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help interact with members and attributes.
    /// </summary>
    public class MemberUtility
    {
        [NonSerialized]
        private static Dictionary<int, MemberInfo> _memberInfoCache;

        public static (T, object) GetMemberInfoFromPropertyPath<T>(object targetObject, string propertyPath) where T : MemberInfo
        {
            T memberInfo = null;

            string[] properties = propertyPath.Replace(".Array", "").Split('.');
            foreach (string property in properties)
            {
                if (targetObject is IList && property.Contains("data["))
                {
                    memberInfo = null;
                    targetObject = ((IList)targetObject)[(int)Char.GetNumericValue(property[5])];
                }
                else
                {
                    memberInfo = GetMemberInfoFromMemberName<T>(targetObject.GetType(), property);
                    if (memberInfo != null)
                    {
                        if (memberInfo is FieldInfo)
                            targetObject = (memberInfo as FieldInfo).GetValue(targetObject);
                        else if (memberInfo is PropertyInfo)
                            targetObject = (memberInfo as PropertyInfo).GetValue(targetObject);
                    }
                }
            }

            return (memberInfo, targetObject);
        }

        public static MethodInfo GetMethodInfoFromMethodName(object targetObject, string methodName)
        {
            return GetMemberInfoFromMemberName<MethodInfo>(targetObject.GetType(), methodName);
        }

        public static T GetMemberInfoFromMemberName<T>(Type targetObjectType, string memberName) where T : MemberInfo
        {
            if (_memberInfoCache == null)
                _memberInfoCache = new Dictionary<int, MemberInfo>();

            int key = (targetObjectType.FullName + memberName).GetHashCode();
            if (!_memberInfoCache.TryGetValue(key, out MemberInfo memberInfo) || memberInfo == null)
            {
                memberInfo = GetMemberInfoFromMemberNameInternal<T>(targetObjectType, memberName);
                _memberInfoCache[key] = memberInfo;
            }

            return memberInfo as T;
        }

        private static T GetMemberInfoFromMemberNameInternal<T>(Type targetObjectType, string memberName) where T : MemberInfo
        {
            T memberInfo = null;

            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (typeof(T) == typeof(MethodInfo))
                memberInfo = targetObjectType.GetMethod(memberName, bindingFlags) as T;
            else if (typeof(T) == typeof(MemberInfo) || typeof(T).IsSubclassOf(typeof(MemberInfo)))
            {
                MemberInfo[] memberInfos = targetObjectType.GetMember(memberName, bindingFlags);
                if (memberInfos != null && memberInfos.Length > 0)
                    memberInfo = memberInfos[0] as T;
            }

            if (memberInfo == null && targetObjectType.BaseType != null)
                memberInfo = GetMemberInfoFromMemberNameInternal<T>(targetObjectType.BaseType, memberName);

            return memberInfo;
        }

        public static T GetFirstAttribute<T>(IEnumerable<CustomAttribute> attributes) where T : CustomAttribute
        {
            if (attributes != null)
            {
                foreach (CustomAttribute attribute in attributes)
                {
                    if (attribute is T)
                        return attribute as T;
                }
            }
            return null;
        }

        public static IEnumerable<T> GetAllAttributes<T>(object targetObject, bool inherit = true) where T : Attribute
        {
            List<T> attributes = new List<T>();

            Type type = targetObject.GetType();

            while (type != null)
            {
                attributes.AddRange(type.GetCustomAttributes<T>(inherit));
                type = type.BaseType;
            }

            return attributes;
        }

        public static IEnumerable<T> GetMemberAttributes<T>(object targetObject, string propertyPath) where T : Attribute
        {
            (MemberInfo, object) memberInfoTargetObject = GetMemberInfoFromPropertyPath<MemberInfo>(targetObject, propertyPath);

            if (memberInfoTargetObject.Item1 != null)
                return memberInfoTargetObject.Item1.GetCustomAttributes<T>();

            return null;
        }

        private static Dictionary<string, Tuple<List<JsonAttribute>, List<PropertyInfo>>> _typeProperties;
        public static void IterateOverJsonAttribute(IJson iJson, Action<IJson, TypeAccessor, string, JsonAttribute, PropertyInfo> callback)
        {
            if (iJson != null)
            {
                Type type = iJson.GetType();

                if (type != null)
                {
                    if (_typeProperties == null)
                        _typeProperties = new Dictionary<string, Tuple<List<JsonAttribute>, List<PropertyInfo>>>();

                    TypeAccessor accessor = null;

#if UNITY_EDITOR || !UNITY_WEBGL
                    //Emit is not supported in WebGL
                    accessor = TypeAccessor.Create(type, true);
#endif
                    string typeName = type.FullName;

                    List<string> category = new List<string>();
                    if (iJson.isFallbackValues)
                        category.Add(typeof(FallbackValues).Name);

                    if (category.Count > 0)
                        typeName += "("+ String.Join(", ", category)+ ")";

                    if (!_typeProperties.TryGetValue(typeName, out Tuple<List<JsonAttribute>, List<PropertyInfo>> properties))
                    {
                        List<JsonAttribute> jsonAttributes = new List<JsonAttribute>();
                        List<PropertyInfo> propertyInfos = new List<PropertyInfo>();
                        properties = new Tuple<List<JsonAttribute>, List<PropertyInfo>>(jsonAttributes, propertyInfos);
                        _typeProperties.Add(typeName, properties);

                        JsonAttribute jsonAttribute;
                        while (type != null)
                        {
                            foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            {
                                jsonAttribute = propertyInfo.GetCustomAttribute<JsonAttribute>();
                                if (jsonAttribute != null)
                                {
                                    jsonAttributes.Add(jsonAttribute);
                                    propertyInfos.Add(propertyInfo);
                                }
                            }

                            type = type.BaseType;
                            if (type == typeof(MonoBehaviour) || type == typeof(ScriptableObject))
                                type = null;
                        }
                    }

                    for (int i = 0; i < properties.Item1.Count; i++)
                    {
                        JsonAttribute jsonAttribute = properties.Item1[i];
                        PropertyInfo propertyInfo = properties.Item2[i];

                        string name = propertyInfo.Name;

                        if (!string.IsNullOrEmpty(jsonAttribute.propertyName))
                            propertyInfo = GetMemberInfoFromMemberName<PropertyInfo>(iJson.GetType(), jsonAttribute.propertyName);

                        callback(iJson, accessor, name, jsonAttribute, propertyInfo);

                        if (propertyInfo.CanRead)
                        {
                            object value = propertyInfo.GetValue(iJson);
                            if (value is IJson)
                                IterateOverJsonAttribute(value as IJson, callback);
                        }
                    }
                }
            }
        }
    }
}
