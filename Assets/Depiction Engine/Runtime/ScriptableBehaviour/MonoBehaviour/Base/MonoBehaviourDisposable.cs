// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [ExecuteAlways]
    public class MonoBehaviourDisposable : MonoBehaviour, IScriptableBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private int _instanceID;
        [SerializeField, HideInInspector]
        private bool _isFallbackValues;

#if UNITY_EDITOR
        private bool _lastNotPoolable;
        [SerializeField, HideInInspector]
        private bool _notPoolable;
#endif

        private bool _initializing;
        private bool _initialized;
        private bool _instanceAdded;
        private bool _disposing;
        private bool _disposingContextUpdated;
        private bool _disposed;
        private bool _poolComplete;

        private DisposeContext _disposingContext;
        private IScriptableBehaviour _originator;

        [NonSerialized]
        private InitializationContext _initializingContext;

        private Action<IDisposable> _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable, DisposeContext> _disposedEvent;

#if UNITY_EDITOR
        protected bool GetShowDebug()
        {
            return SceneManager.Debugging();
        }
#endif

        public virtual void Recycle()
        {
            name = null;
            hideFlags = default;

            _instanceID = default;
            _isFallbackValues = default;
            _initializing = _initialized = _instanceAdded = _disposing = _disposingContextUpdated = _disposed = _poolComplete = default;
            _requiresLateInitialize = _requiresPostLateInitialize = _requiresLateUpdate = default;
            _disposingContext = default;
            _initializingContext = default;

#if UNITY_EDITOR
            _lastNotPoolable = _notPoolable = default;
            _inspectorComponentNameOverride = default;
#endif
        }

        protected virtual void Awake()
        {

        }

        public bool Initialize()
        {
            if (!IsDisposing() && !_initializing)
            {
                Initializing();

                //Create the SceneManager if this is the first MonoBehaviour created
                SceneManager.Instance();

                _initializingContext = InitializationContext.Existing;

                //If the instanceID is not the same it means the component is new.
                if (GetInstanceID() != instanceID)
                {
                    bool isEditor = InstanceManager.initializingContext == InitializationContext.Editor || InstanceManager.initializingContext == InitializationContext.Editor_Duplicate;

                    //If serialized instanceID is zero it means this is not a duplicate.
                    if (instanceID == 0)
                        _initializingContext = isEditor ? InitializationContext.Editor : InitializationContext.Programmatically;
                    else if (IsDuplicateInitializing())
                        _initializingContext = isEditor ? InitializationContext.Editor_Duplicate : InitializationContext.Programmatically_Duplicate;
                }

#if UNITY_EDITOR
                //Existing could be the result of Undoing a Destroy so we keep it out of the pool just in case.
                if (_initializingContext == InitializationContext.Existing || _initializingContext == InitializationContext.Editor || _initializingContext == InitializationContext.Editor_Duplicate)
                    MarkAsNotPoolable();
#endif

                InitializeUID(_initializingContext);

                bool abortInitialization = false;

                SceneManager.UserContext(() =>
                {
                    if (!Initialize(_initializingContext))
                    {
                        try
                        {
                            DestroyAfterFailedInitialization();
                        }
                        catch (MissingReferenceException) { }
                        abortInitialization = true;
                    }
                }, _initializingContext == InitializationContext.Editor || _initializingContext == InitializationContext.Editor_Duplicate);

                if (abortInitialization)
                    return false;

                Initialized(_initializingContext);
                InitializedEvent?.Invoke(this);

                _instanceAdded = AddToInstanceManager();
                return _instanceAdded;
            }

            return false;
        }

        protected bool IsDuplicateInitializing()
        {
            return GetInstanceID() != instanceID && instanceID != 0 && GetInstanceID() < 0;
        }

        protected virtual void DestroyAfterFailedInitialization()
        {
            DisposeManager.Destroy(this);
        }

        /// <summary>
        /// The first step of the initialization process.
        /// </summary>
        protected virtual void Initializing()
        {
            _initializing = true;

            _requiresLateInitialize = _requiresPostLateInitialize = _requiresLateUpdate = true;

            if (!_isFallbackValues)
                _isFallbackValues = InstanceManager.initializeIsFallbackValues;
        }

        /// <summary>
        /// Initializes the object's unique identifiers.
        /// </summary>
        /// <param name="initializingContext"></param>
        protected virtual void InitializeUID(InitializationContext initializingContext)
        {
            instanceID = GetInstanceID();
        }

        /// <summary>
        /// Add the object to the <see cref="DepictionEngine.InstanceManager"/> if possible.
        /// </summary>
        /// <returns>True if the instance was added successfully.</returns>
        protected virtual bool AddToInstanceManager()
        {
            return initialized;
        }

        /// <summary>
        /// The main initialization function.
        /// </summary>
        /// <param name="initializingContext"></param>
        /// <returns>False if the initialization failed.</returns>
        protected virtual bool Initialize(InitializationContext initializingContext)
        {
            return IsValidInitialization(initializingContext);
        }

        /// <summary>
        /// Provides the ability to interrupt the initialization.
        /// </summary>
        /// <param name="initializingContext"></param>
        /// <returns>False to interrupt the initialization.</returns>
        protected virtual bool IsValidInitialization(InitializationContext initializingContext)
        {
            return !(isFallbackValues && initializingContext == InitializationContext.Existing);
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, initializingContext);
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, Func<T> duplicateValue, InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, duplicateValue, initializingContext);
        }

        public static bool InitValueInternal<T>(Action<T> callback, T defaultValue, Func<T> duplicateValue, InitializationContext initializingContext)
        {
            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                callback(duplicateValue());
                return true;
            }
            else
                return InitValueInternal(callback, defaultValue, initializingContext);
        }

        public static bool InitValueInternal<T>(Action<T> callback, T defaultValue, InitializationContext initializingContext)
        {
            if (initializingContext == InitializationContext.Editor || initializingContext == InitializationContext.Programmatically || initializingContext == InitializationContext.Reset)
            {
                callback(defaultValue);
                return true;
            }
            return false;
        }

        public virtual void Initialized(InitializationContext initializingContext)
        {
            //FallbackValues Component are really only used to diplay properties in the Inspector or to validate property change. By preventing initialized we limit the amount of code the object can execute.
            if (!isFallbackValues)
                _initialized = true;

            UpdateHideFlags();

            UpdateAllDelegates();
        }

        protected virtual bool IsFullyInitialized()
        {
            return gameObject.scene.isLoaded;
        }

        /// <summary>
        /// Disables internal calls to <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnEnable"/> and <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnDisable"/>.
        /// </summary>
        public void InhibitEnableDisableAll()
        {
            MonoBehaviourDisposable[] monoBehaviourDisposables = gameObject.GetComponents<MonoBehaviourDisposable>();
            foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                monoBehaviourDisposable.InhibitExplicitOnEnableDisable();
        }

        /// <summary>
        /// Enables internal calls to <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnEnable"/> and <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnDisable"/>.
        /// </summary>
        public void UninhibitEnableDisableAll()
        {
            MonoBehaviourDisposable[] monoBehaviourDisposables = gameObject.GetComponents<MonoBehaviourDisposable>();
            foreach (MonoBehaviourDisposable monoBehaviourDisposable in monoBehaviourDisposables)
                monoBehaviourDisposable.UninhibitExplicitOnEnableDisable();
        }

        /// <summary>
        /// Experimental, do not use.
        /// </summary>
        public virtual void ExplicitOnEnable()
        {
#if UNITY_EDITOR
            //Because Editor objects are not returned by the Object.FindObjectsByType used in the SceneManager.AfterAssemblyReload, we trigger it manually here knowing the OnEnabled is also called AfterAssemblyReload.
            if (SceneManager.IsEditorNamespace(GetType()))
                AfterAssemblyReload();
#endif
        }

        /// <summary>
        /// Experimental, do not use.
        /// </summary>
        public virtual void ExplicitOnDisable()
        {

        }

