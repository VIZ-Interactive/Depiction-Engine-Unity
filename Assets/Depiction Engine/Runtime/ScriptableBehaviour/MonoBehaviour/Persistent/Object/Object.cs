// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    /// <summary>
    /// Main component used to interface with the GameObject / Scripts and children. Only one per GameObject supported. Objects are usually not origin shifted, use <see cref="DepictionEngine.VisualObject"/> if you are looking for a container to use with MeshRenderers.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(Object))]
    [RequireComponent(typeof(TransformDouble))]
    public class Object : PersistentMonoBehaviour, IRequiresComponents
    {
        [Serializable]
        private class VisibleInCamerasDictionary : SerializableDictionary<int, VisibleCameras> { };

        [SerializeField, ConditionalShow(nameof(IsFallbackValues))]
        private ObjectAdditionalFallbackValues _objectAdditionalFallbackValues;

        [BeginFoldout("Meta")]
        [SerializeField, Tooltip("General purpose metadata associated with the object, used for searching or otherwise."), EndFoldout]
        private string _tags;

        [BeginFoldout("Physics")]
        [SerializeField, ConditionalShow(nameof(IsPhysicsObject)), Tooltip("When enabled the GameObject will automatically reacts to '"+nameof(AstroObject)+"' gravitational pull based on 'Object.mass' and 'AstroObject.mass'.")]
        private bool _useGravity;
        [SerializeField, ConditionalShow(nameof(IsPhysicsObject)), Tooltip("Used to determine the amount of gravitational force to apply when '"+nameof(Object.useGravity)+"' is enabled."), EndFoldout]
        private double _mass;

#if UNITY_EDITOR
        [BeginFoldout("Reflection Probe")]
        [SerializeField, Button(nameof(UpdateReflectionProbeBtn)), Tooltip("Automatically add the ReflectionProbe(s) so the Object can manage them. When managed a 'customBakedTexture' will be generated, assigned and updated automatically for the ReflectionProbe if their type is set to 'ReflectionProbeMode.Custom'."), EndFoldout]
        private bool _updateReflectionProbe;
#endif

        [SerializeField, HideInInspector]
        private bool _isHiddenInHierarchy;
      
        [SerializeField, HideInInspector]
        private VisibleInCamerasDictionary _visibleInCameras;

        [SerializeField, HideInInspector]
        private Rigidbody _rigidbodyInternal;

        [SerializeField, HideInInspector]
        private List<ReflectionProbe> _managedReflectionProbes;

        [SerializeField, HideInInspector]
        private RenderTexture _reflectionCustomBakedTexture;

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

        /// <summary>
        /// Dispatched when a child such as <see cref="DepictionEngine.TransformDouble"/> or <see cref="DepictionEngine.MeshRendererVisual"/>(for <see cref="DepictionEngine.VisualObject"/>) is added.
        /// </summary>
        public Action<Object, PropertyMonoBehaviour> ChildAddedEvent;
        /// <summary>
        /// Dispatched when a child such as <see cref="DepictionEngine.TransformDouble"/> or <see cref="DepictionEngine.MeshRendererVisual"/>(for <see cref="DepictionEngine.VisualObject"/>) is removed.
        /// </summary>
        public Action<Object, PropertyMonoBehaviour> ChildRemovedEvent;
        /// <summary>
        /// Dispatched when a property assign event is detected in the <see cref="DepictionEngine.Object"/> of one of the child <see cref="DepictionEngine.TransformDouble"/>.
        /// </summary>
        public Action<Object, string, object, object> ChildObjectPropertyAssignedEvent;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.Script"/> is added.
        /// </summary>
        public Action<Object, Script> ScriptAddedEvent;
        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.Script"/> is removed.
        /// </summary>
        public Action<Object, Script> ScriptRemovedEvent;
        /// <summary>
        /// Dispatched when a property assign event is detected in any of the <see cref="DepictionEngine.Script"/> or <see cref="DepictionEngine.TransformDouble"/>.
        /// </summary>
        public Action<Object, IJson, string, object, object> ComponentPropertyAssignedEvent;

        /// <summary>
        /// Dispatched after changes to the <see cref="DepictionEngine.TransformDouble.localPosition"/>, <see cref="DepictionEngine.TransformDouble.localRotation"/> or <see cref="DepictionEngine.TransformDouble.localScale"/> have been detected. 
        /// </summary>
        public Action<TransformBase.Component, TransformBase.Component> TransformChangedEvent;
        /// <summary>
        /// Dispatched when a property assign event is detected in the <see cref="DepictionEngine.TransformDouble"/>.
        /// </summary>
        public Action<TransformBase, string, object, object> TransformPropertyAssignedEvent;

        /// <summary>
        /// Dispatched when a property assign event is detected in the parent <see cref="DepictionEngine.GeoAstroObject"/>.
        /// </summary>
        public Action<GeoAstroObject, string, object, object> ParentGeoAstroObjectPropertyAssignedEvent;

        /// <summary>
        /// A callback triggered by changes in the <see cref="DepictionEngine.TransformDouble"/> allowing you to make changes to the new values before they are assigned.
        /// </summary>
        public Action<LocalPositionParam, LocalRotationParam, LocalScaleParam, Camera> TransformControllerCallback;

#if UNITY_EDITOR
        private void UpdateReflectionProbeBtn()
        {
            UpdateReflectionProbe(out int added, out int removed);

            UnityEditor.EditorUtility.DisplayDialog("Updated", "Found a total of " + managedReflectionProbes.Count + " ReflectionProbe(s).                Added: " + added + "                                                Removed: " + removed, "OK");
        }
#endif

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

            _rigidbodyInternal = null;

            gameObject.layer = LayerMask.GetMask("Default");
            gameObject.hideFlags = default;

            _forceUpdateTransformPending?.Clear();

            _targetController = default;

            _visibleInCameras?.Clear();

            _managedReflectionProbes?.Clear();
        }

        protected override bool Initialize(InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                UpdateReferences(true);

                return true;
            }
            return false;
        }

        protected override void DestroyAfterFailedInitialization()
        {
            DisposeManager.Destroy(gameObject);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastGameObjectActiveSelf = gameObjectActiveSelf;
                _lastLayer = layer;
                _lastTag = tag;
#if UNITY_EDITOR
                _lastUseGravity = useGravity;
#endif

                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                _reflectionCustomBakedTexture = null;

            if (isFallbackValues)
                InitValue(value => objectAdditionalFallbackValues = value, CreateOptionalProperties<ObjectAdditionalFallbackValues>(initializingContext), initializingContext);

            if (objectAdditionalFallbackValues != null)
            {
                InitValue(value => createAnimatorIfMissing = value, true, initializingContext);
                InitValue(value => animatorId = value, SerializableGuid.Empty, () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.animatorId, objectAdditionalFallbackValues.animator, initializingContext), initializingContext);

                InitValue(value => createControllerIfMissing = value, true, initializingContext);
                InitValue(value => controllerId = value, SerializableGuid.Empty, () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.controllerId, objectAdditionalFallbackValues.controller, initializingContext), initializingContext);

                InitValue(value => createGeneratorIfMissing = value, true, initializingContext);
                InitValue(value => generatorsId = value, new List<SerializableGuid>(), () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.generatorsId, objectAdditionalFallbackValues.generators, initializingContext), initializingContext);

                InitValue(value => createReferenceIfMissing = value, true, initializingContext);
                InitValue(value => referencesId = value, new List<SerializableGuid>(), () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.referencesId, objectAdditionalFallbackValues.references, initializingContext), initializingContext);

                InitValue(value => createEffectIfMissing = value, true, initializingContext);
                InitValue(value => effectsId = value, new List<SerializableGuid>(), () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.effectsId, objectAdditionalFallbackValues.effects, initializingContext), initializingContext);

                InitValue(value => createFallbackValuesIfMissing = value, true, initializingContext);
                InitValue(value => fallbackValuesId = value, new List<SerializableGuid>(), () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.fallbackValuesId, objectAdditionalFallbackValues.fallbackValues, initializingContext), initializingContext);

                InitValue(value => createDatasourceIfMissing = value, true, initializingContext);
                InitValue(value => datasourcesId = value, new List<SerializableGuid>(), () => GetDuplicateComponentReferenceId(objectAdditionalFallbackValues.datasourcesId, objectAdditionalFallbackValues.datasources, initializingContext), initializingContext);
            }

            InitValue(value => tags = value, "", initializingContext);
            InitValue(value => useGravity = value, false, initializingContext);
            InitValue(value => mass = value, GetDefaultMass(), initializingContext);
            InitValue(value => layer = value, LayerUtility.GetDefaultLayer(GetType()), initializingContext);
            InitValue(value => tag = value, "Untagged", initializingContext);
            InitValue(value => isHiddenInHierarchy = value, GetDefaultIsHiddenInHierarchy(), initializingContext);
        }

        protected T CreateOptionalProperties<T>(InitializationContext initializingContext) where T : OptionalPropertiesBase
        {
            OptionalPropertiesBase optionalProperties = ScriptableObject.CreateInstance<T>();
            optionalProperties.parent = this;

#if UNITY_EDITOR
            Editor.UndoManager.QueueRegisterCreatedObjectUndo(optionalProperties, initializingContext);
#endif

            return optionalProperties as T;
        }

        protected override bool AddToInstanceManager()
        {
            if (base.AddToInstanceManager())
            {
                RegisterWithRenderingManager();

                RegisterWithPhysicsManager();
                return true;
            }
            return false;
        }

        public override bool UpdateRelations(Action beforeSiblingsInitializeCallback = null)
        {
            if (initializationJson != null)
            {
                JSONObject transformJson = initializationJson[nameof(transform)] as JSONObject;
                if (transformJson != null)
                {
                    TransformDouble transform = gameObject.GetComponent<TransformDouble>();
                    if (transform != Disposable.NULL)
                        InitializeComponent(transform, transformJson);
                }
            }

            return base.UpdateRelations(beforeSiblingsInitializeCallback);
        }

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            Component[] components;

            GetRequiredComponentTypes(ref _requiredComponentTypes);
            List<Type> requiredComponentTypes = _requiredComponentTypes;
            if (requiredComponentTypes.Count > 0)
            {
                components = GetComponents<Component>();

                foreach (Type requiredComponentType in requiredComponentTypes)
                {
                    Component component = RemoveComponentFromList(requiredComponentType, components);
                    if (Disposable.IsDisposed(component))
                    {
                        //We do not register a create Undo here because when the Undo is executed it breaks the dispose process because the id is no longer set.
                        //The problem was found when manually adding TerrainGridMeshObject to a GameObject in the Editor and undoing the Add Component.
                        //Three of the four AssetReference could not be removed from the instanceManager because their id were missing when OnDispose was triggered.
                        component = AddComponent(requiredComponentType, false);
                    }
                }
            }

            components = GetComponents<MonoBehaviourDisposable>();

            if (initializationJson != null)
            {
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
                            CreateScript(animatorType, animatorJson, initializingContext);
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
                            CreateScript(controllerType, controllerJson, initializingContext);
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
                                CreateScript(generatorType, generatorJson, initializingContext);
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
                                CreateScript(referenceType, referenceJson, initializingContext);
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
                                CreateScript(effectType, effectJson, initializingContext);
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
                                CreateScript(fallbackValueType, fallbackValueJson, initializingContext);
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
                                CreateScript(datasourceType, datasourceJson, initializingContext);
                        }
                    }
                }
            }

            for (int i = components.Length - 1; i >= 0; i--)
                InitializeComponent(components[i]);
        }

        protected void UpdateRigidbody()
        {
            if (!isFallbackValues)
            {
                if (useGravity)
                {
                    if (rigidbodyInternal == null)
                    {
                        InitializationContext initializingContext = SceneManager.GetIsUserChangeContext() ? InitializationContext.Editor : InitializationContext.Programmatically;
                        rigidbodyInternal = AddComponent<Rigidbody>(initializingContext);
                        rigidbodyInternal.drag = 0.05f;
                        rigidbodyInternal.useGravity = false;
                    }
                }
                else
                    DisposeManager.Destroy(rigidbodyInternal, SceneManager.GetIsUserChangeContext() ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Destroy);
                
                RegisterWithPhysicsManager();
            }
        }

        private List<Type> _requiredComponentTypes = new();
        protected Component RemoveComponentFromList(Type type, Component[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!Disposable.IsDisposed(component))
                {
                    if (type.IsAssignableFrom(component.GetType()))
                    {
                        components[i] = null;
                        return component;
                    }
                }
            }
            return null;
        }

        public void GetRequiredComponentTypes(ref List<Type> types)
        {
            MemberUtility.GetRequiredComponentTypes(ref types, GetType());
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            ForceUpdateTransform(true, true, true);
        }

