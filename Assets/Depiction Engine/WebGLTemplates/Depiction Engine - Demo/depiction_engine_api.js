// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

const $OPERATION_TYPE_NAME = "operationType";
const $NAMESPACE = "Depiction Engine";
const $MAPBOX_KEY = "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA";

var $instances = {};

function receiveMessage(instanceId, parameters)
{
    var instance = $instances[instanceId]

    if (instance == null)
    {
        for (var i in $instances)
        {
            instance = $instances[i];
            if (i < 0 && instance.unityInstance != null)
            {
                delete $instances[i];
                $instances[instanceId] = instance;
                instance.initialize(instanceId);
                return;
            }
        }
    }

    instance.receiveMessage(JSON.parse(parameters));
}

class JsonInterface
{
    static createInstance(component, config)
    {
        return new Instance(component, config);
    }
}

class Instance
{
    constructor(element, config)
    {
        this.ready = false;

        this.parentElement = element;
        this.parentElement.className = "unity-container";

        this.canvas = document.createElement("canvas");
        this.canvas.className = "unity-canvas";

        this.canvas.id = this.parentElement.id + "_canvas";
        this.parentElement.prepend(this.canvas);

        var index = -1;
        while ($instances[index] != null)
            index--;
        $instances[index] = this;

        createUnityInstance(this.canvas, config).then((unityInstance) =>
        {
            this.unityInstance = unityInstance;

            var initializeOperation = {};
            initializeOperation[$OPERATION_TYPE_NAME] = "init";
            this.sendMessage(initializeOperation);

        }).catch((message) => {
            alert(message);
        });
    }

    initialize(id)
    {
        this.id = id;

        var managers = this.getType($NAMESPACE + ".ManagerBase", { id: true, type: true })[0];
        for (var i in managers) {
            var manager = managers[i];
            switch (manager.type) {
                case $NAMESPACE + ".SceneManager":
                    this.sceneManager = manager;
                    break;
                case $NAMESPACE + ".TweenManager":
                    this.tweenManager = manager;
                    break;
                case $NAMESPACE + ".PoolManager":
                    this.poolManager = manager;
                    break;
                case $NAMESPACE + ".InputManager":
                    this.inputManager = manager;
                    break;
                case $NAMESPACE + ".CameraManager":
                    this.cameraManager = manager;
                    break;
                case $NAMESPACE + ".RenderingManager":
                    this.renderingManager = manager;
                    break;
                case $NAMESPACE + ".DatasourceManager":
                    this.datasourceManager = manager;
                    break;
                case $NAMESPACE + ".JsonInterface":
                    this.jsonInterface = manager;
                    break;
                case $NAMESPACE + ".InstanceManager":
                    this.instanceManager = manager;
                    break;
            }
        }

        this.eventReceived("initialized");
    }

    get mainCamera()
    {
        var mainCameraResult = this.getStaticPropertyValue($NAMESPACE +".Camera", "main");
        return mainCameraResult != null && mainCameraResult.length > 0 ? mainCameraResult[0] : null;
    }

    getTotalLoadingCount()
    {
        return this.callStaticMethod($NAMESPACE + ".LoaderBase", "GetTotalLoadingCount")[0];
    }

    getTotalLoadedCount() {
        return this.callStaticMethod($NAMESPACE + ".LoaderBase", "GetTotalLoadedCount")[0];
    }

    getStar()
    {
        var star = this.callInstanceMethod(this.instanceManager.id, "GetStar");
        return star != null && star.length > 0 ? star[0] : null;
    }

    starExists()
    {
        return this.callInstanceMethod(this.instanceManager.id, "StarExists")[0];
    }

    worldToViewportPoint(cameraId, point)
    {
        if (cameraId != null)
            return this.callInstanceMethod(cameraId, "WorldToViewportPoint", [{ type: $NAMESPACE +".Vector3Double", value: point }])[0];
        return null;
    }

    getRestDatasource(baseAddress, baseAddress2, baseAddress3, baseAddress4)
    {
        return this.callStaticMethod($NAMESPACE + ".DatasourceManager", "GetRestDatasource", [{ type: "System.String", value: baseAddress }, { type: "System.String", value: baseAddress2 }, { type: "System.String", value: baseAddress3 }, { type: "System.String", value: baseAddress4 }, { type: $NAMESPACE + ".InitializationState", value: "Programmatically"}])[0];
    }

    getFileSystemDatasource(baseAddress, baseAddress2, baseAddress3, baseAddress4)
    {
        return this.callStaticMethod($NAMESPACE + ".DatasourceManager", "GetFileSystemDatasource", [{ type: "System.String", value: baseAddress }, { type: "System.String", value: baseAddress2 }, { type: "System.String", value: baseAddress3 }, { type: "System.String", value: baseAddress4 }, { type: $NAMESPACE + ".InitializationState", value: "Programmatically" }])[0];
    }

    bindElementToTransformPosition(element, transform)
    {
        if (transform == null)
            return;

        if (typeof transform === 'object' && transform.id != null)
            transform = transform.id;

        if (this.bindedElements == null)
            this.bindedElements = [];

        this.bindedElements.push({ element: element, transform: transform });
    }

    unbindElementToTransformPosition(element)
    {
        if (this.bindedElements != null)
        {
            for (var i in this.bindedElements)
            {
                if (this.bindedElements[i].element == element)
                {
                    this.bindedElements.splice(i, 1);
                    break;
                }
            }
        }
    }

