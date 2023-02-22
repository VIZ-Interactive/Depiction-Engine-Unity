// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
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
        private bool _dispose;
        private bool _disposed;
        private bool _disposedComplete;

        private InstanceManager.InitializationContext _initializingState;
        private DisposeManager.DestroyContext _destroyingContext;
        private IScriptableBehaviour _originator;
        private bool _isUserChange;

        private Action _initializedEvent;
        private Action<IDisposable> _disposeEvent;
        private Action<IDisposable> _disposedEvent;

#if UNITY_EDITOR
        protected bool GetDebug()
        {
            return sceneManager.debug;
        }
#endif

        public virtual void Recycle()
        {
            name = PoolManager.NEW_GAME_OBJECT_NAME;
            hideFlags = HideFlags.None;

            _instanceID = 0;
            _isFallbackValues = false;
            _awake = _initializing = _initialized = _instanceAdded = _disposing = _dispose = _disposed = _disposedComplete = false;
            _destroyingContext = DisposeManager.DestroyContext.Unknown;
            _initializingState = InstanceManager.InitializationContext.Unknown;

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

                InstanceManager.InitializationContext initializingState = GetInitializingState();

                InitializeUID(initializingState);

                bool abortInitialization = false;

                IsUserChange(() =>
                {
                    if (!Initialize(initializingState))
                        abortInitialization = true;
                }, initializingState == InstanceManager.InitializationContext.Editor || initializingState == InstanceManager.InitializationContext.Editor_Duplicate);

                if (abortInitialization)
                    return false;

                Initialized(initializingState);
                if (InitializedEvent != null)
                    InitializedEvent();

#if UNITY_EDITOR
                RegisterInitializeObjectUndo(initializingState);
#endif

                _instanceAdded = AddToInstanceManager();
                return _instanceAdded;
            }

            return false;
        }

