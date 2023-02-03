﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Communication/" + nameof(FallbackValues))]
    public class FallbackValues : Script
    {
#if UNITY_EDITOR
        [BeginFoldout("Type")]
        [SerializeField, EndFoldout]
        private string _instanceType;
        public string instanceType { get { return _instanceType; } set { } }
#endif
        [SerializeField, HideInInspector]
        private UnityEngine.Object _fallbackValuesObject;

        [SerializeField, HideInInspector]
        private string _fallbackValuesJsonStr;

        private JSONObject _fallbackValuesJson;

        private int _fallbackValuesObjectReferences;

        private static Dictionary<Type, JSONObject> _jsonCache;

        public override void Recycle()
        {
            base.Recycle();

            _fallbackValuesJson = null;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                JsonUtility.FromJson(out _lastFallbackValuesJsonStr, fallbackValuesJson);
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            DisposeFallbackValuesObject();
        }

#if UNITY_EDITOR
        private string _lastFallbackValuesJsonStr;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            if (_lastFallbackValuesJsonStr != _fallbackValuesJsonStr)
                SetFallbackValuesJson((JSONObject)JSONObject.Parse(_fallbackValuesJsonStr), true);
        }
#endif

        public void SetFallbackJsonFromType(string typeFullName)
        {
            Type type = ParseType(typeFullName);
            if (GetFallbackValuesType() != type && IsValidFallbackValuesType(type))
            {
                DisposeFallbackValuesObject();

                if (_jsonCache == null)
                    _jsonCache = new Dictionary<Type, JSONObject>();

                if (!_jsonCache.TryGetValue(type, out JSONObject fallbackValuesJson))
                {
                    UnityEngine.Object fallbackValuesObject = GetFallbackValuesObject(type);

                    fallbackValuesJson = fallbackValuesObject != null && fallbackValuesObject is IJson ? (fallbackValuesObject as IJson).GetJson() : new JSONObject();
                    
                    ReleaseFallbackValuesObject(fallbackValuesObject);
                    _jsonCache.Add(type, fallbackValuesJson);
                }

                SetFallbackValuesJson((JSONObject)fallbackValuesJson.Clone());
            }
        }

        public static bool IsValidFallbackValuesType(Type type)
        {
            bool isValidFallbackValuesType = false;

            if (type != null)
            {
                if (type != typeof(FallbackValues) && type != typeof(RTTCamera) && type != typeof(Interior) && !type.FullName.EndsWith("Base"))
                {
#if UNITY_EDITOR
                    if (!SceneManager.IsEditorNamespace(type))
                        isValidFallbackValuesType = true;
#else
                    isValidFallbackValuesType = true;
#endif
                }
            }

            return isValidFallbackValuesType;
        }

        public static Type ParseType(string instanceType)
        {
            if (!string.IsNullOrEmpty(instanceType) && JsonUtility.FromJson(out Type type, instanceType))
                return type;
            return null;
        }

#if UNITY_EDITOR
        private Type _pendingInspectorTypeChange;
        public void SetPendingInspectorTypeChange(Type type)
        {
            _pendingInspectorTypeChange = type;
        }
#endif

        public Type GetFallbackValuesType()
        {
#if UNITY_EDITOR
            if (_pendingInspectorTypeChange != null)
                return _pendingInspectorTypeChange;
#endif
            if (GetProperty(out Type type, nameof(type)))
                return type;
            return null;
        }

        private UnityEngine.Object fallbackValuesObject
        {
            get { return _fallbackValuesObject; }
            set
            {
                if (!HasChanged(value, _fallbackValuesObject))
                    return;

               _fallbackValuesObject = value;

                if (_fallbackValuesObject != null)
                    _fallbackValuesObject.name = name;
            }
        }

        /// <summary>
        /// A JSON representation of the fallback values
        /// </summary>
        [Json]
        public JSONObject fallbackValuesJson
        {
            get { return _fallbackValuesJson; }
            set { SetFallbackValuesJson(ValidateFallbackValuesJson(value)); }
        }

        private bool SetFallbackValuesJson(JSONObject value, bool updateFallbackValuesObject = false)
        {
            return SetValue(nameof(fallbackValuesJson), value, ref _fallbackValuesJson, (newValue, oldValue) =>
            {
#if UNITY_EDITOR
                _pendingInspectorTypeChange = null;

                JsonUtility.FromJson(out string jsonStr, newValue);

                if (fallbackValuesObject != null)
                {
                    if (updateFallbackValuesObject)
                        (fallbackValuesObject as IJson).SetJson(newValue);
                }

                UpdateFallbackJsonStr(jsonStr);

                Type fallbackValuesType = GetFallbackValuesType();
                inspectorComponentNameOverride = (fallbackValuesType != null ? fallbackValuesType.Name : "") + "FallbackValues";
#endif
            });
        }

