// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Reflection;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    public class JsonScriptableObject : PropertyScriptableObject , IJson
    {
        private JSONNode _initializationJson;

        protected override void Initializing()
        {
            base.Initializing();

            _initializationJson = InstanceManager.initializeJSON;
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

        protected override void Initialized()
        {
            base.Initialized();

            _initializationJson = null;
        }

        protected JSONNode initializationJson
        {
            get { return _initializationJson; }
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

        public void SetJson(JSONNode json)
        {
            JsonUtility.SetJSON(json, this);
        }

        public JSONObject GetJson(Datasource outOfSynchDatasource = null, JSONNode filter = null)
        {
            return JsonUtility.GetJson(this, this, outOfSynchDatasource, filter) as JSONObject;
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
