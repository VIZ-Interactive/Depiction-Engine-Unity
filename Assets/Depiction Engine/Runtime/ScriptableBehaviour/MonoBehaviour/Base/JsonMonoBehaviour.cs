// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    public class JsonMonoBehaviour : PropertyMonoBehaviour, IJson
    {
        private JSONNode _initializationJson;

        protected override void Initializing()
        {
            base.Initializing();

            _initializationJson = GetInitializationJson(InstanceManager.initializeJSON);
        }

        protected virtual JSONNode GetInitializationJson(JSONNode initializeJSON)
        {
            return initializeJSON;
        }

        protected override SerializableGuid GetId(SerializableGuid id, InstanceManager.InitializationContext initializingContext)
        {
            if (_initializationJson != null)
            {
                if (SerializableGuid.TryParse(_initializationJson[nameof(PropertyScriptableObject.id)], out SerializableGuid parsedId))
                    id = parsedId;

                _initializationJson.Remove(nameof(PropertyScriptableObject.id));
            }

            return base.GetId(id, initializingContext);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastEnabled = enabled;

                return true;
            }
            return false;
        }

        protected override void  InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);
          
            InitValue(value => enabled = value, true, initializingContext);
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                if (_initializationJson != null)
                    SetJson(_initializationJson);

                return true;
            }
            return false;
        }

        protected override void Initialized(InstanceManager.InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            _initializationJson = null;
        }

        protected override void DetectChanges()
        {
            base.DetectChanges();

            if (_lastEnabled != enabled)
            {
                bool newValue = enabled;
                (this as MonoBehaviour).enabled = _lastEnabled;
                enabled = newValue;
            }
        }

        protected JSONNode initializationJson
        {
            get { return _initializationJson; }
        }

        public void SetJson(JSONNode json)
        {
            JsonUtility.SetJSON(json, this);
        }

        public JSONObject GetJson(Datasource outOfSynchDatasource = null, JSONNode filter = null)
        {
            return JsonUtility.GetJson(this, this, outOfSynchDatasource, filter) as JSONObject;
        }

        private bool _lastEnabled;
        /// <summary>
        /// Enabled Behaviours are Updated, disable Behaviours are not.
        /// </summary>
        [Json(conditionalMethod: nameof(IsNotFallbackValues))]
        public new bool enabled
        {
            get { return (this as MonoBehaviour).enabled; }
            set 
            {
                if (!CanBeDisabled())
                    value = true;

                bool oldValue = enabled;
                if (HasChanged(value, oldValue))
                {
                    _lastEnabled = (this as MonoBehaviour).enabled = value;
                    PropertyAssigned(this, nameof(enabled), value, oldValue);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetJsonAttribute(string name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo)
        {
            if (!SceneManager.IsSceneBeingDestroyed() && this != Disposable.NULL)
            {
                propertyInfo = MemberUtility.GetMemberInfoFromMemberName<PropertyInfo>(GetType(), name);
                if (propertyInfo != null)
                {
                    jsonAttribute = propertyInfo.GetCustomAttribute<JsonAttribute>();
                    if (jsonAttribute != null && jsonAttribute.get && propertyInfo.CanWrite)
                        return true;
                }
            }

            jsonAttribute = null;
            propertyInfo = null;
            return false;
        }
    }
}