#if UNITY_EDITOR
        private void UpdateFallbackJsonStr(string jsonStr)
        {
            _lastFallbackValuesJsonStr = _fallbackValuesJsonStr = jsonStr;
        }
#endif

        private JSONObject ValidateFallbackValuesJson(JSONObject fallbackValuesJson)
        {
            if (fallbackValuesJson != null)
            {
                string idName = nameof(id);
                if (fallbackValuesJson[idName] != null)
                    fallbackValuesJson.Remove(idName);

                string enabledName = nameof(Object.enabled);
                if (fallbackValuesJson[enabledName] != null)
                    fallbackValuesJson.Remove(enabledName);

                string nameName = nameof(Object.name);
                if (fallbackValuesJson[nameName] != null)
                    fallbackValuesJson.Remove(nameName);
            }

            return fallbackValuesJson;
        }

        public bool GetProperty<T>(out T value, string name)
        {
            value = default(T);
            return fallbackValuesJson != null && JsonUtility.FromJson(out value, fallbackValuesJson[name]);
        }

        public bool SetProperty(string name, object value)
        {
            if (fallbackValuesJson != null && fallbackValuesJson[name] != null)
            {
                fallbackValuesJson[name] = JsonUtility.ToJson(value);

#if UNITY_EDITOR
                JsonUtility.FromJson(out string jsonStr, fallbackValuesJson);
                UpdateFallbackJsonStr(jsonStr);
#endif

                DisposeFallbackValuesObject();

                PropertyAssigned(this, nameof(fallbackValuesJson), fallbackValuesJson, null);

                return true;
            }
            return false;
        }

        public UnityEngine.Object GetFallbackValuesObject(Type type, JSONNode json = null)
        {
            if (fallbackValuesObject == null || fallbackValuesObject.GetType() != type)
            {
                if (fallbackValuesObject != null)
                    DisposeFallbackValuesObject();

                InstanceManager instanceManager = InstanceManager.Instance(false);

                if (instanceManager != Disposable.NULL)
                    fallbackValuesObject = instanceManager.CreateInstance(type, null, json, isFallbackValues: true) as UnityEngine.Object;
            }

            _fallbackValuesObjectReferences++;

            return fallbackValuesObject;
        }

        public void ReleaseFallbackValuesObject(UnityEngine.Object fallbackValuesObject)
        {
            if (Object.ReferenceEquals(fallbackValuesObject, this.fallbackValuesObject))
            {
                _fallbackValuesObjectReferences--;
                if (_fallbackValuesObjectReferences == 0)
                    DisposeFallbackValuesObject();
            }
        }

        private void DisposeFallbackValuesObject()
        {
            if (!Disposable.IsDisposed(fallbackValuesObject))
                Dispose(fallbackValuesObject is MonoBehaviour ? (fallbackValuesObject as MonoBehaviour).gameObject : fallbackValuesObject);
            fallbackValuesObject = null;

            _fallbackValuesObjectReferences = 0;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        private JSONObject GetFallbackValuesJson()
        {
            return fallbackValuesJson;
        }

        public void ApplyFallbackValuesToJson(JSONObject toJson)
        {
            MergeJson(GetFallbackValuesJson(), toJson);
        }

        private bool MergeJson(JSONObject fallbackJson, JSONNode toJson)
        {
            if (fallbackJson != null && toJson != null)
            {
                foreach (string fallbackValuesKey in fallbackJson.m_Dict.Keys)
                {
                    string key = fallbackValuesKey;

                    JSONNode value = fallbackJson.m_Dict[key];

                    switch(key)
                    {
                        case nameof(Object.animatorId):

                            key = nameof(Object.animator);
                            value = GetFallbackValuesJsonFromId(typeof(AnimatorBase), nameof(Object.animatorId), nameof(Object.createAnimatorIfMissing), fallbackJson);

                            break;

                        case nameof(Object.controllerId):

                            key = nameof(Object.controller);
                            value = GetFallbackValuesJsonFromId(typeof(ControllerBase), nameof(Object.controllerId), nameof(Object.createControllerIfMissing), fallbackJson);

                            break;

                        case nameof(Object.generatorsId):

                            key = nameof(Object.generators);
                            value = GetFallbackValuesJsonFromId(typeof(GeneratorBase), nameof(Object.generatorsId), nameof(Object.createGeneratorIfMissing), fallbackJson);

                            break;

                        case nameof(Object.effectsId):

                            key = nameof(Object.effects);
                            value = GetFallbackValuesJsonFromId(typeof(EffectBase), nameof(Object.effectsId), nameof(Object.createEffectIfMissing), fallbackJson);

                            break;

                        case nameof(Object.referencesId):

                            key = nameof(Object.references);
                            value = GetFallbackValuesJsonFromId(typeof(ReferenceBase), nameof(Object.referencesId), nameof(Object.createReferenceIfMissing), fallbackJson);

                            break;

                        case nameof(Object.fallbackValuesId):

                            key = nameof(Object.fallbackValues);
                            value = GetFallbackValuesJsonFromId(typeof(FallbackValues), nameof(Object.fallbackValuesId), nameof(Object.createFallbackValuesIfMissing), fallbackJson);

                            break;

                        case nameof(Object.datasourcesId):

                            key = nameof(Object.datasources);
                            value = GetFallbackValuesJsonFromId(typeof(DatasourceBase), nameof(Object.datasourcesId), nameof(Object.createDatasourceIfMissing), fallbackJson);

                            break;
                    }

                    if (toJson[key] == null)
                        toJson[key] = value;
                    else if (fallbackJson[key] is JSONObject)
                        MergeJson(fallbackJson[key] as JSONObject, toJson[key]);
                }

                return true;
            }

            return false;
        }

        private JSONNode GetFallbackValuesJsonFromId(Type type, string key, string createIfMissingKey, JSONNode fallbackJson)
        {
            JSONNode newValue = null;

            JSONNode idJson = fallbackJson[key];
            if (idJson != null && fallbackJson[createIfMissingKey].AsBool)
            {
                if (idJson.IsArray)
                {
                    JSONArray fallbackValueArr = idJson.AsArray;

                    if (fallbackValueArr != null && fallbackValueArr.Count > 0)
                    {
                        for (int i = 0; i < fallbackValueArr.Count; i++)
                        {
                            if (TryGetFallbackValuesJson(type, fallbackValueArr[i], out JSONNode fallbackValueJson))
                            {
                                if (newValue == null)
                                    newValue = new JSONArray();
                                newValue.Add(fallbackValueJson);
                            }
                        }
                    }
                }
                else
                {
                    if (TryGetFallbackValuesJson(type, idJson, out JSONNode fallbackValueJson))
                        newValue = fallbackValueJson;
                }
            }

            return newValue;
        }

        private bool TryGetFallbackValuesJson(Type type, string id, out JSONNode scriptFallbackValuesJson)
        {
            if (SerializableGuid.TryParse(id, out SerializableGuid parsedScriptFallbackValuesId))
            {
                FallbackValues scriptFallbackValues = instanceManager.GetFallbackValues(parsedScriptFallbackValuesId);
                if (scriptFallbackValues != Disposable.NULL)
                {
                    Type fallbackValuesType = scriptFallbackValues.GetFallbackValuesType();
                    if (fallbackValuesType != null && type.IsAssignableFrom(fallbackValuesType))
                    {
                        scriptFallbackValuesJson = scriptFallbackValues.GetFallbackValuesJson();
                        return true;
                    }
                }
            }

            scriptFallbackValuesJson = null;
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            {
                DisposeFallbackValuesObject();

                return true;
            }
            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            JsonUtility.FromJson(out _fallbackValuesJsonStr, _fallbackValuesJson);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            _fallbackValuesJson = JSONObject.Parse(_fallbackValuesJsonStr) as JSONObject;
        }
    }
}