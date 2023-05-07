// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
                    if (GetMemberInfoFromMemberName(targetObject.GetType(), property, out memberInfo))
                    {
                        if (memberInfo is FieldInfo fieldInfo)
                            targetObject = fieldInfo.GetValue(targetObject);
                        else if (memberInfo is PropertyInfo propertyInfo)
                            targetObject = propertyInfo.GetValue(targetObject);
                    }
                }
            }

            return (memberInfo, targetObject);
        }

        public static bool GetMethodInfoFromMethodName(object targetObject, string methodName, out MethodInfo member)
        {
            return GetMemberInfoFromMemberName(targetObject.GetType(), methodName, out member);
        }

        public static bool GetMemberInfoFromMemberName<T>(Type targetObjectType, string memberName, out T member) where T : MemberInfo
        {
            int key = (targetObjectType.FullName + memberName).GetHashCode();

            _memberInfoCache ??= new Dictionary<int, MemberInfo>();
            if (!_memberInfoCache.TryGetValue(key, out MemberInfo memberInfo) || memberInfo == null)
            {
                memberInfo = GetMemberInfoFromMemberNameInternal<T>(targetObjectType, memberName);
                _memberInfoCache[key] = memberInfo;
            }

            member = memberInfo as T;
            return member != null;
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
            return GetAllAttributes<T>(targetObject.GetType(), inherit);
        }

        /// <summary>
        /// Retrieves a collection of custom attributes of a specified type T in a specific Class, and optionally inspects the ancestors of that member.
        /// </summary>
        /// <param name="classType">The class type in which attributes should be found.</param>
        /// <param name="inherit">true to inspect the ancestors of element; otherwise, false.</param>
        /// <returns>A collection of the custom attributes that are applied to element and that match T, or an empty collection if no such attributes exist.</returns>
        public static IEnumerable<T> GetAllAttributes<T>(Type classType, bool inherit = true) where T : Attribute
        {
            List<T> attributes = new();

            while (classType != null)
            {
                attributes.AddRange(classType.GetCustomAttributes<T>(inherit));
                classType = classType.BaseType;
            }

            return attributes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> GetMemberAttributes<T>(object targetObject, string propertyPath) where T : Attribute
        {
            (MemberInfo, object) memberInfoTargetObject = GetMemberInfoFromPropertyPath<MemberInfo>(targetObject, propertyPath);

            if (memberInfoTargetObject.Item1 != null)
                return memberInfoTargetObject.Item1.GetCustomAttributes<T>();

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetPropertyValue(IJson iJson, TypeAccessor accessor, PropertyInfo propertyInfo)
        {
            return accessor != null ? accessor[iJson, propertyInfo.Name] : propertyInfo.GetValue(iJson);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPropertyValue(IJson iJson, TypeAccessor accessor, PropertyInfo propertyInfo, object value)
        {
            try
            {
                if (accessor != null)
                    accessor[iJson, propertyInfo.Name] = value;
                else
                    propertyInfo.SetValue(iJson, value);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeAccessor GetTypeAccessor(Type type)
        {
            TypeAccessor accessor = null;

#if ENABLE_MONO
            //Emit is not supported in IL2CPP
            accessor = TypeAccessor.Create(type, true);
#endif

            return accessor;
        }

        /// <summary>
        /// Populate a list with the <see cref="UnityEngine.RequireComponent"/> && optionally <see cref="DepictionEngine.CreateComponentAttribute"/> attribute types found in a specific Class.
        /// </summary>
        /// <param name="types">The reference to the list that will be populated. The list will be cleared first.</param>
        /// <param name="classType">The class type in which attributes should be found.</param>
        /// <param name="includeCreateComponentAttribute">true to include <see cref="DepictionEngine.CreateComponentAttribute"/>.</param>
        /// <returns></returns>
        public static void GetRequiredComponentTypes(ref List<Type> types, Type classType, bool includeCreateComponentAttribute = true)
        {
            types.Clear();

            IEnumerable<RequireComponent> requiredComponents = GetAllAttributes<RequireComponent>(classType, false);
            foreach (RequireComponent requiredComponent in requiredComponents)
            {
                if (requiredComponent.m_Type0 != null)
                    types.Add(requiredComponent.m_Type0);
                if (requiredComponent.m_Type1 != null)
                    types.Add(requiredComponent.m_Type1);
                if (requiredComponent.m_Type2 != null)
                    types.Add(requiredComponent.m_Type2);
            }

            if (includeCreateComponentAttribute)
            {
                IEnumerable<CreateComponentAttribute> createComponents = GetAllAttributes<CreateComponentAttribute>(classType, false);
                foreach (CreateComponentAttribute createComponent in createComponents)
                {
                    if (createComponent.createComponent != null)
                        types.Add(createComponent.createComponent);
                    if (createComponent.createComponent2 != null)
                        types.Add(createComponent.createComponent2);
                    if (createComponent.createComponent3 != null)
                        types.Add(createComponent.createComponent3);
                }
            }
        }
    }
}