#if UNITY_EDITOR
        /// <summary>
        /// Use to reinitialize any fields that were not serialized and kept between assembly reloads.
        /// </summary>
        /// <returns></returns>
        public virtual bool AfterAssemblyReload()
        {
            if (initialized)
            {
                UpdateAllDelegates();
                return true;
            }
            return false;
        }
#endif

        /// <summary>
        /// Function to initialize event handlers.
        /// </summary>
        /// <returns></returns>
        protected virtual bool UpdateAllDelegates()
        {
            SceneManager.LateInitializeEvent -= LateInitializeHandler;
            SceneManager.PostLateInitializeEvent -= PostLateInitializeHandler;
            //SceneManager.LateUpdateEvent -= LateUpdateHandler;
            if (!IsDisposing())
            {
                if (_requiresLateInitialize)
                    SceneManager.LateInitializeEvent += LateInitializeHandler;
                if (_requiresPostLateInitialize)
                    SceneManager.PostLateInitializeEvent += PostLateInitializeHandler;
                if (_requiresLateUpdate)
                    SceneManager.LateUpdateEvent += LateUpdateHandler;
            }

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= Saving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= Saved;
            Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformedHandler;
            SceneManager.ResetRegisterCompleteUndoEvent -= ResetRegisterCompleteUndo;
            if (!IsDisposing())
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += Saving;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += Saved;
                Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformedHandler;
                SceneManager.ResetRegisterCompleteUndoEvent += ResetRegisterCompleteUndo; 
            }

            SceneManager sceneManager = SceneManager.Instance(false);
            if (sceneManager != Disposable.NULL)
                sceneManager.PropertyAssignedEvent -= SceneManagerPropertyAssignedHandler;
            if (!IsDisposing() && !SceneManager.IsSceneBeingDestroyed())
                this.sceneManager.PropertyAssignedEvent += SceneManagerPropertyAssignedHandler;