    //Create
    newGuid()
    {
        return this.callStaticMethod($NAMESPACE +".JsonInterface", "NewGuid")[0];
    }

    //Get Operations
    getType(type, fields) { return this.sendMessage(this.getTypeOperation(type, null, fields, null)); }
    getObjectType(objectId, type, fields, findInChildren) { return this.sendMessage(this.getTypeOperation(type, objectId, fields, findInChildren)); }
    getTypeOperation(type, objectId, fields, findInChildren)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "get";
        operation.getType = type;
        if (objectId != null)
            operation.objectId = objectId;
        operation.fields = fields;
        if (findInChildren)
            operation.findInChildren = findInChildren;

        return operation;
    }

    getId(id, fields) { return this.sendMessage(this.getIdOperation(id, fields)); }
    getIdOperation(id, fields)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "get";
        operation.getId = id;
        operation.fields = fields;

        return operation;
    }

    getName(name, fields) { return this.sendMessage(this.getNameOperation(name, fields)); }
    getNameOperation(name, fields)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "get";
        operation.getName = name;
        operation.fields = fields;

        return operation;
    }

    //Set Operations
    set(values) { return this.sendMessage(this.setOperation(values)); }
    setOperation(values)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "set";
        operation.values = values;
   
        return operation;
    }

    //Reflection Operations
    //Parameters are a Type / Value pair. The Type should include the assembly for UnityEngine Types in the following format: 'UnityEngine.Vector3, UnityEngine' 
    //Other Types: System.Int32, System.Double, System.Single
    callInstanceMethod(id, name, parameters) { return this.sendMessage(this.methodOperation(id, null, name, parameters)); }
    callStaticMethod(type, name, parameters) { return this.sendMessage(this.methodOperation(null, type, name, parameters)); }
    methodOperation(id, type, name, parameters)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "reflection";
        operation.reflectionType = "method";
        operation.id = id;
        operation.type = type;
        operation.name = name;
        operation.parameters = parameters;

        return operation;
    }

    getInstancePropertyValue(id, name) { return this.sendMessage(this.getPropertyOperation(id, null, name)); }
    getStaticPropertyValue(type, name) { return this.sendMessage(this.getPropertyOperation(null, type, name)); }
    getPropertyOperation(id, type, name)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "reflection";
        operation.reflectionType = "property";
        operation.id = id;
        operation.type = type;
        operation.name = name;

        return operation;
    }

    getInstanceFieldValue(id, name) { return this.sendMessage(this.getFieldOperation(id, null, name)); }
    getStaticFieldValue(type, name) { return this.sendMessage(this.getFieldOperation(null, type, name)); }
    getFieldOperation(id, type, name)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "reflection";
        operation.reflectionType = "field";
        operation.id = id;
        operation.type = type;
        operation.name = name;

        return operation;
    }

    //Create Operations
    create(parameters, objectId) { return this.sendMessage(this.createOperation(parameters, objectId)); }
    createOperation(parameters, objectId)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "create";
        operation.objectId = objectId;
        operation.parameters = parameters;

        return operation;
    }

    //Dispose Operations
    dispose(id, gameObject = true) { return this.sendMessage(this.disposeOperation(id, gameObject)); }
    disposeOperation(id, gameObject)
    {
        var operation = {};

        operation[$OPERATION_TYPE_NAME] = "dispose";
        operation.id = id;
        operation.gameObject = gameObject;

        return operation;
    }
    
    sendMessage(operations)
    {
        if (this.unityInstance != null)
        {
            this.returnResults = null;
            if (!Array.isArray(operations))
                operations = [operations];
            this.unityInstance.SendMessage("Managers (Required)", "ReceiveExternalMessage", JSON.stringify(operations));
            return this.returnResults;
        }
        return "Not Initialized";
    }

    receiveMessage(parameters)
    {
        var eventType = parameters.eventType;
        if (eventType != null) {
            delete parameters.eventType;
            this.eventReceived(eventType, parameters);
        }
        else
            this.returnResults = parameters;
    }

    eventReceived(type, parameters)
    {
        this.parentElement.dispatchEvent(new CustomEvent(type, parameters != null ? { detail: parameters } : null));

        if (type == "hierarchicalBeginCameraRendering")
        {
            if (this.bindedElements != null)
            {
                for (var i in this.bindedElements)
                {
                    let bindedElement = this.bindedElements[i];
                    bindedElement.viewportPoint = this.worldToViewportPoint(this.mainCamera.id, this.getInstancePropertyValue(bindedElement.transform, "position")[0]);
                }

                this.bindedElements.sort((a, b) => { return b.viewportPoint.z - a.viewportPoint.z });

                for (var i in this.bindedElements)
                {
                    let bindedElement = this.bindedElements[i];

                    let element = bindedElement.element;
                    let viewportPoint = bindedElement.viewportPoint;

                    element.style.position = "absolute";
                    element.style.display = viewportPoint.z > 0.0 ? "block" : "none";
                    element.style.left = viewportPoint.x * this.canvas.offsetWidth + "px";
                    element.style.bottom = viewportPoint.y * this.canvas.offsetHeight + "px";
                    element.style.zIndex = i;
                }
            }
        }
    }
}