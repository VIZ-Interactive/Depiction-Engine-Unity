// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the creation of instances.
    /// </summary>
    public static class InstanceUtility
    {
        /// <summary>
        /// Create a new <see cref="DepictionEngine.Camera"/> equipped with a <see cref="DepictionEngine.CameraController"/> and target.
        /// </summary>
        /// <param name="parent">The parent <see cref="UnityEngine.Transform"/> under which we will create the <see cref="DepictionEngine.Camera"/>.</param>
        /// <param name="initializingContext"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns>The newly created <see cref="DepictionEngine.Camera"/> instance.</returns>
        public static Camera CreateTargetCamera(Transform parent, InitializationContext initializingContext = InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false)
        {
            Camera camera = null;

            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                Object target = instanceManager.CreateInstance<VisualObject>(parent, json: "Target", initializingContext: initializingContext, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
                target.CreateScript<GeoCoordinateController>(initializingContext);
                target.CreateScript<TransformAnimator>(initializingContext);

                camera = instanceManager.CreateInstance<Camera>(parent, json: "Camera", initializingContext: initializingContext);

                CameraController cameraController = camera.CreateScript<CameraController>(initializingContext);
                cameraController.targetId = target.id;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(cameraController, initializingContext);
#endif

                camera.CreateScript<TargetControllerAnimator>(initializingContext);
            }

            return camera;
        }

        /// <summary>
        /// Create a new planet.
        /// </summary>
        /// <param name="parentId">The id of the parent <see cref="UnityEngine.Transform"/> under which we will create the <see cref="DepictionEngine.Planet"/>.</param>
        /// <param name="name">The name of the planet.</param>
        /// <param name="spherical">Display as a sphere (true) or flat (false)?</param>
        /// <param name="size">The size (radius in spherical mode or width in flat mode), in world units.</param>
        /// <param name="mass">Used to determine the amount of gravitational force to apply when <see cref="DepictionEngine.Object.useGravity"/> is enabled.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingContext"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns>The newly created <see cref="DepictionEngine.Planet"/> instance.</returns>
        public static Planet CreatePlanet(
            SerializableGuid parentId,
            string name,
            bool spherical,
            double size,
            double mass,
            JSONNode json = null,
            InitializationContext initializingContext = InitializationContext.Programmatically,
            bool setParentAndAlign = false,
            bool moveToView = false)
        {
            Component component = null;
            
            if (parentId != SerializableGuid.Empty)
                component = InstanceManager.Instance().GetIJson(parentId) as Component;

            return CreatePlanet(component != null ? component.transform : null, name, spherical, size, mass, json, initializingContext, setParentAndAlign, moveToView);
        }

        /// <summary>
        /// Create a new planet.
        /// </summary>
        /// <param name="parent">The parent <see cref="UnityEngine.Transform"/> under which we will create the <see cref="DepictionEngine.Planet"/>.</param>
        /// <param name="name">The name of the planet.</param>
        /// <param name="spherical">Display as a sphere (true) or flat (false)?</param>
        /// <param name="size">The size (radius in spherical mode or width in flat mode), in world units.</param>
        /// <param name="mass">Used to determine the amount of gravitational force to apply when <see cref="DepictionEngine.Object.useGravity"/> is enabled.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingContext"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns>The newly created <see cref="DepictionEngine.Planet"/> instance.</returns>
        public static Planet CreatePlanet(
        Transform parent,
        string name,
        bool spherical,
        double size,
        double mass,
        JSONNode json = null,
        InitializationContext initializingContext = InitializationContext.Programmatically,
        bool setParentAndAlign = false,
        bool moveToView = false)
        {
            if (json == null)
                json = new JSONObject();
            json[nameof(Object.name)] = name;

            Planet planet = InstanceManager.Instance().CreateInstance<Planet>(parent, json: json, initializingContext: initializingContext, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
            planet.SetSpherical(spherical, false);
            if (spherical)
                planet.transform.localRotation = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);

#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet.transform, initializingContext);
#endif

            planet.size = size;
            planet.mass = mass;

#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet, initializingContext);
#endif

            return planet;
        }

        /// <summary>
        /// Create a new <see cref="DepictionEngine.DatasourceRoot"/>.
        /// </summary>
        /// <param name="planetId">The id of the parent <see cref="DepictionEngine.Planet"/> under which we will create the <see cref="DepictionEngine.DatasourceRoot"/>.</param>
        /// <param name="name">The name of the layer.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingContext">.</param>
        /// <returns>The newly created <see cref="DepictionEngine.DatasourceRoot"/> instance.</returns>
        public static DatasourceRoot CreateLayer(SerializableGuid planetId, string name, JSONNode json = null, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            return CreateLayer(InstanceManager.Instance().GetAstroObject(planetId) as Planet, name, json, initializingContext);
        }

        /// <summary>
        /// Create a new <see cref="DepictionEngine.DatasourceRoot"/>.
        /// </summary>
        /// <param name="planet">The parent <see cref="DepictionEngine.Planet"/> under which we will create the <see cref="DepictionEngine.DatasourceRoot"/>.</param>
        /// <param name="name">The name of the layer.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingContext">.</param>
        /// <returns>The newly created <see cref="DepictionEngine.DatasourceRoot"/> instance.</returns>
        public static DatasourceRoot CreateLayer(Planet planet, string name, JSONNode json = null, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            if (planet == Disposable.NULL)
                return null;

            if (json == null)
                json = new JSONObject();
            json[nameof(DatasourceRoot.name)] = name;

            return InstanceManager.Instance().CreateInstance<DatasourceRoot>(planet.gameObject.transform, json: json, initializingContext: initializingContext);
        }

        private static List<Type> _requiredComponentTypes;
        /// <summary>
        /// Creates a <see cref="DepictionEngine.JSONArray"/> containing the initialization values for a <see cref="DepictionEngine.LoaderBase"/>, <see cref="DepictionEngine.FallbackValues"/> and coresponding <see cref="DepictionEngine.AssetReference"/> that can be passed on to the json parameter of <see cref="DepictionEngine.InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="loaderType">The <see cref="DepictionEngine.LoaderBase"/> type.</param>
        /// <param name="fallbackType">The <see cref="DepictionEngine.FallbackValues"/> type.</param>
        /// <returns>The newly created <see cref="DepictionEngine.JSONArray"/> containing the components initialization values.</returns>
        public static JSONArray GetLoaderJson(Type loaderType, Type fallbackType)
        {
            JSONArray components = new();

            SerializableGuid fallbackValuesId = SerializableGuid.NewGuid();

            JSONObject loaderJson = GetComponentJson(loaderType);
            loaderJson[nameof(LoaderBase.fallbackValuesId)] = JsonUtility.ToJson(new SerializableGuid[] { fallbackValuesId });
            components.Add(loaderJson);

            JSONObject fallbackValuesJson = GetComponentJson(typeof(FallbackValues), fallbackValuesId);
            fallbackValuesJson[nameof(FallbackValues.fallbackValuesJson)] = GetComponentJson(fallbackType);
            fallbackValuesJson[nameof(FallbackValues.fallbackValuesJson)][nameof(IPersistent.createPersistentIfMissing)] = true;
            components.Add(fallbackValuesJson);

            if (typeof(Object).IsAssignableFrom(fallbackType))
            {
                _requiredComponentTypes ??= new List<Type>();
                MemberUtility.GetRequiredComponentTypes(ref _requiredComponentTypes, fallbackType);

                List<SerializableGuid> assetReferencesFallbackValuesId = new();

                foreach (Type type in _requiredComponentTypes)
                {
                    if (type.IsAssignableFrom(typeof(AssetReference)))
                        assetReferencesFallbackValuesId.Add(SerializableGuid.NewGuid());
                }

                if (assetReferencesFallbackValuesId.Count > 0)
                {
                    fallbackValuesJson[nameof(FallbackValues.fallbackValuesJson)][nameof(Object.referencesId)] = JsonUtility.ToJson(assetReferencesFallbackValuesId);
                    fallbackValuesJson[nameof(FallbackValues.fallbackValuesJson)][nameof(Object.createReferenceIfMissing)] = true;

                    for (int i = 0; i < assetReferencesFallbackValuesId.Count; i++)
                    {
                        JSONObject assetReferenceJson = GetComponentJson(typeof(FallbackValues), assetReferencesFallbackValuesId[i]);
                        assetReferenceJson[nameof(FallbackValues.fallbackValuesJson)] = GetComponentJson(typeof(AssetReference));
                        components.Add(assetReferenceJson);
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// Creates a <see cref="DepictionEngine.JSONObject"/> of specified type so it can be passed on to the json parameter of <see cref="DepictionEngine.InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="id">An Optional component id. If ommited a new guid will be generated.</param>
        /// <returns>The newly create <see cref="DepictionEngine.JSONObject"/> containing the component initialization values.</returns>
        public static JSONObject GetComponentJson(Type type, SerializableGuid id = default)
        {
            JSONObject json = new()
            {
                [nameof(Object.type)] = JsonUtility.ToJson(type)
            };
            if (id == SerializableGuid.Empty)
                id = SerializableGuid.NewGuid();
            json[nameof(Object.id)] = JsonUtility.ToJson(id);
            return json;
        }

        /// <summary>
        /// Merge the componentsJson into the objectInitializationJson so it can be passed on to the json parameter of <see cref="DepictionEngine.InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="componentsJson">A <see cref="DepictionEngine.JSONObject"/> or <see cref="DepictionEngine.JSONArray"/> composed of component(s) initialization values.</param>
        /// <param name="objectInitializationJson">The json to merge the component(s) values into.</param>
        /// <returns>The objectInitialization json.</returns>
        public static JSONNode MergeComponentsToObjectInitializationJson(JSONNode componentsJson, JSONObject objectInitializationJson)
        {
            if (componentsJson.IsArray)
            {
                foreach (JSONObject componentJson in componentsJson.AsArray)
                    MergeComponentToObjectInitializationJson(componentJson, objectInitializationJson);
            }
            else if (componentsJson.IsObject)
                MergeComponentToObjectInitializationJson(componentsJson.AsObject, objectInitializationJson);

            return objectInitializationJson;
        }

        private static void MergeComponentToObjectInitializationJson(JSONObject componentJson, JSONObject objectInitializationJson)
        {
            Type type = Type.GetType(componentJson[nameof(Object.type)]);
            if (typeof(AnimatorBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.animator)] == null)
                    objectInitializationJson[nameof(Object.animator)] = new JSONArray();
                objectInitializationJson[nameof(Object.animator)].Add(componentJson);
            }
            else if (typeof(ControllerBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.controller)] == null)
                    objectInitializationJson[nameof(Object.controller)] = new JSONArray();
                objectInitializationJson[nameof(Object.controller)].Add(componentJson);
            }
            else if (typeof(GeneratorBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.generators)] == null)
                    objectInitializationJson[nameof(Object.generators)] = new JSONArray();
                objectInitializationJson[nameof(Object.generators)].Add(componentJson);
            }
            else if (typeof(ReferenceBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.references)] == null)
                    objectInitializationJson[nameof(Object.references)] = new JSONArray();
                objectInitializationJson[nameof(Object.references)].Add(componentJson);
            }
            else if (typeof(EffectBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.effects)] == null)
                    objectInitializationJson[nameof(Object.effects)] = new JSONArray();
                objectInitializationJson[nameof(Object.effects)].Add(componentJson);
            }
            else if (typeof(FallbackValues).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.fallbackValues)] == null)
                    objectInitializationJson[nameof(Object.fallbackValues)] = new JSONArray();
                objectInitializationJson[nameof(Object.fallbackValues)].Add(componentJson);
            }
            else if (typeof(DatasourceBase).IsAssignableFrom(type))
            {
                if (objectInitializationJson[nameof(Object.datasources)] == null)
                    objectInitializationJson[nameof(Object.datasources)] = new JSONArray();
                objectInitializationJson[nameof(Object.datasources)].Add(componentJson);
            }
        }
    }
}
