// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

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

        private bool _lateInitialized;
        private bool _wasFirstUpdated;

        [NonSerialized]
        private bool _initializeLastFields;

        private bool _hasDirtyFlags;
        private HashSet<int> _dirtyKeys;

        private Action<IProperty, string, object, object> _propertyAssignedEvent;

        protected virtual void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {  
        }

        public override void Recycle()
        {
            base.Recycle();

            ResetId();

            _lateInitialized = false;
            _wasFirstUpdated = false;

            ClearDirtyFlags();
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

        protected override void InitializeUID(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeUID(initializingContext);

            id = GetId(id, initializingContext);
        }

        protected virtual SerializableGuid GetId(SerializableGuid id, InstanceManager.InitializationContext initializingContext)
        {
            if (id == SerializableGuid.Empty || initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate)
                return SerializableGuid.NewGuid();
            else
                return id;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            InitializeLastFields();

            UpdateActiveAndEnabled();
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                if (!UpdateRelations(
                    () => 
                    {
                        InitializeTransform(initializingContext);
                    },
                    () => 
                    {
                        CreateComponents(initializingContext);

                        InitializeScripts(initializingContext);

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

        protected virtual void InitializeTransform(InstanceManager.InitializationContext initializingContext)
        {

        }

        protected virtual void CreateComponents(InstanceManager.InitializationContext initializingContext)
        {

        }

        protected virtual void InitializeScripts(InstanceManager.InitializationContext initializingContext)
        {

        }

        protected override void Initialized(InstanceManager.InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateFields();
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
                return AddInstanceToManager() ? instanceManager.Add(this) : true;
            return false;
        }

        protected virtual void UpdateFields()
        {
        }

        protected virtual bool UpdateRelations(Action beforeParentInitializeCallback = null, Action beforeSiblingsInitializeCallback = null)
        {
            if (beforeParentInitializeCallback != null)
                beforeParentInitializeCallback();

            if (ParentHasChanged())
                UpdateParent();

            if (Disposable.IsDisposed(this))
                return false;

            if (beforeSiblingsInitializeCallback != null)
                beforeSiblingsInitializeCallback();

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

                if (parent != Disposable.NULL)
                {
                    RemoveParentDelegates(parent);
                    AddParentDelegates(parent);
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
            return !Object.ReferenceEquals(parent, null);
        }

        protected virtual bool AddParentDelegates(PropertyMonoBehaviour parent)
        {
            return !IsDisposing() && parent != Disposable.NULL;
        }

        protected virtual PropertyMonoBehaviour GetParent()
        {
            PropertyMonoBehaviour parent = null;

            Type parentType = GetParentType();
            if (parentType != null)
                parent = InitializeComponent(transform.GetComponentInParent(parentType, true), null, isFallbackValues);

            return parent;
        }

        /// <summary>
        /// Finds and sets the parent.
        /// </summary>
        /// <param name="originator"></param>
        /// <param name="isEditorUndo"></param>
        public virtual void UpdateParent(PropertyMonoBehaviour originator = null)
        {
            Originator(() =>  { SetParent(GetParent()); }, originator);
        }

        protected void UpdateSiblings()
        {
            Type siblingType = GetSiblingType();
            if (siblingType != null)
            {
                Component[] siblings = transform.GetComponents(siblingType);
                if (siblings != null)
                {
                    foreach (PropertyMonoBehaviour sibling in siblings)
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
                List<Component> children = null;

                if (transform != null)
                {
                    foreach (Transform childTransform in transform)
                    {
                        if (children == null)
                            children = new List<Component>();
                        children.AddRange(childTransform.GetComponents(childType));
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

        protected PropertyMonoBehaviour InitializeComponent(Component component, JSONNode json = null, bool isFallbackValues = false)
        {
            if (component is PropertyMonoBehaviour)
            {
                PropertyMonoBehaviour propertyMonoBehaviour = component as PropertyMonoBehaviour;
                InstanceManager.Initialize(propertyMonoBehaviour, GetInitializeContext(InstanceManager.InitializationContext.Editor), json, null, isFallbackValues);
                return propertyMonoBehaviour;
            }

            return null;
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
                    if (components == null)
                        components = new List<T>();
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

        protected List<SerializableGuid> GetDuplicateComponentReferenceId<T>(List<SerializableGuid> ids, List<T> componentReferences, InstanceManager.InitializationContext initializingContext) where T : PropertyMonoBehaviour
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

        protected SerializableGuid GetDuplicateComponentReferenceId<T>(SerializableGuid id, T componentReference, InstanceManager.InitializationContext initializingContext) where T: PropertyMonoBehaviour
        {
            if (componentReference != Disposable.NULL)
            {
                InstanceManager.Initialize(componentReference, initializingContext);
                if (id != componentReference.id)
                    id = componentReference.id;
            }

            return id;
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

        /// <summary>
        /// Is this property subject to change from external factors such as physics engine.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool IsDynamicProperty(int key)
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void PropertyAssigned<T>(IProperty property, string name, T newValue, T oldValue)
        {
            SetPropertyDirty(name);

            if (IsUserChangeContext() && property is IJson)
            {
                IJson iJson = property as IJson;
                if (iJson.GetJsonAttribute(name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo))
                    UserPropertyAssigned(iJson, name, jsonAttribute, propertyInfo);
            }

            if (initialized && PropertyAssignedEvent != null)
                PropertyAssignedEvent(property, name, newValue, oldValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void UserPropertyAssigned(IJson iJson, string name, JsonAttribute jsonAttribute, PropertyInfo propertyInfo)
        {
#if UNITY_EDITOR
            if (!iJson.IsDynamicProperty(GetPropertyKey(name)))
                EditorUndoRedoDetected();
#endif
        }

        private void SetPropertyDirty(string name)
        {
            if (_dirtyKeys == null)
                _dirtyKeys = new HashSet<int>();
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
            get 
            {
                if (_children == null)
                    _children = new List<PropertyMonoBehaviour>();
                return _children; 
            }
        }

        public bool wasFirstUpdated
        {
            get { return _wasFirstUpdated; }
        }

        protected virtual bool CanBeDisabled()
        {
            return true;
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

        public override void ExplicitOnDisable()
        {
            base.ExplicitOnDisable();

            UpdateActiveAndEnabled();
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            UpdateActiveAndEnabled();
        }

        private bool GetActiveAndEnabled()
        {
            if (IsDisposing())
                return false;
            return gameObject.activeInHierarchy && enabled;
        }

        private void UpdateActiveAndEnabled()
        {
            activeAndEnabled = GetActiveAndEnabled();
        }

        /// <summary>
        /// Returns true if gameObject.activeInHierarchy == true and enabled == true.
        /// </summary>
        public bool activeAndEnabled
        {
            get { return _activeAndEnabled; }
            set 
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

        protected virtual bool IncludeParentJson()
        {
            return false;
        }

        /// <summary>
        /// The object above in the hierarchy. 
        /// </summary>
        [Json(propertyName: nameof(parentJson), conditionalMethod: nameof(IncludeParentJson))]
        public PropertyMonoBehaviour parent
        {
            get { return _parent; }
            set { SetParent(value); }
        }

        protected virtual JSONNode parentJson
        {
            get { return null; }
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
            value = ValidatedParent(value);

            return SetValue(nameof(parent), value, ref _parent, (newValue, oldValue) =>
               {
                    if (RemoveParentDelegates(oldValue))
                        oldValue.RemoveProperty(this);

                    if (AddParentDelegates(newValue))
                        newValue.AddProperty(this);
               });
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalDetectChangesChild(PropertyMonoBehaviour child) { child.HierarchicalDetectChanges(); }
        /// <summary>
        /// Capture changes happening in Unity(Inspector, Transform etc...) such as: Name, Index, Layer, Tag, MeshRenderer Material, Enabled, GameObjectActive, Transform in the sceneview(localPosition, localRotation, localScale)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HierarchicalDetectChanges()
        {
            if (!isFallbackValues)
            {
                IsUserChange(() =>
                {
                    DetectChanges();

                    UpdateActiveAndEnabled();
                });
            }

            IterateOverChildrenAndSiblings(HierarchicalDetectChangesChild);
        }

        /// <summary>
        /// Detect changes that happen as a result of an external influence.
        /// </summary>
        protected virtual void DetectChanges()
        {
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

        private ScriptableRenderContext? _contextParam;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalUpdateEnvironmentChild(PropertyMonoBehaviour child) { child.HierarchicalUpdateEnvironmentAndReflection(_cameraParam, _contextParam); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HierarchicalUpdateEnvironmentAndReflection(Camera camera, ScriptableRenderContext? context)
        {
            _cameraParam = camera;
            _contextParam = context;
            IterateOverChildrenAndSiblings(HierarchicalUpdateEnvironmentChild);

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

        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine FixedUpdate.
        /// </summary>
        public virtual void HierarchicalFixedUpdate()
        {
            IterateOverChildrenAndSiblings((child) => { child.HierarchicalFixedUpdate(); });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LateInitializeChild(PropertyMonoBehaviour child) { child.LateInitialize(); }
        /// <summary>
        /// Objects that were not initialized are automatically initialized.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool LateInitialize()
        {
            bool lateInitialized = false;

            if (!_lateInitialized)
                lateInitialized = _lateInitialized = true;

            IterateOverChildrenAndSiblings(LateInitializeChild);

            if (IsDisposing())
                return false;

            return initialized && lateInitialized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PreHierarchicalUpdateChild(PropertyMonoBehaviour child) { child.PreHierarchicalUpdate(); }
        /// <summary>
        /// Called as a result of a hierarchical traversal of the scenegraph initiated at the same time as the UnityEngine Update. It is called before the <see cref="DepictionEngine.PropertyMonoBehaviour.HierarchicalUpdate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool PreHierarchicalUpdate()
        {
            bool updateRelationsFailed = true;
            IsUserChange(() => 
            {
                updateRelationsFailed = UpdateRelations();
            });
            if (!updateRelationsFailed)
                return false;

            if (initialized)
            {
                UpdateFields();

                PreHierarchicalUpdateBeforeChildrenAndSiblings();
            }

            IterateOverChildrenAndSiblings(PreHierarchicalUpdateChild);

            if (IsDisposing())
                return false;

            return initialized;
        }

        protected virtual void PreHierarchicalUpdateBeforeChildrenAndSiblings()
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

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HierarchicalActivateChild(PropertyMonoBehaviour child) { child.HierarchicalActivate(); }
        /// <summary>
        /// The hierarchy is traversed and gameObjects that have never been active are temporarly activated and deactivated to allow for their Awake to be called.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void HierarchicalActivate()
        {
            IterateOverChildrenAndSiblings(HierarchicalActivateChild);
        }

        protected virtual bool AddProperty(PropertyMonoBehaviour property)
        {
            if (!children.Contains(property))
            {
                children.Add(property);
                return true;
            }
            return false;
        }

        protected virtual bool RemoveProperty(PropertyMonoBehaviour property)
        {
            return RemoveListItem(children, property);
        }

        protected virtual bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            return false;
        }

        protected virtual void IterateOverChildrenAndSiblings(Action<PropertyMonoBehaviour> callback = null)
        {
            if (SceneManager.sceneExecutionState != SceneManager.UpdateExecutionState.HierarchicalActivate)
            {
                //Apply to Siblings before Children
                ApplyBeforeChildren(callback);
            }

            //Apply to Children
            IterateOverChildren(callback);

            if (SceneManager.sceneExecutionState != SceneManager.UpdateExecutionState.HierarchicalActivate)
            {
                //Apply to Siblings after Children
                ApplyAfterChildren(callback);
            }
        }

        protected virtual void IterateOverChildren(Action<PropertyMonoBehaviour> callback = null)
        {
            ListTriggerCallback(children, callback);
        }

        protected bool RemoveListItem<T>(List<T> list, T property)
        {
            if (list != null)
            {
                int removeIndex = list.IndexOf(property);
                if (removeIndex != -1)
                {
                    list.RemoveAt(removeIndex);

                    return true;
                }
            }
            return false;
        }

        protected void ListTriggerCallback<T>(List<T> list, Action<PropertyMonoBehaviour> callback) where T : PropertyMonoBehaviour
        {
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    TriggerCallback(list[i], callback);
            }
        }

        protected virtual bool ApplyAfterChildren(Action<PropertyMonoBehaviour> callback)
        {
            return false;
        }

        protected bool TriggerCallback<T>(T child, Action<T> callback) where T : PropertyMonoBehaviour
        { 
            try
            {
                if (child != Disposable.NULL)
                {
                    if (callback != null)
                        callback(child);
                }
                else
                    return true;
            }
            catch (MissingReferenceException e)
            {
                Debug.LogError(e.StackTrace);
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public virtual bool PasteComponentAllowed()
        {
            return true;
        }
#endif

        /// <summary>
        /// The hierarchy is traversed and dirty flags are cleared.
        /// </summary>
        public virtual void HierarchicalClearDirtyFlags()
        {
            ClearDirtyFlags();
            _wasFirstUpdated = true;
            IterateOverChildrenAndSiblings((child) => { child.HierarchicalClearDirtyFlags(); });
        }

        protected virtual void ClearDirtyFlags()
        {
            if (_hasDirtyFlags)
            {
                _hasDirtyFlags = false;
                if (_dirtyKeys != null)
                    _dirtyKeys.Clear();
            }
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                if (instanceAdded && AddInstanceToManager())
                {
                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != Disposable.NULL)
                    {
                        if (!instanceManager.Remove(id, this) && !SceneManager.IsSceneBeingDestroyed())
                            Debug.LogError(GetType() + " '"+ this +"' not properly removed From InstanceManager!");
                    }
                }

                _propertyAssignedEvent = null;

                SetParent(null);

                return true;
            }
            return false;
        }
    }
}