#if UNITY_EDITOR
        protected virtual void RegisterInitializeObjectUndo(InstanceManager.InitializationContext initializingState)
        {
            Editor.UndoManager.RegisterCompleteObjectUndo(this, initializingState);
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
        private InstanceManager.InitializationContext GetInitializingState()
        {
            InstanceManager.InitializationContext newInitializingState = InstanceManager.InitializationContext.Existing_Or_Editor_UndoRedo;

            if (_initializingState == InstanceManager.InitializationContext.Existing_Or_Editor_UndoRedo || _initializingState == InstanceManager.InitializationContext.Editor_Duplicate || _initializingState == InstanceManager.InitializationContext.Programmatically_Duplicate)
                newInitializingState = _initializingState;
            else
            {
                InstanceManager.InitializationContext initializingState = InstanceManager.initializingState;
                int newInstanceID = GetInstanceID();
                if (newInstanceID != instanceID)
                {
                    if (instanceID == 0)
                        newInitializingState = initializingState == InstanceManager.InitializationContext.Programmatically ? InstanceManager.InitializationContext.Programmatically : InstanceManager.InitializationContext.Editor;
                    else if (newInstanceID < 0)
                        newInitializingState = _initializingState == InstanceManager.InitializationContext.Programmatically ? InstanceManager.InitializationContext.Programmatically_Duplicate : InstanceManager.InitializationContext.Editor_Duplicate;
                }
            }

            return newInitializingState;
        }

        /// <summary>
        /// Initializes the object's unique identifiers.
        /// </summary>
        /// <param name="initializatingState"></param>
        protected virtual void InitializeUID(InstanceManager.InitializationContext initializatingState)
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
        /// <param name="initializingState"></param>
        /// <returns>False if the initialization failed.</returns>
        protected virtual bool Initialize(InstanceManager.InitializationContext initializatingState)
        {
            if (!IsValidInitialization(initializatingState))
                return false;

            if (!isFallbackValues)
                InitializeFields(initializatingState);

            InitializeSerializedFields(initializatingState);

            UpdateAllDelegates();

            return true;
        }

        /// <summary>
        /// Provides the ability to interrupt the initialization.
        /// </summary>
        /// <param name="initializingState"></param>
        /// <returns>False to interrupt the initialization.</returns>
        protected virtual bool IsValidInitialization(InstanceManager.InitializationContext initializingState)
        {
            return true;
        }

        protected virtual void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
#if UNITY_EDITOR
            RenderingManager.UpdateIcon(this);
#endif
        }

        /// <summary>
        /// Initialize SerializedField's to their default values.
        /// </summary>
        /// <param name="initializingState"></param>
        protected virtual void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, InstanceManager.InitializationContext initializingState, bool reset = true)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, initializingState, reset);
        }

        protected bool InitValue<T>(Action<T> callback, T defaultValue, Func<T> duplicateValue, InstanceManager.InitializationContext initializingState, bool reset = true)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, duplicateValue, initializingState, reset);
        }

        public static bool InitValueInternal<T>(Action<T> callback, T defaultValue, Func<T> duplicateValue, InstanceManager.InitializationContext initializingState, bool reset = true)
        {
            if (initializingState == InstanceManager.InitializationContext.Editor_Duplicate || initializingState == InstanceManager.InitializationContext.Programmatically_Duplicate)
            {
                callback(duplicateValue());
                return true;
            }
            else
                return InitValueInternal(callback, defaultValue, initializingState, reset);
        }

        public static bool InitValueInternal<T>(Action<T> callback, T defaultValue, InstanceManager.InitializationContext initializingState, bool reset = true)
        {
            if (initializingState == InstanceManager.InitializationContext.Editor || initializingState == InstanceManager.InitializationContext.Programmatically || (initializingState == InstanceManager.InitializationContext.Reset && reset))
            {
                callback(defaultValue);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Acts as a reliable constructor and will always by called unlike Awake which is sometimes skipped.
        /// </summary>
        protected virtual void Initialized(InstanceManager.InitializationContext initializingState)
        {
            _initializingState = InstanceManager.InitializationContext.Unknown;
            if (!isFallbackValues)
                _initialized = true;

            UpdateHideFlags();
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
        /// Called after <see cref="DepictionEngine.Disposable.Initialize"/> and when the component enable state changed.
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
            IsUserChange(() =>
            {
                UndoRedoPerformed();
            });

            if (!DisposeManager.TriggerOnDestroyIfNull(this))
                InstanceManager.Initialize(this, InstanceManager.InitializationContext.Existing_Or_Editor_UndoRedo);
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
            get { return _inspectorComponentNameOverride; }
            set { _inspectorComponentNameOverride = value; }
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

        private void RemoveDisposingDelegate(IDisposable disposable)
        {
            disposable.DisposeEvent -= ObjectDisposeHandler;
        }

        private void ObjectDisposeHandler(IDisposable disposable)
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
            T oldValue = valueField;

            if (HasChanged(value, oldValue))
            {
                valueField = value;

                if (assignedCallback != null)
                    assignedCallback(value, oldValue);

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
            if (forceChangeDuringInitializing && !initialized)
                return true;

            if (newValue is IList && oldValue is IList && newValue.GetType() == oldValue.GetType())
            {
                IList newList = newValue as IList;
                IList oldList = oldValue as IList;

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
            return _isUserChange || (!Object.ReferenceEquals(_originator, null) && _originator.IsUserChangeContext());
        }

        public InstanceManager.InitializationContext GetInitializeState(InstanceManager.InitializationContext defaultState = InstanceManager.InitializationContext.Programmatically)
        {
            InstanceManager.InitializationContext initializeState = defaultState;

            if (_initializing && !_initialized)
                initializeState = _initializingState;
            else if (_originator != null)
                initializeState = _originator.GetInitializeState(initializeState);
            else if (_disposing)
            {
                switch (_destroyingContext)
                {
                    case DisposeManager.DestroyContext.Editor:
                        initializeState = InstanceManager.InitializationContext.Editor;
                        break;
                    case DisposeManager.DestroyContext.Editor_UndoRedo:
                        initializeState = InstanceManager.InitializationContext.Existing_Or_Editor_UndoRedo;
                        break;
                    case DisposeManager.DestroyContext.Programmatically:
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
            return _disposing && _destroyingContext != DisposeManager.DestroyContext.Unknown;
        }

        public DisposeManager.DestroyContext destroyingContext
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

        public Action<IDisposable> DisposeEvent
        {
            get { return _disposeEvent; }
            set { _disposeEvent = value; }
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

        protected InputManager inputManager
        {
            get { return InputManager.Instance(); }
        }

        protected CameraManager cameraManager
        {
            get { return CameraManager.Instance(); }
        }

        protected PoolManager poolManager
        {
            get { return PoolManager.Instance(); }
        }

        protected RenderingManager renderingManager
        {
            get { return RenderingManager.Instance(); }
        }

        protected virtual bool UpdateHideFlags()
        {
            if (this != null)
            {
                if (!IsDisposing())
                    hideFlags = HideFlags.None;
                else
                {
                    bool debug = false;

                    if (!SceneManager.IsSceneBeingDestroyed())
                        debug = sceneManager.debug;

                    hideFlags = debug ? HideFlags.DontSave : HideFlags.HideAndDontSave;
                }
                return true;
            }
            return false;
        }

        public T Duplicate<T>(T objectToDuplicate) where T : UnityEngine.Object
        {
            if (DisposeManager.IsNullOrDisposing(objectToDuplicate))
                return null;
            return InstanceManager.Duplicate(objectToDuplicate, GetInitializeState());
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

        public virtual bool OnDisposing()
        {
            if (!_disposing)
            {
                _disposing = true;

                if (this != null)
                    UpdateHideFlags();

                return true;
            }
            return false;
        }

        public virtual bool OnDispose()
        {
            if (!_dispose)
            {
                _dispose = true;

                if (_destroyingContext == DisposeManager.DestroyContext.Unknown)
                    _destroyingContext = DisposeManager.destroyingContext;

                if (DisposeEvent != null)
                    DisposeEvent(this);
                DisposeEvent = null;

                return true;
            }
            return false;
        }

        public void OnDisposedInternal(DisposeManager.DestroyContext destroyContext)
        {
            IsUserChange(() => { OnDisposed(destroyContext); }, destroyContext != DisposeManager.DestroyContext.Programmatically);
        }

        /// <summary>
        /// This is the last chance to clear or dipose any remaining references. It will be called immediately after the <see cref="DepictionEngine.IDisposable.OnDispose"/> unless a <see cref="DepictionEngine.DisposeManager.DestroyDelay"/> was passed to the <see cref="DepictionEngine.DisposeManager.Dispose"/> call.
        /// </summary>
        /// <param name="destroyContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        protected virtual bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (DisposedEvent != null)
                    DisposedEvent(this);
                DisposedEvent = null;

                SceneManager.UnityInitializedEvent -= UnityInitialized;
                UpdateAllDelegates();

                return true;
            }
            return false;
        }

        private void OnDestroyInternal()
        {
            OnDispose();
            OnDisposedInternal(_destroyingContext);
        }

        public virtual void OnDestroy()
        {
            OnDisposing();

            _destroyingContext = OverrideDestroyingContext(destroyingContext);

            if (_destroyingContext != DisposeManager.DestroyContext.Editor_Unknown)
                OnDestroyInternal();
            else
            {
                _destroyingContext = DisposeManager.DestroyContext.Editor;
                SceneManager.DelayedOnDestroyEvent += OnDestroyInternal;
#if UNITY_EDITOR
                if (!_initialized)
                {
                    Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformed;
                    Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformed;
                }
#endif
            }
        }

        protected virtual DisposeManager.DestroyContext OverrideDestroyingContext(DisposeManager.DestroyContext destroyingContext)
        {
            if (SceneManager.IsSceneBeingDestroyed())
                destroyingContext = DisposeManager.DestroyContext.Programmatically;

            return destroyingContext;
        }

        public virtual void ExplicitAwake()
        {
            if (!_initializing)
            {
                _initializingState = InstanceManager.initializingState;

                if (!_awake)
                {
                    _awake = true;

                    SceneManager.UnityInitializedEvent += UnityInitialized;
                }

#if UNITY_EDITOR
                if (GetInitializingState() == InstanceManager.InitializationContext.Editor_Duplicate)
                    Initialize();
#endif
            }
        }

        private void UnityInitialized()
        {
            _unityInitialized = true;
        }

        private void Awake()
        {
            //Create the SceneManager if this is the first MonoBehaviour created
            SceneManager.Instance();

            if (!InstanceManager.inhibitExplicitAwake)
                ExplicitAwake();
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
                SceneManager.Reseting(this);

                Editor.UndoManager.RevertAllInCurrentGroup();
            }
        }

        public void InspectorReset()
        {
            IsUserChange(() =>
            {
                InitializeSerializedFields(InstanceManager.InitializationContext.Reset);
            });
        }
#endif

        public DisposeManager.DestroyContext GetDestroyContext()
        {
            DisposeManager.DestroyContext destroyContext = DisposeManager.DestroyContext.Unknown;

            if (IsDisposing())
                destroyContext = _destroyingContext;
            else if (!Object.ReferenceEquals(_originator, null))
                destroyContext = _originator.GetDestroyContext();

            return destroyContext;
        }

        protected void Dispose(object obj, DisposeManager.DestroyDelay destroyDelay = DisposeManager.DestroyDelay.None, DisposeManager.DestroyContext destroyContext = DisposeManager.DestroyContext.Unknown)
        {
            DisposeManager.Dispose(obj, destroyContext == DisposeManager.DestroyContext.Unknown ? GetDestroyContext() : destroyContext, destroyDelay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MonoBehaviourDisposable lhs, Disposable.Null rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MonoBehaviourDisposable lhs, Disposable.Null rhs)
        {
            return DisposeManager.IsNullOrDisposing(lhs);
        }

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
