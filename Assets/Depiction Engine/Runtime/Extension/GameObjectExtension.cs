// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public static class GameObjectExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddSafeComponent<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false) where T : Component
        {
            T component;

            InstanceManager.preventAutoInitialize = true;
            component = go.AddComponent<T>();
            InstanceManager.preventAutoInitialize = false;

            return ComponentAdded(component, initializingContext , json, propertyModifiers, isFallbackValues, initialize, registerCreatedUndo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component AddSafeComponent(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false)
        {
            Component component;

            InstanceManager.preventAutoInitialize = true;
            component = go.AddComponent(type);
            InstanceManager.preventAutoInitialize = false;

            return ComponentAdded(component, initializingContext, json, propertyModifiers, isFallbackValues, initialize, registerCreatedUndo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        private static T ComponentAdded<T>(T component, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false, bool initialize = true, bool registerCreatedUndo = false) where T : Component
        {
#if UNITY_EDITOR
            if (registerCreatedUndo)
                Editor.UndoManager.RegisterCreatedObjectUndo(component, initializingContext);
#endif

            return initialize ? Initialize(component, initializingContext, json, propertyModifiers, isFallbackValues) : component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSafeComponent<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return Initialize(go.GetComponent<T>(), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component GetSafeComponent(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return Initialize(go.GetComponent(type), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSafeComponentInParent<T>(this GameObject go, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return Initialize(go.GetComponentInParent<T>(includeInactive), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component GetSafeComponentInParent(this GameObject go, Type type, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return Initialize(go.GetComponentInParent(type, includeInactive), initializingContext, json, propertyModifiers, isFallbackValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        private static T Initialize<T>(T component, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false) where T : Component
        {
            return InstanceManager.Initialize(component, initializingContext, json, propertyModifiers, isFallbackValues);
        }

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
