// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Main component used to interface with the GameObject / Scripts and children. Only one per GameObject supported.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(Object))]
    [RequireComponent(typeof(TransformDouble))]
    public class Object : PersistentMonoBehaviour
    {
        [Serializable]
        private class VisibleCamerasDictionary : SerializableDictionary<int, VisibleCameras> { };

        [SerializeField, ConditionalShow(nameof(IsFallbackValues))]
        private ObjectAdditionalFallbackValues _objectAdditionalFallbackValues;

        [BeginFoldout("Meta")]
        [SerializeField, Tooltip("General purpose metadata associated with the object, used for searching or otherwise."), EndFoldout]
        private string _tags;

        [BeginFoldout("Physics")]
        [SerializeField, ConditionalShow(nameof(IsPhysicsObject)), Tooltip("When enabled the GameObject will automaticaly reacts to '"+nameof(AstroObject)+"' gravitational pull based on 'Object.mass' and 'AstroObject.mass'.")]
        private bool _useGravity;
        [SerializeField, ConditionalShow(nameof(IsPhysicsObject)), Tooltip("Used to determine the amount of gravitational force to apply when '"+nameof(Object.useGravity)+"' is enabled."), EndFoldout]
        private double _mass;

        [SerializeField, HideInInspector]
        private bool _isHiddenInHierarchy;
      
        [SerializeField, HideInInspector]
        private VisibleCamerasDictionary _visibleCameras;

        private TransformDouble _transform;
        private GeoAstroObject _parentGeoAstroObject;

        private AnimatorBase _animator;
        private ControllerBase _controller;
        private List<GeneratorBase> _generators;
        private List<ReferenceBase> _references;
        private List<EffectBase> _effects;
        private List<FallbackValues> _fallbackValues;
        private List<DatasourceBase> _datasources;

        private TargetControllerBase _targetController;

        private Rigidbody _rigidbodyInternal;

        public Action<Object, PropertyMonoBehaviour> ChildAddedEvent;
        public Action<Object, PropertyMonoBehaviour> ChildRemovedEvent;
        public Action<IProperty, string> ChildPropertyAssignedEvent;

        public Action<Object, Script> ScriptAddedEvent;
        public Action<Object, Script> ScriptRemovedEvent;
        public Action<IProperty, string> ComponentPropertyAssignedEvent;

        public Action<TransformBase.Component, TransformBase.Component> TransformChangedEvent;
        public Action<IProperty, string, object, object> TransformPropertyAssignedEvent;
        
        public Action<IProperty, string, object, object> ParentGeoAstroObjectPropertyAssignedEvent;

        public Action<LocalPositionParam, LocalRotationParam, LocalScaleParam, Camera> TransformControllerCallback;

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (objectAdditionalFallbackValues != null)
            {
                callback(objectAdditionalFallbackValues.animatorId, UpdateFallbackValuesAnimator);
                callback(objectAdditionalFallbackValues.controllerId, UpdateFallbackValuesController);
                foreach (SerializableGuid id in objectAdditionalFallbackValues.generatorsId)
                    callback(id, UpdateFallbackValuesGenerators);
                foreach (SerializableGuid id in objectAdditionalFallbackValues.referencesId)
                    callback(id, UpdateFallbackValuesReferences);
                foreach (SerializableGuid id in objectAdditionalFallbackValues.effectsId)
                    callback(id, UpdateFallbackValuesEffects);
                foreach (SerializableGuid id in objectAdditionalFallbackValues.fallbackValuesId)
                    callback(id, UpdateFallbackValuesFallbackValues);
                foreach (SerializableGuid id in objectAdditionalFallbackValues.datasourcesId)
                    callback(id, UpdateFallbackValuesDatasources);
            }
        }

        public override void Recycle()
        {
            base.Recycle();

            gameObject.layer = LayerMask.GetMask("Default");
            gameObject.hideFlags = HideFlags.None;

            if (_forceUpdateTransformPending != null)
                _forceUpdateTransformPending.Clear();

            _targetController = null;

            if (_visibleCameras != null)
                _visibleCameras.Clear();
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastGameObjectActiveSelf = gameObjectActiveSelf;
                _lastLayer = layer;
                _lastTag = tag;

                return true;
            }
            return false;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            InitRigidbody();
        }

        protected void InitRigidbody()
        {
            if (rigidbodyInternal == null)
            {
                rigidbodyInternal = gameObject.GetComponent<Rigidbody>();
                if (useGravity && rigidbodyInternal == null)
                {
                    rigidbodyInternal = gameObject.AddComponent<Rigidbody>();
                    rigidbodyInternal.drag = 0.05f;
                    rigidbodyInternal.useGravity = false;
                }
            }
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            if (isFallbackValues)
                InitValue(value => objectAdditionalFallbackValues = value, CreateOptionalProperties<ObjectAdditionalFallbackValues>(initializingState), initializingState);

            if (objectAdditionalFallbackValues != null)
            {
                InitValue(value => createAnimatorIfMissing = value, true, initializingState);
                InitValue(value => animatorId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.animatorId, objectAdditionalFallbackValues.animator, initializingState); }, initializingState);

                InitValue(value => createControllerIfMissing = value, true, initializingState);
                InitValue(value => controllerId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.controllerId, objectAdditionalFallbackValues.controller, initializingState); }, initializingState);

                InitValue(value => createGeneratorIfMissing = value, true, initializingState);
                InitValue(value => generatorsId = value, new List<SerializableGuid>(), () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.generatorsId, objectAdditionalFallbackValues.generators, initializingState); }, initializingState);

                InitValue(value => createReferenceIfMissing = value, true, initializingState);
                InitValue(value => referencesId = value, new List<SerializableGuid>(), () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.referencesId, objectAdditionalFallbackValues.references, initializingState); }, initializingState);

                InitValue(value => createEffectIfMissing = value, true, initializingState);
                InitValue(value => effectsId = value, new List<SerializableGuid>(), () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.effectsId, objectAdditionalFallbackValues.effects, initializingState); }, initializingState);

                InitValue(value => createFallbackValuesIfMissing = value, true, initializingState);
                InitValue(value => fallbackValuesId = value, new List<SerializableGuid>(), () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.fallbackValuesId, objectAdditionalFallbackValues.fallbackValues, initializingState); }, initializingState);

                InitValue(value => createDatasourceIfMissing = value, true, initializingState);
                InitValue(value => datasourcesId = value, new List<SerializableGuid>(), () => { return GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.datasourcesId, objectAdditionalFallbackValues.datasources, initializingState); }, initializingState);
            }

            InitValue(value => tags = value, "", initializingState);
            InitValue(value => useGravity = value, false, initializingState);
            InitValue(value => mass = value, GetDefaultMass(), initializingState);
            InitValue(value => layer = value, LayerUtility.GetDefaultLayer(GetType()), initializingState);
            InitValue(value => tag = value, "Untagged", initializingState);
            InitValue(value => isHiddenInHierarchy = value, GetDefaultIsHiddenInHierarchy(), () => { return false; }, initializingState);
        }

        protected T CreateOptionalProperties<T>(InstanceManager.InitializationContext initializingState) where T : OptionalPropertiesBase
        {
            OptionalPropertiesBase optionalProperties = ScriptableObject.CreateInstance<T>();
            optionalProperties.parent = this;

#if UNITY_EDITOR
            Editor.UndoManager.RegisterCreatedObjectUndo(optionalProperties, initializingState);
#endif

            return optionalProperties as T;
        }

        protected override void InitializeDependencies(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeDependencies(initializingState);

            if (initializationJson != null)
            {
                MonoBehaviourDisposable[] components = gameObject.GetComponents<MonoBehaviourDisposable>();

                JSONObject transformJson = initializationJson[nameof(transform)] as JSONObject;
                if (transformJson != null)
                {
                    string typeStr = transformJson[nameof(type)];
                    if (string.IsNullOrEmpty(typeStr))
                        typeStr = typeof(TransformDouble).FullName;
                    Type transformType = Type.GetType(typeStr);
                    TransformDouble transform = RemoveComponentFromList(transformType, components) as TransformDouble;
                    if (transform != Disposable.NULL)
                        InitializeComponent(transform, transformJson);
                }

                JSONObject animatorJson = initializationJson[nameof(animator)] as JSONObject;
                if (animatorJson != null)
                {
                    string typeStr = animatorJson[nameof(type)];
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        Type animatorType = Type.GetType(typeStr);
                        AnimatorBase animator = RemoveComponentFromList(animatorType, components) as AnimatorBase;
                        if (animator != Disposable.NULL)
                            InitializeComponent(animator, animatorJson);
                        else
                            CreateScript(animatorType, animatorJson, initializingState);
                    }
                }

                JSONObject controllerJson = initializationJson[nameof(controller)] as JSONObject;
                if (controllerJson != null)
                {
                    string typeStr = controllerJson[nameof(type)];
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        Type controllerType = Type.GetType(typeStr);
                        ControllerBase controller = RemoveComponentFromList(controllerType, components) as ControllerBase;
                        if (controller != Disposable.NULL)
                            InitializeComponent(controller, controllerJson);
                        else
                            CreateScript(controllerType, controllerJson, initializingState);
                    }
                }

                JSONArray generatorsJson = initializationJson[nameof(generators)] as JSONArray;
                if (generatorsJson != null)
                {
                    foreach (JSONObject generatorJson in generatorsJson)
                    {
                        string typeStr = generatorJson[nameof(type)];
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            Type generatorType = Type.GetType(typeStr);
                            GeneratorBase generator = RemoveComponentFromList(generatorType, components) as GeneratorBase;
                            if (generator != Disposable.NULL)
                                InitializeComponent(generator, generatorJson);
                            else
                                CreateScript(generatorType, generatorJson, initializingState);
                        }
                    }
                }

                JSONArray referencesJson = initializationJson[nameof(references)] as JSONArray;
                if (referencesJson != null)
                {
                    foreach (JSONObject referenceJson in referencesJson)
                    {
                        string typeStr = referenceJson[nameof(type)];
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            Type referenceType = Type.GetType(typeStr);
                            ReferenceBase reference = RemoveComponentFromList(referenceType, components) as ReferenceBase;
                            if (reference != Disposable.NULL)
                                InitializeComponent(reference, referenceJson);
                            else
                                CreateScript(referenceType, referenceJson, initializingState);
                        }
                    }
                }

                JSONArray effectsJson = initializationJson[nameof(effects)] as JSONArray;
                if (effectsJson != null)
                {
                    foreach (JSONObject effectJson in effectsJson)
                    {
                        string typeStr = effectJson[nameof(type)];
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            Type effectType = Type.GetType(typeStr);
                            EffectBase effect = RemoveComponentFromList(effectType, components) as EffectBase;
                            if (effect != Disposable.NULL)
                                InitializeComponent(effect, effectJson);
                            else
                                CreateScript(effectType, effectJson, initializingState);
                        }
                    }
                }

                JSONArray fallbackValuesJson = initializationJson[nameof(fallbackValues)] as JSONArray;
                if (fallbackValuesJson != null)
                {
                    foreach (JSONObject fallbackValueJson in fallbackValuesJson)
                    {
                        string typeStr = fallbackValueJson[nameof(type)];
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            Type fallbackValueType = Type.GetType(typeStr);
                            FallbackValues fallbackValue = RemoveComponentFromList(fallbackValueType, components) as FallbackValues;
                            if (fallbackValue != Disposable.NULL)
                                InitializeComponent(fallbackValue, fallbackValueJson);
                            else
                                CreateScript(fallbackValueType, fallbackValueJson, initializingState);
                        }
                    }
                }

                JSONArray datasourcesJson = initializationJson[nameof(datasources)] as JSONArray;
                if (datasourcesJson != null)
                {
                    foreach (JSONObject datasourceJson in datasourcesJson)
                    {
                        string typeStr = datasourceJson[nameof(type)];
                        if (!string.IsNullOrEmpty(typeStr))
                        {
                            Type datasourceType = Type.GetType(typeStr);
                            DatasourceBase datasource = RemoveComponentFromList(datasourceType, components) as DatasourceBase;
                            if (datasource != Disposable.NULL)
                                InitializeComponent(datasource, datasourceJson);
                            else
                                CreateScript(datasourceType, datasourceJson, initializingState);
                        }
                    }
                }
            }
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializingState)
        {
            if (base.Initialize(initializingState))
            {
                UpdateReferences(true);

                return true;
            }
            return false;
        }

        protected override void Initialized(InstanceManager.InitializationContext initializingState)
        {
            base.Initialized(initializingState);

            ForceUpdateTransform(true, true, true);
        }

