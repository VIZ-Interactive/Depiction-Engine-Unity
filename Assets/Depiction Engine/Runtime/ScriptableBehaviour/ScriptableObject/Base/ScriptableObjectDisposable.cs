// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace DepictionEngine
{
    [ExecuteAlways]
    public class ScriptableObjectDisposable : ScriptableObject, IScriptableBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private int _instanceID;
        [SerializeField, HideInInspector]
        private bool _isFallbackValues;

#if UNITY_EDITOR
        private bool _lastHasEditorUndoRedo;
        [SerializeField, HideInInspector]
        private bool _hasEditorUndoRedo;
#endif

        [NonSerialized]
        private bool _delegatesInitialized;
        private bool _unityInitialized;

        private bool _initializing;
        private bool _initialized;
        private bool _instanceAdded;
        private bool _disposing;
        private bool _disposingContextUpdated;
        private bool _disposed;
        private bool _poolComplete;

        private DisposeContext _disposingContext;
        private IScriptableBehaviour _originator;
        private bool _isUserChange;

        private Action<IDisposable> _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable, DisposeContext> _disposedEvent;

#if UNITY_EDITOR
        protected bool GetDebug()
        {
            return SceneManager.Debugging();
        }
#endif

        public virtual void Recycle()
        {
            name = PoolManager.NEW_SCRIPT_OBJECT_NAME;
            hideFlags = default;

            _instanceID = default;
            _isFallbackValues = default;
            _initializing = _initialized = _instanceAdded = _disposing = _disposingContextUpdated = _disposed = _poolComplete = default;
            _disposingContext = default;
            _initializingContext = default;

#if UNITY_EDITOR
            _lastHasEditorUndoRedo = _hasEditorUndoRedo = default;
            _inspectorComponentNameOverride = default;
#endif
        }

        public bool Initialize()
        {
            if (!_initializing)
            {
                Initializing();

                InitializationContext initializingContext = GetinitializingContext();
             
                InitializeUID(initializingContext);

                bool abortInitialization = false;

                IsUserChange(() =>
                {
                    if (!Initialize(initializingContext))
                        abortInitialization = true;
                }, initializingContext == InitializationContext.Editor || initializingContext == InitializationContext.Editor_Duplicate);

                if (abortInitialization)
                    return false;

                Initialized();

#if UNITY_EDITOR
                RegisterInitializeObjectUndo(initializingContext);
#endif

                _instanceAdded = AddToInstanceManager();
                return _instanceAdded;
            }

            return false;
        }

#if UNITY_EDITOR
        private void RegisterInitializeObjectUndo(InitializationContext initializingContext)
        {
            Editor.UndoManager.RegisterCompleteObjectUndo(this, initializingContext);
        }
#endif

        /// <summary>
        /// The first step of the initialization process.
        /// </summary>
        protected virtual void Initializing()
        {
            _initializing = true;

            SceneManager.UnityInitializedEvent += UnityInitialized;

            if (!_isFallbackValues)
                _isFallbackValues = InstanceManager.initializeIsFallbackValues;
        }

        private void UnityInitialized()
        {
            _unityInitialized = true;
        }

        /// <summary>
        /// Get the context in which the <see cref="DepictionEngine.IDisposable.Initialize"/> was triggered.
        /// </summary>
        /// <returns></returns>
        [NonSerialized]
        private InitializationContext _initializingContext;
        private InitializationContext GetinitializingContext()
        {
            //The _initializingContext == InitializationContext.Editor is necessary for when a Compoenent is copied as New in the inspector. Because the serialization wont happen immediatly on Awake we have to wait for the Update initialization to know wheter the component was duplicated or not. 
            if (!_initialized && (_initializingContext == InitializationContext.Unknown || _initializingContext == InitializationContext.Editor))
            {
                InitializationContext initializingContext = InitializationContext.Existing;

                //If the instanceID is not the same it means the component is new.
                int newInstanceID = GetInstanceID();
                if (newInstanceID != instanceID)
                {
                    bool isEditor = InstanceManager.initializingContext == InitializationContext.Editor || InstanceManager.initializingContext == InitializationContext.Editor_Duplicate;

                    //If serialized instanceID is zero it means this is not a duplicate.
                    if (instanceID == 0)
                        initializingContext = isEditor ? InitializationContext.Editor : InitializationContext.Programmatically;
                    else if (newInstanceID < 0)
                        initializingContext = isEditor ? InitializationContext.Editor_Duplicate : InitializationContext.Programmatically_Duplicate;
                }

                _initializingContext = initializingContext;
            }

            return _initializingContext;
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
            if (!IsValidInitialization(initializingContext))
                return false;

            UpdateAllDelegates();

            return true;
        }

        /// <summary>
        /// Provides the ability to interrupt the initialization.
        /// </summary>
        /// <param name="initializingContext"></param>
        /// <returns>False to interrupt the initialization.</returns>
        protected virtual bool IsValidInitialization(InitializationContext initializingContext)
        {
            return true;
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, initializingContext);
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, T duplicateValue, InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, () => { return duplicateValue; }, initializingContext);
        }

        /// <summary>
        /// Acts as a reliable constructor and will always by called unlike Awake which is sometimes skipped.
        /// </summary>
        protected virtual void Initialized()
        {
            //FallbackValues Component are really only used to diplay properties in the Inspector or to validate property change. By preventing initialized we limit the amount of code the object can execute.
            if (!isFallbackValues)
                _initialized = true;

            UpdateHideFlags();
        }

        /// <summary>
        /// Disables internal calls to <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnEnable"/> and <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnDisable"/>.
        /// </summary>
        public void InhibitExplicitOnEnableDisable()
        {
            _inhibitExplicitOnEnableDisable = true;
        }

        /// <summary>
        /// Enables internal calls to <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnEnable"/> and <see cref="DepictionEngine.IScriptableBehaviour.ExplicitOnDisable"/>.
        /// </summary>
        public void UninhibitExplicitOnEnableDisable()
        {
            _inhibitExplicitOnEnableDisable = false;
        }

        /// <summary>
        /// Called after <see cref="DepictionEngine.IDisposable.Initialize"/> and when the component enable state changed.
        /// </summary>
        public virtual void ExplicitOnEnable()
        {
            //Call UpdateAllDelegates in case some delegates need to be activated/deactivated for a specific state
            UpdateAllDelegates();
        }

        /// <summary>
        /// Called after the component enable state changed.
        /// </summary>
        public virtual void ExplicitOnDisable()
        {
            //Call UpdateAllDelegates in case some delegates need to be activated/deactivated for a specific state
            UpdateAllDelegates();
        }

        /// <summary>
        /// Function to initialize event handlers.
        /// </summary>
        /// <returns></returns>
        protected virtual bool UpdateAllDelegates()
        {
            _delegatesInitialized = true;
            SceneManager.PostLateInitializeEvent -= UpdateAllDelegatesHandler;

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= Saving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= Saved;
            Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformedHandler;
            if (!IsDisposing())
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += Saving;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += Saved;
                Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformedHandler;
            }

            SceneManager sceneManager = SceneManager.Instance(false);
            if (sceneManager != Disposable.NULL)
                sceneManager.PropertyAssignedEvent -= SceneManagerPropertyAssignedHandler;
            if (!IsDisposing() && !SceneManager.IsSceneBeingDestroyed())
                this.sceneManager.PropertyAssignedEvent += SceneManagerPropertyAssignedHandler;
