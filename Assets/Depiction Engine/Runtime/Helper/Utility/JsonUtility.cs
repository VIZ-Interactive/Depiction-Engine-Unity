// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the manipulation of JSON data.
    /// </summary>
    public class JsonUtility
    {
        public static bool FromJson<T>(out T value, JSONNode json)
        {
            if (FromJson(out object parsedValue, json, typeof(T)))
            {
                value = (T)parsedValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public static bool FromJson(out object value, JSONNode json, Type type)
        {
            bool success = true;

            object parsedValue = null;

            try
            {
                if (type.IsSubclassOf(typeof(JSONNode)) || type == typeof(JSONNode))
                    parsedValue = json;
                else if (json != null && type != null)
                {
                    if (json.IsArray)
                    {
                        JSONArray jsonArr = json.AsArray;
                        if (type == typeof(string))
                            parsedValue = jsonArr.ToString();
                        else if (type.IsArray)
                        {
                            Type itemType = type.GetElementType();
                            Array arr = Array.CreateInstance(itemType, jsonArr.Count);
                            parsedValue = arr;
                            for (int i = 0; i < jsonArr.Count; i++)
                            {
                                if (FromJson(out object itemValue, jsonArr[i], itemType))
                                    arr.SetValue(itemValue, i);
                            }
                        }
                        else if (typeof(IEnumerable).IsAssignableFrom(type))
                        {
                            Type itemType = type.GetGenericArguments().Single();
                            IList list = (IList)Activator.CreateInstance(type);
                            parsedValue = list;
                            for (int i = 0; i < jsonArr.Count; i++)
                            {
                                if (FromJson(out object itemValue, jsonArr[i], itemType))
                                    list.Add(itemValue);
                            }
                        }
                        else
                            success = false;
                    }
                    else if (json.IsBoolean)
                        parsedValue = json.AsBool;
                    else if (json.IsNumber)
                    {
                        if (type == typeof(string))
                            parsedValue = json.ToString();
                        else if (type == typeof(int))
                            parsedValue = json.AsInt;
                        else if (type == typeof(uint))
                            parsedValue = (uint)json.AsInt;
                        else if (type == typeof(double))
                            parsedValue = json.AsDouble;
                        else if (type == typeof(float))
                            parsedValue = json.AsFloat;
                        else if (type.IsEnum)
                        {
                            if (Enum.TryParse(type, json, out object parsedEnum))
                                parsedValue = parsedEnum;
                            else
                                success = false;
                        }
                        else
                            success = false;
                    }
                    else if (json.IsString)
                    {
                        string jsonStr = Regex.Unescape(json.Value != null ? json : "");

                        if (type == typeof(string))
                            parsedValue = jsonStr;
                        else if (type == typeof(Type))
                        {
                            if (!string.IsNullOrEmpty(jsonStr))
                            {
                                if (jsonStr.StartsWith("PPtr<$"))
                                    jsonStr = jsonStr[6..^1];

                                parsedValue ??= Type.GetType(jsonStr);
                                parsedValue ??= Type.GetType("System." + jsonStr);
                                parsedValue ??= Type.GetType(typeof(JsonUtility).Namespace + "." + jsonStr);
                                parsedValue ??= Type.GetType("UnityEngine." + jsonStr + ", UnityEngine");
                                if (parsedValue == null)
                                {
                                    switch (jsonStr)
                                    {
                                        case "bool":
                                            parsedValue = typeof(bool);
                                            break;
                                        case "string":
                                            parsedValue = typeof(string);
                                            break;
                                        case "double":
                                            parsedValue = typeof(double);
                                            break;
                                        case "float":
                                            parsedValue = typeof(float);
                                            break;
                                        case "int":
                                            parsedValue = typeof(int);
                                            break;
                                        case "uint":
                                            parsedValue = typeof(uint);
                                            break;
                                    }
                                }
                            }
                            if (parsedValue == null)
                                success = false;
                        }
                        else if (type == typeof(double))
                        {
                            parsedValue = Convert.ToDouble(jsonStr);
                            success = true;
                        }
                        else if (type == typeof(SerializableGuid))
                        {
                            if (SerializableGuid.TryParse(jsonStr, out SerializableGuid parsedGuid))
                                parsedValue = parsedGuid;
                            else
                                success = false;
                        }
                        else if (type == typeof(Guid))
                        {
                            if (Guid.TryParse(jsonStr, out Guid parsedGuid))
                                parsedValue = parsedGuid;
                            else
                                success = false;
                        }
                        else if (type == typeof(Color))
                        {
                            if (ColorUtility.ColorFromString(out Color color, jsonStr))
                                parsedValue = color;
                            else
                                success = false;
                        }
                        else if (type.IsEnum)
                        {
                            if (Enum.TryParse(type, jsonStr, out object parsedEnum))
                                parsedValue = parsedEnum;
                            else
                                success = false;
                        }
                        else
                            success = false;
                    }
                    else if (json.IsObject)
                    {
                        string jsonStr = json.ToString();

                        if (type == typeof(string))
                            parsedValue = jsonStr;
                        else if (type.IsSubclassOf(typeof(UnityEngine.Object)) || type == typeof(UnityEngine.Object) || type == typeof(Vector2) || type == typeof(Vector2Double) || type == typeof(Vector2Int) || type == typeof(Vector4) || type == typeof(Vector4Double) || type == typeof(Vector3) || type == typeof(Vector3Int) || type == typeof(Vector3Double) || type == typeof(GeoCoordinate3) || type == typeof(GeoCoordinate3Double) || type == typeof(GeoCoordinate2) || type == typeof(GeoCoordinate2Double) || type == typeof(Color) || type == typeof(Quaternion) || type == typeof(QuaternionDouble) || type == typeof(Grid2DIndex) || type == typeof(GeoCoordinateGeometries) || type == typeof(GeoCoordinateGeometry) || type == typeof(GeoCoordinatePolygon) || type == typeof(Disposable) || type.IsSubclassOf(typeof(Disposable)))
                        {
                            if (!string.IsNullOrEmpty(jsonStr))
                            {
                                parsedValue = UnityEngine.JsonUtility.FromJson(jsonStr, type);
                                if (parsedValue is Disposable)
                                    InstanceManager.Initialize(parsedValue as Disposable);
                            }
                            else
                                success = false;
                        }
                        else if (type == typeof(Color))
                        {
                            if (ColorUtility.ColorFromString(out Color color, jsonStr))
                                parsedValue = color;
                            else
                                success = false;
                        }
                        else
                            success = false;
                    }
                }
                else
                    success = false;
            }
            catch (Exception)
            {
                success = false;
            }

            if (!success && json != null)
               Debug.LogWarning("Json: "+ json.ToString() +", not successfully parsed to '" + (type != null ? type.Name : "Null") + "'");

            value = parsedValue;

            return success;
        }

        public static void ApplyJsonToObject(IJson iJson, JSONObject json)
        {
            if (iJson != null && json != null)
            {
                Type type = iJson.GetType();

                TypeAccessor accessor = MemberUtility.GetTypeAccessor(type);

                foreach (KeyValuePair<string, JSONNode> jsonProperty in json.AsObject)
                {
                    if (MemberUtility.GetMemberInfoFromMemberName(type, jsonProperty.Key, out PropertyInfo propertyInfo) && GetJsonAttributeFromPropertyInfo(out JsonAttribute jsonAttribute, propertyInfo))
                    {
                        if (propertyInfo.CanWrite && propertyInfo.GetSetMethod() != null)
                        {
                            if (FromJson(out object value, jsonProperty.Value, propertyInfo.PropertyType))
                                MemberUtility.SetPropertyValue(iJson, accessor, propertyInfo, value);
                        }
                        else
                        {
                            if (jsonProperty.Value.IsObject && typeof(IJson).IsAssignableFrom(propertyInfo.PropertyType))
                                ApplyJsonToObject(propertyInfo.GetValue(iJson) as IJson, jsonProperty.Value.AsObject);
                        }
                    }
                }
            }
        }

        public static JSONNode ToJson(object obj)
        {
            JSONNode json = null;

            try
            {
                if (obj != null)
                {
                    Type type = obj.GetType();

                    if (obj is JSONNode)
                        json = obj as JSONNode;
                    else if (obj is ICollection)
                    {
                        json = new JSONArray();

                        foreach (object item in obj as ICollection)
                        {
                            JSONNode itemJson = ToJson(item);
                            if (itemJson != null)
                                json.Add(itemJson);
                        }
                    }
                    else if (obj is bool boolean)
                        json = new JSONBool(boolean);
                    else if (obj is uint || obj is int || obj is double || obj is float)
                        json = new JSONNumber(Convert.ToDouble(obj));
                    else if (obj is string || obj is SerializableGuid || obj is Guid)
                        json = new JSONString(obj.ToString());
                    else if (obj is Enum)
                        json = new JSONString(Enum.GetName(type, obj));
                    else if (obj is Type type1)
                        json = new JSONString(type1.FullName);
                    else if (obj is IJson iJson)
                        json = GetObjectJson(iJson);
                    else if (obj is UnityEngine.Object || obj is Vector2 || obj is Vector2Double || obj is Vector2Int || obj is Vector4 || obj is Vector4Double || obj is Vector3 || obj is Vector3Int || obj is Vector3Double || obj is GeoCoordinate3 || obj is GeoCoordinate3Double || obj is GeoCoordinate2 || obj is GeoCoordinate2Double || obj is Color || obj is Quaternion || obj is QuaternionDouble || obj is Grid2DIndex || obj is GeoCoordinateGeometries || obj is GeoCoordinateGeometry || obj is GeoCoordinatePolygon || obj is Disposable || type.IsSubclassOf(typeof(Disposable)))
                        json = JSONObject.Parse(UnityEngine.JsonUtility.ToJson(obj));

                    if (json == null)
                        Debug.LogError("Object of type '" + (type != null ? type.Name : "Null") + "' is not supported");
                }
            }
            catch (Exception)
            {
            }

            return json;
        }

        public static JSONNode GetObjectJson(IJson iJson, Datasource outOfSynchDatasource = null, JSONNode filter = null)
        {
            JSONNode json = null;

            if (!Disposable.IsDisposed(iJson))
            {
                Type type = iJson.GetType();

                bool isEditorObject = false;
#if UNITY_EDITOR
                isEditorObject = type.Namespace.Length != nameof(DepictionEngine).Length;
#endif

                if (!isEditorObject)
                {
                    if (filter != null && filter.Count == 0)
                        filter = null;

                    json = new JSONObject();

                    if (filter == null || filter.Count == 0)
                    {
                        IterateOverJsonAttribute(iJson,
                            (iJson, accessor, jsonAttribute, propertyInfo) =>
                            {
                                AddPropertyValueToJson(json, propertyInfo.Name, accessor, jsonAttribute, propertyInfo);
                            }, false);
                    }
                    else
                    {
                        TypeAccessor accessor = MemberUtility.GetTypeAccessor(type);

                        foreach (KeyValuePair<string, JSONNode> filteredProperty in filter.AsObject)
                        {
                            string name = filteredProperty.Key;

                            if (MemberUtility.GetMemberInfoFromMemberName(type, name, out PropertyInfo propertyInfo) && GetJsonAttributeFromPropertyInfo(out JsonAttribute jsonAttribute, propertyInfo))
                                AddPropertyValueToJson(json, name, accessor, jsonAttribute, propertyInfo, filteredProperty.Value);
                        }
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void AddPropertyValueToJson(JSONNode json, string name, TypeAccessor accessor, JsonAttribute jsonAttribute, PropertyInfo propertyInfo, JSONNode filter = null)
                    {
                        bool getAllowed = jsonAttribute.get;

                        if (getAllowed && !string.IsNullOrEmpty(jsonAttribute.conditionalGetMethod))
                        {
                            if (MemberUtility.GetMethodInfoFromMethodName(iJson, jsonAttribute.conditionalGetMethod, out MethodInfo conditionalJsonMethodInfo))
                                getAllowed = (bool)conditionalJsonMethodInfo.Invoke(iJson, null);
                            else
                                Debug.LogWarning("Json ConditionalMethod '" + jsonAttribute.conditionalGetMethod + "' was not found on '" + iJson.GetType().Name + "'");
                        }

                        if (getAllowed && (outOfSynchDatasource == null || !outOfSynchDatasource.GetPersistent(iJson.id, out IPersistent persistent) || outOfSynchDatasource.IsPersistentComponentPropertyOutOfSync(persistent, iJson, name)))
                        {
                            object value = MemberUtility.GetPropertyValue(iJson, accessor, propertyInfo);

                            JSONNode jsonValue = null;

                            if (typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                JSONArray jsonValueArray = new JSONArray();

                                foreach (var item in value as ICollection)
                                    jsonValueArray.Add(GetJsonFromValue(item, outOfSynchDatasource, filter));

                                jsonValue = jsonValueArray;
                            }
                            else
                                jsonValue = GetJsonFromValue(value, outOfSynchDatasource, filter);

                            json[name] = jsonValue;
                        }

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        JSONNode GetJsonFromValue(object value, Datasource outOfSynchDatasource = null, JSONNode filter = null)
                        {
                            JSONNode valueJson;

                            if (value != null && typeof(IJson).IsAssignableFrom(value.GetType()))
                                valueJson = GetObjectJson(value as IJson, outOfSynchDatasource, filter);
                            else
                                valueJson = ToJson(value);

                            return valueJson;
                        }
                    }

                }
            }

            return json;
        }

        private static Dictionary<string, Tuple<List<JsonAttribute>, List<PropertyInfo>>> _typeProperties;
        public static void IterateOverJsonAttribute(IJson iJson, Action<IJson, TypeAccessor, JsonAttribute, PropertyInfo> callback, bool deepTraversal = true)
        {
            if (iJson != null)
            {
                Type type = iJson.GetType();

                if (type != null)
                {
                    string typeName = type.FullName;
                    if (iJson.isFallbackValues)
                        typeName += "(" + String.Join(", ", typeof(FallbackValues).Name) + ")";

                    _typeProperties ??= new Dictionary<string, Tuple<List<JsonAttribute>, List<PropertyInfo>>>();
                    if (!_typeProperties.TryGetValue(typeName, out Tuple<List<JsonAttribute>, List<PropertyInfo>> properties))
                    {
                        List<JsonAttribute> jsonAttributes = new();
                        List<PropertyInfo> propertyInfos = new();
                        properties = new(jsonAttributes, propertyInfos);
                        _typeProperties.Add(typeName, properties);

                        IterateOverJsonProperty(type, (jsonAttribute, propertyInfo) =>
                        {
                            jsonAttributes.Add(jsonAttribute);
                            propertyInfos.Add(propertyInfo);
                        });
                    }

                    TypeAccessor accessor = MemberUtility.GetTypeAccessor(type);

                    for (int i = 0; i < properties.Item1.Count; i++)
                    {
                        JsonAttribute jsonAttribute = properties.Item1[i];
                        PropertyInfo propertyInfo = properties.Item2[i];

                        callback(iJson, accessor, jsonAttribute, propertyInfo);

                        if (deepTraversal && propertyInfo.CanRead && propertyInfo.GetValue(iJson) is IJson iJsonProperty)
                            IterateOverJsonAttribute(iJsonProperty, callback);
                    }
                }
            }
        }

        public static void IterateOverJsonProperty(Type type, Action<JsonAttribute, PropertyInfo> callback)
        {
            while (type != null)
            {
                foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (GetJsonAttributeFromPropertyInfo(out JsonAttribute jsonAttribute, propertyInfo))
                        callback(jsonAttribute, propertyInfo);
                }

                type = type.BaseType;
                if (type == typeof(MonoBehaviour) || type == typeof(ScriptableObject))
                    type = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetJsonAttribute(IJson iJson, string name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && !Disposable.IsDisposed(iJson))
            {
                if (MemberUtility.GetMemberInfoFromMemberName(iJson.GetType(), name, out propertyInfo))
                {
                    if (GetJsonAttributeFromPropertyInfo(out jsonAttribute, propertyInfo))
                        return true;
                }
            }

            jsonAttribute = null;
            propertyInfo = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetJsonAttributeFromPropertyInfo(out JsonAttribute jsonAttribute, PropertyInfo propertyInfo)
        {
            jsonAttribute = propertyInfo.GetCustomAttribute<JsonAttribute>();
            return jsonAttribute != null;
        }

        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            UnityEngine.JsonUtility.FromJsonOverwrite(json, objectToOverwrite);
        }
    }
}
