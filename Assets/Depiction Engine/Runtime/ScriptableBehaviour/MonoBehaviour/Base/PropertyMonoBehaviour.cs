// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public class PropertyMonoBehaviour : MonoBehaviourDisposable, IProperty
    {
        [SerializeField, HideInInspector]
        private SerializableGuid _id;

        [SerializeField, HideInInspector]
        private PropertyMonoBehaviour _parent;
        private List<PropertyMonoBehaviour> _children;

        private List<PropertyModifier> _initializationPropertyModifiers;

        private bool _activeAndEnabled;

        private bool _hasDirtyFlags;
        private HashSet<int> _dirtyKeys;

        [NonSerialized]
        private bool _initializeLastFields;

        private Action<IProperty, string, object, object> _propertyAssignedEvent;

        protected virtual void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {  
        }

        public override void Recycle()
        {
            base.Recycle();

            ResetId();

            ClearDirtyFlags();
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
                if (!UpdateRelations(() => 
                {
                    if (!isFallbackValues)
                    {
                        CreateAndInitializeDependencies(initializingContext);
                        InitializeFields(initializingContext);
                    }

                    InitializeSerializedFields(initializingContext);

                    if (!isFallbackValues)
                        InitializeLastFields();
                }))
                    return false;

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

        protected virtual void InitializeFieldsBeforeChildren(InitializationContext initializingContext)
        {

        }

        /// <summary>
        /// Initialize SerializedField's to their default values.
        /// </summary>
        /// <param name="initializingContext"></param>
        protected virtual void InitializeSerializedFieldsBeforeChildren(InitializationContext initializingContext)
        {

        }

        protected virtual void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {

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

        protected virtual bool InitializeLastFields()
        {
            if (!_initializeLastFields)
            {
                _initializeLastFields = true;

#if UNITY_EDITOR
                _lastParent = _parent;
#endif
                return true;
            }
            return false;
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateActiveAndEnabled();

            UpdateDependencies();
        }

#if UNITY_EDITOR
        private PropertyMonoBehaviour _lastParent;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                UpdateActiveAndEnabled();

                _parent = _lastParent;
                if (!IsDisposing())
                    UpdateRelations();

                return true;
            }
            return false;
        }
#endif

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

        public virtual void UpdateDependencies()
        {
        }

        public virtual bool UpdateRelations(Action beforeSiblingsInitializeCallback = null)
        {
            if (ParentHasChanged())
                UpdateParent();

            if (Disposable.IsDisposed(this))
                return false;

            beforeSiblingsInitializeCallback?.Invoke();

            if (SiblingsHasChanged())
                UpdateSiblings();

            if (Disposable.IsDisposed(this))
                return false;

            if (ChildrenHasChanged())
                UpdateChildren();

            if (Disposable.IsDisposed(this))
                return false;

            return true;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                InstanceManager.AddedEvent -= InstanceAddedHandler;
                InstanceManager.RemovedEvent -= InstanceRemovedHandler;
                if (!IsDisposing())
                {
                    InstanceManager.AddedEvent += InstanceAddedHandler;
                    InstanceManager.RemovedEvent += InstanceRemovedHandler;
                }

                if (_parent != Disposable.NULL)
                {
                    RemoveParentDelegates(_parent);
                    AddParentDelegates(_parent);
                }

                if (_children != null)
                {
                    foreach (PropertyMonoBehaviour child in _children)
                    {
                        RemoveChildDelegates(child);
                        AddChildDelegates(child);
                    }
                }

                return true;
            }
            return false;
        }

        protected virtual void InstanceAddedHandler(IProperty property)
        {
            IterateOverComponentReference((id, callback) =>
            {
                if (property.id == id)
                    callback();
            });
        }

        protected virtual void InstanceRemovedHandler(IProperty property)
        {
            IterateOverComponentReference((id, callback) =>
            {
                if (property.id == id)
                    callback();
            });
        }

        protected virtual bool RemoveParentDelegates(PropertyMonoBehaviour parent)
        {
            return parent is not null;
        }

        protected virtual bool AddParentDelegates(PropertyMonoBehaviour parent)
        {
            return !IsDisposing() && parent != Disposable.NULL;
        }

        protected virtual bool RemoveChildDelegates(PropertyMonoBehaviour child)
        {
            if (child is not null)
            {
                child.DisposedEvent -= ChildDisposedHandler;
                return true;
            }
            return false;
        }

        protected virtual bool AddChildDelegates(PropertyMonoBehaviour child)
        {
            if (!IsDisposing() && child != Disposable.NULL)
            {
                child.DisposedEvent += ChildDisposedHandler;
                return true;
            }
            return false;
        }

        private void ChildDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemoveChild(disposable as PropertyMonoBehaviour);
        }

        protected virtual PropertyMonoBehaviour GetParent()
        {
            PropertyMonoBehaviour parent = null;

            Type parentType = GetParentType();
            if (parentType != null)
                parent = (PropertyMonoBehaviour)gameObject.GetComponentInParentInitialized(parentType, true, InitializationContext.Programmatically, null, null, isFallbackValues);

            return parent;
        }

        /// <summary>
        /// Finds and sets the parent.
        /// </summary>
        /// <param name="originator"></param>
        public virtual void UpdateParent(PropertyMonoBehaviour originator = null)
        {
            Originator(() => { SetParent(GetParent()); }, originator);
        }

        protected void UpdateSiblings()
        {
            Type siblingType = GetSiblingType();
            if (siblingType != null)
            {
                Component[] siblings = transform.GetComponents(siblingType);
                if (siblings != null)
                {
                    foreach (PropertyMonoBehaviour sibling in siblings.Cast<PropertyMonoBehaviour>())
                    {
                        if (sibling != this && sibling != Disposable.NULL)
                        {
                            if (InitializeComponent(sibling))
                                sibling.UpdateParent(this);
                        }
                    }
                }
            }
        }

        protected virtual void UpdateChildren()
        {
            Type childType = GetChildType();
            if (childType != null)
            {
                List<PropertyMonoBehaviour> children = null;

                if (transform != null)
                {
                    foreach (Transform childTransform in transform)
                    {
                        children ??= new List<PropertyMonoBehaviour>();
                        children.AddRange(childTransform.GetComponents(childType).Cast<PropertyMonoBehaviour>());
                    }
                }

                if (children != null)
                {
                    foreach (PropertyMonoBehaviour child in children)
                    {
                        if (child != Disposable.NULL)
                        {
                            if (InitializeComponent(child))
                                child.UpdateParent(this);
                        }
                    }
                }
            }
        }

        protected T InitializeComponent<T>(T component, JSONObject json = null, bool isFallbackValues = false)
        {
            return InstanceManager.Initialize(component, GetInitializeContext(), json, null, isFallbackValues);
        }

        protected virtual bool ParentHasChanged()
        {
            return !initialized;
        }

        protected virtual bool SiblingsHasChanged()
        {
            return !initialized;
        }

        protected virtual bool ChildrenHasChanged()
        {
            return !initialized;
        }

        protected List<T> GetComponentFromId<T>(List<SerializableGuid> ids) where T : PropertyMonoBehaviour
        {
            List<T> components = null;

            foreach (SerializableGuid id in ids)
            {
                T component = GetComponentFromId<T>(id);
                if (component != null)
                {
                    components ??= new List<T>();
                    components.Add(component);
                }
            }

            return components;
        }

        protected T GetComponentFromId<T>(SerializableGuid id) where T : PropertyMonoBehaviour
        {
            T component = null;

            if (!isFallbackValues)
            {
                InstanceManager instanceManager = InstanceManager.Instance(false);
                if (instanceManager != Disposable.NULL)
                    component = (T)instanceManager.GetIJson(id);
            }

            return component;
        }

        protected List<SerializableGuid> GetDuplicateComponentReferenceId<T>(List<SerializableGuid> ids, List<T> componentReferences, InitializationContext initializingContext) where T : PropertyMonoBehaviour
        {
            if (componentReferences != null)
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    T componentReference = componentReferences[i];
                    InstanceManager.Initialize(componentReference, initializingContext);
                    if (ids[i] != componentReference.id)
                        ids[i] = componentReference.id;
                }
            }

            return ids;
        }

        protected SerializableGuid GetDuplicateComponentReferenceId<T>(SerializableGuid id, T componentReference, InitializationContext initializingContext) where T: PropertyMonoBehaviour
        {
            if (componentReference != Disposable.NULL)
            {
                InstanceManager.Initialize(componentReference, initializingContext);
                if (id != componentReference.id)
                    id = componentReference.id;
            }

            return id;
        }

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

        /// <summary>
        /// Are the two objects equals?
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <param name="forceChangeDuringInitializing">When true, the function will always return true if the object is not initialized.</param>
        /// <returns>True of the objects are the same.</returns>
        /// <remarks>List's will compare their items not the collection reference.</remarks>
        protected bool HasChanged<T>(T newValue, T oldValue, bool forceChangeDuringInitializing = true)
        {
            if (forceChangeDuringInitializing && !initialized && !isFallbackValues)
                return true;

            if (newValue is IList newList && oldValue is IList oldList && newValue.GetType() == oldValue.GetType())
            {
                if (newList.Count == oldList.Count)
                {
                    for (int i = 0; i < newList.Count; i++)
                    {
                        if (!Object.Equals(newList[i], oldList[i]))
                            return true;
                    }
                }
            }

            return !Object.Equals(newValue, oldValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        protected void PropertyAssigned<T>(IProperty property, string name, T newValue, T oldValue)
        {
            SetPropertyDirty(name);

            if (initialized)
            {
#if UNITY_EDITOR
                if (SceneManager.GetIsUserChangeContext() && property is IJson iJson && JsonUtility.GetJsonAttribute(iJson, name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo))
                    MarkAsNotPoolable();
#endif
                PropertyAssignedEvent?.Invoke(property, name, newValue, oldValue);
            }
        }

        private void SetPropertyDirty(string name)
        {
            _dirtyKeys ??= new HashSet<int>();
            _dirtyKeys.Add(GetPropertyKey(name));
            _hasDirtyFlags = true;
        }

        /// <summary>
        /// Returns a key for the property name.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>An int hash code</returns>
        public static int GetPropertyKey(string name)
        {
            return name.GetHashCode();
        }

        public bool PropertyDirty(string name)
        {
            return _dirtyKeys != null && _dirtyKeys.Contains(GetPropertyKey(name));
        }

        protected virtual Type GetParentType()
        {
            return null;
        }

        protected virtual Type GetSiblingType()
        {
            return null;
        }

        protected virtual Type GetChildType()
        {
            return null;
        }

        public List<PropertyMonoBehaviour> children
        {
            get { _children ??= new List<PropertyMonoBehaviour>(); return _children; }
        }

        protected virtual bool CanBeDisabled()
        {
            return true;
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
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public SerializableGuid id
        {
            get => _id;
            private set => _id = value;
        }

        protected bool ignoreGameObjectActiveChange;
        protected override void OnDisable()
        {
            base.OnDisable();

            if (initialized && !ignoreGameObjectActiveChange)
                UpdateActiveAndEnabled();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (initialized && !ignoreGameObjectActiveChange)
                UpdateActiveAndEnabled();
        }

        private bool GetActiveAndEnabled()
        {
            if (IsDisposing())
                return false;
            return gameObject.activeInHierarchy && enabled;
        }

        public void UpdateActiveAndEnabled()
        {
            activeAndEnabled = GetActiveAndEnabled();
        }

        /// <summary>
        /// Returns true if gameObject.activeInHierarchy == true and enabled == true.
        /// </summary>
        public bool activeAndEnabled
        {
            get => _activeAndEnabled;
            private set 
            { 
                SetValue(nameof(activeAndEnabled), value, ref _activeAndEnabled, (newValue, oldValue) => 
                {
                    ActiveAndEnabledChanged(newValue, oldValue);
                }); 
            }
        }

        protected virtual void ActiveAndEnabledChanged(bool newValue, bool oldValue)
        {

        }

        protected virtual PropertyMonoBehaviour GetRootParent()
        {
            return sceneManager;
        }

        /// <summary>
        /// The object above in the hierarchy. 
        /// </summary>
        public PropertyMonoBehaviour parent
        {
            get => _parent;
            set => SetParent(value);
        }

        protected virtual JSONNode parentJson
        {
            get => null;
            set { }
        }

        protected virtual PropertyMonoBehaviour ValidatedParent(PropertyMonoBehaviour value)
        {
            if (!IsDisposing())
            {
                if (value == Disposable.NULL)
                    value = GetRootParent();
                if (value != Disposable.NULL)
                    InstanceManager.Initialize(value, GetInitializeContext());
            }

            return value;
        }

        protected virtual bool SetParent(PropertyMonoBehaviour value)
        {
            return SetValue(nameof(parent), ValidatedParent(value), ref _parent, (newValue, oldValue) =>
                {
                    if (HasChanged(newValue, oldValue, false))
                    {
    #if UNITY_EDITOR
                        _lastParent = parent;
    #endif
                        RemoveParentDelegates(oldValue);
                        AddParentDelegates(newValue);

                        if (oldValue is not null)
                            oldValue.RemoveChild(this);

                        ParentChanged(newValue, oldValue);
                    }

                    //Children are not serialized so while the value might be the same in this class it does not mean the parent has a proper reference to it, so we always add it just in case.
                    if (newValue != Disposable.NULL)
                        newValue.AddChild(this);
                });
        }

        protected virtual void ParentChanged(PropertyMonoBehaviour newValue, PropertyMonoBehaviour oldValue)
        {

        }

        private Vector3Double _originParam;
        private bool _originShiftSelectedParam;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalApplyOriginShiftingChild(PropertyMonoBehaviour child) { child.HierarchicalApplyOriginShifting(_originParam, _originShiftSelectedParam); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void HierarchicalApplyOriginShifting(Vector3Double origin, bool originShiftSelected)
        {
            _originParam = origin;
            _originShiftSelectedParam = originShiftSelected;
            IterateOverChildren(HierarchicalApplyOriginShiftingChild);
        }

        private Camera _cameraParam;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalBeginCameraRenderingChild(PropertyMonoBehaviour child) { child.HierarchicalBeginCameraRendering(_cameraParam); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the RenderPipelineManager.beginCameraRendering.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HierarchicalBeginCameraRendering(Camera camera)
        {
            _cameraParam = camera;
            IterateOverChildrenAndSiblings(HierarchicalBeginCameraRenderingChild);

            return initialized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalEndCameraRenderingChild(PropertyMonoBehaviour child) { child.HierarchicalEndCameraRendering(_cameraParam); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the RenderPipelineManager.endCameraRendering.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HierarchicalEndCameraRendering(Camera camera)
        {
            _cameraParam = camera;
            IterateOverChildrenAndSiblings(HierarchicalEndCameraRenderingChild);

            return initialized;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalInitializeChild(PropertyMonoBehaviour child) { child.HierarchicalInitialize(); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine Update. It is called before the <see cref="DepictionEngine.PropertyMonoBehaviour.HierarchicalUpdate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HierarchicalInitialize()
        {
            UpdateRelations();

            IterateOverChildrenAndSiblings(HierarchicalInitializeChild);

            if (IsDisposing())
                return false;

            return initialized;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreHierarchicalUpdateChild(PropertyMonoBehaviour child) { child.PreHierarchicalUpdate(); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine Update. It is called before the <see cref="DepictionEngine.PropertyMonoBehaviour.HierarchicalUpdate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool PreHierarchicalUpdate()
        {
            if (initialized)
            {
                UpdateDependencies();

                PreHierarchicalUpdateBeforeChildrenAndSiblings();
            }

            IterateOverChildrenAndSiblings(PreHierarchicalUpdateChild);

            if (IsDisposing())
                return false;

            return initialized;
        }

        public virtual void PreHierarchicalUpdateBeforeChildrenAndSiblings()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalUpdateChild(PropertyMonoBehaviour child) { child.HierarchicalUpdate(); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine Update. It is called after the <see cref="DepictionEngine.PropertyMonoBehaviour.PreHierarchicalUpdate"/> and before the <see cref="DepictionEngine.PropertyMonoBehaviour.PostHierarchicalUpdate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HierarchicalUpdate()
        {
            IterateOverChildrenAndSiblings(HierarchicalUpdateChild);

            if (IsDisposing())
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PostHierarchicalUpdateChild(PropertyMonoBehaviour child) { child.PostHierarchicalUpdate(); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine Update. It is called after the <see cref="DepictionEngine.PropertyMonoBehaviour.HierarchicalUpdate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool PostHierarchicalUpdate()
        {
            IterateOverChildrenAndSiblings(PostHierarchicalUpdateChild);

            if (IsDisposing())
                return false;

            ClearDirtyFlags();

            return true;
        }

        protected virtual bool AddChild(PropertyMonoBehaviour child)
        {
            if (!children.Contains(child))
            {
                children.Add(child);
                AddChildDelegates(child);
                return true;
            }
            return false;
        }

        protected virtual bool RemoveChild(PropertyMonoBehaviour child)
        {
            if (RemoveListItem(children, child))
            {
                RemoveChildDelegates(child);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IterateOverChildrenAndSiblings(Action<PropertyMonoBehaviour> callback = null)
        {
            //Apply to Siblings before Children
            ApplyBeforeChildren(callback);

            //Apply to Children
            IterateOverChildren(callback);

            //Apply to Siblings after Children
            ApplyAfterChildren(callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void IterateOverChildren(Action<PropertyMonoBehaviour> callback = null)
        {
            IterateThroughList(children, callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool RemoveListItem<T>(List<T> list, T property)
        {
            return list != null && list.Remove(property);
        }

        protected void IterateThroughList<T>(IList<T> list, Action<T> callback) where T : PropertyMonoBehaviour
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int count = list.Count;
                if (count > 0)
                    TriggerCallback(list[i >= count ? count - 1 : i], callback);
            }
        }

        protected virtual bool ApplyAfterChildren(Action<PropertyMonoBehaviour> callback)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TriggerCallback<T>(T child, Action<T> callback) where T : PropertyMonoBehaviour
        { 
            try
            {
                if (child != Disposable.NULL)
                {
                    callback?.Invoke(child);
                    return true;
                }
            }
            catch (MissingReferenceException e)
            {
                Debug.LogError(e.Message);
            }
            return false;
        }

#if UNITY_EDITOR
        public virtual bool PasteComponentAllowed()
        {
            return true;
        }
#endif

        protected virtual void ClearDirtyFlags()
        {
            if (_hasDirtyFlags)
            {
                _hasDirtyFlags = false;
                _dirtyKeys?.Clear();
            }
        }

#if UNITY_EDITOR
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
                    instanceAdded = false;

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