#if UNITY_EDITOR
        protected override void RegisterInitializeObjectUndo(InstanceManager.InitializationContext initializingState)
        {
            base.RegisterInitializeObjectUndo(initializingState);

            //Register GameObject name/layer/enabled etc...
            Editor.UndoManager.RegisterCompleteObjectUndo(gameObject, initializingState);
        }
#endif

        protected override bool IsValidInitialization(InstanceManager.InitializationContext initializingState)
        {
            if (base.IsValidInitialization(initializingState))
            {
                if (!CanBeDuplicated() && (initializingState == InstanceManager.InitializationContext.Editor_Duplicate || initializingState == InstanceManager.InitializationContext.Programmatically_Duplicate))
                {
                    DisposeManager.Destroy(gameObject);

                    return false;
                }
                return true;
            }
            return false;
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
#if UNITY_EDITOR
                transform.InitializeToTopInInspector();
                InitializeToTopInInspector();
#endif
                ForceUpdateTransformIfPending();

                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        protected virtual void InitializeToTopInInspector()
        {
            Component[] components = gameObject.GetComponents<Component>();
            if (components[2] != this)
            {
                Editor.ComponentUtility.MoveComponentRelativeToComponent(this, components[1], false);
                transform.InitializeToTopInInspector();
            }
        }

        public UnityEngine.Object[] GetTransformAdditionalRecordObjects()
        {
            return new UnityEngine.Object[] { transform };
        }
#endif

        protected virtual double GetDefaultMass()
        {
            return 1.0d;
        }

        /// <summary>
        /// Used to tell whether or not this object should be positioned in space according to its transform or kept to identity transform(origin) during origin shifting.
        /// </summary>
        /// <returns>If true the object will be positioned in space otherwise it will remain static at identity transform(origin).</returns>
        public virtual bool RequiresPositioning()
        {
            return false;
        }

        protected virtual bool CanBeDuplicated()
        {
            return true;
        }

        protected override void DetectChanges()
        {
            base.DetectChanges();

            if (_lastGameObjectActiveSelf != gameObjectActiveSelf)
            {
                bool newValue = gameObjectActiveSelf;
                gameObject.SetActive(_lastGameObjectActiveSelf);
                gameObjectActiveSelf = newValue;
            }

            if (_lastLayer != layer)
            {
                int newValue = layer;
                gameObject.layer = _lastLayer;
                layer = newValue;
            }

            if (_lastTag != tag)
            {
                string newValue = tag;
                gameObject.tag = _lastTag;
                tag = newValue;
            }
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                if (AddInputManagerDelegates())
                {
                    InputManager.OnMouseClickedEvent -= InputManagerOnMouseClickedHandler;
                    if (!IsDisposing())
                        InputManager.OnMouseClickedEvent += InputManagerOnMouseClickedHandler;
                }

                RemoveParentGeoAstroObjectDelegate(parentGeoAstroObject);
                AddParentGeoAstroObjectDelegate(parentGeoAstroObject);

                IterateOverComponents((component) =>
                {
                    if (component != Disposable.NULL)
                    {
                        if (component is TransformBase)
                        {
                            TransformBase transform = component as TransformBase;
                            if (Object.ReferenceEquals(component, transform))
                            {
                                RemoveTransformDelegate(transform);
                                AddTransformDelegate(transform);
                            }
                        }

                        if (component is Script)
                        {
                            Script script = component as Script;
                            RemoveScriptDelegate(script);
                            AddScriptDelegate(script);
                        }
                    }
                });

                IterateOverReferences<ReferenceBase>((reference) =>
                {
                    RemoveLoadScopeDataReferenceDelegate(reference);
                    AddLoadScopeDataReferenceDelegate(reference);
                    return true;
                });

                return true;
            }
            return false;
        }

        protected virtual bool AddInputManagerDelegates()
        {
            return false;
        }

        protected virtual void InputManagerOnMouseClickedHandler(RaycastHitDouble hit)
        {

        }

        public virtual void OnMouseMoveHit(RaycastHitDouble hit)
        {
           
        }

        public virtual void OnMouseUpHit(RaycastHitDouble hit)
        {

        }

        public virtual void OnMouseDownHit(RaycastHitDouble hit)
        {

        }

        public virtual void OnMouseEnterHit(RaycastHitDouble hit)
        {

        }

        public virtual void OnMouseExitHit(RaycastHitDouble hit)
        {

        }

        public virtual void OnMouseClickedHit(RaycastHitDouble hit)
        {

        }

        protected virtual void RemoveTransformDelegate(TransformBase transform)
        {
            if (!Object.ReferenceEquals(transform, null))
            {
                transform.PropertyAssignedEvent -= TransformPropertyAssignedHandler;
                transform.ChangedEvent -= TransformChangedHandler;
                transform.ChildAddedEvent -= TransformChildAddedHandler;
                transform.ChildRemovedEvent -= TransformChildRemovedHandler;
                transform.ChildPropertyChangedEvent -= TransformChildPropertyChangedHandler;
                if (transform is TransformDouble)
                {
                    TransformDouble transformDouble = transform as TransformDouble;
                    transformDouble.ObjectCallback -= TransformObjectCallbackHandler;
                }

                if (transform.children != null)
                {
                    for (int i = transform.children.Count - 1; i >= 0; i--)
                    {
                        TransformBase child = transform.children[i] as TransformBase;
                        if (child != Disposable.NULL && child.objectBase != Disposable.NULL)
                            RemoveObjectChildDelegates(child.objectBase);
                    }
                }
            }
        }

        protected virtual void AddTransformDelegate(TransformBase transform)
        {
            if (!IsDisposing() && transform != Disposable.NULL)
            {
                transform.PropertyAssignedEvent += TransformPropertyAssignedHandler;
                transform.ChangedEvent += TransformChangedHandler;
                transform.ChildAddedEvent += TransformChildAddedHandler;
                transform.ChildRemovedEvent += TransformChildRemovedHandler;
                transform.ChildPropertyChangedEvent += TransformChildPropertyChangedHandler;

                if (transform is TransformDouble)
                {
                    TransformDouble transformDouble = transform as TransformDouble;
                    transformDouble.ObjectCallback += TransformObjectCallbackHandler;
                }

                if (transform.children != null)
                {
                    for (int i = transform.children.Count - 1; i >= 0; i--)
                    {
                        TransformBase child = transform.children[i] as TransformBase;
                        if (child != Disposable.NULL && child.objectBase != Disposable.NULL)
                            AddObjectChildDelegates(child.objectBase);
                    }
                }
            }
        }

        protected virtual void TransformPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(newValue, oldValue) || property.IsDisposing())
                return;

            ComponentPropertyAssigned(property, name);

            Originator(() =>
            {
                if (name == nameof(parentGeoAstroObject))
                    UpdateParentGeoAstroObject();
            }, property);

            if (TransformPropertyAssignedEvent != null)
                TransformPropertyAssignedEvent(property, name, newValue, oldValue);
        }

        private void TransformChangedHandler(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            TransformChanged(changedComponent, capturedComponent);

            if (TransformChangedEvent != null)
                TransformChangedEvent(changedComponent, capturedComponent);
        }

        protected virtual bool TransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            return initialized;
        }

        private void RemoveScriptDelegate(Script script)
        {
            if (!Object.ReferenceEquals(script, null))
                script.PropertyAssignedEvent -= ScriptPropertyAssignedHandler;
        }

        private void AddScriptDelegate(Script script)
        {
            if (!IsDisposing() && script != Disposable.NULL)
                script.PropertyAssignedEvent += ScriptPropertyAssignedHandler;
        }

        protected virtual void ScriptPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (property.IsDisposing())
                return;

            ComponentPropertyAssigned(property, name);
        }

        private void ComponentPropertyAssigned(IProperty property, string name)
        {
            Originator(() => 
            {
                (string, object) localProperty = GetProperty(property);
                PropertyAssigned(this, localProperty.Item1, localProperty.Item2, null);

                if (ComponentPropertyAssignedEvent != null)
                    ComponentPropertyAssignedEvent(property, name);
 
            }, property);
        }

        protected bool RemoveLoadScopeDataReferenceDelegate(ReferenceBase reference)
        {
            if (!Object.ReferenceEquals(reference, null))
            {
                reference.DataChangedEvent -= ReferenceDataChangedHandler;
                reference.LoaderPropertyAssignedChangedEvent -= ReferenceLoaderPropertyAssignedChangedHandler;
                
                return true;
            }
            return false;
        }

        protected bool AddLoadScopeDataReferenceDelegate(ReferenceBase reference)
        {
            if (!IsDisposing() && reference != Disposable.NULL)
            {
                reference.DataChangedEvent += ReferenceDataChangedHandler;
                reference.LoaderPropertyAssignedChangedEvent += ReferenceLoaderPropertyAssignedChangedHandler;

                return true;
            }
            return false;
        }

        private void ReferenceDataChangedHandler(ReferenceBase reference, ScriptableObjectDisposable newValue, ScriptableObjectDisposable oldValue)
        {
            UpdateReferences();
        }

        protected virtual bool UpdateReferences(bool forceUpdate = false)
        {
            return forceUpdate || initialized;
        }

        protected virtual void ReferenceLoaderPropertyAssignedChangedHandler(ReferenceBase reference, IProperty serializable, string name, object newValue, object oldValue)
        {

        }

        public override bool IsDynamicProperty(int key)
        {
            bool isDynamicProperty = base.IsDynamicProperty(key);

            if (!isDynamicProperty && key == GetPropertyKey(nameof(transform)))
                isDynamicProperty = true;

            return isDynamicProperty;
        }

        protected virtual void TransformChildAddedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
            {
                if (child != Disposable.NULL)
                    AddObjectChildDelegates((child as TransformBase).objectBase);
            }

            if (ChildAddedEvent != null)
                ChildAddedEvent(this, child);
        }

        protected virtual void TransformChildRemovedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
            {
                if (!Object.ReferenceEquals(child, null))
                    RemoveObjectChildDelegates((child as TransformBase).objectBase);
            }

            if (ChildRemovedEvent != null)
                ChildRemovedEvent(this, child);
        }

        protected virtual void TransformChildPropertyChangedHandler(TransformBase transform, string name, object newValue, object oldValue)
        {
            if (name == nameof(TransformBase.objectBase))
            {
                RemoveObjectChildDelegates(oldValue as Object);
                AddObjectChildDelegates(newValue as Object);
            }
        }

        private void RemoveObjectChildDelegates(Object objectBase)
        {
            if (!Object.ReferenceEquals(objectBase, null))
                objectBase.PropertyAssignedEvent -= ObjectChildPropertyAssignedHandler;
        }

        private void AddObjectChildDelegates(Object objectBase)
        {
            if (objectBase != Disposable.NULL)
                objectBase.PropertyAssignedEvent += ObjectChildPropertyAssignedHandler;
        }

        protected void ObjectChildPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(oldValue, newValue))
                return;

            if (ChildPropertyAssignedEvent != null)
                ChildPropertyAssignedEvent(property, name);
        }

        public virtual int GetAdditionalChildCount()
        {
            return 0;
        }

        public virtual void UpdateAdditionalChildren()
        {

        }

        protected void TransformObjectCallbackHandler(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            TransformObjectCallback(localPositionParam, localRotationParam, localScaleParam, camera);
            
            if (TransformControllerCallback != null)
                TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera);
        }

        protected virtual bool TransformObjectCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            return initialized;
        }

        protected virtual bool RemoveParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
        {
            if (!Object.ReferenceEquals(parentGeoAstroObject, null))
            {
                parentGeoAstroObject.PropertyAssignedEvent -= ParentGeoAstroObjectPropertyAssignedHandler;
            
                return true;
            }
            return false;
        }

        protected virtual bool AddParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
        {
            if (!IsDisposing() && parentGeoAstroObject != Disposable.NULL)
            {
                parentGeoAstroObject.PropertyAssignedEvent += ParentGeoAstroObjectPropertyAssignedHandler;

                return true;
            }
            return false;
        }

        protected void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (ParentGeoAstroObjectPropertyAssignedEvent != null)
                ParentGeoAstroObjectPropertyAssignedEvent(property, name, newValue, oldValue);
        }

        private void UpdateParentGeoAstroObject()
        {
            parentGeoAstroObject = transform != Disposable.NULL ? transform.parentGeoAstroObject : null;
        }

        public GeoAstroObject parentGeoAstroObject
        {
            get { return _parentGeoAstroObject; }
            private set 
            {
                SetValue(nameof(parentGeoAstroObject), value, ref _parentGeoAstroObject, (newValue, oldValue) =>
                {
                    ParentGeoAstroObjectChanged(newValue, oldValue);
                });
            }
        }

        protected virtual void ParentGeoAstroObjectChanged(GeoAstroObject newValue, GeoAstroObject oldValue)
        {
            RemoveParentGeoAstroObjectDelegate(oldValue);
            AddParentGeoAstroObjectDelegate(newValue);
        }

        public void ApplyAutoAlignToSurface(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, GeoAstroObject parentGeoAstroObject, bool forceUpdate = false)
        {
            if (forceUpdate || localPositionParam.changed || localRotationParam.changed)
            {
                QuaternionDouble rotation = parentGeoAstroObject.GetUpVectorFromGeoCoordinate(localPositionParam.geoCoordinateValue);
                localRotationParam.SetValue(transform.parent != Disposable.NULL ? QuaternionDouble.Inverse(transform.parent.rotation) * rotation : rotation);
            }
        }

        protected override bool IsFullyInitialized()
        {
            return base.IsFullyInitialized() && transform.parentGeoAstroObject != Disposable.NULL ? transform.parentGeoAstroObject.GetLoadingInitialized() : true;
        }

        protected override Type GetParentType()
        {
            return typeof(TransformBase);
        }

        protected override Type GetSiblingType()
        {
            return typeof(Script);
        }

        private List<Script> _scriptList;
        protected override bool SiblingsHasChanged()
        {
            bool siblingChanged = base.SiblingsHasChanged();

            if (!siblingChanged)
            {
                if (_scriptList == null)
                    _scriptList = new List<Script>();
                GetComponents(_scriptList);
                int unityScriptCount = _scriptList.Count;

                int scriptCount = GetScriptCount();

                if (scriptCount != unityScriptCount)
                    siblingChanged = true;
            }

            return siblingChanged;
        }

        private VisibleCamerasDictionary visibleCameras
        {
            get 
            {
                if (_visibleCameras == null)
                    _visibleCameras = new VisibleCamerasDictionary();
                return _visibleCameras; 
            }
        }

        protected virtual bool GetDefaultIsHiddenInHierarchy()
        {
            return false;
        }

        protected virtual bool CanGameObjectBeDeactivated()
        {
            return true;
        }

        public ObjectAdditionalFallbackValues objectAdditionalFallbackValues
        {
            get { return _objectAdditionalFallbackValues; }
            set 
            {
                if (_objectAdditionalFallbackValues == value)
                    return;

                _objectAdditionalFallbackValues = value;
            }
        }

        private bool _lastGameObjectActiveSelf;
        /// <summary>
        /// ActivatesDeactivates the GameObject, depending on the given true or false/ value.
        /// </summary>
        [Json]
        public bool gameObjectActiveSelf
        {
            get { return gameObject.activeSelf; }
            set
            {
                if (!CanGameObjectBeDeactivated())
                    value = true;

                bool oldValue = gameObjectActiveSelf;
                if (HasChanged(value, oldValue))
                {
                    gameObject.SetActive(value);
                    _lastGameObjectActiveSelf = gameObject.activeSelf;
                    PropertyAssigned(this, nameof(gameObjectActiveSelf), value, oldValue);
                }
            }
        }

        /// <summary>
        /// General purpose metadata associated with the object, used for searching or otherwise.
        /// </summary>
        [Json]
        public string tags
        {
            get { return _tags; }
            set { SetValue(nameof(tags), value, ref _tags); }
        }

        /// <summary>
        /// When enabled, the GameObject will automaticaly reacts to <see cref="DepictionEngine.AstroObject"/> gravitational pull based on <see cref="DepictionEngine.Object.mass"/> and <see cref="DepictionEngine.AstroObject.mass"/>.
        /// </summary>
        [Json(conditionalMethod: nameof(IsPhysicsObject))]
        public bool useGravity
        {
            get { return _useGravity; }
            set
            {
                if (!IsPhysicsObject())
                    value = false;
                SetValue(nameof(useGravity), value, ref _useGravity, (newValue, oldValue) =>
                {
                    InitRigidbody();
                });
            }
        }

        /// <summary>
        /// Used to determine the amount of gravitational force to apply when <see cref="DepictionEngine.Object.useGravity"/> is enabled. 
        /// </summary>
        [Json(conditionalMethod: nameof(IsPhysicsObject))]
        public double mass
        {
            get { return _mass; }
            set { SetValue(nameof(mass), value, ref _mass); }
        }

        private int _lastLayer;
        /// <summary>
        /// The layer the GameObject is in.
        /// </summary>
        [Json]
        public int layer
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.layer : gameObject.layer; }
            set
            {
                int oldValue = layer;
                if (HasChanged(value, oldValue))
                {
                    if (objectAdditionalFallbackValues != null)
                        objectAdditionalFallbackValues.layer = value;
                    else
                        _lastLayer = gameObject.layer = value;

                    PropertyAssigned(this, nameof(layer), value, oldValue);
                }
            }
        }

        private string _lastTag;
        /// <summary>
        /// The tag of this GameObject.
        /// </summary>
        [Json]
        public new string tag
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.tag : gameObject.tag; }
            set
            {
                string oldValue = tag;
                if (HasChanged(value, oldValue))
                {
                    if (objectAdditionalFallbackValues != null)
                        objectAdditionalFallbackValues.tag = value;
                    else
                        _lastTag = gameObject.tag = value;

                    PropertyAssigned(this, nameof(tag), value, oldValue);
                }
            }
        }

        /// <summary>
        /// When enabled, the <see cref="UnityEngine.GameObject"/> will not be displayed in the hierarchy.
        /// </summary>
        [Json]
        public virtual bool isHiddenInHierarchy
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.isHiddenInHierarchy : _isHiddenInHierarchy; }
            set
            {
                SetValue(nameof(isHiddenInHierarchy), value, ref _isHiddenInHierarchy, (newValue, oldValue) =>
                {
                    if (objectAdditionalFallbackValues != null)
                        objectAdditionalFallbackValues.isHiddenInHierarchy = newValue;

                    UpdateHideFlags();
                });
            }
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.AnimatorBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createAnimatorIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createAnimatorIfMissing : false; }
            set 
            { 
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createAnimatorIfMissing), value, ref objectAdditionalFallbackValues.createAnimatorIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the animator.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public SerializableGuid animatorId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.animatorId : SerializableGuid.Empty; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(animatorId), value, ref objectAdditionalFallbackValues.animatorId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesAnimator();
                    });
                }
            }
        }

        private void UpdateFallbackValuesAnimator()
        {
            objectAdditionalFallbackValues.animator = GetComponentFromId<AnimatorBase>(animatorId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.ControllerBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createControllerIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createControllerIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createControllerIfMissing), value, ref objectAdditionalFallbackValues.createControllerIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the controller.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public SerializableGuid controllerId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.controllerId : SerializableGuid.Empty; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(controllerId), value, ref objectAdditionalFallbackValues.controllerId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesController();
                    });
                }
            }
        }

        private void UpdateFallbackValuesController()
        {
            objectAdditionalFallbackValues.controller = GetComponentFromId<ControllerBase>(controllerId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.GeneratorBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createGeneratorIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createGeneratorIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createGeneratorIfMissing), value, ref objectAdditionalFallbackValues.createGeneratorIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the generators.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> generatorsId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.generatorsId : null; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(generatorsId), value, ref objectAdditionalFallbackValues.generatorsId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesGenerators();
                    });
                }
            }
        }

        private void UpdateFallbackValuesGenerators()
        {
            objectAdditionalFallbackValues.generators = GetComponentFromId<GeneratorBase>(generatorsId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.ReferenceBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createReferenceIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createReferenceIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createReferenceIfMissing), value, ref objectAdditionalFallbackValues.createReferenceIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the references.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> referencesId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.referencesId : null; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(referencesId), value, ref objectAdditionalFallbackValues.referencesId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesReferences();
                    });
                }
            }
        }

        private void UpdateFallbackValuesReferences()
        {
            objectAdditionalFallbackValues.references = GetComponentFromId<ReferenceBase>(referencesId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.EffectBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createEffectIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createEffectIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createEffectIfMissing), value, ref objectAdditionalFallbackValues.createEffectIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the effects.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> effectsId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.effectsId : null; }
            set
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(effectsId), value, ref objectAdditionalFallbackValues.effectsId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesEffects();
                    });
                }
            }
        }

        private void UpdateFallbackValuesEffects()
        {
            objectAdditionalFallbackValues.effects = GetComponentFromId<EffectBase>(effectsId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.FallbackValues"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createFallbackValuesIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createFallbackValuesIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createFallbackValuesIfMissing), value, ref objectAdditionalFallbackValues.createFallbackValuesIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the fallbackValues.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> fallbackValuesId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.fallbackValuesId : null; }
            set
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(fallbackValuesId), value, ref objectAdditionalFallbackValues.fallbackValuesId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesFallbackValues();
                    });
                }
            }
        }

        private void UpdateFallbackValuesFallbackValues()
        {
            objectAdditionalFallbackValues.fallbackValues = GetComponentFromId<FallbackValues>(fallbackValuesId);
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.DatasourceBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createDatasourceIfMissing
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.createDatasourceIfMissing : false; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createDatasourceIfMissing), value, ref objectAdditionalFallbackValues.createDatasourceIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the datasources.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> datasourcesId
        {
            get { return objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.datasourcesId : null; }
            set 
            {
                if (objectAdditionalFallbackValues != null)
                {
                    SetValue(nameof(datasourcesId), value, ref objectAdditionalFallbackValues.datasourcesId, (newValue, oldValue) =>
                    {
                        UpdateFallbackValuesDatasources();
                    });
                }
            }
        }

        private void UpdateFallbackValuesDatasources()
        {
            objectAdditionalFallbackValues.datasources = GetComponentFromId<DatasourceBase>(datasourcesId);
        }

        /// <summary>
        /// The <see cref="DepictionEngine.AnimatorBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(animatorJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public AnimatorBase animator
        {
            get { return _animator; }
            private set { SetValue(nameof(animator), value, ref _animator); }
        }

        /// <summary>
        /// The <see cref="DepictionEngine.ControllerBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(controllerJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public ControllerBase controller
        {
            get { return _controller; }
            private set { SetController(value); }
        }

        protected virtual bool SetController(ControllerBase value)
        {
            return SetValue(nameof(controller), value, ref _controller, (newValue, oldValue) =>
            {
                if (HasChanged(newValue, oldValue))
                    UpdateGeoController();
            });
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.GeneratorBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(generatorsJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public List<GeneratorBase> generators
        {
            get 
            {
                if (_generators == null)
                    _generators = new List<GeneratorBase>();
                return _generators; 
            }
            private set { SetValue(nameof(generators), value, ref _generators); }
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.ReferenceBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(referencesJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public List<ReferenceBase> references
        {
            get 
            {
                if (_references == null)
                    _references = new List<ReferenceBase>();
                return _references; 
            }
            private set { SetValue(nameof(references), value, ref _references); }
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.EffectBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(effectsJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public List<EffectBase> effects
        {
            get 
            {
                if (_effects == null)
                    _effects = new List<EffectBase>();
                return _effects; 
            }
            private set { SetValue(nameof(effects), value, ref _effects); }
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.FallbackValues"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(fallbackValuesJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public List<FallbackValues> fallbackValues
        {
            get 
            {
                if (_fallbackValues == null)
                    _fallbackValues = new List<FallbackValues>();
                return _fallbackValues; 
            }
            private set { SetValue(nameof(fallbackValues), value, ref _fallbackValues); }
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.DatasourceBase"/> used by this object.
        /// </summary>
        [Json(propertyName: nameof(datasourcesJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public List<DatasourceBase> datasources
        {
            get 
            {
                if (_datasources == null)
                    _datasources = new List<DatasourceBase>();
                return _datasources; 
            }
            private set { SetValue(nameof(datasources), value, ref _datasources); }
        }

        private JSONNode transformJson
        {
            get { return JsonUtility.ToJson(GetComponent<TransformBase>()); }
        }

        private JSONNode animatorJson
        {
            get { return JsonUtility.ToJson(GetComponent<AnimatorBase>()); }
        }

        private JSONNode controllerJson
        {
            get { return JsonUtility.ToJson(GetComponent<ControllerBase>()); }
        }

        private JSONNode generatorsJson
        {
            get { return JsonUtility.ToJson(GetComponents<GeneratorBase>()); }
        }

        private JSONNode referencesJson
        {
            get { return JsonUtility.ToJson(GetComponents<ReferenceBase>()); }
        }

        private JSONNode effectsJson
        {
            get { return JsonUtility.ToJson(GetComponents<EffectBase>()); }
        }

        private JSONNode fallbackValuesJson
        {
            get { return JsonUtility.ToJson(GetComponents<FallbackValues>()); }
        }

        private JSONNode datasourcesJson
        {
            get { return JsonUtility.ToJson(GetComponents<DatasourceBase>()); }
        }

        protected ReferenceBase AppendToReferenceComponentName(ReferenceBase reference, string postfix)
        {
#if UNITY_EDITOR
            if (reference != null)
                reference.inspectorComponentNameOverride = Editor.ObjectNames.GetInspectorTitle(reference, true) + " (" + postfix + ")";
#endif
            return reference;
        }

        public ReferenceBase GetReferenceAt(int index)
        {
            return references.Count > index ? references[index] : null;
        }

        protected override bool SetParent(PropertyMonoBehaviour value)
        {
            if (base.SetParent(value))
            {
                transform = !IsDisposing() ? value as TransformDouble : null;

                return true;
            }
            return false;
        }

        public void SetParent(TransformBase parent, bool worldPositionStays = true)
        {
            if (transform != Disposable.NULL)
                transform.SetParent(parent, worldPositionStays);
        }

        public TargetControllerBase targetController
        {
            get { return controller as TargetControllerBase; }
            set { SetTargetController(value); }
        }

        protected virtual bool SetTargetController(TargetControllerBase value)
        {
            if (Object.ReferenceEquals(_targetController, value))
                return false;

            _targetController = value;

            UpdateGeoController();

            return true;
        }

        [Json(propertyName: nameof(transformJson), conditionalMethod: nameof(IsNotFallbackValues))]
        public new TransformDouble transform
        {
            get { return _transform; }
            protected set { SetTransform(value); }
        }

        protected virtual bool SetTransform(TransformDouble value)
        {
            return SetValue(nameof(transform), value, ref _transform, (newValue, oldValue) =>
            {
                RemoveTransformDelegate(oldValue);
                AddTransformDelegate(newValue);

                UpdateParentGeoAstroObject();
            });
        }

        protected int GetScriptCount()
        {
            int scriptCount = 0;

            if (animator != Disposable.NULL)
                scriptCount++;
            if (controller != Disposable.NULL)
                scriptCount++;
            if (generators != null)
            {
                foreach (GeneratorBase generator in generators)
                {
                    if (generator != Disposable.NULL)
                        scriptCount++;
                }
            }
            if (references != null)
            {
                foreach (ReferenceBase reference in references)
                {
                    if (reference != Disposable.NULL)
                        scriptCount++;
                }
            }
            if (effects != null)
            {
                foreach (EffectBase effect in effects)
                {
                    if (effect != Disposable.NULL)
                        scriptCount++;
                }
            }
            if (fallbackValues != null)
            {
                foreach (FallbackValues fallbackValue in fallbackValues)
                {
                    if (fallbackValue != Disposable.NULL)
                        scriptCount++;
                }
            }
            if (datasources != null)
            {
                foreach (DatasourceBase datasource in datasources)
                {
                    if (datasource != Disposable.NULL)
                        scriptCount++;
                }
            }

            return scriptCount;
        }

        protected void IterateOverComponents(Action<PropertyMonoBehaviour> callback)
        {
            //Transform
            TriggerCallback(transform, callback);

            //Scripts
            TriggerCallback(animator, callback);
            TriggerCallback(controller, callback);
            ListTriggerCallback(generators, callback);
            ListTriggerCallback(references, callback);
            ListTriggerCallback(effects, callback);
            ListTriggerCallback(fallbackValues, callback);
            ListTriggerCallback(datasources, callback);
        }

        public void IterateOverEffects<T>(Func<T, bool> callback) where T : EffectBase
        {
            if (effects != null)
            {
                foreach (EffectBase effect in effects)
                {
                    if (effect != Disposable.NULL && effect is T)
                    {
                        if (!callback(effect as T))
                            return;
                    }
                }
            }
        }

        public void IterateOverGenerators<T>(Func<T, bool> callback) where T : GeneratorBase
        {
            if (generators != null)
            {
                foreach (GeneratorBase generator in generators)
                {
                    if (generator != Disposable.NULL && generator.isActiveAndEnabled && generator is T)
                    {
                        if (!callback(generator as T))
                            return;
                    }
                }
            }
        }

        public void IterateOverReferences<T>(Func<T, bool> callback) where T : ReferenceBase
        {
            if (references != null)
            {
                foreach (ReferenceBase reference in references)
                {
                    if (reference != Disposable.NULL && reference is T)
                    {
                        if (!callback(reference as T))
                            return;
                    }
                }
            }
        }

        public T CreateScript<T>(InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically) where T : Script
        {
            return CreateScript(typeof(T), null, initializingState) as T;
        }

        public T CreateScript<T>(JSONNode json, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically) where T : Script
        {
            return CreateScript(typeof(T), json, initializingState) as T;
        }

        public Script CreateScript(Type type, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically, bool isFallbackValues = false)
        {
            return CreateScript(type, null, initializingState, isFallbackValues);
        }

        public Script CreateScript(Type type, JSONNode json, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically, bool isFallbackValues = false)
        {
            if (!type.IsSubclassOf(typeof(Script)))
                return null;
            return gameObject.AddSafeComponent(type, initializingState, json, null, isFallbackValues) as Script;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (string,object) GetProperty(IProperty property)
        {
            if (property is TransformBase)
                return (nameof(transform), transform);
            else if (property is AnimatorBase)
                return (nameof(animator), animator);
            else if (property is ControllerBase)
                return (nameof(controller), controller);
            else if (property is GeneratorBase)
                return (nameof(generators), generators);
            else if (property is ReferenceBase)
                return (nameof(references), references);
            else if (property is EffectBase)
                return (nameof(effects), effects);
            else if (property is FallbackValues)
                return (nameof(fallbackValues), fallbackValues);
            else if (property is DatasourceBase)
                return (nameof(datasources), datasources);
            return (null,null);
        }

        protected override bool AddProperty(PropertyMonoBehaviour child)
        {
            bool added = false;

            Originator(() =>
            {
                if (child is Script)
                    added = AddScript(child as Script);
                else
                {
                    if (base.AddProperty(child))
                        added = true;
                }
            }, child);

            return added;
        }

        protected virtual bool AddScript(Script script)
        {
            bool hasChanged = false;

            if (script is AnimatorBase)
                hasChanged = animator = script as AnimatorBase;
            else if (script is ControllerBase)
                hasChanged = controller = script as ControllerBase;
            else
            {
                if (script is GeneratorBase)
                    hasChanged = AddGenerator(script as GeneratorBase);
                else if (script is ReferenceBase)
                    hasChanged = AddReference(script as ReferenceBase);
                else if (script is EffectBase)
                    hasChanged = AddEffects(script as EffectBase);
                else if (script is FallbackValues)
                    hasChanged = AddFallbackValues(script as FallbackValues);
                else if (script is DatasourceBase)
                    hasChanged = AddDatasource(script as DatasourceBase);

                if (hasChanged)
                {
                    (string, object) localProperty = GetProperty(script);
                    PropertyAssigned(this, localProperty.Item1, localProperty.Item2, null);
                }
            }

            if (hasChanged)
            {
                if (ScriptAddedEvent != null)
                    ScriptAddedEvent(this, script);

                AddScriptDelegate(script);
            }

            return hasChanged;
        }

        protected virtual bool AddGenerator(GeneratorBase generator)
        {
            if (!generators.Contains(generator))
            {
                generators.Add(generator);
                return true;
            }
            return false;
        }

        protected virtual bool AddReference(ReferenceBase reference)
        {
            if (!references.Contains(reference))
            {
                references.Add(reference);

                UpdateReferences();

                AddLoadScopeDataReferenceDelegate(reference);

                return true;
            }
            return false;
        }

        private bool AddEffects(EffectBase effect)
        {
            if (!effects.Contains(effect))
            {
                effects.Add(effect);
                return true;
            }
            return false;
        }

        protected virtual bool AddFallbackValues(FallbackValues fallbackValue)
        {
            if (!fallbackValues.Contains(fallbackValue))
            {
                fallbackValues.Add(fallbackValue);
                return true;
            }
            return false;
        }

        private bool AddDatasource(DatasourceBase datasource)
        {
            if (!datasources.Contains(datasource))
            {
                datasources.Add(datasource);
                return true;
            }
            return false;
        }

        protected override bool RemoveProperty(PropertyMonoBehaviour child)
        {
            bool removed = false;

            Originator(() =>
            {
                if (child is Script)
                    removed = RemoveScript(child as Script);
                else
                {
                    if (base.RemoveProperty(child))
                        removed = true;
                }
            }, child);

            return removed;
        }

        protected virtual bool RemoveScript(Script script)
        {
            bool hasChanged = false;

            if (Object.ReferenceEquals(script, animator))
                hasChanged = animator = null;
            else if (Object.ReferenceEquals(script, controller))
                hasChanged = controller = null;
            else
            {
                if (script is GeneratorBase)
                    hasChanged = RemoveGenerator(script as GeneratorBase);
                else if (script is ReferenceBase)
                    hasChanged = RemoveReference(script as ReferenceBase);
                else if (script is EffectBase)
                    hasChanged = RemoveEffect(script as EffectBase);
                else if (script is FallbackValues)
                    hasChanged = RemoveFallbackValues(script as FallbackValues);
                else if (script is DatasourceBase)
                    hasChanged = RemoveDatasource(script as DatasourceBase);

                if (hasChanged)
                {
                    (string, object) localProperty = GetProperty(script);
                    PropertyAssigned(this, localProperty.Item1, localProperty.Item2, null);
                }
            }

            if (hasChanged)
            {
                if (ScriptRemovedEvent != null)
                    ScriptRemovedEvent(this, script);

                RemoveScriptDelegate(script);
            }

            return hasChanged;
        }

        protected virtual bool RemoveGenerator(GeneratorBase generator)
        {
            return RemoveListItem(generators, generator);
        }

        protected virtual bool RemoveReference(ReferenceBase reference)
        {
            if (RemoveListItem(references, reference))
            {
                RemoveLoadScopeDataReferenceDelegate(reference);

                UpdateReferences();

                return true;
            }
            return false;
        }

        private bool RemoveEffect(EffectBase effect)
        {
            return RemoveListItem(effects, effect);
        }

        protected bool RemoveFallbackValues(FallbackValues fallbackValue)
        {
            return RemoveListItem(fallbackValues, fallbackValue);
        }

        private bool RemoveDatasource(DatasourceBase datasource)
        {
            return RemoveListItem(datasources, datasource);
        }

        public T CreateChild<T>(string name = null, Transform parent = null, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, List<PropertyModifier> propertyModifiers = null) where T : IScriptableBehaviour
        {
            return (T)CreateChild(typeof(T), name, parent, initializationState, propertyModifiers);
        }

        public IScriptableBehaviour CreateChild(Type type, string name = null, Transform parent = null, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, List<PropertyModifier> propertyModifiers = null)
        {
            IScriptableBehaviour scriptableBehaviour = instanceManager.CreateInstance(type, parent != null ? parent : gameObject.transform, initializingState: initializationState, propertyModifiers: propertyModifiers) as IScriptableBehaviour;
            
            if (!string.IsNullOrEmpty(name))
                scriptableBehaviour.name = name;

            return scriptableBehaviour;
        }

        public void SetGridProperties(int loadScopeInstanceId, VisibleCameras visibleCamerasInstanceId)
        {
            bool visibleCamerasChanged = false;

            if (visibleCamerasInstanceId != null)
            {
                if (!visibleCameras.TryGetValue(loadScopeInstanceId, out VisibleCameras currentVisibleCamerasInstanceId) || currentVisibleCamerasInstanceId != visibleCamerasInstanceId)
                {
                    visibleCameras[loadScopeInstanceId] = visibleCamerasInstanceId;
                    visibleCamerasChanged = true;
                }
            }
            else if (visibleCameras.Remove(loadScopeInstanceId))
                visibleCamerasChanged = true;

            if (visibleCamerasChanged)
                VisibleCamerasChanged();
        }

        protected virtual void VisibleCamerasChanged()
        {

        }

        public bool CameraIsMasked(Camera camera)
        {
            bool masked = false;

            if (camera != Disposable.NULL)
            {
                masked = visibleCameras.Count > 0;

                foreach (VisibleCameras visibleCamera in visibleCameras.Values)
                {
                    if (visibleCamera != null && visibleCamera.CameraVisible(camera))
                    {
                        masked = false;
                        break;
                    }
                }
            }

            return masked;
        }


        private void UpdateGeoController()
        {
            GeoCoordinateController geoCoordinateController = controller as GeoCoordinateController;
            if (geoCoordinateController != Disposable.NULL)
            {
                Camera camera = null;

                if (targetController != Disposable.NULL)
                {
                    if (targetController is CameraController)
                        camera = (targetController as CameraController).objectBase as Camera;
#if UNITY_EDITOR
                    if (targetController is Editor.SceneCameraController)
                        camera = (targetController as Editor.SceneCameraController).GetComponent<Camera>();
#endif
                }
               
                geoCoordinateController.filterTerrainCamera = camera;
            }
        }

        public virtual bool IsPhysicsObject()
        {
            return true;
        }

        public virtual bool IsPhysicsDriven()
        {
            return Application.isPlaying && rigidbodyInternal != null;
        }

        private Rigidbody rigidbodyInternal
        {
            get { return _rigidbodyInternal; }
            set
            {
                if (_rigidbodyInternal == value)
                    return;
                _rigidbodyInternal = value;
            }
        }

        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (!Object.ReferenceEquals(animator, null) && TriggerCallback(animator, callback))
                containsDisposed = true;

            if (!Object.ReferenceEquals(controller, null) && TriggerCallback(controller, callback))
                containsDisposed = true;

            //Effects such as Atmosphere need to be executed before loaders
            if (effects != null)
            {
                for (int i = effects.Count - 1; i >= 0 ; i--)
                {
                    if (TriggerCallback(effects[i], callback))
                        containsDisposed = true;
                }
            }

            if (generators != null)
            {
                for (int i = generators.Count - 1; i >= 0; i--)
                {
                    if (TriggerCallback(generators[i], callback))
                        containsDisposed = true;
                }
            }

            if (references != null)
            {
                for (int i = references.Count - 1; i >= 0; i--)
                {
                    if (TriggerCallback(references[i], callback))
                        containsDisposed = true;
                }
            }

            if (fallbackValues != null)
            {
                for (int i = fallbackValues.Count - 1; i >= 0; i--)
                {
                    if (TriggerCallback(fallbackValues[i], callback))
                        containsDisposed = true;
                }
            }

            if (datasources != null)
            {
                for (int i = datasources.Count - 1; i >= 0; i--)
                {
                    if (TriggerCallback(datasources[i], callback))
                        containsDisposed = true;
                }
            }

            return containsDisposed;
        }

        public override void HierarchicalFixedUpdate()
        {
            base.HierarchicalFixedUpdate();

            if (IsPhysicsDriven())
            {
                if (useGravity)
                {
                    instanceManager.IterateOverInstances<AstroObject>(
                        (astroObject) =>
                        {
                            Vector3 forceVector = astroObject.GetGravitationalForce(this);
                            if (!forceVector.Equals(Vector3.zero))
                                rigidbodyInternal.AddForce(forceVector, ForceMode.Impulse);

                            return true;
                        });
                }
            }
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                ForceUpdateTransformIfPending();

                return true;
            }
            return false;
        }

        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                ForceUpdateTransformIfPending();

                return true;
            }
            return false;
        }

        private ComponentChangedPending _forceUpdateTransformPending;
        public ComponentChangedPending forceUpdateTransformPending
        {
            get
            {
                if (_forceUpdateTransformPending == null)
                    _forceUpdateTransformPending = new ComponentChangedPending();
                return _forceUpdateTransformPending;
            }
        }

        public void ClearForceUpdateTransformPending(bool clearLocalPosition = false, bool clearLocalRotation = false, bool clearLocalScale = false)
        {
            ComponentChangedPending forceUpdateTransformPending = this.forceUpdateTransformPending;

            if (clearLocalPosition)
                forceUpdateTransformPending.localPositionChanged = false;
            if (clearLocalRotation)
                forceUpdateTransformPending.localRotationChanged = false;
            if (clearLocalScale)
                forceUpdateTransformPending.localScaleChanged = false;

            if (!forceUpdateTransformPending.localPositionChanged && !forceUpdateTransformPending.localRotationChanged && !forceUpdateTransformPending.localScaleChanged)
                forceUpdateTransformPending.pending = false;
        }

        public void ForceUpdateTransformPending(bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false)
        {
            ComponentChangedPending forceUpdateTransformPending = this.forceUpdateTransformPending;

            if (localPositionChanged)
                forceUpdateTransformPending.localPositionChanged = true;
            if (localRotationChanged)
                forceUpdateTransformPending.localRotationChanged = true;
            if (localScaleChanged)
                forceUpdateTransformPending.localScaleChanged = true;

            if (forceUpdateTransformPending.localPositionChanged || forceUpdateTransformPending.localRotationChanged || forceUpdateTransformPending.localScaleChanged)
                forceUpdateTransformPending.pending = true;
        }

        public void ForceUpdateTransformIfPending()
        {
            ComponentChangedPending forceUpdateTransformPending = this.forceUpdateTransformPending;

            if (forceUpdateTransformPending.pending)
                ForceUpdateTransform(forceUpdateTransformPending.localPositionChanged, forceUpdateTransformPending.localRotationChanged, forceUpdateTransformPending.localScaleChanged);
        }

        public bool ForceUpdateTransform(bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false, Camera camera = null)
        {
            if (initialized && transform != Disposable.NULL && transform.ForceUpdateTransform(out TransformBase.Component changeComponents, localPositionChanged, localRotationChanged, localScaleChanged, camera))
            {
                forceUpdateTransformPending.Clear();
                return true;
            }
            return false;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                if (isHiddenInHierarchy)
                {
                    bool debug = false;

                    if (!SceneManager.IsSceneBeingDestroyed())
                        debug = sceneManager.debug;

                    if (!debug)
                        gameObject.hideFlags |= HideFlags.HideInHierarchy;
                }

                if (transform != Disposable.NULL)
                    transform.UpdateTransformHideFlags();

                return true;
            }
            return false;
        }

        protected void DisposeDataProcessor(Processor dataProcessor)
        {
            if (dataProcessor != null)
                dataProcessor.Dispose();
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                Dispose(objectAdditionalFallbackValues);

                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class VisibleCameras
    {
        [SerializeField]
        private List<int> _values;

        public VisibleCameras(int[] values)
        {
            _values = new List<int>(values);
        }

        public bool CameraVisible(Camera camera)
        {
            return _values.Contains(camera.GetCameraInstanceId());
        }

        public bool SequenceEqual(List<int> other)
        {
            return Enumerable.SequenceEqual(_values, other);
        }

        public static implicit operator VisibleCameras(int[] d) 
        {
            return new VisibleCameras(d);
        }
    };
}
