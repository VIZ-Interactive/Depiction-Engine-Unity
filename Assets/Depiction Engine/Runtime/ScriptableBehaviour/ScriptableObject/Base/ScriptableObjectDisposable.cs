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

        private bool _awake;
        private bool _initializing;
        private bool _initialized;
        private bool _instanceAdded;
        private bool _disposing;
        private bool _destroyingContextUpdated;
        private bool _disposed;
        private bool _disposedComplete;

        private DisposeManager.DisposeContext _destroyingContext;
        private IScriptableBehaviour _originator;
        private bool _isUserChange;

        private Action _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable> _disposedEvent;

#if UNITY_EDITOR
        protected bool GetDebug()
        {
            return SceneManager.Debugging();
        }
#endif

        public virtual void Recycle()
        {
            name = PoolManager.NEW_SCRIPT_OBJECT_NAME;
            hideFlags = HideFlags.None;

            _instanceID = 0;
            _isFallbackValues = false;
            _awake = _initializing = _initialized = _instanceAdded = _disposing = _destroyingContextUpdated = _disposed = _disposedComplete = false;
            _destroyingContext = DisposeManager.DisposeContext.Unknown;
            _initializingContext = InstanceManager.InitializationContext.Unknown;

#if UNITY_EDITOR
            _lastHasEditorUndoRedo = _hasEditorUndoRedo = false;
            _inspectorComponentNameOverride = null;
#endif
        }

        public bool Initialize()
        {
            if (!_initializing)
            {
                Initializing();

                InstanceManager.InitializationContext initializingContext = GetinitializingContext();

                InitializeUID(initializingContext);

                bool abortInitialization = false;

                IsUserChange(() =>
                {
                    if (!Initialize(initializingContext))
                        abortInitialization = true;
                }, initializingContext == InstanceManager.InitializationContext.Editor || initializingContext == InstanceManager.InitializationContext.Editor_Duplicate);

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
        private void RegisterInitializeObjectUndo(InstanceManager.InitializationContext initializingContext)
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

            if (!_isFallbackValues)
                _isFallbackValues = InstanceManager.initializeIsFallbackValues;
        }

        /// <summary>
        /// Get the context in which the <see cref="DepictionEngine.IDisposable.Initialize"/> was triggered.
        /// </summary>
        /// <returns></returns>
        [NonSerialized]
        private InstanceManager.InitializationContext _initializingContext;
        private InstanceManager.InitializationContext GetinitializingContext()
        {
            if (_initializingContext == InstanceManager.InitializationContext.Unknown)
            {
                InstanceManager.InitializationContext initializingContext = InstanceManager.InitializationContext.Existing;

                //If the instanceID is not the same it means the component is new.
                int newInstanceID = GetInstanceID();
                if (newInstanceID != instanceID)
                {
                    bool isEditor = InstanceManager.initializingContext == InstanceManager.InitializationContext.Editor || InstanceManager.initializingContext == InstanceManager.InitializationContext.Editor_Duplicate;

                    //If serialized instanceID is zero it means this is not a duplicate.
                    if (instanceID == 0)
                        initializingContext = isEditor ? InstanceManager.InitializationContext.Editor : InstanceManager.InitializationContext.Programmatically;
                    else if (newInstanceID < 0)
                        initializingContext = isEditor ? InstanceManager.InitializationContext.Editor_Duplicate : InstanceManager.InitializationContext.Programmatically_Duplicate;
                }

                _initializingContext = initializingContext;
            }

            return _initializingContext;
        }

        /// <summary>
        /// Initializes the object's unique identifiers.
        /// </summary>
        /// <param name="initializatingState"></param>
        protected virtual void InitializeUID(InstanceManager.InitializationContext initializingContext)
        {
            instanceID = GetInstanceID();
        }

        protected virtual void InitializeFields(InstanceManager.InitializationContext initializingContext)
        {
#if UNITY_EDITOR
            RenderingManager.UpdateIcon(this);
#endif
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
        protected virtual bool Initialize(InstanceManager.InitializationContext initializingContext)
        {
            if (!IsValidInitialization(initializingContext))
                return false;

            if (!isFallbackValues)
                InitializeFields(initializingContext);

            InitializeSerializedFields(initializingContext);

            UpdateAllDelegates();

            return true;
        }

        /// <summary>
        /// Provides the ability to interrupt the initialization.
        /// </summary>
        /// <param name="initializingContext"></param>
        /// <returns>False to interrupt the initialization.</returns>
        protected virtual bool IsValidInitialization(InstanceManager.InitializationContext initializingContext)
        {
            return true;
        }

        /// <summary>
        /// Initialize SerializedField's to their default values.
        /// </summary>
        /// <param name="initializingContext"></param>
        protected virtual void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
           
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, InstanceManager.InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, initializingContext);
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, T duplicateValue, InstanceManager.InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, () => { return duplicateValue; }, initializingContext);
        }

        /// <summary>
        /// Acts as a reliable constructor and will always by called unlike Awake which is sometimes skipped.
        /// </summary>
        protected virtual void Initialized()
        {
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
            Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformedHandler;
            if (!IsDisposing())
                Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformedHandler;

            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= Saving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= Saved;
            if (!IsDisposing())
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += Saving;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += Saved;
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
            if (!DisposeManager.TriggerOnDestroyIfNull(this))
            {
                InstanceManager.Initialize(this, InstanceManager.InitializationContext.Existing);
                IsUserChange(() =>
                {
                    UndoRedoPerformed();
                });
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

        private void RemoveDisposingDelegate(IDisposable disposable)
        {
            disposable.DisposingEvent -= ObjectDisposingHandler;
        }

        private void ObjectDisposingHandler(IDisposable disposable)
        {
            RemoveDisposingDelegate(disposable);
        }

        private void RemoveDisposedDelegate(IDisposable disposable)
        {
            disposable.DisposedEvent -= ObjectDisposedHandler;
        }

        private void ObjectDisposedHandler(IDisposable disposable)
        {
            RemoveDisposedDelegate(disposable);
        }

        private int instanceID
        {
            get { return _instanceID; }
            set { _instanceID = value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool SetValue<T>(string name, T value, ref T valueField, Action<T, T> assignedCallback = null)
        {
            if (HasChanged(value, valueField))
            {
                T oldValue = valueField;

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
            return (forceChangeDuringInitializing && !initialized) || !Object.Equals(newValue, oldValue);
        }

        public bool initialized
        {
            get { return _initialized; }
        }

        protected bool instanceAdded
        {
            get { return _instanceAdded; }
        }

        protected bool IsFallbackValues()
        {
            return isFallbackValues;
        }

        protected bool IsNotFallbackValues()
        {
            return !isFallbackValues;
        }

        public bool isFallbackValues
        {
            get { return _isFallbackValues; }
        }

        public IScriptableBehaviour originator
        {
            get { return _originator; }
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

        public InstanceManager.InitializationContext GetInitializeContext(InstanceManager.InitializationContext defaultState = InstanceManager.InitializationContext.Programmatically)
        {
            InstanceManager.InitializationContext initializeState = defaultState;

            if (_initializing && !_initialized)
                initializeState = GetinitializingContext();
            else if (_originator != null)
                initializeState = _originator.GetInitializeContext(initializeState);
            else if (_disposing)
            {
                switch (_destroyingContext)
                {
                    case DisposeManager.DisposeContext.Editor:
                        initializeState = InstanceManager.InitializationContext.Editor;
                        break;
                    case DisposeManager.DisposeContext.Editor_UndoRedo:
                        initializeState = InstanceManager.InitializationContext.Programmatically;
                        break;
                    case DisposeManager.DisposeContext.Programmatically:
                        initializeState = InstanceManager.InitializationContext.Programmatically;
                        break;
                }
            }

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

        public bool disposedComplete
        {
            get { return _disposedComplete; }
            set { _disposedComplete = value; }
        }

        /// <summary>
        /// Is the object destroying?
        /// </summary>
        /// <returns>True if the object as already been destroyed.</returns>
        protected bool IsDestroying()
        {
            return _disposing && _destroyingContext != DisposeManager.DisposeContext.Unknown;
        }

        public DisposeManager.DisposeContext destroyingContext
        {
            get { return _destroyingContext; }
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

        public Action InitializedEvent
        {
            get { return _initializedEvent; }
            set { _initializedEvent = value; }
        }

        public Action<IDisposable> DisposingEvent
        {
            get { return _disposingEvent; }
            set { _disposingEvent = value; }
        }

        public Action<IDisposable> DisposedEvent
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

        public virtual bool OnDisposing(DisposeManager.DisposeContext disposeContext)
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

        public bool UpdateDestroyingContext()
        {
            if (!_destroyingContextUpdated)
            {
                _destroyingContextUpdated = true;

                _destroyingContext = GetDestroyingContext();

                return true;
            }
            return false;
        }

        public void OnDisposedInternal(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            IsUserChange(() => { OnDisposed(disposeContext, pooled); }, disposeContext != DisposeManager.DisposeContext.Programmatically);
        }

        /// <summary>
        /// This is the last chance to clear or dipose any remaining references. It will be called immediately after the <see cref="DepictionEngine.IDisposable.UpdateDestroyingContext"/> unless a <see cref="DepictionEngine.DisposeManager.DisposeDelay"/> was passed to the <see cref="DepictionEngine.DisposeManager.Dispose"/> call.
        /// </summary>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        protected virtual bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposedEvent?.Invoke(this);
                DisposedEvent = null;

                InitializedEvent = null;
                DisposingEvent = null;
                DisposedEvent = null;

                SceneManager.UnityInitializedEvent -= UnityInitialized;
                UpdateAllDelegates();

                return true;
            }
            return false;
        }

        private void OnDestroyInternal()
        {
            UpdateDestroyingContext();
            OnDisposedInternal(_destroyingContext);
        }

        public void OnDestroy()
        {
            OnDisposing(DisposeManager.DisposeContext.Editor);

            if (GetDestroyingContext() != DisposeManager.DisposeContext.Unknown)
                OnDestroyInternal();
            else
            {
#if UNITY_EDITOR
                SceneManager.DelayedOnDestroyEvent += OnDestroyInternal;

                if (!_initialized)
                {
                    Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformed;
                    Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformed;
                }
#endif
            }
        }

        protected virtual DisposeManager.DisposeContext GetDestroyingContext()
        {
            DisposeManager.DisposeContext destroyingContext = DisposeManager.disposingContext;

            if (SceneManager.IsSceneBeingDestroyed())
                destroyingContext = DisposeManager.DisposeContext.Programmatically;

            return destroyingContext;
        }

        public virtual void ExplicitAwake()
        {
            if (!_initializing)
            {
                if (!_awake)
                {
                    _awake = true;

                    SceneManager.UnityInitializedEvent += UnityInitialized;
                }

                Initialize();
            }
        }

        private void UnityInitialized()
        {
            _unityInitialized = true;
        }

        private void Awake()
        {
            if (!InstanceManager.inhibitExplicitAwake)
                ExplicitAwake();
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

#if UNITY_EDITOR
        public void Reset()
        {
            if (_unityInitialized)
            {
                if (ResetAllowed())
                    SceneManager.Reseting(this);

                Editor.UndoManager.RevertAllInCurrentGroup();
            }
        }

        protected virtual bool ResetAllowed()
        {
            return true;
        }

        public void InspectorReset()
        {
            IsUserChange(() =>
            {
                InitializeSerializedFields(InstanceManager.InitializationContext.Reset);
            });
        }
#endif

        public DisposeManager.DisposeContext GetDisposeContext()
        {
            DisposeManager.DisposeContext disposeContext = DisposeManager.DisposeContext.Programmatically;

            if (IsDisposing())
                disposeContext = _destroyingContext;
            else if (_originator is not null)
                disposeContext = _originator.GetDisposeContext();

            return disposeContext;
        }

        protected void Dispose(object obj, DisposeManager.DisposeContext disposeContext = DisposeManager.DisposeContext.Unknown, DisposeManager.DisposeDelay disposeDelay = DisposeManager.DisposeDelay.None)
        {
            DisposeManager.Dispose(obj, disposeContext == DisposeManager.DisposeContext.Unknown ? GetDisposeContext() : disposeContext, disposeDelay);
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
