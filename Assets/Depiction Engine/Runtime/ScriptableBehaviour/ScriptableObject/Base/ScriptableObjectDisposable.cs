﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

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
            _requiresLateUpdate = default;
            _disposingContext = default;
            _initializingContext = default;

#if UNITY_EDITOR
            _lastNotPoolable = _notPoolable = default;
            _inspectorComponentNameOverride = default;
#endif
        }

        public bool Initialize()
        {
            if (!_initializing && !IsDisposing())
            {
                Initializing();

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

                bool isUserContext = _initializingContext == InitializationContext.Editor || _initializingContext == InitializationContext.Editor_Duplicate;
                if (isUserContext)
                    SceneManager.StartUserContext();

                if (!Initialize(_initializingContext))
                {
                    try
                    {
                        DestroyAfterFailedInitialization();
                    }
                    catch (MissingReferenceException) { }
                    abortInitialization = true;
                }

                if (isUserContext)
                    SceneManager.EndUserContext();

                if (abortInitialization)
                    return false;

                Initialized(_initializingContext);
                InitializedEvent?.Invoke(this);

#if UNITY_EDITOR
                RegisterInitializeObjectUndo(_initializingContext);
#endif

                _instanceAdded = AddToInstanceManager();
                return _instanceAdded;
            }

            return false;
        }

        protected bool IsDuplicateInitializing()
        {
            return instanceID != 0 && GetInstanceID() < 0;
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

            _requiresLateUpdate = true;

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

        protected bool InitValue<T>(Action<T> callback, T defaultValue, T duplicateValue, InitializationContext initializingContext)
        {
            return MonoBehaviourDisposable.InitValueInternal(callback, defaultValue, () => { return duplicateValue; }, initializingContext);
        }

        public virtual void Initialized(InitializationContext initializingContext)
        {
            //FallbackValues Component are really only used to display properties in the Inspector or to validate property change. By preventing initialized we limit the amount of code the object can execute.
            if (!isFallbackValues)
                _initialized = true;

            UpdateHideFlags();

            UpdateAllDelegates();
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
            if (!IsDisposing())
            {
                if (_requiresLateUpdate)
                    SceneManager.LateUpdateEvent += LateUpdateHandler;
            }

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= Saving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= Saved; 
            if (!IsDisposing())
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += Saving;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += Saved;
            }

            SceneManager.DebugChangedEvent -= SceneManagerDebugChangedHandler;
            if (!IsDisposing())
                SceneManager.DebugChangedEvent += SceneManagerDebugChangedHandler;
#endif

            return !isFallbackValues;
        }

#if UNITY_EDITOR
        protected virtual bool UpdateUndoRedoSerializedFields()
        {
            return !isFallbackValues;
        }

        /// <summary>
        /// Triggered right after an undo or redo operation was performed (Editor Only).
        /// </summary>
        public void UndoRedoPerformed()
        {
            UpdateUndoRedoSerializedFields();

            //Are the Destroy and Initialize check necessary?
            if (!DisposeManager.TriggerOnDestroyIfNull(this))
                InstanceManager.Initialize(this, InitializationContext.Existing);
        }

        private string _inspectorComponentNameOverride;
        public string inspectorComponentNameOverride
        {
            get => _inspectorComponentNameOverride;
            set => _inspectorComponentNameOverride = value;
        }

                private void SceneManagerDebugChangedHandler(bool debug)
        {
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

        private void Saved(UnityEngine.SceneManagement.Scene scene)
        {
            UpdateHideFlags();
        }

        private bool _requiresLateUpdate;
        private void LateUpdateHandler()
        {
            _requiresLateUpdate = false;
        }

        private int instanceID
        {
            get => _instanceID;
            set => _instanceID = value;
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

        public bool wasFirstUpdated { get => _initialized && !_requiresLateUpdate; }

        public bool initialized { get => _initialized; }

        protected bool instanceAdded { get => _instanceAdded; set => _instanceAdded = value; }

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

        public bool poolComplete
        {
            get => _poolComplete;
            set => _poolComplete = value;
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

        public Action<IDisposable, DisposeContext> DisposedEvent
        {
            get => _disposedEvent;
            set => _disposedEvent = value;
        }

        protected SceneManager sceneManager { get => SceneManager.Instance(); }
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

        [HideInCallstack]
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
                return true;
            }
            return false;
        }

        public bool UpdateDisposingContext(bool forceUpdate = false)
        {
            if (!_disposingContextUpdated || forceUpdate)
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
            bool isUserContext = disposeContext == DisposeContext.Editor_Destroy;
            if (isUserContext)
                SceneManager.StartUserContext();

            OnDispose(disposeContext);

            if (isUserContext)
                SceneManager.EndUserContext();
        }

        public virtual bool OnDispose(DisposeContext disposeContext)
        {
            if (!_disposed || ((disposeContext == DisposeContext.Programmatically_Destroy || disposeContext == DisposeContext.Editor_Destroy) && !DisposeManager.IsUnityNull(this)))
            {
                _disposed = true;

                DisposedEvent?.Invoke(this, disposeContext);

                UpdateAllDelegates();

                InitializedEvent = null;
                DisposedEvent = null;

                return initialized;
            }
            return false;
        }

        private void OnDestroyInternal()
        {
#if UNITY_EDITOR
            Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformed;
#endif
            UpdateDisposingContext(true);
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
                Editor.UndoManager.UndoRedoPerformedEvent -= UndoRedoPerformed;
                Editor.UndoManager.UndoRedoPerformedEvent += UndoRedoPerformed;
            }
            else
#endif
                OnDestroyInternal();
        }

        protected virtual DisposeContext GetDisposingContext()
        {
            DisposeContext disposingContext = DisposeManager.disposingContext;

#if UNITY_EDITOR
            if (SceneManager.IsSceneBeingDestroyed() || SceneManager.Instance(false) == Disposable.NULL)
                disposingContext = DisposeContext.Programmatically_Destroy;
#endif

            return disposingContext;
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void OnDisable()
        {

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
            Editor.UndoManager.RegisterCompleteObjectUndo(this, initializingContext);
        }

        public void RegisterCompleteObjectUndo()
        {
            Editor.UndoManager.RegisterCompleteObjectUndo(this);
        }

        public void RegisterCompleteObjectUndo(DisposeContext disposeContext)
        {
            Editor.UndoManager.RegisterCompleteObjectUndo(this, disposeContext);
        }

        public void RegisterCompleteObjectUndo(InitializationContext initializingContext)
        {
            Editor.UndoManager.RegisterCompleteObjectUndo(this, initializingContext);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public static bool operator !=(ScriptableObjectDisposable lhs, Disposable.Null rhs) { return !(lhs == rhs); }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public static bool operator ==(ScriptableObjectDisposable lhs, Disposable.Null _) => DisposeManager.IsNullOrDisposing(lhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public bool Equals(Disposable.Null value) { return this == value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public override bool Equals(object value) { return base.Equals(value); }

        [MethodImpl(MethodImplOptions.AggressiveInlining), HideInCallstack]
        public override int GetHashCode() { return base.GetHashCode(); }
    }
}