#endif
            return !isFallbackValues;
        }

#if UNITY_EDITOR
        private void UndoRedoPerformedHandler()
        {
            //Are the Destroy and Initialize check necessary?
            if (!DisposeManager.TriggerOnDestroyIfNull(this))
            {
                InstanceManager.Initialize(this, InitializationContext.Existing);
                UndoRedoPerformed();
            }
        }

        /// <summary>
        /// Trigered right after an undo or redo operation was performed (Editor Only).
        /// </summary>
        protected virtual void UndoRedoPerformed()
        {

        }

        [SerializeField, HideInInspector]
        private string _inspectorComponentNameOverride;
        public string inspectorComponentNameOverride
        {
            get => _inspectorComponentNameOverride;
            set => _inspectorComponentNameOverride = value;
        }

        private void SceneManagerPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(SceneManager.debug))
                DebugChanged();
        }

        protected virtual void DebugChanged()
        {
            UpdateHideFlags();
        }
#endif

        protected virtual void Saving(UnityEngine.SceneManagement.Scene scene, string path)
        {

        }

        protected virtual void Saved(UnityEngine.SceneManagement.Scene scene)
        {
            UpdateHideFlags();
        }

        private bool _requiresLateInitialize;
        private void LateInitializeHandler()
        {
            _requiresLateInitialize = false;
            LateInitialize(_initializingContext);
        }

        private bool _requiresPostLateInitialize;
        private void PostLateInitializeHandler()
        {
            _requiresPostLateInitialize = false;

            InitializationContext initializingContext = _initializingContext;

            PostLateInitialize(initializingContext);

#if UNITY_EDITOR
            RegisterInitializeObjectUndo(initializingContext);
#endif
        }

        private bool _requiresLateUpdate;
        private void LateUpdateHandler()
        {
            _requiresLateUpdate = false;
        }

        /// <summary>
        /// Every other objects are initialized at this point.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool LateInitialize(InitializationContext initializingContext)
        {
            return initialized;
        }

        /// <summary>
        /// Every other objects are initialized at this point.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool PostLateInitialize(InitializationContext initializingContext)
        {
            return initialized;
        }

        private int instanceID
        {
            get => _instanceID;
            set => _instanceID = value;
        }

        public bool wasFirstUpdated { get => _initialized && !_requiresLateUpdate; }

        public bool initialized { get => _initialized; }

        protected bool instanceAdded { get => _instanceAdded; }

        public bool isFallbackValues { get => _isFallbackValues; }

        public IScriptableBehaviour originator { get => _originator; }

        protected bool IsFallbackValues()
        {
            return isFallbackValues;
        }

        protected bool IsNotFallbackValues()
        {
            return !isFallbackValues;
        }

        public InitializationContext GetInitializeContext()
        {
            InitializationContext initializeState = InitializationContext.Programmatically;

#if UNITY_EDITOR
            initializeState = InitializationContext.Editor;
#endif

            if (_initializing && !_initialized)
                initializeState = _initializingContext;
            else if (_originator != null)
                initializeState = _originator.GetInitializeContext();
            else if (_disposing)
                initializeState = _disposingContext == DisposeContext.Editor_Destroy ? InitializationContext.Editor : InitializationContext.Programmatically;

            return initializeState;
        }

        public bool IsDisposing()
        {
            return _disposing;
        }

        public bool IsDisposed()
        {
            return _disposed;
        }

        public bool poolComplete
        {
            get => _poolComplete;
            set => _poolComplete = value;
        }

        /// <summary>
        /// Is the object destroying?
        /// </summary>
        /// <returns>True if the object as already been destroyed.</returns>
        protected bool IsDestroying()
        {
            return _disposing && _disposingContext != DisposeContext.Programmatically_Pool;
        }

        public DisposeContext disposingContext
        {
            get => _disposingContext;
        }

