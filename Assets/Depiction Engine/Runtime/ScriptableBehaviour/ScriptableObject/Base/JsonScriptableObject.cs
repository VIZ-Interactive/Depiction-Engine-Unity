// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class JsonScriptableObject : PropertyScriptableObject , IJson
    {
        private JSONObject _initializationJson;

        protected override void Initializing()
        {
            base.Initializing();

            _initializationJson = InstanceManager.initializeJSON;
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

        protected JSONNode initializationJson { get => _initializationJson; }
    }
}
