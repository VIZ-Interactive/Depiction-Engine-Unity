// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public static class TransformExtension
    {
        public static void SetPosition(this Transform transform, Vector3Double value)
        {
            transform.position += (Vector3)(value - transform.GetPosition());
        }

        public static Vector3Double GetPosition(this Transform transform)
        {
            Vector3Double position;

            TransformDouble parentTransformDouble = transform.GetComponentInParent<TransformDouble>();
            if (parentTransformDouble != Disposable.NULL)
                position = parentTransformDouble.position + (Vector3Double)(transform.position - parentTransformDouble.transform.position);
            else
                position = TransformDouble.AddOrigin(transform.position);

            return position;
        }

        public static T GetSafeComponent<T>(this Transform transform, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) where T : Component
        {
            return (T)transform.GetSafeComponent(typeof(T), initializationState);
        }

        public static Component GetSafeComponent(this Transform transform, Type type, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            return ComponentDisposeCheck(InitializeComponent(transform.GetComponent(type), initializationState, json, propertyModifiers, isFallbackValues));
        }

        public static T GetSafeComponentInParent<T>(this Transform transform, bool includeInactive, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            return (T)transform.GetSafeComponentInParent(typeof(T), includeInactive, initializationState, json, propertyModifiers);
        }

        public static Component GetSafeComponentInParent(this Transform transform, Type type, bool includeInactive, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            return ComponentDisposeCheck(InitializeComponent(transform.GetComponentInParent(type, includeInactive), initializationState, json, propertyModifiers));
        }

        public static List<T> GetSafeComponents<T>(this Transform transform, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            List<T> safeComponents = new List<T>();
            GetValidComponents(safeComponents, transform.GetComponents<T>(), initializationState, json, propertyModifiers);
            return safeComponents;
        }

        public static List<Component> GetSafeComponents(this Transform transform, Type type, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            List<Component> safeComponents = new List<Component>();
            GetValidComponents(safeComponents, transform.GetComponents(type), initializationState, json, propertyModifiers);
            return safeComponents;
        }

        public static List<T> GetSafeComponentsInChildren<T>(this Transform transform, bool includeSibling = false, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            List<T> safeComponents = new List<T>();

            if (includeSibling)
                GetValidComponents(safeComponents, transform.GetComponents<T>(), initializationState, json, propertyModifiers);

            foreach (Transform childTransform in transform)
                GetValidComponents(safeComponents, childTransform.GetComponents<T>(), initializationState, json, propertyModifiers);

            return safeComponents;
        }

        public static List<Component> GetSafeComponentsInChildren(this Transform transform, Type type, bool includeSibling = false, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null)
        {
            List<Component> safeComponents = new List<Component>();

            if (includeSibling)
                GetValidComponents(safeComponents, transform.GetComponents(type), initializationState, json, propertyModifiers);

            foreach (Transform childTransform in transform)
                GetValidComponents(safeComponents, childTransform.GetComponents(type), initializationState, json, propertyModifiers);

            return safeComponents;
        }

        private static Component ComponentDisposeCheck(Component component)
        {
            return !DisposeManager.IsNullOrDisposing(component) ? component : null;
        }

        private static void GetValidComponents<T>(List<T> validComponents, T[] components, InstanceManager.InitializationContext initializationState, JSONNode json = null, List<PropertyModifier> propertyModifiers = null) where T : Component
        {
            foreach (T component in components)
            {
                if (!DisposeManager.IsNullOrDisposing(component))
                {
                    InitializeComponent(component, initializationState, json, propertyModifiers);
                    validComponents.Add(component);
                }
            }
        }

        public static Component InitializeComponent(Component component, InstanceManager.InitializationContext initializationState, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            //Call Initialize to make sure even if the Object is disabled the script will still be initialized properly
            if (component is MonoBehaviourBase)
            {
                MonoBehaviourBase monoBehaviourBase = component as MonoBehaviourBase;
                InstanceManager.Initialize(monoBehaviourBase, initializationState, json, propertyModifiers, isFallbackValues);
            }
            return component;
        }
    }
}