#if UNITY_EDITOR
        public bool notPoolable
        {
            get => _notPoolable;
        }

        public virtual void MarkAsNotPoolable()
        {
            _lastNotPoolable = _notPoolable = true;
        }
#endif

        public Action<IDisposable> InitializedEvent
        {
            get => _initializedEvent;
            set => _initializedEvent = value;
        }

        public Action<IDisposable> DisposingEvent
        {
            get => _disposingEvent;
            set => _disposingEvent = value;
        }

        public Action<IDisposable, DisposeContext> DisposedEvent
        {
            get => _disposedEvent;
            set => _disposedEvent = value;
        }

        protected SceneManager sceneManager { get => SceneManager.Instance();  }
        protected InstanceManager instanceManager { get => InstanceManager.Instance(); }
        protected DatasourceManager datasourceManager { get => DatasourceManager.Instance(); }
        protected TweenManager tweenManager { get => TweenManager.Instance(); }
        protected InputManager inputManager { get => InputManager.Instance(); }
        protected CameraManager cameraManager { get => CameraManager.Instance(); }
        protected PoolManager poolManager { get => PoolManager.Instance(); }
        protected RenderingManager renderingManager { get => RenderingManager.Instance(); }

        protected virtual bool UpdateHideFlags()
        {
            if (this != null)
            {
                hideFlags = HideFlags.None;

                return true;
            }
            return false;
        }

        public void Originator(Action callback, IScriptableBehaviour originator)
        {
            if (callback != null)
            {
                if (originator != null && !Object.ReferenceEquals(originator.originator, this))
                {
                    IScriptableBehaviour lastOriginator = _originator;
                    _originator = originator;
                    
                    callback();
                    
                    _originator = lastOriginator;
                }
                else
                    callback();
            }
        }

        public bool OnDisposing()
        {
            if (!_disposing)
            {
                _disposing = true;

                DisposingEvent?.Invoke(this);
                DisposingEvent = null;

                return true;
            }
            return false;
        }

        protected virtual void LateUpdate()
        {
        }

        public bool UpdateDisposingContext()
        {
            if (!_disposingContextUpdated)
            {
                _disposingContextUpdated = true;

                _disposingContext = GetDisposingContext();
#if UNITY_EDITOR
                if (_disposingContext == DisposeContext.Editor_Unknown)
                    _disposingContext = DisposeContext.Editor_Destroy;
#endif
                return true;
            }
            return false;
        }

        public void OnDisposeInternal(DisposeContext disposeContext)
        {
            SceneManager.UserContext(() => { OnDispose(disposeContext); }, disposeContext == DisposeContext.Editor_Destroy);
        }

        public virtual bool OnDispose(DisposeContext disposeContext)
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposedEvent?.Invoke(this, disposeContext);

                UpdateAllDelegates();

                InitializedEvent = null;
                DisposingEvent = null;
                DisposedEvent = null;

                return true;
            }
            return false;
        }

        private void OnDestroyInternal()
        {
#if UNITY_EDITOR
            Editor.UndoManager.UndoRedoPerformedEvent -= TriggerOnDestroyIfNullHandler;
#endif
            UpdateDisposingContext();
            OnDisposeInternal(_disposingContext);
        }

        public void OnDestroy()
        {
            OnDisposing();

#if UNITY_EDITOR
            if (GetDisposingContext() == DisposeContext.Editor_Unknown)
            {
                //Give us more time to identify whether this is an Editor Undo/Redo dispose
                SceneManager.DelayedOnDestroyEvent -= OnDestroyInternal;
                SceneManager.DelayedOnDestroyEvent += OnDestroyInternal;
                Editor.UndoManager.UndoRedoPerformedEvent -= TriggerOnDestroyIfNullHandler;
                Editor.UndoManager.UndoRedoPerformedEvent += TriggerOnDestroyIfNullHandler;
            }
            else
#endif
                OnDestroyInternal();
        }

