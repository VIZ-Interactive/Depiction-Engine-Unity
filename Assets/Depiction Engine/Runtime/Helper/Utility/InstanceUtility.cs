﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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
        /// Create a new <see cref="Camera"/> equipped with a <see cref="CameraController"/> and target.
        /// </summary>
        /// <param name="parent">The parent <see cref="Transform"/> under which we will create the <see cref="Camera"/>.</param>
        /// <param name="initializationState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns>The newly created <see cref="Camera"/> instance.</returns>
        public static Camera CreateTargetCamera(Transform parent, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false)
        {
            Camera camera = null;

            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                Object target = instanceManager.CreateInstance<VisualObject>(parent, json: "Target", initializingState: initializationState, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
                target.CreateScript<GeoCoordinateController>(initializationState);
                target.CreateScript<TransformAnimator>(initializationState);

                camera = instanceManager.CreateInstance<Camera>(parent, json: "Camera", initializingState: initializationState);

                CameraController cameraController = camera.CreateScript<CameraController>(initializationState);
                cameraController.targetId = target.id;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(cameraController, initializationState);
#endif

                camera.CreateScript<TargetControllerAnimator>(initializationState);
            }

            return camera;
        }

        /// <summary>
        /// Create a new planet.
        /// </summary>
        /// <param name="parent">The parent <see cref="Transform"/> under which we will create the <see cref="Camera"/>.</param>
        /// <param name="name">The name of the planet.</param>
        /// <param name="spherical">Display as a sphere (true) or flat (false)?</param>
        /// <param name="size">The size (radius in spherical mode or width in flat mode), in world units.</param>
        /// <param name="mass">Used to determine the amount of gravitational force to apply when <see cref="Object.useGravity"/> is enabled.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns>The newly created <see cref="Planet"/> instance.</returns>
        public static Planet CreatePlanet(
            Transform parent,
            string name,
            bool spherical,
            double size,
            double mass,
            JSONNode json = null,
            InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically,
            bool setParentAndAlign = false,
            bool moveToView = false)
        {
            if (json == null)
                json = new JSONObject();
            json[nameof(Object.name)] = name;

            Planet planet = InstanceManager.Instance().CreateInstance<Planet>(parent, json: json, initializingState: initializingState, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
            planet.SetSpherical(spherical, false);
            if (spherical)
                planet.transform.localRotation = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);

#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet.transform, initializingState);
#endif

            planet.size = size;
            planet.mass = mass;
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet, initializingState);
#endif

            return planet;
        }

        /// <summary>
        /// Create a new <see cref="DatasourceRoot"/>.
        /// </summary>
        /// <param name="planet">The parent <see cref="Planet"/> under which we will create the <see cref="DatasourceRoot"/>.</param>
        /// <param name="name">The name of the layer.</param>
        /// <param name="json">Optional initialization values.</param>
        /// <param name="initializingState">.</param>
        /// <returns>The newly created <see cref="DatasourceRoot"/> instance.</returns>
        public static DatasourceRoot CreateLayer(Planet planet, string name, JSONNode json = null, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically)
        {
            if (json == null)
                json = new JSONObject();
            json[nameof(DatasourceRoot.name)] = name;

            return InstanceManager.Instance().CreateInstance<DatasourceRoot>(planet.gameObject.transform, json: json, initializingState: initializingState);
        }

        private static List<Type> _requiredComponentTypes;
        /// <summary>
        /// Creates a <see cref="JSONArray"/> containing the initialization values for a <see cref="LoaderBase"/>, <see cref="FallbackValues"/> and coresponding <see cref="AssetReference"/> that can be passed on to the json parameter of <see cref="InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="loaderType">The <see cref="LoaderBase"/> type.</param>
        /// <param name="fallbackType">The <see cref="FallbackValues"/> type.</param>
        /// <returns>The newly created <see cref="JSONArray"/> containing the components initialization values.</returns>
        public static JSONArray GetLoaderJson(Type loaderType, Type fallbackType)
        {
            JSONArray components = new JSONArray();

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
                if (_requiredComponentTypes == null)
                    _requiredComponentTypes = new List<Type>();
                MemberUtility.GetRequiredComponentTypes(ref _requiredComponentTypes, fallbackType);

                List<SerializableGuid> assetReferencesFallbackValuesId = new List<SerializableGuid>();

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
        /// Creates a <see cref="JSONObject"/> of specified type so it can be passed on to the json parameter of <see cref="InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="type">The component type.</param>
        /// <param name="id">An Optional component id. If ommited a new guid will be generated.</param>
        /// <returns>The newly create <see cref="JSONObject"/> containing the component initialization values.</returns>
        public static JSONObject GetComponentJson(Type type, SerializableGuid id = default(SerializableGuid))
        {
            JSONObject json = new JSONObject();
            json[nameof(Object.type)] = JsonUtility.ToJson(type);
            if (id == SerializableGuid.Empty)
                id = SerializableGuid.NewGuid();
            json[nameof(Object.id)] = JsonUtility.ToJson(id);
            return json;
        }

        /// <summary>
        /// Merge the componentsJson into the objectInitializationJson so it can be passed on to the json parameter of <see cref="InstanceManager.Initialize"/> or some other instancing derivative methods.
        /// </summary>
        /// <param name="componentsJson">A <see cref="JSONObject"/> or <see cref="JSONArray"/> composed of component(s) initialization values.</param>
        /// <param name="objectInitializationJson">The json to merge the component(s) values into.</param>
        /// <returns></returns>
        public static void MergeComponentsToObjectInitializationJson(JSONNode componentsJson, JSONObject objectInitializationJson)
        {
            if (componentsJson.IsArray)
            {
                foreach (JSONObject componentJson in componentsJson.AsArray)
                    MergeComponentToObjectInitializationJson(componentJson, objectInitializationJson);
            }
            else if (componentsJson.IsObject)
                MergeComponentToObjectInitializationJson(componentsJson.AsObject, objectInitializationJson);
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
