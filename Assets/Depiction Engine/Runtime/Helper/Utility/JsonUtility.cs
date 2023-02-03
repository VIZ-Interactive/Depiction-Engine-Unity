// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the manipulation of JSON data.
    /// </summary>
    public class JsonUtility
    {
        public static JSONNode ToJson(object obj)
        {
            JSONNode json = null;

            try
            {
                if (obj != null)
                {
                    Type type = obj.GetType();

                    if (obj is ICollection)
                    {
                        json = new JSONArray();

                        foreach (object item in obj as ICollection)
                        {
                            JSONNode itemJson = ToJson(item);
                            if (itemJson != null)
                                json.Add(itemJson);
                        }
                    }
                    else if (obj is bool)
                        json = new JSONBool((bool)obj);
                    else if (obj is uint || obj is int || obj is double || obj is float)
                        json = new JSONNumber(Convert.ToDouble(obj));
                    else if (obj is string || obj is SerializableGuid || obj is Guid)
                        json = new JSONString(obj.ToString());
                    else if (obj is Enum)
                        json = new JSONString(Enum.GetName(type, obj));
                    else if (obj is Type)
                        json = new JSONString(((Type)obj).FullName);
                    else if (obj is IJson)
                        json = (obj as IJson).GetJson();
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

        public static bool FromJson<T>(out T value, JSONNode json)
        {
            if (FromJson(out object parsedValue, json, typeof(T)))
            {
               value = (T)parsedValue;
                return true;
            }
            else
            {
                value = default(T);
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
                        if (type.IsArray)
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
                        if (type == typeof(int))
                            parsedValue = json.AsInt;
                        else if (type == typeof(uint))
                            parsedValue = (uint)json.AsInt;
                        else if (type == typeof(double))
                            parsedValue = json.AsDouble;
                        else if (type == typeof(float))
                            parsedValue = json.AsFloat;
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
                                    jsonStr = jsonStr.Substring(6, jsonStr.Length - 7);

                                if (parsedValue == null)
                                    parsedValue = Type.GetType(jsonStr);
                                if (parsedValue == null)
                                    parsedValue = Type.GetType("System." + jsonStr);
                                if (parsedValue == null)
                                    parsedValue = Type.GetType(typeof(JsonUtility).Namespace + "." + jsonStr);
                                if (parsedValue == null)
                                    parsedValue = Type.GetType("UnityEngine."+ jsonStr + ", UnityEngine");
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
            }
            catch (Exception)
            {
                success = false;
            }

            if (!success)
               Debug.LogWarning("Json: '"+ (json != null ? json.ToString() : "") +"', not successfully parsed to '" + (type != null ? type.Name : "Null") + "'");

            value = parsedValue;

            return success;
        }

        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            UnityEngine.JsonUtility.FromJsonOverwrite(json, objectToOverwrite);
        }

        public static JSONNode GetJson(IJson iJson, object property, Datasource outOfSynchDatasource = null, JSONNode filter = null)
        {
            if (property is JSONNode)
                return property as JSONNode;

            JSONNode json = null;

            if (!Disposable.IsDisposed(iJson))
            {
                bool isEditorObject = false;

#if UNITY_EDITOR
                isEditorObject = iJson.GetType().Namespace.Length != nameof(DepictionEngine).Length;
#endif

                if (!isEditorObject)
                {
                    if (property is IJson)
                    {
                        IJson jsonPropertyObject = property as IJson;
                        Type type = jsonPropertyObject.GetType();

                        if (filter != null && filter.Count == 0)
                            filter = null;

                        json = new JSONObject();

                        MemberUtility.IterateOverJsonAttribute(jsonPropertyObject,
                            (iJson, accessor, name, jsonAttribute, propertyInfo) =>
                            {
                                bool addJson = jsonAttribute.get;

                                if (addJson && !string.IsNullOrEmpty(jsonAttribute.conditionalMethod))
                                {
                                    MethodInfo conditionalJsonMethodInfo = MemberUtility.GetMethodInfoFromMethodName(iJson, jsonAttribute.conditionalMethod);
                                    if (conditionalJsonMethodInfo != null)
                                        addJson = (bool)conditionalJsonMethodInfo.Invoke(iJson, null);
                                    else
                                        Debug.LogWarning("Json ConditionalMethod '" + jsonAttribute.conditionalMethod + "' was not found on '" + type.Name + "'");
                                }

                                if (addJson)
                                {
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        if (propertyInfo != null)
                                            property = accessor != null ? accessor[jsonPropertyObject, propertyInfo.Name] : propertyInfo.GetValue(jsonPropertyObject);
                                        else
                                            property = null;

                                        if (outOfSynchDatasource == Disposable.NULL || !outOfSynchDatasource.GetPersistenceData(iJson.id, out PersistenceData persistenceData) || persistenceData.IsPropertyOutOfSync(iJson, name))
                                        {
                                            JSONNode filterNode = GetFilterNode(filter, name);
                                            if (property is ICollection)
                                            {
                                                JSONArray jsonArray = new JSONArray();
                                                ICollection collection = property as ICollection;
                                                foreach (object propertyItem in collection)
                                                    jsonArray.Add(GetJson(iJson, propertyItem, outOfSynchDatasource, filterNode));

                                                AddPropertyToJSON(json, name, jsonArray, filter);
                                            }
                                            else
                                                AddPropertyToJSON(json, name, GetJson(iJson, property, outOfSynchDatasource, filterNode), filter);
                                        }
                                    }
                                }
                            });
                    }
                    else
                        json = ToJson(property);
                }
            }

            return json;
        }

        private static JSONObject GetFilterNode(JSONNode filter, string propertyName)
        {
            if (filter != null)
            {
                filter = filter[propertyName];
                if (filter.IsArray)
                {
                    JSONObject mergedFilter = new JSONObject();

                    foreach (JSONNode filterChild in filter.AsArray)
                    {
                        if (filterChild.IsObject)
                        {
                            JSONObject filterChildObject = filterChild.AsObject;
                            foreach (KeyValuePair<string, JSONNode> keyValuePair in filterChildObject)
                                mergedFilter[keyValuePair.Key] = keyValuePair.Value;
                        }
                    }

                    filter = mergedFilter;
                }
            }
            return filter != null && filter.IsObject ? filter as JSONObject : null;
        }

        private static void AddPropertyToJSON(JSONNode json, string propertyName, JSONNode value, JSONNode filter)
        {
            bool addProperty = filter == null;

            if (!addProperty && filter is JSONObject)
                addProperty = (filter as JSONObject).m_Dict.ContainsKey(propertyName);

            if (addProperty)
                json[propertyName] = value;
        }

        public static void SetJSON(JSONNode json, IJson iJson)
        {
            if (iJson != null && json != null)
            {
                MemberUtility.IterateOverJsonAttribute(iJson,
                     (iJson, accessor, name, jsonAttribute, propertyInfo) =>
                     {
                         if (propertyInfo.CanWrite)
                         {
                             JSONNode jsonValue = json[name];
                             if (jsonValue != null)
                             {
                                 Type propertyType = propertyInfo.PropertyType;

                                 if (FromJson(out object value, jsonValue, propertyType))
                                     AssignValue(accessor, propertyInfo, iJson, propertyInfo.Name, value);
                             }
                         }
                     });
            }
        }

        private static void AssignValue(TypeAccessor accessor, PropertyInfo propertyInfo, IJson iJson, string name, object value)
        {
            try
            {
                if (accessor != null)
                    accessor[iJson, name] = value;
                else
                    propertyInfo.SetValue(iJson, value);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