#if UNITY_EDITOR
        private void TriggerOnDestroyIfNullHandler()
        {
            DisposeManager.TriggerOnDestroyIfNull(this);
        }
#endif

        protected virtual DisposeContext GetDisposingContext()
        {
            DisposeContext destroyingContext = DisposeContext.Programmatically_Pool;

#if UNITY_EDITOR
            destroyingContext = DisposeManager.disposingContext;

            if (SceneManager.IsSceneBeingDestroyed())
                destroyingContext = DisposeContext.Programmatically_Destroy;
#endif

            return destroyingContext;
        }

        //Used in TransformBase, do not delete
        public void InhibitExplicitOnEnableDisable()
        {
            _inhibitExplicitOnEnableDisable = true;
        }

        //Used in TransformBase, do not delete
        public void UninhibitExplicitOnEnableDisable()
        {
            _inhibitExplicitOnEnableDisable = false;
        }

        private bool _inhibitExplicitOnEnableDisable;
        protected virtual void OnEnable()
        {
            if (!IsDisposing() && wasFirstUpdated && !_inhibitExplicitOnEnableDisable && SceneManager.IsValidActiveStateChange())
                ExplicitOnEnable();
        }

        protected virtual void OnDisable()
        {
            if (!IsDisposing() && wasFirstUpdated && !_inhibitExplicitOnEnableDisable && SceneManager.IsValidActiveStateChange())
                ExplicitOnDisable();
        }

        public virtual void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _lastNotPoolable = _notPoolable;
#endif
        }

        public virtual void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_lastNotPoolable)
                _notPoolable = true;
#endif
        }

        public virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (wasFirstUpdated)
                Editor.UndoManager.Validating(this);
#endif
        }

#if UNITY_EDITOR
        protected virtual void RegisterInitializeObjectUndo(InitializationContext initializingContext)
        {
            _registeredCompleteObjectUndo = true;
            Editor.UndoManager.RegisterCompleteObjectUndo(this, initializingContext);
        }

        private bool _registeredCompleteObjectUndo;
        protected void RegisterCompleteObjectUndo(DisposeContext disposeContext = DisposeContext.Editor_Destroy)
        {
            if (disposeContext == DisposeContext.Editor_Destroy)
            {
                MarkAsNotPoolable();
                if (!_registeredCompleteObjectUndo)
                {
                    _registeredCompleteObjectUndo = true;
                    Editor.UndoManager.RegisterCompleteObjectUndo(this);
                }
            }
        }

        private void ResetRegisterCompleteUndo()
        {
            _registeredCompleteObjectUndo = false;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MonoBehaviourDisposable lhs, Disposable.Null rhs) { return !(lhs == rhs); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MonoBehaviourDisposable lhs, Disposable.Null _) { return DisposeManager.IsNullOrDisposing(lhs); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Disposable.Null value) { return this == value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object value) { return base.Equals(value); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return base.GetHashCode(); }
    }
}
