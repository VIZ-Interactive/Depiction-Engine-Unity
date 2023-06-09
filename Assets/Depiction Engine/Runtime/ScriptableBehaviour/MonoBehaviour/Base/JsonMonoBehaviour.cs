// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class JsonMonoBehaviour : PropertyMonoBehaviour, IJson
    {
        private JSONObject _initializationJson;

        protected override void Initializing()
        {
            base.Initializing();

            _initializationJson = GetInitializationJson(InstanceManager.initializeJSON);
        }

        protected virtual JSONObject GetInitializationJson(JSONObject initializeJSON)
        {
            return initializeJSON;
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

        protected override void  InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);
          
            InitValue(value => enabled = value, true, initializingContext);
        }

        protected override bool Initialize(InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                if (_initializationJson != null)
                    JsonUtility.ApplyJsonToObject(this, _initializationJson);

                return true;
            }
            return false;
        }

        protected override SerializableGuid GetId(SerializableGuid id, InitializationContext initializingContext)
        {
            if (_initializationJson != null)
            {
                if (SerializableGuid.TryParse(_initializationJson[nameof(PropertyScriptableObject.id)], out SerializableGuid parsedId))
                    id = parsedId;

                _initializationJson.Remove(nameof(PropertyScriptableObject.id));
            }

            return base.GetId(id, initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            _initializationJson = null;
        }

#if UNITY_EDITOR
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                DetectUserGameObjectChanges();

                return true; 
            }
            return false;
        }
#endif

        /// <summary>
        /// Detect changes that happen as a result of an external influence.
        /// </summary>
        public virtual bool DetectUserGameObjectChanges()
        {
            if (!initialized || IsDisposing())
                return false;

            if (_lastEnabled != enabled)
            {
                bool newValue = enabled;
                (this as MonoBehaviour).enabled = _lastEnabled;
                enabled = newValue;
            }

            return true;
        }

        protected JSONNode initializationJson { get => _initializationJson; }

        private bool _lastEnabled;
        /// <summary>
        /// Enabled Behaviours are Updated, disable Behaviours are not.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public new bool enabled
        {
            get => (this as MonoBehaviour).enabled;
            set 
            {
                if (!CanBeDisabled())
                    value = true;

                bool oldValue = enabled;
                if (HasChanged(value, oldValue, false))
                {
                    _lastEnabled = (this as MonoBehaviour).enabled = value;

                    EnabledChanged(value, oldValue);

                    PropertyAssigned(this, nameof(enabled), value, oldValue);
                }
            }
        }

        protected virtual void EnabledChanged(bool newValue, bool oldValue)
        {

        }
    }
}
