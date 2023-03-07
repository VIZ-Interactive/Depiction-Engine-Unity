// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class ReferenceBase : Script
    {
        [BeginFoldout("Reference")]
        [SerializeField, ComponentReference, Tooltip("The id of the loader containing the referenced data.")]
        private SerializableGuid _loaderId;

        [SerializeField, Tooltip("An id key used to retreive the data from an "+nameof(IdLoader)+".")]
        private SerializableGuid _dataId;

        [SerializeField, Tooltip("A 2D grid index key used to retreive the data from an "+nameof(Index2DLoaderBase)+".")]
        private Grid2DIndex _dataIndex2D;

        [Space]

        [SerializeField, Tooltip("The referenced data (Read Only).")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowData)), ConditionalEnable(nameof(GetEnableData))]
#endif
        private PersistentScriptableObject _data;

        [SerializeField, Tooltip("The referenced data current loading state (Read Only).")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowLoadingState)), ConditionalEnable(nameof(GetEnableLoadingState)), EndFoldout]
#endif
        private DatasourceOperationBase.LoadingState _loadingState;

        [SerializeField, HideInInspector]
        private SerializableGuid _loadedOrfailedIdLoadScope;
        [SerializeField, HideInInspector]
        private Grid2DIndex _loadedOrfailedIndexLoadScope;

        [SerializeField, HideInInspector]
        private LoaderBase _loader;
        [SerializeField, HideInInspector]
        private LoadScope _loadScope;

        [HideInInspector]
        public bool managedByObject;

        /// <summary>
        /// Dispatched when the <see cref="DepictionEngine.ReferenceBase.data"/> changed.
        /// </summary>
        public Action<ReferenceBase, PersistentScriptableObject, PersistentScriptableObject> DataChangedEvent;
        /// <summary>
        /// Dispatched when a property assignment is detected in the <see cref="DepictionEngine.ReferenceBase.loader"/>. 
        /// </summary>
        public Action<ReferenceBase, IProperty, string, object, object> LoaderPropertyAssignedChangedEvent;

#if UNITY_EDITOR
        private bool GetShowData()
        {
            return !isFallbackValues;
        }

        private bool GetEnableData()
        {
            return false;
        }

        private bool GetShowLoadingState()
        {
            return !isFallbackValues;
        }

        private bool GetEnableLoadingState()
        {
            return false;
        }
#endif

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (_loaderId != null)
                callback(_loaderId, UpdateLoader);
        }

        public override void Recycle()
        {
            base.Recycle();

            _loader = null;
            _loadScope = null;
            _data = null;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastData = data;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            AddReferenceFromLoader(loadScope);
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => loaderId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(loaderId, loader, initializingContext); }, initializingContext);
            InitValue(value => dataId = value, SerializableGuid.Empty, initializingContext);
            InitValue(value => dataIndex2D = value, Grid2DIndex.empty, initializingContext);
        }