#endif
            return !isFallbackValues;
        }

        private void UpdateAllDelegatesHandler()
        {
            UpdateAllDelegates();
        }

#if UNITY_EDITOR
        private void UndoRedoPerformedHandler()
        {
            //Are the Destroy and Initialize check necessary?
            if (!DisposeManager.TriggerOnDestroyIfNull(this))
            {
                InstanceManager.Initialize(this, InitializationContext.Existing);
                IsUserChange(() => { UndoRedoPerformed(); });
            }
        }

        /// <summary>
        /// Trigered right after an undo or redo operation was performed (Editor Only).
        /// </summary>
        protected virtual void UndoRedoPerformed()
        {
            
        }

        private string _inspectorComponentNameOverride;
        public string inspectorComponentNameOverride
        {
            get { return _inspectorComponentNameOverride; }
            set { _inspectorComponentNameOverride = value; }
        }

        private void SceneManagerPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(SceneManager.debug))
                DebugChanged();
        }

        private void DebugChanged()
        {
            UpdateHideFlags();
        }
#endif

        protected virtual void Saving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            
        }

        private void Saved(UnityEngine.SceneManagement.Scene scene)
        {
            UpdateHideFlags();
        }

        private void RemoveDisposedDelegate(IDisposable disposable, DisposeContext disposeContext)
        {
            disposable.DisposedEvent -= ObjectDisposedHandler;
        }

        private void ObjectDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemoveDisposedDelegate(disposable, disposeContext);
        }

        private int instanceID
        {
            get { return _instanceID; }
            set { _instanceID = value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool SetValue<T>(string name, T value, ref T valueField, Action<T, T> assignedCallback = null)
        {
            T oldValue = valueField;

            if (HasChanged(value, oldValue))
            { 
                valueField = value;

                assignedCallback?.Invoke(value, oldValue);

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
        protected bool HasChanged(object newValue, object oldValue, bool forceChangeDuringInitializing = true)
        {
            return (!isFallbackValues && forceChangeDuringInitializing && !initialized) || !Object.Equals(newValue, oldValue);
        }

        protected bool unityInitialized { get => _unityInitialized; }

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

        public virtual void IsUserChange(Action callback, bool isUserChange = true)
        {
            if (callback != null)
            {
                bool lastIsUserChange = _isUserChange;
                _isUserChange = isUserChange;

                callback();

                _isUserChange = lastIsUserChange;
            }
        }

        public bool IsUserChangeContext()
        {
            return _isUserChange || (_originator is not null && _originator.IsUserChangeContext());
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
            get { return _poolComplete; }
            set { _poolComplete = value; }
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
            get { return _disposingContext; }
        }

#if UNITY_EDITOR
        public bool hasEditorUndoRedo
        {
            get { return _hasEditorUndoRedo; }
        }

        protected void EditorUndoRedoDetected()
        {
            _lastHasEditorUndoRedo = _hasEditorUndoRedo = true;
        }
#endif

        public Action<IDisposable> InitializedEvent
        {
            get { return _initializedEvent; }
            set { _initializedEvent = value; }
        }

        public Action<IDisposable> DisposingEvent
        {
            get { return _disposingEvent; }
            set { _disposingEvent = value; }
        }

        public Action<IDisposable, DisposeContext> DisposedEvent
        {
            get { return _disposedEvent; }
            set { _disposedEvent = value; }
        }

        protected SceneManager sceneManager
        {
            get { return SceneManager.Instance(); }
        }

        protected RenderingManager renderingManager
        {
            get { return RenderingManager.Instance(); }
        }

        protected CameraManager cameraManager
        {
            get { return CameraManager.Instance(); }
        }

        protected InstanceManager instanceManager
        {
            get { return InstanceManager.Instance(); }
        }

        protected DatasourceManager datasourceManager
        {
            get { return DatasourceManager.Instance(); }
        }

        protected TweenManager tweenManager
        {
            get { return TweenManager.Instance(); }
        }

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
            IsUserChange(() => { OnDispose(disposeContext); }, disposeContext == DisposeContext.Editor_Destroy);
        }

        /// <summary>
        /// This is where you dispose any remaining dependencies.
        /// </summary>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        public virtual bool OnDispose(DisposeContext disposeContext)
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposedEvent?.Invoke(this, disposeContext);

                SceneManager.UnityInitializedEvent -= UnityInitialized;
                UpdateAllDelegates();

                InitializedEvent = null;
                DisposingEvent = null;
                DisposedEvent = null;

                return true;
            }
            return false;
        }

        private void OnDisposingInternal()
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
                SceneManager.DelayedOnDestroyEvent += OnDisposingInternal;
                Editor.UndoManager.UndoRedoPerformedEvent -= TriggerOnDestroyIfNullHandler;
                Editor.UndoManager.UndoRedoPerformedEvent += TriggerOnDestroyIfNullHandler;
            }
            else
