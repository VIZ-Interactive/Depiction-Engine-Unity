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
            get => GetType();
        }

        protected override void Initializing()
        {
            base.Initializing();
    
            _initializationPropertyModifiers = InstanceManager.initializePropertyModifiers;
        }

        protected override void InitializeUID(InitializationContext initializingContext)
        {
            base.InitializeUID(initializingContext);

            SerializableGuid lastId = id;
            SerializableGuid newId = GetId(id, initializingContext);
            id = newId;
            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                InstanceManager.RegisterDuplicating(lastId, newId);
        }

        protected virtual SerializableGuid GetId(SerializableGuid id, InitializationContext initializingContext)
        {
            if (id == SerializableGuid.Empty || initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                return SerializableGuid.NewGuid();
            else
                return id;
        }

        protected override bool Initialize(InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                if (!isFallbackValues)
                    InitializeFields(initializingContext);

                InitializeSerializedFields(initializingContext);

                if (!isFallbackValues)
                    InitializeLastFields();

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

        protected virtual void InitializeFields(InitializationContext initializingContext)
        {
#if UNITY_EDITOR
            UpdateIcon();
#endif
        }

#if UNITY_EDITOR
        private void UpdateIcon()
        {
            RenderingManager.UpdateIcon(this);
        }
#endif

        /// <summary>
        /// Initialize SerializedField's to their default values.
        /// </summary>
        /// <param name="initializingContext"></param>
        protected virtual void InitializeSerializedFields(InitializationContext initializingContext)
        {

        }

        /// <summary>
        /// Should this object be added to the <see cref="DepictionEngine.InstanceManager"/>.
        /// </summary>
        /// <returns>True of the object should be added.</returns>
        protected virtual bool AddInstanceToManager()
        {
            return false;
        }

        protected override bool AddToInstanceManager()
        {
            if (base.AddToInstanceManager())
                return !AddInstanceToManager() || instanceManager.Add(this);
            return false;
        }

#if UNITY_EDITOR
        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                if (!IsDisposing())
                {
                    UpdateIcon();
                    return true;
                }
            }
            return false;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        protected virtual bool SetValue<T>(string name, T value, ref T valueField, Action<T, T> assignedCallback = null, bool allowAutoDisposeOnOutOfSynchProperty = false)
        {
            T oldValue = valueField;

            if (HasChanged(value, oldValue))
            {
                valueField = value;

                if (allowAutoDisposeOnOutOfSynchProperty)
                    Datasource.StartAllowAutoDisposeOnOutOfSynchProperty();

                assignedCallback?.Invoke(value, oldValue);

                PropertyAssigned(this, name, value, oldValue);

                if (allowAutoDisposeOnOutOfSynchProperty)
                    Datasource.EndAllowAutoDisposeOnOutOfSynchProperty();

                return true;
            }

            return false;
        }

        public Action<IProperty, string, object, object> PropertyAssignedEvent
        {
            get => _propertyAssignedEvent;
            set => _propertyAssignedEvent = value;
        }

        public void ResetId()
        {
            _id = default;
        }

        /// <summary>
        /// Global unique identifier.
        /// </summary>
        [Json(conditionalMethod: nameof(IsNotFallbackValues))]
        public SerializableGuid id
        {
            get => _id;
            private set => _id = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        protected void PropertyAssigned<T>(IProperty property, string name, T newValue, T oldValue)
        {
            if (initialized)
            {
#if UNITY_EDITOR
                if (SceneManager.GetIsUserChangeContext() && property is IJson iJson && iJson.GetJsonAttribute(name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo))
                    MarkAsNotPoolable();
#endif
                PropertyAssignedEvent?.Invoke(property, name, newValue, oldValue);
            }
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

#if UNITY_EDITOR
        public virtual bool PasteComponentAllowed()
        {
            return true;
        }

        public void Reset()
        {
            if (wasFirstUpdated)
            {
                if (ResetAllowed())
                    Editor.InspectorManager.Resetting(this);

                Editor.UndoManager.RevertAllInCurrentGroup();

                MarkAsNotPoolable();
            }
        }

        protected virtual bool ResetAllowed()
        {
            return true;
        }

        public void InspectorReset()
        {
            RegisterCompleteObjectUndo();

            SceneManager.StartUserContext();

            InitializeSerializedFields(InitializationContext.Reset);

            SceneManager.EndUserContext();
        }
#endif

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (instanceAdded && AddInstanceToManager())
                {
                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != Disposable.NULL)
                    {
                        if (!instanceManager.Remove(id, this) && !SceneManager.IsSceneBeingDestroyed())
                            Debug.LogError(GetType() + " '" + this + "' not properly removed From InstanceManager!");
                    }
                }

                PropertyAssignedEvent = null;

                return true;
            }
            return false;
        }
    }
}