#if UNITY_EDITOR
        protected override void RegisterInitializeObjectUndo(InitializationContext initializingContext)
        {
            base.RegisterInitializeObjectUndo(initializingContext);

            //Register GameObject name/layer/enabled etc...
            Editor.UndoManager.QueueRegisterCompleteObjectUndo(gameObject, initializingContext);
        }
#endif

        protected override bool IsValidInitialization(InitializationContext initializingContext)
        {
            if (base.IsValidInitialization(initializingContext))
            {
                if (!CanBeDuplicated() && (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate))
                    return false;
                return true;
            }
            return false;
        }

        protected override bool LateInitialize(InitializationContext initializingContext)
        {
            if (base.LateInitialize(initializingContext))
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
            Component[] components = GetComponents<Component>();
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

        private bool _lastUseGravity;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                SerializationUtility.RecoverLostReferencedObject(ref _rigidbodyInternal);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => useGravity = value, ref _useGravity, ref _lastUseGravity);

                return true;
            }
            return false;
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
            return managedReflectionProbes.Count > 0;
        }

        /// <summary>
        /// Can the <see cref="UnityEngine.Object"/> be duplicated.
        /// </summary>
        /// <returns>True if the Object can be duplicated.</returns>
        protected virtual bool CanBeDuplicated()
        {
            return true;
        }

        public override bool DetectUserGameObjectChanges()
        {
            if (base.DetectUserGameObjectChanges())
            {
                if (_lastGameObjectActiveSelf != gameObjectActiveSelf)
                {
                    bool newValue = gameObjectActiveSelf;
                    ignoreGameObjectActiveChange = true;
                    gameObject.SetActive(_lastGameObjectActiveSelf);
                    ignoreGameObjectActiveChange = false;
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

                return true;
            }
            return false;
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
                        if (component is TransformBase transform)
                        {
                            if (Object.ReferenceEquals(component, transform))
                            {
                                RemoveTransformDelegate(transform);
                                AddTransformDelegate(transform);
                            }
                        }

                        if (component is Script script)
                        {
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
            if (transform is not null)
            {
                transform.ChangedEvent -= TransformChangedHandler;
                transform.PropertyAssignedEvent -= TransformPropertyAssignedHandler;
                transform.ChildAddedEvent -= TransformChildAddedHandler;
                transform.ChildRemovedEvent -= TransformChildRemovedHandler;
                transform.ChildPropertyAssignedEvent -= TransformChildPropertyChangedHandler;
                if (transform is TransformDouble transformDouble)
                    transformDouble.ObjectCallback -= TransformObjectCallbackHandler;

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
                transform.ChangedEvent += TransformChangedHandler;
                transform.PropertyAssignedEvent += TransformPropertyAssignedHandler;
                transform.ChildAddedEvent += TransformChildAddedHandler;
                transform.ChildRemovedEvent += TransformChildRemovedHandler;
                transform.ChildPropertyAssignedEvent += TransformChildPropertyChangedHandler;

                if (transform is TransformDouble transformDouble)
                    transformDouble.ObjectCallback += TransformObjectCallbackHandler;

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

        private void TransformChangedHandler(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            TransformChanged(changedComponent, capturedComponent);

            TransformChangedEvent?.Invoke(changedComponent, capturedComponent);
        }

        protected virtual bool TransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            return initialized;
        }

        protected virtual void TransformPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(newValue, oldValue) || property.IsDisposing())
                return;

            TransformBase transform = property as TransformBase;

            ComponentPropertyAssigned(transform, name, newValue, oldValue);

            if (name == nameof(TransformBase.parentGeoAstroObject))
                Originator(() => { UpdateParentGeoAstroObject(); }, property);

            TransformPropertyAssignedEvent?.Invoke(transform, name, newValue, oldValue);
        }

        private void RemoveScriptDelegate(Script script)
        {
            if (script is not null)
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

            ComponentPropertyAssigned(property as IJson, name, newValue, oldValue);
        }

        private void ComponentPropertyAssigned(IJson iJson, string name, object newValue, object oldValue)
        {
            Originator(() => { ComponentPropertyAssignedEvent?.Invoke(this, iJson, name, newValue, oldValue); }, iJson);
        }

        protected bool RemoveLoadScopeDataReferenceDelegate(ReferenceBase reference)
        {
            if (reference is not null)
            {
                reference.PropertyAssignedEvent -= ReferencePropertyAssignedHandler;
                reference.LoaderPropertyAssignedChangedEvent -= ReferenceLoaderPropertyAssignedChangedHandler;
                
                return true;
            }
            return false;
        }

        protected bool AddLoadScopeDataReferenceDelegate(ReferenceBase reference)
        {
            if (!IsDisposing() && reference != Disposable.NULL)
            {
                reference.PropertyAssignedEvent += ReferencePropertyAssignedHandler;
                reference.LoaderPropertyAssignedChangedEvent += ReferenceLoaderPropertyAssignedChangedHandler;

                return true;
            }
            return false;
        }

        private void ReferencePropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            //If the object is Disposed we do not need to update the asset references as this will switch the meshRendererVisualDirtyFlags.isDirty flags to true which will force the AutoGenerateVisualObject to regenerate its mesh if the destroy was triggered in the Editor(and the dirty state switch were recorded) and it is undone/created later.
            if (IsDisposing())
                return;

            if (name == nameof(ReferenceBase.data) || name == nameof(ReferenceBase.dataType))
                UpdateReferences();
        }

        protected virtual bool UpdateReferences(bool forceUpdate = false)
        {
            return forceUpdate || initialized;
        }

        protected T GetAssetFromAssetReference<T>(AssetReference assetReference) where T : AssetBase
        {
            return assetReference != Disposable.NULL ? assetReference.data as T : null;
        }

        protected virtual bool IterateOverAssetReferences(Func<AssetBase, AssetReference, bool, bool> callback)
        {
            return callback != null;
        }

        protected virtual void ReferenceLoaderPropertyAssignedChangedHandler(ReferenceBase reference, IProperty serializable, string name, object newValue, object oldValue)
        {

        }

        protected virtual void TransformChildAddedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
            {
                if (child != Disposable.NULL)
                    AddObjectChildDelegates((child as TransformBase).objectBase);
            }

            ChildAddedEvent?.Invoke(this, child);
        }

        protected virtual void TransformChildRemovedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
            {
                if (child is not null)
                    RemoveObjectChildDelegates((child as TransformBase).objectBase);
            }

            ChildRemovedEvent?.Invoke(this, child);
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
            if (objectBase is not null)
                objectBase.PropertyAssignedEvent -= ChildObjectPropertyAssignedHandler;
        }

        private void AddObjectChildDelegates(Object objectBase)
        {
            if (objectBase != Disposable.NULL)
                objectBase.PropertyAssignedEvent += ChildObjectPropertyAssignedHandler;
        }

        protected void ChildObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(oldValue, newValue))
                return;

            ChildObjectPropertyAssignedEvent?.Invoke(property as Object, name, newValue, oldValue);
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

            TransformControllerCallback?.Invoke(localPositionParam, localRotationParam, localScaleParam, camera);
        }

        protected virtual bool TransformObjectCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            return initialized;
        }

        protected virtual bool RemoveParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
        {
            if (parentGeoAstroObject is not null)
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

        protected virtual void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            ParentGeoAstroObjectPropertyAssignedEvent?.Invoke(property as GeoAstroObject, name, newValue, oldValue);
        }

        private void UpdateParentGeoAstroObject()
        {
            parentGeoAstroObject = transform != Disposable.NULL ? transform.parentGeoAstroObject : null;
        }

        public GeoAstroObject parentGeoAstroObject
        {
            get => _parentGeoAstroObject;
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

        /// <summary>
        /// Use this is the Object may not be initialized at the moment and we cannot rely on transform.parentObject.
        /// </summary>
        /// <returns>The parent Object or null if none exists.</returns>
        public Object GetParentObject()
        {
            return gameObject.transform != null && gameObject.transform.parent != null ? gameObject.transform.parent.GetComponent<Object>() : null;
        }

        /// <summary>
        /// Use this if the Object may not be initialized at the moment and we cannot rely on transform.IterateOverChildren().
        /// </summary>
        /// <param name="callback"></param>
        public void IterateOverChildrenObject(Func<Object, bool> callback)
        {
            foreach (Transform childTransform in gameObject.transform)
            {
                Object childObject = childTransform.GetComponent<Object>();
                if (!callback(childObject))
                    break;
            }
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
            return !base.IsFullyInitialized() || transform.parentGeoAstroObject == Disposable.NULL || transform.parentGeoAstroObject.GetLoadingInitialized();
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
                _scriptList ??= new List<Script>();
                GetComponents(_scriptList);
                int unityScriptCount = _scriptList.Count;

                int scriptCount = GetScriptCount();

                if (scriptCount != unityScriptCount)
                    siblingChanged = true;
            }

            return siblingChanged;
        }

        protected override bool GetDontSaveToScene()
        {
            bool dontSaveToScene = base.GetDontSaveToScene();

#if UNITY_EDITOR
            if (!dontSaveToScene && visibleInCameras.Count > 0)
            {
                bool visibleInCamera = false;

                instanceManager.IterateOverInstances<Camera>((camera) => 
                {
                    if (camera is not Editor.SceneCamera)
                    {
                        foreach (VisibleCameras visibleInCameras in visibleInCameras.Values)
                        {
                            if (visibleInCameras.CameraVisible(camera))
                            {
                                visibleInCamera = true;
                                break;
                            }
                        }
                    }
                    return !visibleInCamera;
                });

                if (!visibleInCamera)
                    dontSaveToScene = true;
            }
#endif

            return dontSaveToScene;
        }

        private VisibleInCamerasDictionary visibleInCameras
        {
            get { _visibleInCameras ??= new VisibleInCamerasDictionary(); return _visibleInCameras; }
        }

        protected virtual bool GetDefaultIsHiddenInHierarchy()
        {
            return isFallbackValues;
        }

        protected virtual bool CanGameObjectBeDeactivated()
        {
            return true;
        }

        public ObjectAdditionalFallbackValues objectAdditionalFallbackValues
        {
            get => _objectAdditionalFallbackValues;
            set => _objectAdditionalFallbackValues = value;
        }

        private bool _lastGameObjectActiveSelf;
        /// <summary>
        /// ActivatesDeactivates the GameObject, depending on the given true or false/ value.
        /// </summary>
        [Json]
        public bool gameObjectActiveSelf
        {
            get => gameObject.activeSelf;
            set
            {
                if (!CanGameObjectBeDeactivated())
                    value = true;

                bool oldValue = gameObjectActiveSelf;
                if (HasChanged(value, oldValue))
                {
                    gameObject.SetActive(value);
                    _lastGameObjectActiveSelf = gameObjectActiveSelf;

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
            get => _tags;
            set => SetValue(nameof(tags), value, ref _tags);
        }

        /// <summary>
        /// When enabled, the GameObject will automatically reacts to <see cref="DepictionEngine.AstroObject"/> gravitational pull based on <see cref="DepictionEngine.Object.mass"/> and <see cref="DepictionEngine.AstroObject.mass"/>.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsPhysicsObject))]
        public bool useGravity
        {
            get => _useGravity;
            set
            {
                SetValue(nameof(useGravity), IsPhysicsObject() && value, ref _useGravity, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastUseGravity = newValue;
#endif
                    UpdateRigidbody();
                });
            }
        }

        private void RegisterWithPhysicsManager()
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL)
            {
                if (rigidbodyInternal != null)
                    instanceManager.AddPhysicTransform(id, GetComponent<TransformDouble>());
                else
                    instanceManager.RemovePhysicTransform(id);
            }
        }

        /// <summary>
        /// Used to determine the amount of gravitational force to apply when <see cref="DepictionEngine.Object.useGravity"/> is enabled. 
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsPhysicsObject))]
        public double mass
        {
            get => _mass;
            set => SetValue(nameof(mass), value, ref _mass);
        }

        private int _lastLayer;
        /// <summary>
        /// The layer the GameObject is in.
        /// </summary>
        [Json]
        public int layer
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.layer : gameObject.layer;
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
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.tag : gameObject.tag;
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
        public bool isHiddenInHierarchy
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.isHiddenInHierarchy : _isHiddenInHierarchy;
            set
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(isHiddenInHierarchy), value, ref objectAdditionalFallbackValues.isHiddenInHierarchy, (newValue, oldValue) => { UpdateHideFlags(); });
                else
                    SetValue(nameof(isHiddenInHierarchy), value, ref _isHiddenInHierarchy, (newValue, oldValue) => { UpdateHideFlags(); });
            }
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.AnimatorBase"/> will be created if none is present in the <see cref="DepictionEngine.IPersistent"/> returned from the datasource.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createAnimatorIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createAnimatorIfMissing;
            set 
            { 
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createAnimatorIfMissing), value, ref objectAdditionalFallbackValues.createAnimatorIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the animator.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public SerializableGuid animatorId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.animatorId : SerializableGuid.Empty;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createControllerIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createControllerIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createControllerIfMissing), value, ref objectAdditionalFallbackValues.createControllerIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the controller.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public SerializableGuid controllerId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.controllerId : SerializableGuid.Empty;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createGeneratorIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createGeneratorIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createGeneratorIfMissing), value, ref objectAdditionalFallbackValues.createGeneratorIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the generators.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> generatorsId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.generatorsId : null;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createReferenceIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createReferenceIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createReferenceIfMissing), value, ref objectAdditionalFallbackValues.createReferenceIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the references.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> referencesId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.referencesId : null;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createEffectIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createEffectIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createEffectIfMissing), value, ref objectAdditionalFallbackValues.createEffectIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the effects.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> effectsId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.effectsId : null;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createFallbackValuesIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createFallbackValuesIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createFallbackValuesIfMissing), value, ref objectAdditionalFallbackValues.createFallbackValuesIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the fallbackValues.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> fallbackValuesId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.fallbackValuesId : null;
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
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createDatasourceIfMissing
        {
            get => objectAdditionalFallbackValues != null && objectAdditionalFallbackValues.createDatasourceIfMissing;
            set 
            {
                if (objectAdditionalFallbackValues != null)
                    SetValue(nameof(createDatasourceIfMissing), value, ref objectAdditionalFallbackValues.createDatasourceIfMissing); 
            }
        }

        /// <summary>
        /// The Id of the datasources.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public List<SerializableGuid> datasourcesId
        {
            get => objectAdditionalFallbackValues != null ? objectAdditionalFallbackValues.datasourcesId : null;
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
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public AnimatorBase animator
        {
            get => _animator;
            private set => SetValue(nameof(animator), value, ref _animator);
        }

        /// <summary>
        /// The <see cref="DepictionEngine.ControllerBase"/> used by this object.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public ControllerBase controller
        {
            get => _controller;
            private set => SetController(value);
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
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public List<GeneratorBase> generators
        {
            get { _generators ??= new List<GeneratorBase>(); return _generators; }
            private set => SetValue(nameof(generators), value, ref _generators);
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.ReferenceBase"/> used by this object.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public List<ReferenceBase> references
        {
            get { _references ??= new List<ReferenceBase>(); return _references; }
            private set => SetValue(nameof(references), value, ref _references);
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.EffectBase"/> used by this object.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public List<EffectBase> effects
        {
            get { _effects ??= new List<EffectBase>(); return _effects; }
            private set => SetValue(nameof(effects), value, ref _effects);
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.FallbackValues"/> used by this object.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public List<FallbackValues> fallbackValues
        {
            get { _fallbackValues ??= new List<FallbackValues>(); return _fallbackValues; }
            private set => SetValue(nameof(fallbackValues), value, ref _fallbackValues);
        }

        /// <summary>
        /// A list of <see cref="DepictionEngine.DatasourceBase"/> used by this object.
        /// </summary>
        [Json( conditionalGetMethod: nameof(IsNotFallbackValues))]
        public List<DatasourceBase> datasources
        {
            get { _datasources ??= new List<DatasourceBase>(); return _datasources; }
            private set => SetValue(nameof(datasources), value, ref _datasources);
        }

        protected void InitializeReferenceDataType(string dataType, Type referenceType = null)
        {
            if (!typeof(ReferenceBase).IsAssignableFrom(referenceType))
            {
                Debug.LogError("ReferenceType must extend "+nameof(ReferenceBase));
                return;
            }

            if (GetFirstReferenceOfType(dataType) == Disposable.NULL)
            {
                ReferenceBase reference = references.Where(reference => (referenceType != null ? referenceType : typeof(ReferenceBase)).IsAssignableFrom(reference.GetType()) && string.IsNullOrEmpty(reference.dataType)).FirstOrDefault();
                if (reference != Disposable.NULL)
                    reference.dataType = dataType;
            }
        }

        public ReferenceBase GetFirstReferenceOfType(string dataType)
        {
            return references.Where(reference => reference.dataType == dataType).FirstOrDefault();
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
            get => controller as TargetControllerBase;
            set => SetTargetController(value);
        }

        protected virtual bool SetTargetController(TargetControllerBase value)
        {
            if (Object.ReferenceEquals(_targetController, value))
                return false;

            _targetController = value;

            UpdateGeoController();

            return true;
        }

        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public new TransformDouble transform
        {
            get => _transform;
            protected set => SetTransform(value);
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
            IterateThroughList(generators, callback);
            IterateThroughList(references, callback);
            IterateThroughList(effects, callback);
            IterateThroughList(fallbackValues, callback);
            IterateThroughList(datasources, callback);
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

        public T CreateScript<T>(InitializationContext initializingContext = InitializationContext.Programmatically) where T : Script
        {
            return CreateScript(typeof(T), null, initializingContext) as T;
        }

        public T CreateScript<T>(JSONObject json, InitializationContext initializingContext = InitializationContext.Programmatically) where T : Script
        {
            return CreateScript(typeof(T), json, initializingContext) as T;
        }

        public Script CreateScript(Type type, InitializationContext initializingContext = InitializationContext.Programmatically, bool isFallbackValues = false)
        {
            return CreateScript(type, null, initializingContext, isFallbackValues);
        }

        public Script CreateScript(Type type, JSONObject json, InitializationContext initializingContext = InitializationContext.Programmatically, bool isFallbackValues = false)
        {
            if (!type.IsSubclassOf(typeof(Script)))
                return null;
            return AddComponent(type, initializingContext, json, null, isFallbackValues) as Script;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (string,object) GetScriptProperty(IProperty property)
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

        protected override bool AddChild(PropertyMonoBehaviour child)
        {
            bool added = false;

            Originator(() =>
            {
                if (child is Script script)
                    added = AddScript(script);
                else
                {
                    if (base.AddChild(child))
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
                    (string, object) localProperty = GetScriptProperty(script);
                    PropertyAssigned(this, localProperty.Item1, localProperty.Item2, null);
                }
            }

            if (hasChanged)
            {
                ScriptAddedEvent?.Invoke(this, script);

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

        protected override bool RemoveChild(PropertyMonoBehaviour child)
        {
            bool removed = false;

            Originator(() =>
            {
                if (child is Script)
                    removed = RemoveScript(child as Script);
                else
                {
                    if (base.RemoveChild(child))
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
                    (string, object) localProperty = GetScriptProperty(script);
                    PropertyAssigned(this, localProperty.Item1, localProperty.Item2, null);
                }
            }

            if (hasChanged)
            {
                ScriptRemovedEvent?.Invoke(this, script);

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

        public T CreateChild<T>(string name = null, Transform parent = null, InitializationContext initializingContext = InitializationContext.Programmatically, List<PropertyModifier> propertyModifiers = null) where T : IScriptableBehaviour
        {
            return (T)CreateChild(typeof(T), name, parent, initializingContext, propertyModifiers);
        }

        public IScriptableBehaviour CreateChild(Type type, string name = null, Transform parent = null, InitializationContext initializingContext = InitializationContext.Programmatically, List<PropertyModifier> propertyModifiers = null)
        {
            IScriptableBehaviour scriptableBehaviour = instanceManager.CreateInstance(type, parent != null ? parent : gameObject.transform, initializingContext: initializingContext, propertyModifiers: propertyModifiers) as IScriptableBehaviour;
            
            if (!string.IsNullOrEmpty(name))
                scriptableBehaviour.name = name;

            return scriptableBehaviour;
        }

        public void SetGridProperties(int loadScopeInstanceId, VisibleCameras visibleInCamerasInstanceId)
        {
            bool visibleInCamerasChanged = false;

            if (visibleInCamerasInstanceId != null)
            {
                if (!visibleInCameras.TryGetValue(loadScopeInstanceId, out VisibleCameras currentVisibleInCamerasInstanceId) || currentVisibleInCamerasInstanceId != visibleInCamerasInstanceId)
                {
                    visibleInCameras[loadScopeInstanceId] = visibleInCamerasInstanceId;
                    visibleInCamerasChanged = true;
                }
            }
            else if (visibleInCameras.Remove(loadScopeInstanceId))
                visibleInCamerasChanged = true;

            if (visibleInCamerasChanged)
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
                masked = visibleInCameras.Count > 0;

                foreach (VisibleCameras visibleCamera in visibleInCameras.Values)
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
            get => _rigidbodyInternal;
            set
            {
                if (_rigidbodyInternal == value)
                    return;
                _rigidbodyInternal = value;
            }
        }

        public int reflectionProbeCount { get => managedReflectionProbes.Count; }

        protected List<ReflectionProbe> managedReflectionProbes
        {
            get { _managedReflectionProbes ??= new List<ReflectionProbe>(); return _managedReflectionProbes; }
            set => _managedReflectionProbes = value;
        }

        public void IterateOverReflectionProbes(Action<ReflectionProbe> callback)
        {
            if (callback != null)
            {
                for (int i = managedReflectionProbes.Count - 1; i >= 0; i--)
                    callback(managedReflectionProbes[i]);
            }
        }

        /// <summary>
        /// Automatically add the <see cref="UnityEngine.ReflectionProbe"/>(s) so the <see cref="DepictionEngine.Object"/> can manage them. When managed a 'customBakedTexture' will be generated, assigned and updated automatically for the <see cref="UnityEngine.ReflectionProbe"/> if their type is set to <see cref="UnityEngine.Rendering.ReflectionProbeMode.Custom"/>.
        /// </summary>
        /// <param name="added">The number of added <see cref="UnityEngine.ReflectionProbe"/>(s)</param>
        /// <param name="removed">The number of removed <see cref="UnityEngine.ReflectionProbe"/>(s)</param>
        public void UpdateReflectionProbe(out int added, out int removed)
        {
            removed = 0;
            added = 0;

            List<ReflectionProbe> newManagedReflectionProbes = new();
            GetReflectionProbes(ref newManagedReflectionProbes);
            for (int i = managedReflectionProbes.Count - 1; i >= 0; i--)
            {
                ReflectionProbe reflectionProbe = managedReflectionProbes[i];
                if (!newManagedReflectionProbes.Contains(reflectionProbe) && RemoveReflectionProbe(reflectionProbe))
                    removed++;
            }

            foreach (ReflectionProbe reflectionProbe in newManagedReflectionProbes)
            {
                if (!managedReflectionProbes.Contains(reflectionProbe))
                {
                    if (AddReflectionProbe(reflectionProbe))
                        added++;
                }
            }
        }

        protected virtual void GetReflectionProbes(ref List<ReflectionProbe> managedReflectionProbes)
        {
            gameObject.GetComponents(managedReflectionProbes);
        }

        /// <summary>
        /// Add the <see cref="UnityEngine.ReflectionProbe"/> so the <see cref="DepictionEngine.Object"/> can manage it. When managed a 'customBakedTexture' will be generated, assigned and updated automatically for the <see cref="UnityEngine.ReflectionProbe"/> if their type is set to <see cref="UnityEngine.Rendering.ReflectionProbeMode.Custom"/>.
        /// </summary>
        /// <param name="reflectionProbe"></param>
        public virtual bool AddReflectionProbe(ReflectionProbe reflectionProbe)
        {
            if (!managedReflectionProbes.Contains(reflectionProbe))
            {
                managedReflectionProbes.Add(reflectionProbe);
                RegisterWithRenderingManager();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the <see cref="UnityEngine.ReflectionProbe"/> from the managed list.
        /// </summary>
        /// <param name="reflectionProbe"></param>
        /// <returns>True if successfully removed.</returns>
        public bool RemoveReflectionProbe(ReflectionProbe reflectionProbe)
        {
            if (managedReflectionProbes.Remove(reflectionProbe))
            {
                RegisterWithRenderingManager();
                return true;
            }
            return false;
        }

        protected void RegisterWithRenderingManager()
        {
            if (IsReflectionObject())
                renderingManager.AddReflectionObject(this);
            else
                renderingManager.RemoveReflectionObject(this);
        }

        public virtual bool IsReflectionObject()
        {
            return reflectionProbeCount > 0;
        }

        public RenderTexture reflectionCustomBakedTexture
        {
            get => _reflectionCustomBakedTexture;
            private set
            {
                if (Object.ReferenceEquals(_reflectionCustomBakedTexture, value))
                    return;

                DisposeManager.Dispose(_reflectionCustomBakedTexture);

                _reflectionCustomBakedTexture = value;
            }
        }

        protected virtual void UpdateReflectionCustomBakedTexture(int textureSize)
        {
            RenderTexture customBakedTexture = reflectionCustomBakedTexture;
            if (customBakedTexture == null || customBakedTexture.width != textureSize || customBakedTexture.height != textureSize)
            {
                reflectionCustomBakedTexture = new(textureSize, textureSize, 0, RenderTextureFormat.ARGB32, 0)
                {
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    useMipMap = false,
                    dimension = TextureDimension.Cube,
                    name = name + "_ReflectionProbe_Cubemap",
                    isPowerOfTwo = true
                };
            }
        }

        public bool ReflectionRequiresRender(out Camera camera)
        {
            if (activeAndEnabled && GetReflectionRenderCamera(out camera))
                return true;

            camera = null;
            return false;
        }

        protected virtual bool GetReflectionRenderCamera(out Camera camera)
        {
            ReflectionProbe reflectionProbe = managedReflectionProbes.Count > 0 ? managedReflectionProbes[0] : null;
            if (reflectionProbe != null && reflectionProbe.isActiveAndEnabled)
            {
                if (reflectionProbe.mode == ReflectionProbeMode.Custom || (reflectionProbe.mode == ReflectionProbeMode.Realtime && reflectionProbe.refreshMode == ReflectionProbeRefreshMode.ViaScripting))
                {
                    Camera closestCamera = null;
                    double closestCameraDistance = double.MaxValue;

                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != null)
                    {
                        instanceManager.IterateOverInstances<Camera>((camera) =>
                        {
                            if (camera.activeAndEnabled)
                            {
                                double cameraDistance = Vector3Double.Distance(reflectionProbe.gameObject.transform.position, camera.gameObject.transform.position);
                                if (cameraDistance < closestCameraDistance)
                                {
                                    closestCamera = camera;
                                    closestCameraDistance = cameraDistance;
                                }
                            }

                            return true;
                        });
                    }

                    if (closestCamera != Disposable.NULL && closestCameraDistance < reflectionProbe.size.magnitude * 50.0f)
                    {
                        camera = closestCamera;
                        return true;
                    }
                }
            }

            camera = null;
            return false;
        }

        protected virtual int GetReflectionTextureSize()
        {
            int textureSize = 0;

            IterateOverReflectionProbes((reflectionProbe) =>
            {
                if (reflectionProbe != null)
                    textureSize = reflectionProbe.resolution;
            });

            return textureSize;
        }

        public void UpdateEnvironmentReflection(RTTCamera rttCamera, Camera camera, ScriptableRenderContext? context = null)
        {
            UpdateReflectionCustomBakedTexture(GetReflectionTextureSize());

            RenderToReflectionCubemap(rttCamera, camera);
        }

        protected virtual void RenderToReflectionCubemap(RTTCamera rttCamera, Camera camera)
        {
            IterateOverReflectionProbes((reflectionProbe) =>
            {
                if (reflectionProbe != null)
                {
                    if (reflectionProbe.mode == ReflectionProbeMode.Custom)
                        rttCamera.RenderToCubemap(reflectionProbe.clearFlags == ReflectionProbeClearFlags.Skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor, reflectionProbe.backgroundColor, camera.skybox.material, reflectionProbe.cullingMask, reflectionProbe.nearClipPlane, reflectionProbe.farClipPlane, reflectionProbe.transform.position, reflectionProbe.transform.rotation, reflectionCustomBakedTexture);
                    else
                    {
                        if (reflectionProbe.mode == ReflectionProbeMode.Realtime && reflectionProbe.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
                        {
                            //These render calls are asynchronous which can be problematic as the renders, being done outside the scope of this method, will include other reflection, dynamicSkybox and GI.
                            //It should be avoided and mode == "ReflectionProbeMode.Custom should be used instead".
                            reflectionProbe.RenderProbe();
                        }
                    }
                }
                else
                    reflectionCustomBakedTexture = null;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (animator is not null && !TriggerCallback(animator, callback))
                containsDisposed = true;

            if (controller is not null && !TriggerCallback(controller, callback))
                containsDisposed = true;

            //Effects such as Atmosphere need to be executed before loaders
            if (effects != null)
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (!TriggerCallback(effects[i], callback))
                        containsDisposed = true;
                }
            }

            if (generators != null)
            {
                for (int i = generators.Count - 1; i >= 0; i--)
                {
                    if (!TriggerCallback(generators[i], callback))
                        containsDisposed = true;
                }
            }

            if (references != null)
            {
                for (int i = references.Count - 1; i >= 0; i--)
                {
                    if (!TriggerCallback(references[i], callback))
                        containsDisposed = true;
                }
            }

            if (fallbackValues != null)
            {
                for (int i = fallbackValues.Count - 1; i >= 0; i--)
                {
                    if (!TriggerCallback(fallbackValues[i], callback))
                        containsDisposed = true;
                }
            }

            if (datasources != null)
            {
                for (int i = datasources.Count - 1; i >= 0; i--)
                {
                    if (!TriggerCallback(datasources[i], callback))
                        containsDisposed = true;
                }
            }

            return containsDisposed;
        }

        private void FixedUpdate()
        {
            if (IsPhysicsDriven())
            {
                instanceManager.IterateOverInstances<AstroObject>((astroObject) =>
                {
                    Vector3 forceVector = astroObject.GetGravitationalForce(this);
                    if (!forceVector.Equals(Vector3.zero))
                        rigidbodyInternal.AddForce(forceVector, ForceMode.Impulse);

                    return true;
                });
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
            get { _forceUpdateTransformPending ??= new ComponentChangedPending(); return _forceUpdateTransformPending; }
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
            if (initialized && transform != Disposable.NULL && transform.ForceUpdateTransform(out TransformBase.Component _, localPositionChanged, localRotationChanged, localScaleChanged, camera))
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
                if (!IsDisposing() && isHiddenInHierarchy && !SceneManager.Debugging())
                    gameObject.hideFlags |= HideFlags.HideInHierarchy;

                if (transform != Disposable.NULL)
                    transform.UpdateTransformHideFlags();

                return true;
            }
            return false;
        }

        protected void DisposeDataProcessor(Processor dataProcessor)
        {
            dataProcessor?.Dispose();
        }

        protected override void ActiveAndEnabledChanged(bool newValue, bool oldValue)
        {
            base.ActiveAndEnabledChanged(newValue, oldValue);

            if (newValue)
                RenderingManager.MarkReflectionObjectDirty(this);
        }

        /// <summary>
        /// Dispose all children visuals.
        /// </summary>
        /// <param name="disposeDelay"></param>
        /// <returns></returns>
        protected virtual bool DisposeAllChildren(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (initialized && transform != null)
            {
                for (int i = transform.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.transform.GetChild(i);
                    if (child != null)
                    {
#if UNITY_EDITOR
                        if (disposeContext == DisposeContext.Programmatically_Pool && notPoolable)
                            disposeContext = DisposeContext.Programmatically_Destroy;
#endif
                        DisposeManager.Dispose(child.gameObject, disposeContext);
                    }
                }

                return true;
            }
            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                RegisterWithPhysicsManager();

                DisposeManager.Dispose(_objectAdditionalFallbackValues, disposeContext);

                DisposeManager.Dispose(_reflectionCustomBakedTexture, disposeContext);

                ChildAddedEvent = null;
                ChildRemovedEvent = null;
                ChildObjectPropertyAssignedEvent = null;
                ScriptAddedEvent = null;
                ScriptRemovedEvent = null;
                ComponentPropertyAssignedEvent = null;
                TransformChangedEvent = null;
                TransformPropertyAssignedEvent = null;
                ParentGeoAstroObjectPropertyAssignedEvent = null;
                TransformControllerCallback = null;

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
            return _values != null && _values.Contains(camera.GetCameraInstanceID());
        }

        public bool SequenceEqual(List<int> other)
        {
            return _values != null && other != null ? Enumerable.SequenceEqual(_values, other) : _values == null && other == null;
        }

        public static implicit operator VisibleCameras(int[] d) 
        {
            return new VisibleCameras(d);
        }
    };
}
