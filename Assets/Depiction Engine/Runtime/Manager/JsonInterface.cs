// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// JSON Interface exposing engine functionalities to an external source.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(JsonInterface))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class JsonInterface : ManagerBase
    {
        //Error Msg
        private const string INVALID_TYPE_ERROR_MSG = "Invalid Type";
        private const string ID_NOT_FOUND_ERROR_MSG = "Id not found";
        private const string NAME_NOT_FOUND_ERROR_MSG = "Name not found";
        private const string NOTHING_FOUND_ERROR_MSG = "Nothing found";
        private const string ID_ALREADY_EXIST_ERROR_MSG = "Id already exist";
        private const string METHOD_NOT_FOUND_ERROR_MSG = "Method not found";
        private const string PROPERTY_NOT_FOUND_ERROR_MSG = "Property not found";
        private const string FIELD_NOT_FOUND_ERROR_MSG = "Field not found";
        private const string MISSING_TYPE_ERROR_MSG = "Missing Type";
        private const string CREATION_FAILED_ERROR_MSG = "Creation failed";

        //Events
        private const string INSTANCE_ADDED_EVENT = "instanceAdded";
        private const string INSTANCE_REMOVED_EVENT = "instanceRemoved";
        private const string MOUSE_MOVE_EVENT = "mouseMove";
        private const string MOUSE_ENTER_EVENT = "mouseEnter";
        private const string MOUSE_EXIT_EVENT = "mouseExit";
        private const string MOUSE_UP_EVENT = "mouseUp";
        private const string MOUSE_DOWN_EVENT = "mouseDown";
        private const string MOUSE_CLICKED_EVENT = "mouseClicked";
        private const string PRE_HIERARCHICAL_UPDATE_EVENT = "preHierarchicalUpdate";
        private const string HIERARCHICAL_UPDATE_EVENT = "hierarchicalUpdate";
        private const string POST_HIERARCHICAL_UPDATE_EVENT = "postHierarchicalUpdate";
        private const string HIERARCHICAL_FIXED_UPDATE_EVENT = "hierarchicalFixedUpdate";
        private const string HIERARCHICAL_BEGIN_CAMERA_RENDERING_EVENT = "hierarchicalBeginCameraRendering";
        private const string HIERARCHICAL_END_CAMERA_RENDERING_EVENT = "hierarchicalEndCameraRendering";

        //JSON
        //OperationType
        private const string INIT_OPERATION = "init";
        private const string SET_OPERATION = "set";
        private const string GET_OPERATION = "get";
        private const string REFLECTION_OPERATION = "reflection";
        private const string CREATE_OPERATION = "create";
        private const string DISPOSE_OPERATION = "dispose";

        //Keys
        private const string POINT = "point";
        private const string TRANSFORM = "transform";
        private const string MESHRENDERER_VISUAL = "meshRendererVisual";
        private const string VISUAL_OBJECT = "visualObject";
        private const string OPERATION_TYPE = "operationType";
        private const string REFLECTION_TYPE = "reflectionType";
        private const string GET_ID = "getId";
        private const string GET_NAME = "getName";
        private const string GET_TYPE = "getType";
        private const string ID = "id";
        private const string NAME = "name";
        private const string TYPE = "type";
        private const string CAMERA = "camera";
        private const string VALUE = "value";
        private const string VALUES = "values";
        private const string METHOD = "method";
        private const string PROPERTY = "property";
        private const string FIELD = "field";
        private const string FIELDS = "fields";
        private const string FIND_IN_CHILDREN = "findInChildren";
        private const string PARAMETERS = "parameters";
        private const string GAME_OBJECT = "gameObject";
        private const string OBJECT_ID = "objectId";
        private const string EVENT_TYPE = "eventType";

        private string _instanceId;

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                InputManager.OnMouseMoveEvent -= InputManagerOnMouseMoveHandler;
                InputManager.OnMouseEnterEvent -= InputManagerOnMouseEnterHandler;
                InputManager.OnMouseExitEvent -= InputManagerOnMouseExitHandler;
                InputManager.OnMouseUpEvent -= InputManagerOnMouseUpHandler;
                InputManager.OnMouseDownEvent -= InputManagerOnMouseDownHandler;
                InputManager.OnMouseClickedEvent -= InputManagerOnMouseClickedHandler;
                if (!IsDisposing())
                {
                    InputManager.OnMouseMoveEvent += InputManagerOnMouseMoveHandler;
                    InputManager.OnMouseEnterEvent += InputManagerOnMouseEnterHandler;
                    InputManager.OnMouseExitEvent += InputManagerOnMouseExitHandler;
                    InputManager.OnMouseUpEvent += InputManagerOnMouseUpHandler;
                    InputManager.OnMouseDownEvent += InputManagerOnMouseDownHandler;
                    InputManager.OnMouseClickedEvent += InputManagerOnMouseClickedHandler;
                }
                
                return true;
            }
            return false;
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            SentExternalEventMessage(INSTANCE_ADDED_EVENT, GetInstanceEventJSON(property));
        }

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceRemovedHandler(property);

            SentExternalEventMessage(INSTANCE_REMOVED_EVENT, GetInstanceEventJSON(property));
        }

        private JSONObject _instanceEventJson;
        private JSONObject GetInstanceEventJSON(IProperty property)
        {
            if (_instanceEventJson == null)
                _instanceEventJson = new JSONObject();
            _instanceEventJson[ID] = JsonUtility.ToJson(property.id);
            _instanceEventJson[TYPE] = JsonUtility.ToJson(property.GetType());
            return _instanceEventJson;
        }

        private void InputManagerOnMouseMoveHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_MOVE_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        private void InputManagerOnMouseEnterHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_ENTER_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        private void InputManagerOnMouseExitHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_EXIT_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        private void InputManagerOnMouseUpHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_UP_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        private void InputManagerOnMouseDownHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_DOWN_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        private void InputManagerOnMouseClickedHandler(RaycastHitDouble raycastHitDouble)
        {
            SentExternalEventMessage(MOUSE_CLICKED_EVENT, GetMouseEventJSON(raycastHitDouble));
        }

        public static string NewGuid()
        {
            return SerializableGuid.NewGuid().ToString();
        }

        private JSONObject _mouseEventJson;
        private JSONObject _mouseEventVisualObjectJson;
        private JSONObject GetMouseEventJSON(RaycastHitDouble raycastHitDouble)
        {
            if (raycastHitDouble != null)
            {
                if (_mouseEventJson == null)
                    _mouseEventJson = new JSONObject();
                _mouseEventJson[POINT] = JsonUtility.ToJson(raycastHitDouble.point);
                JSONObject transformJson = new()
                {
                    [ID] = raycastHitDouble.transform.id.ToString()
                };
                _mouseEventJson[TRANSFORM] = transformJson;
                _mouseEventJson[MESHRENDERER_VISUAL] = raycastHitDouble.meshRendererVisual.id.ToString();
                if (_mouseEventVisualObjectJson == null)
                {
                    _mouseEventVisualObjectJson = new JSONObject();
                    _mouseEventJson[VISUAL_OBJECT] = _mouseEventVisualObjectJson;
                }
                _mouseEventVisualObjectJson[ID] = JsonUtility.ToJson(raycastHitDouble.meshRendererVisual.visualObject.id);
                _mouseEventVisualObjectJson[NAME] = raycastHitDouble.meshRendererVisual.visualObject.name;
                _mouseEventVisualObjectJson[TYPE] = JsonUtility.ToJson(raycastHitDouble.meshRendererVisual.visualObject.GetType());
                return _mouseEventJson;
            }
            return null;
        }

        private IJson GetIJSONFromId(string id)
        {
            if (!string.IsNullOrEmpty(id) && SerializableGuid.TryParse(id, out SerializableGuid guid))
                return instanceManager.GetIJson(guid);
            return null;
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                SentExternalEventMessage(PRE_HIERARCHICAL_UPDATE_EVENT);
                return true;
            }
            return false;
        }

        public override bool HierarchicalUpdate()
        {
            if (base.HierarchicalUpdate())
            {
                SentExternalEventMessage(HIERARCHICAL_UPDATE_EVENT);
                return true;
            }
            return false;
        }

        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                SentExternalEventMessage(POST_HIERARCHICAL_UPDATE_EVENT);
                return true;
            }
            return false;
        }

        public override void HierarchicalFixedUpdate()
        {
            base.HierarchicalFixedUpdate();
            SentExternalEventMessage(HIERARCHICAL_FIXED_UPDATE_EVENT);
        }

        private JSONObject _hierarchicalBeginCameraRenderingarameters;
        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                if (_hierarchicalBeginCameraRenderingarameters == null)
                    _hierarchicalBeginCameraRenderingarameters = new JSONObject();
                _hierarchicalBeginCameraRenderingarameters[CAMERA] = camera.id.ToString();
                SentExternalEventMessage(HIERARCHICAL_BEGIN_CAMERA_RENDERING_EVENT, _hierarchicalBeginCameraRenderingarameters);
                return true;
            }
            return false;
        }

        private JSONObject _hierarchicalEndCameraRenderingarameters;
        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                if (_hierarchicalEndCameraRenderingarameters == null)
                    _hierarchicalEndCameraRenderingarameters = new JSONObject();
                _hierarchicalEndCameraRenderingarameters[CAMERA] = camera.id.ToString();
                SentExternalEventMessage(HIERARCHICAL_END_CAMERA_RENDERING_EVENT, _hierarchicalEndCameraRenderingarameters);
                return true;
            }
            return false;
        }

        public void ReceiveExternalMessage(string jsonStr)
        {
            JSONArray results = null;
            JSONNode operations = JSON.Parse(jsonStr);
            if (operations is JSONArray && operations.Count > 0)
            {
                foreach (JSONNode operation in operations.AsArray)
                {
                    JSONNode operationResult = ProcessOperation(operation);
                    if (operationResult != null)
                    {
                        if (results == null)
                            results = new JSONArray();
                        results.Add(operationResult);
                    }
                }
            }
            if (results != null)
                SendExternalMessage(results);
        }

        private JSONNode ProcessOperation(JSONNode operation)
        {
            JSONNode result = null;
            IJson iJson;
            JSONNode jsonParameters;
            string operationType = operation[OPERATION_TYPE];
            switch (operationType)
            {
                case INIT_OPERATION:
                    _instanceId = NewGuid();
                    result = new JSONString(_instanceId);
                    break;
                case SET_OPERATION:
                    JSONNode jsonValues = operation[VALUES];
                    iJson = GetIJSONFromId(jsonValues[ID]);
                    if (!Disposable.IsDisposed(iJson))
                    {
                        jsonValues.Remove(ID);
                        iJson.SetJson(jsonValues);
                        result = iJson.id.ToString();
                    }
                    else
                        result = ID_NOT_FOUND_ERROR_MSG;
                    break;
                case GET_OPERATION:
                    JSONNode operationFields = operation[FIELDS];
                    if (operation[GET_ID] != null)
                    {
                        string id = operation[GET_ID];
                        iJson = GetIJSONFromId(id);
                        if (!Disposable.IsDisposed(iJson))
                            result = iJson.GetJson(null, operationFields);
                        else
                            result = ID_NOT_FOUND_ERROR_MSG;
                    }
                    else if (operation[GET_NAME] != null)
                    {
                        string name = operation[GET_NAME];
                        GameObject go = GameObject.Find(name);
                        if (go != null)
                        {
                            iJson = go.GetComponent<Object>();
                            if (!Disposable.IsDisposed(iJson))
                                result = iJson.GetJson(null, operationFields);
                            else
                                result = NAME_NOT_FOUND_ERROR_MSG;
                        }
                        else
                            result = NAME_NOT_FOUND_ERROR_MSG;
                    }
                    else if (operation[GET_TYPE] != null)
                    {
                        Type type = Type.GetType(operation[GET_TYPE]);
                        if (type != null)
                        {
                            if (operation[OBJECT_ID] != null)
                            {
                                iJson = GetIJSONFromId(operation[OBJECT_ID]);
                                if (!Disposable.IsDisposed(iJson))
                                {
                                    if (iJson is MonoBehaviour)
                                    {
                                        MonoBehaviour iJsonMonoBehaviour = iJson as MonoBehaviour;
                                        if (result == null)
                                            result = new JSONArray();
                                        Component[] components;
                                        if (operation[FIND_IN_CHILDREN] != null && operation[FIND_IN_CHILDREN].AsBool)
                                            components = iJsonMonoBehaviour.GetComponentsInChildren(type);
                                        else
                                            components = iJsonMonoBehaviour.GetComponents(type);
                                        foreach (Component component in components)
                                        {
                                            if (component is IJson)
                                                result.Add((component as IJson).GetJson(null, operationFields));
                                        }
                                    }
                                }
                                else
                                    result = ID_NOT_FOUND_ERROR_MSG;
                            }
                            else
                            {
                                if (typeof(IJson).IsAssignableFrom(type))
                                {
                                    instanceManager.IterateOverInstances(type,
                                    (iProperty) =>
                                    {
                                        IJson iJson = (IJson)iProperty;
                                        if (!Disposable.IsDisposed(iJson))
                                        {
                                            bool validIJson = true;
#if UNITY_EDITOR
                                                validIJson = !SceneManager.IsEditorNamespace(iJson.GetType());
#endif
                                                if (validIJson)
                                            {
                                                if (result == null)
                                                    result = new JSONArray();
                                                result.Add(iJson.GetJson(null, operationFields));
                                            }
                                        }
                                        return true;
                                    });
                                }
                            }
                            if (result == null)
                                result = NOTHING_FOUND_ERROR_MSG;
                        }
                        else
                            result = INVALID_TYPE_ERROR_MSG;
                    }
                    break;
                case REFLECTION_OPERATION:
                    try
                    {
                        Type type = null;
                        iJson = GetIJSONFromId(operation[ID]);
                        if (!Disposable.IsDisposed(iJson))
                            type = iJson.GetType();
                        else
                            JsonUtility.FromJson(out type, operation[TYPE]);
                        if (type != null)
                        {
                            object reflectionResult = null;
                            string reflectionName = operation[NAME];
                            string reflectionType = operation[REFLECTION_TYPE];
                            switch (reflectionType)
                            {
                                case METHOD:
                                    object[] parameters = null;
                                    Type[] parameterTypes = null;
                                    jsonParameters = operation[PARAMETERS];
                                    if (jsonParameters != null && jsonParameters.IsArray)
                                    {
                                        JSONArray jsonParametersArr = jsonParameters.AsArray;
                                        parameters = new object[jsonParametersArr.Count];
                                        parameterTypes = new Type[jsonParametersArr.Count];
                                        for (int i = 0; i < jsonParametersArr.Count; i++)
                                        {
                                            JSONNode parameter = jsonParametersArr[i];
                                            if (JsonUtility.FromJson(out Type parameterType, parameter[TYPE]))
                                            {
                                                parameterTypes[i] = parameterType;
                                                JsonUtility.FromJson(out object parameterValue, parameter[VALUE], parameterType);
                                                parameters[i] = parameterValue;
                                            }
                                        }
                                    }
                                    else
                                        parameterTypes = new Type[0];
                                    MethodInfo methodInfo = type.GetMethod(reflectionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null);
                                    if (methodInfo != null)
                                        reflectionResult = methodInfo.Invoke(iJson, parameters);
                                    else
                                        result = METHOD_NOT_FOUND_ERROR_MSG;
                                    break;
                                case PROPERTY:
                                    PropertyInfo propertyInfo = type.GetProperty(reflectionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                                    if (propertyInfo != null)
                                        reflectionResult = propertyInfo.GetValue(iJson);
                                    else
                                        result = PROPERTY_NOT_FOUND_ERROR_MSG;
                                    break;
                                case FIELD:
                                    FieldInfo fieldInfo = type.GetField(reflectionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                                    if (fieldInfo != null)
                                        reflectionResult = fieldInfo.GetValue(iJson);
                                    else
                                        result = FIELD_NOT_FOUND_ERROR_MSG;
                                    break;
                            }
                            if (reflectionResult != null)
                            {
                                if (reflectionResult is IJson)
                                    result = (reflectionResult as IJson).GetJson();
                                else
                                {
                                    if (reflectionResult is JSONNode)
                                        result = reflectionResult as JSONNode;
                                    else
                                        result = JsonUtility.ToJson(reflectionResult);
                                }
                            }
                        }
                        else
                            result = MISSING_TYPE_ERROR_MSG;
                    }
                    catch (Exception e)
                    {
                        result = e.Message;
                    }
                    break;
                case CREATE_OPERATION:
                    jsonParameters = operation[PARAMETERS];
                    JsonUtility.FromJson(out Type instanceType, jsonParameters[TYPE]);
                    if (instanceType != null)
                    {
                        iJson = GetIJSONFromId(jsonParameters[ID]);
                        if (Disposable.IsDisposed(iJson))
                        {
                            if (!string.IsNullOrEmpty(operation[OBJECT_ID]))
                            {
                                Object objectBase = GetIJSONFromId(operation[OBJECT_ID]) as Object;
                                if (objectBase != Disposable.NULL)
                                    iJson = objectBase.CreateScript(instanceType, jsonParameters);
                            }
                            else
                                iJson = instanceManager.CreateInstance(instanceType, json: jsonParameters) as IJson;
                            if (!Disposable.IsDisposed(iJson))
                                result = iJson.GetJson();
                            else
                                result = CREATION_FAILED_ERROR_MSG;
                        }
                        else
                            result = ID_ALREADY_EXIST_ERROR_MSG;
                    }
                    else
                        result = INVALID_TYPE_ERROR_MSG;
                    break;
                case DISPOSE_OPERATION:
                    object obj = GetIJSONFromId(operation[ID]);
                    if (obj is MonoBehaviour && operation[GAME_OBJECT] != null && operation[GAME_OBJECT].AsBool)
                        obj = (obj as MonoBehaviour).gameObject;
                    DisposeManager.Dispose(obj);
                    result = obj is not null ? operation[ID] : ID_NOT_FOUND_ERROR_MSG;
                    break;
            }
            return result;
        }

        private void SentExternalEventMessage(string type, JSONNode parameters = null)
        {
            JSONNode json = parameters;

            if (json == null)
                json = new JSONObject();

            json[EVENT_TYPE] = type;

            SendExternalMessage(json);
        }

        [DllImport("__Internal")]
        private static extern void SendExternalMessageInternal(string instanceId, string parameters);

        private void SendExternalMessage(JSONNode json)
        {
#if UNITY_WEBGL
            if (isActiveAndEnabled && Application.isPlaying && !string.IsNullOrEmpty(_instanceId))
            {
                JsonUtility.FromJson(out string jsonStr, json);
                SendExternalMessageInternal(_instanceId, jsonStr);
            }
#endif
        }
    }
}