#if UNITY_EDITOR
        private PersistentScriptableObject _lastData;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            Editor.UndoManager.PerformUndoRedoPropertyChange((value) => { data = value; }, ref _data, ref _lastData);
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveLoaderDelegate(loader);
                AddLoaderDelegate(loader);

                RemoveLoadScopeDelegate(loadScope);
                AddLoadScopeDelegate(loadScope);

                RemoveDataDelegates(data);
                AddDataDelegates(data);

                return true;
            }
            return false;
        }

        private void RemoveLoaderDelegate(LoaderBase loader)
        {
            if (!Object.ReferenceEquals(loader, null))
                loader.PropertyAssignedEvent -= LoaderPropertyAssignedHandler;
        }

        private void AddLoaderDelegate(LoaderBase loader)
        {
            if (!IsDisposing() && loader != Disposable.NULL)
                loader.PropertyAssignedEvent += LoaderPropertyAssignedHandler;
        }

        private void LoaderPropertyAssignedHandler(IProperty serializable, string name, object newValue, object oldValue)
        {
            if (name == nameof(LoaderBase.datasource))
                ForceUpdateLoadScope();

            if (LoaderPropertyAssignedChangedEvent != null)
                LoaderPropertyAssignedChangedEvent(this, serializable, name, newValue, oldValue);
        }

        private void RemoveLoadScopeDelegate(LoadScope loadScope)
        {
            if (!Object.ReferenceEquals(loadScope, null))
            {
                loadScope.DisposingEvent -= LoadScopeDisposingHandler;
                loadScope.LoadingStateChangedEvent -= LoadScopeChangedHandler;
                loadScope.PersistentAddedEvent -= LoadScopeChangedHandler;
            }
        }

        private void AddLoadScopeDelegate(LoadScope loadScope)
        {
            if (!IsDisposing() && loadScope != Disposable.NULL)
            {
                loadScope.DisposingEvent += LoadScopeDisposingHandler;
                loadScope.LoadingStateChangedEvent += LoadScopeChangedHandler;
                loadScope.PersistentAddedEvent += LoadScopeChangedHandler;
            }
        }

        private void LoadScopeDisposingHandler(IDisposable disposable)
        {
            SetLoadScope(null);
        }

        private void LoadScopeChangedHandler(LoadScope loadScope)
        {
            UpdateData();
        }

        private void RemoveDataDelegates(ScriptableObjectDisposable data)
        {
            if (!Object.ReferenceEquals(data, null))
                data.DisposingEvent -= DataDisposingHandler;
        }

        private void AddDataDelegates(ScriptableObjectDisposable data)
        {
            if (!IsDisposing() && data != Disposable.NULL)
                data.DisposingEvent += DataDisposingHandler;
        }

        private void DataDisposingHandler(IDisposable disposable)
        {
            SetData(null);
        }

        public LoaderBase loader
        {
            get { return _loader; }
            private set { loaderId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        /// <summary>
        /// The id of the loader containing the referenced data.
        /// </summary>
        [Json]
        public SerializableGuid loaderId
        {
            get { return _loaderId ; }
            set 
            { 
                SetValue(nameof(loaderId), value, ref _loaderId, (newValue, oldValue) => 
                {
                    UpdateLoader();
                }); 
            }
        }

        private void UpdateLoader()
        {
            SetValue(nameof(loader), GetComponentFromId<LoaderBase>(loaderId), ref _loader, (newValue, oldValue) =>
            {
                if (initialized)
                {
                    RemoveLoaderDelegate(oldValue);
                    AddLoaderDelegate(newValue);
                }

                ForceUpdateLoadScope();
            });
        }

        /// <summary>
        /// An id key used to retreive the data from an <see cref="DepictionEngine.IdLoader"/>.
        /// </summary>
        [Json]
        public SerializableGuid dataId
        {
            get { return _dataId; }
            set
            {
                SetValue(nameof(dataId), value, ref _dataId, (newValue, oldValue) =>
                {
                    ForceUpdateLoadScope();
                });
            }
        }

        /// <summary>
        /// A 2D grid index key used to retreive the data from an <see cref="DepictionEngine.Index2DLoaderBase"/>.
        /// </summary>
        [Json]
        public Grid2DIndex dataIndex2D
        {
            get { return _dataIndex2D; }
            set
            {
                SetValue(nameof(dataIndex2D), value, ref _dataIndex2D, (newValue, oldValue) =>
                {
                    ForceUpdateLoadScope();
                });
            }
        }

        private void ForceUpdateLoadScope()
        {
            _loadedOrfailedIdLoadScope = SerializableGuid.Empty;
            _loadedOrfailedIndexLoadScope = Grid2DIndex.empty;

            UpdateLoadScope();
        }

        public LoadScope loadScope
        {
            get { return _loadScope; }
            private set { SetLoadScope(value); }
        }

        private bool UpdateLoadScope()
        {
            LoadScope loadScope = null;

            if (loader != Disposable.NULL)
            {
                bool createLoadScopeIfMissing = !SceneManager.IsSceneBeingDestroyed() && !WasLoaded();

                IdLoader idLoader = loader as IdLoader;
                if (idLoader != Disposable.NULL)
                {
                    if (idLoader != Disposable.NULL && idLoader.GetLoadScope(out IdLoadScope idLoadScope, dataId, createIfMissing: createLoadScopeIfMissing))
                        loadScope = idLoadScope;
                    else
                        loadScope = null;
                }

                Index2DLoader index2DLoader = loader as Index2DLoader;
                if (index2DLoader != Disposable.NULL)
                { 
                    if (index2DLoader != Disposable.NULL && index2DLoader.GetLoadScope(out Index2DLoadScope index2DLoadScope, dataIndex2D.dimensions, dataIndex2D.index, createIfMissing: createLoadScopeIfMissing))
                        loadScope = index2DLoadScope;
                    else
                        loadScope = null;
                }
            }

            return SetLoadScope(loadScope != Disposable.NULL ? loadScope : null);
        }

        private bool SetLoadScope(LoadScope value)
        {
            return SetValue(nameof(loadScope), value, ref _loadScope, (newValue, oldValue) => 
            {
                if (initialized)
                {
                    if (!Object.ReferenceEquals(oldValue, null))
                    {
                        RemoveLoadScopeDelegate(oldValue);
                        RemoveReferenceFromLoader(oldValue);
                    }
                    if (newValue != Disposable.NULL)
                    {
                        AddReferenceFromLoader(newValue);
                        AddLoadScopeDelegate(newValue);
                    }

                    if (newValue is IdLoadScope)
                    {
                        IdLoadScope index2DLoadScope = newValue as IdLoadScope;
                        _loadedOrfailedIdLoadScope = index2DLoadScope.scopeId;
                    }
                    if (newValue is Index2DLoadScope)
                    {
                        Index2DLoadScope index2DLoadScope = newValue as Index2DLoadScope;
                        _loadedOrfailedIndexLoadScope = new Grid2DIndex(index2DLoadScope.scopeIndex, index2DLoadScope.scopeDimensions);
                    }
                }

                UpdateData();
            });
        }

        private void RemoveReferenceFromLoader(LoadScope loadScope)
        {
            if (loadScope != Disposable.NULL && loadScope.loader != Disposable.NULL)
                loadScope.loader.RemoveReference(loadScope, this);
        }

        private void AddReferenceFromLoader(LoadScope loadScope)
        {
            if (loadScope != Disposable.NULL && loadScope.loader != Disposable.NULL)
                loadScope.loader.AddReference(loadScope, this);
        }

        private bool UpdateData()
        {
            PersistentScriptableObject data = null;

            if (loadScope != Disposable.NULL)
            {
                loadingState = loadScope.loadingState;
                if (loadingState == DatasourceOperationBase.LoadingState.Loaded)
                    data = loadScope.GetFirstPersistent() as PersistentScriptableObject;
            }
            else
                loadingState = DatasourceOperationBase.LoadingState.None;

            return SetData(data != Disposable.NULL ? data : null);
        }

        /// <summary>
        /// The referenced data (Read Only).
        /// </summary>
        public PersistentScriptableObject data
        {
            get { return _data; }
            private set { SetData(value); }
        }

        private bool SetData(PersistentScriptableObject value)
        {
            return SetValue(nameof(data), value, ref _data, (newValue, oldValue) =>
            {
#if UNITY_EDITOR
                _lastData = newValue;
#endif
                RemoveDataDelegates(oldValue);
                AddDataDelegates(newValue);

                DataChanged(newValue, oldValue);
            });
        }

        protected virtual void DataChanged(PersistentScriptableObject newValue, PersistentScriptableObject oldValue)
        {
            DataChangedEvent?.Invoke(this, newValue, oldValue);
        }

        private bool WasLoaded()
        {
            if (loader is IdLoader)
                return _loadedOrfailedIdLoadScope != SerializableGuid.Empty && dataId == _loadedOrfailedIdLoadScope;
            if (loader is Index2DLoaderBase)
                return _loadedOrfailedIndexLoadScope != Grid2DIndex.empty && dataIndex2D == _loadedOrfailedIndexLoadScope;
            
            return false;
        }

        public bool IsLoaded()
        {
            return loader == Disposable.NULL || loadingState == DatasourceOperationBase.LoadingState.Failed || loadingState == DatasourceOperationBase.LoadingState.Loaded;
        }

        /// <summary>
        /// The referenced data current loading state (Read Only).
        /// </summary>
        public DatasourceOperationBase.LoadingState loadingState
        {
            get { return _loadingState; }
            private set
            {
                if (_loadingState == value)
                    return;

                _loadingState = value;
            }
        }

        public override bool OnDisposing(DisposeManager.DisposeContext disposeContext)
        {
            if (base.OnDisposing(disposeContext))
            {
                RemoveReferenceFromLoader(loadScope);

                return true;
            }
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled)
        {
            if (base.OnDisposed(disposeContext, pooled))
            {
                DataChangedEvent = null;
                LoaderPropertyAssignedChangedEvent = null;

                return true;
            }
            return false;
        }
    }
}
