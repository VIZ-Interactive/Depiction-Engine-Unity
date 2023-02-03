// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace DepictionEngine
{
    public class PropertyScriptableObject : MultithreadSafeScriptableObject, IProperty
    {
        [SerializeField, HideInInspector]
        private SerializableGuid _id;

        private List<PropertyModifier> _initializationPropertyModifiers;

        [NonSerialized]
        private bool _initializeLastFields;

        private Action<IProperty, string, object, object> _propertyAssignedEvent;
        
        public override void Recycle()
        {
            base.Recycle();

            ResetId();
        }

        /// <summary>
        /// The Cass type.
        /// </summary>
        [Json]
        public Type type
        {
            get { return GetType(); }
        }

        protected override void Initializing()
        {
            base.Initializing();
    
            _initializationPropertyModifiers = InstanceManager.initializePropertyModifiers;
        }

        protected override void InitializeUID(InstanceManager.InitializationContext initializationState)
        {
            base.InitializeUID(initializationState);
          
           id = GetId(id, initializationState);
        }

        protected virtual SerializableGuid GetId(SerializableGuid id, InstanceManager.InitializationContext initializationState)
        {
            if (id == SerializableGuid.Empty || initializationState == InstanceManager.InitializationContext.Editor_Duplicate || initializationState == InstanceManager.InitializationContext.Programmatically_Duplicate)
                return SerializableGuid.NewGuid();
            else
                return id;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            InitializeLastFields();
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializationState)
        {
            if (base.Initialize(initializationState))
            {
                if (_initializationPropertyModifiers != null)
                {
                    foreach (PropertyModifier propertyModifier in _initializationPropertyModifiers)
                        propertyModifier.ModifyProperties(this);
                    _initializationPropertyModifiers = null;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Should this object be added to the <see cref="InstanceManager"/>.
        /// </summary>
        /// <returns>True of the object should be added.</returns>
        protected virtual bool AddInstanceToManager()
        {
            return false;
        }

        protected override bool AddToInstanceManager()
        {
            if (base.AddToInstanceManager())
                return AddInstanceToManager() ? instanceManager.Add(this) : true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool SetValue<T>(string name, T value, ref T valueField, Action<T, T> assignedCallback = null)
        {
            T oldValue = valueField;

            if (base.SetValue(name, value, ref valueField, assignedCallback))
            {
                PropertyAssigned(this, name, value, oldValue);

                return true;
            }
            return false;
        }

        public Action<IProperty, string, object, object> PropertyAssignedEvent
        {
            get { return _propertyAssignedEvent; }
            set { _propertyAssignedEvent = value; }
        }

        public void ResetId()
        {
            _id = SerializableGuid.Empty;
        }

        /// <summary>
        /// Global unique identifier.
        /// </summary>
        [Json(conditionalMethod: nameof(IsNotFallbackValues))]
        public SerializableGuid id
        {
            get { return _id; }
            set { _id = value; }
        }

        public virtual bool IsDynamicProperty(int key)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PropertyAssigned<T>(IProperty property, string name, T newValue, T oldValue)
        {
            if (IsUserChangeContext() && property is IJson)
            {
                IJson iJson = property as IJson;
                if (iJson.GetJsonAttribute(name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo))
                    UserPropertyAssigned(iJson, name, jsonAttribute, propertyInfo);
            }

            if (PropertyAssignedEvent != null)
                PropertyAssignedEvent(property, name, newValue, oldValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void UserPropertyAssigned(IJson iJson, string name, JsonAttribute jsonAttribute, PropertyInfo propertyInfo)
        {
#if UNITY_EDITOR
            if (!iJson.IsDynamicProperty(PropertyMonoBehaviour.GetPropertyKey(name)))
                EditorUndoRedoDetected();
#endif
        }

        protected virtual bool InitializeLastFields()
        {
            if (!_initializeLastFields)
            {
                _initializeLastFields = true;

                return true;
            }
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            {
                if (initialized && AddInstanceToManager())
                {
                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != Disposable.NULL)
                    {
                        if (!instanceManager.Remove(id, this) && !SceneManager.IsSceneBeingDestroyed())
                            Debug.LogError(GetType() + " '" + this + "' not properly removed From InstanceManager!");
                    }
                }

                _propertyAssignedEvent = null;

                return true;
            }
            return false;
        }
    }
}