#endif
            OnDisposingInternal();
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

        protected virtual void Awake()
        {
            Initialize();
        }

        private bool _inhibitExplicitOnEnableDisable;
        protected virtual void OnEnable()
        {
            if (!IsDisposing() && _unityInitialized && !_inhibitExplicitOnEnableDisable && SceneManager.IsValidActiveStateChange())
                ExplicitOnEnable();
        }

        protected virtual void OnDisable()
        {
            if (!IsDisposing() && _unityInitialized && !_inhibitExplicitOnEnableDisable && SceneManager.IsValidActiveStateChange())
                ExplicitOnDisable();
        }

        public virtual void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _lastHasEditorUndoRedo = _hasEditorUndoRedo;
#endif
        }

        public virtual void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_lastHasEditorUndoRedo)
                _hasEditorUndoRedo = true;
#endif     
            //Update Delegates after Recompile since they are not serialized and will be null
            if (initialized && !_delegatesInitialized)
            {
                SceneManager.PostLateInitializeEvent -= UpdateAllDelegatesHandler;
                SceneManager.PostLateInitializeEvent += UpdateAllDelegatesHandler;
            }
        }

        public virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (_unityInitialized)
                Editor.UndoManager.Validating(this);
#endif
        }

        public DisposeContext GetDisposeContext()
        {
            DisposeContext disposeContext = DisposeContext.Programmatically_Pool;

            if (IsDisposing())
                disposeContext = _disposingContext;
            else if (_originator is not null)
                disposeContext = _originator.GetDisposeContext();

            return disposeContext;
        }

        protected void Dispose(object obj)
        {
            DisposeManager.Dispose(obj, GetDisposeContext());
        }

        protected void Dispose(object obj, DisposeContext disposeContext)
        {
            DisposeManager.Dispose(obj, disposeContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ScriptableObjectDisposable lhs, Disposable.Null rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ScriptableObjectDisposable lhs, Disposable.Null _) => DisposeManager.IsNullOrDisposing(lhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Disposable.Null value)
        {
            return this == value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object value)
        {
            return base.Equals(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
