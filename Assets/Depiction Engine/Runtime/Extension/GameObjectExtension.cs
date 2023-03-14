// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public static class GameObjectExtension
    {
        public static T AddSafeComponent<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return (T)go.AddSafeComponent(typeof(T), initializingContext, json, propertyModifiers);
        }

        public static Component AddSafeComponent(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            Component component = null;

            InstanceManager.InitializingContext(() => 
            {
#if UNITY_EDITOR
                component = Editor.UndoManager.AddComponent(go, type, initializingContext);
#else
                component = go.AddComponent(type);
#endif
            }, initializingContext, json, propertyModifiers, isFallbackValues);

            //The Null check is because Unity sometimes prevent component creation if a component of similar type already exists
            if (component is MonoBehaviourDisposable && !DisposeManager.IsNullOrDisposing(component))
            {
                MonoBehaviourDisposable monoBehaviourDisposable = component as MonoBehaviourDisposable;
                TransformExtension.InitializeComponent(monoBehaviourDisposable, initializingContext, json, propertyModifiers, isFallbackValues);
                monoBehaviourDisposable.ExplicitOnEnable();
            }

            return component;
        }

        public static T GetSafeComponent<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return (T)go.transform.GetSafeComponent(typeof(T), initializingContext, json, propertyModifiers);
        }

        public static Component GetSafeComponent(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return go.transform.GetSafeComponent(type, initializingContext, json, propertyModifiers, isFallbackValues);
        }

        public static T GetSafeComponentInParent<T>(this GameObject go, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return (T)go.transform.GetSafeComponentInParent(typeof(T), includeInactive, initializingContext, json, propertyModifiers);
        }

        public static Component GetSafeComponentInParent(this GameObject go, Type type, bool includeInactive, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            return go.transform.GetSafeComponentInParent(type, includeInactive, initializingContext, json, propertyModifiers);
        }

        public static List<T> GetSafeComponents<T>(this GameObject go, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return go.transform.GetSafeComponents<T>(initializingContext, json, propertyModifiers);
        }

        public static List<Component> GetSafeComponents(this GameObject go, Type type, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            return go.transform.GetSafeComponents(type, initializingContext, json, propertyModifiers);
        }

        public static List<T> GetSafeComponentsInChildren<T>(this GameObject go, bool includeSibling = true, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return go.transform.GetSafeComponentsInChildren<T>(includeSibling, initializingContext, json, propertyModifiers);
        }

        public static List<Component> GetSafeComponentsInChildren(this GameObject go, Type type, bool includeSibling = false, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            return go.transform.GetSafeComponentsInChildren(type, includeSibling, initializingContext, json, propertyModifiers);
        }

        public static IDisposable GetDisposableInComponents(this GameObject go)
        {
            return GetDisposableInComponents(go, go.GetComponents<Component>());
        }

        public static IDisposable GetDisposableInComponents(this GameObject _, Component[] components)
        {
            foreach (Component component in components)
            {
                if (component is Object || component is Visual)
                    return component as IDisposable;
            }
            return null;
        }

        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            if (go.layer != layer)
                go.layer = layer;

            for (int i = 0, count = go.transform.childCount; i < count; i++)
                go.transform.GetChild(i).gameObject.SetLayerRecursively(layer);
        }
    }
}
