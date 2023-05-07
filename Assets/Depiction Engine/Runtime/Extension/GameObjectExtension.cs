// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public static class GameObjectExtension
    {
        /// <summary>
        /// A wrapper method to <see cref="GameObject.AddComponent{T}"/> which automatically initializes the component, if it is an <see cref="IDisposable"/>, and optionaly registers a create Undo if in Editor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <param name="initialize"></param>
        /// <param name="registerCreatedUndo"></param>
        /// <returns>The created Component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddComponentInitialized<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false) where T : Component
        {
            T component;

            InstanceManager.preventAutoInitialize = true;
            component = go.AddComponent<T>();
            InstanceManager.preventAutoInitialize = false;

            return ComponentAdded(component, initializingContext , json, propertyModifiers, isFallbackValues, initialize, registerCreatedUndo);
        }

        /// <summary>
        /// A wrapper method to <see cref="GameObject.AddComponent(Type)"/> which automatically initializes the component, if it is an <see cref="IDisposable"/>, and optionaly registers a create Undo if in Editor.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="type"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <param name="initialize"></param>
        /// <param name="registerCreatedUndo"></param>
        /// <returns>The created Component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component AddComponentInitialized(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false)
        {
            Component component;

            InstanceManager.preventAutoInitialize = true;
            component = go.AddComponent(type);
            InstanceManager.preventAutoInitialize = false;

            return ComponentAdded(component, initializingContext, json, propertyModifiers, isFallbackValues, initialize, registerCreatedUndo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        private static T ComponentAdded<T>(T component, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false) where T : Component
        {
#if UNITY_EDITOR
            if (registerCreatedUndo)
                Editor.UndoManager.RegisterCreatedObjectUndo(component, initializingContext);
#endif

            return initialize ? Initialize(component, initializingContext, json, propertyModifiers, isFallbackValues) : component;
        }

        /// <summary>
        /// A wrapper method to <see cref="GameObject.GetComponent{T}"/> which returns an initialized Component, if it is an <see cref="IDisposable"/>, of the requested type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <returns>The initialized Component, if one was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponentInitialized<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return Initialize(go.GetComponent<T>(), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        /// <summary>
        /// A wrapper method to <see cref="GameObject.GetComponent(Type)"/> which returns an initialized Component, if it is an <see cref="IDisposable"/>, of the requested type.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="type"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <returns>The initialized Component, if one was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component GetComponentInitialized(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return Initialize(go.GetComponent(type), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        /// <summary>
        /// A wrapper method to <see cref="GameObject.GetComponentInParent{T}"/> which returns an initialized Component found in a parent, if it is an <see cref="IDisposable"/>, of the requested type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="includeInactive"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <returns>The initialized Component, if one was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponentInParentInitialized<T>(this GameObject go, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return Initialize(go.GetComponentInParent<T>(includeInactive), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        /// <summary>
        /// A wrapper method to <see cref="GameObject.GetComponentInParent(Type)"/> which returns an initialized Component found in a parent, if it is an <see cref="IDisposable"/>, of the requested type.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="type"></param>
        /// <param name="includeInactive"></param>
        /// <param name="initializingContext"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues"></param>
        /// <returns>The initialized Component, if one was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component GetComponentInParentInitialized(this GameObject go, Type type, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return Initialize(go.GetComponentInParent(type, includeInactive), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        private static T Initialize<T>(T component, InitializationContext initializingContext = InitializationContext.Programmatically, JSONObject json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return InstanceManager.Initialize(component, initializingContext, json, propertyModifiers, isFallbackValues);
        }

        /// <summary>
        /// Returns an <see cref="DepictionEngine.Object"/> or <see cref="DepictionEngine.Visual"/> component found in the GameObject.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable GetObjectOrVisualComponent(this GameObject _, Component[] components)
        {
            foreach (Component component in components)
            {
                if (component is Object || component is Visual)
                    return component as IDisposable;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            if (go.layer != layer)
                go.layer = layer;

            for (int i = 0, count = go.transform.childCount; i < count; i++)
                go.transform.GetChild(i).gameObject.SetLayerRecursively(layer);
        }
    }
}
