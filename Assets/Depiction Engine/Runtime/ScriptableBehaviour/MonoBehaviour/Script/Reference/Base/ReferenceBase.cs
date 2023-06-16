// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class ReferenceBase : Script
    {
        [BeginFoldout("Reference")]
        [SerializeField, Tooltip("Optional metadata used to identify the type of data referenced.")]
        private string _dataType;

        [SerializeField, ComponentReference, Tooltip("The id of the loader containing the referenced data.")]
        private SerializableGuid _loaderId;

        [SerializeField, Tooltip("An id key used to retrieve the data from an "+nameof(IdLoader)+".")]
        private SerializableGuid _dataId;

        [SerializeField, Tooltip("A 2D grid index key used to retrieve the data from an "+nameof(Index2DLoaderBase)+".")]
        private Grid2DIndex _dataIndex2D;

        [Space]

        [SerializeField, Tooltip("The referenced data (Read Only).")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowData)), ConditionalEnable(nameof(GetEnableData))]
#endif
        private PersistentScriptableObject _data;

        [SerializeField, Tooltip("The referenced data current loading state (Read Only).")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowLoadingState)), ConditionalEnable(nameof(GetEnableLoadingState))]
#endif
        private DatasourceOperationBase.LoadingState _loadingState;

#if UNITY_EDITOR
        [SerializeField, Tooltip("Ask the loader for a matching loadScope.")]
        [Button(nameof(LoadBtn)), ConditionalEnable(nameof(GetEnableLoad)), EndFoldout]
        private bool _load;
#endif

        [SerializeField, HideInInspector]
        private SerializableGuid _loadedOrFailedIdLoadScope;
        [SerializeField, HideInInspector]
        private Grid2DIndex _loadedOrFailedIndexLoadScope;

        [SerializeField, HideInInspector]
        private LoaderBase _loader;
        [SerializeField, HideInInspector]
        private LoadScope _loadScope;

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

        private bool GetEnableLoad()
        {
            return !isFallbackValues && loadScope == Disposable.NULL;
        }

        private void LoadBtn()
        {
            ForceUpdateLoadScope();
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

            _loader = default;
            _loadScope = default;
            _data = default;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            LoaderBase lastLoader = null;

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                _loaderId = default;
                lastLoader = _loader;
                _loader = default;
                _loadScope = default;
                _data = default;
                _loadingState = default;
                _loadedOrFailedIdLoadScope = default;
                _loadedOrFailedIndexLoadScope = default;
            }

            if (initializingContext == InitializationContext.Existing)
            {
#if UNITY_EDITOR
                RecoverLoadScopeAndData();
#endif
            }

            InitValue(value => dataType = value, "", initializingContext);
            InitValue(value => loaderId = value, SerializableGuid.Empty, () => GetDuplicateComponentReferenceId(loaderId, lastLoader, initializingContext), initializingContext);
            InitValue(value => dataId = value, SerializableGuid.Empty, initializingContext);
            InitValue(value => dataIndex2D = value, Grid2DIndex.Empty, initializingContext);
        }

        protected override bool PostLateInitialize(InitializationContext initializingContext)
        {
            if (base.PostLateInitialize(initializingContext))
            {
                //Executed in PostLateInitialize because we have to wait for all potentially duplicating FallbackValues to update their loader id in the event of a bulk object duplicate.
                if (!UpdateLoadScope())
                    UpdateData();

                return true;
            }
            return false;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastDataType = dataType;
                _lastLoaderId = loaderId;
                _lastDataId = dataId;
                _lastDataIndex2D = dataIndex2D;
                _lastLoadScope = loadScope;
                _lastData = data;
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private string _lastDataType;
        private SerializableGuid _lastLoaderId;
        private SerializableGuid _lastDataId;
        private Grid2DIndex _lastDataIndex2D;
        private LoadScope _lastLoadScope;
        private PersistentScriptableObject _lastData;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { dataType = value; }, ref _dataType, ref _lastDataType);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { loaderId = value; }, ref _loaderId, ref _lastLoaderId);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { dataId = value; }, ref _dataId, ref _lastDataId);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { dataIndex2D = value; }, ref _dataIndex2D, ref _lastDataIndex2D);

                RecoverLoadScopeAndData();
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { loadScope = value; }, ref _loadScope, ref _lastLoadScope);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { data = value; }, ref _data, ref _lastData);

                return true;
            }
            return false;
        }

        private void RecoverLoadScopeAndData()
        {
            SerializationUtility.RecoverLostReferencedObject(ref _loadScope);
            SerializationUtility.RecoverLostReferencedObject(ref _data);
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
            if (loader is not null)
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

            LoaderPropertyAssignedChangedEvent?.Invoke(this, serializable, name, newValue, oldValue);
        }

        private void RemoveLoadScopeDelegate(LoadScope loadScope)
        {
            if (loadScope is not null)
            {
                loadScope.DisposedEvent -= LoadScopeDisposedHandler;
                loadScope.LoadingStateChangedEvent -= LoadScopeChangedHandler;
                loadScope.PersistentAddedEvent -= LoadScopeChangedHandler;
                loadScope.PersistentRemovedEvent -= LoadScopeChangedHandler;
            }
        }

        private void AddLoadScopeDelegate(LoadScope loadScope)
        {
            if (!IsDisposing() && loadScope != Disposable.NULL)
            {
                loadScope.DisposedEvent += LoadScopeDisposedHandler;
                loadScope.LoadingStateChangedEvent += LoadScopeChangedHandler;
                loadScope.PersistentAddedEvent += LoadScopeChangedHandler;
                loadScope.PersistentRemovedEvent += LoadScopeChangedHandler;
            }
        }

        private void LoadScopeDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            SetLoadScope(null, disposeContext);
        }

        private void LoadScopeChangedHandler(LoadScope loadScope)
        {
            UpdateData(SceneManager.GetIsUserChangeContext() ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Pool);
        }

        private void RemoveDataDelegates(ScriptableObjectDisposable data)
        {
            if (data is not null)
                data.DisposedEvent -= DataDisposedHandler;
        }

        private void AddDataDelegates(ScriptableObjectDisposable data)
        {
            if (!IsDisposing() && data != Disposable.NULL)
                data.DisposedEvent += DataDisposedHandler;
        }

        private void DataDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            SetData(null, disposeContext);
        }

        /// <summary>
        /// Optional metadata used to identify the type of data referenced.
        /// </summary>
        [Json]
        public string dataType
        {
            get => _dataType;
            set
            {
                SetValue(nameof(dataType), value, ref _dataType, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastDataType = newValue;
                    UpdateInspectorComponentNameOverride();
#endif
                });
            }
        }

#if UNITY_EDITOR
        protected override string GetInspectorComponentNameOverride()
        {
            return base.GetInspectorComponentNameOverride() + (string.IsNullOrEmpty(dataType) ? "" : " (" + dataType + ")");
        }
#endif

        public LoaderBase loader
        {
            get => _loader;
            private set { loaderId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        /// <summary>
        /// The id of the loader containing the referenced data.
        /// </summary>
        [Json]
        public SerializableGuid loaderId
        {
            get => _loaderId;
            set 
            { 
                SetValue(nameof(loaderId), value, ref _loaderId, (newValue, oldValue) => 
                {
#if UNITY_EDITOR
                    _lastLoaderId = newValue;
#endif
                    UpdateLoader();
                }); 
            }
        }

        private void UpdateLoader()
        {
            SetValue(nameof(loader), GetComponentFromId<LoaderBase>(loaderId), ref _loader, (newValue, oldValue) =>
            {
                if (HasChanged(newValue, oldValue, false))
                {
                    RemoveLoaderDelegate(oldValue);
                    AddLoaderDelegate(newValue);

                    ForceUpdateLoadScope();
                }
            });
        }

        /// <summary>
        /// An id key used to retrieve the data from an <see cref="DepictionEngine.IdLoader"/>.
        /// </summary>
        [Json]
        public SerializableGuid dataId
        {
            get => _dataId;
            set
            {
                SetValue(nameof(dataId), value, ref _dataId, (newValue, oldValue) =>
                {
                    if (HasChanged(newValue, oldValue, false))
                    {
#if UNITY_EDITOR
                        _lastDataId = newValue;
#endif
                        ForceUpdateLoadScope();
                    }
                });
            }
        }

        /// <summary>
        /// A 2D grid index key used to retrieve the data from an <see cref="DepictionEngine.Index2DLoaderBase"/>.
        /// </summary>
        [Json]
        public Grid2DIndex dataIndex2D
        {
            get => _dataIndex2D;
            set
            {
                SetValue(nameof(dataIndex2D), value, ref _dataIndex2D, (newValue, oldValue) =>
                {
                    if (HasChanged(newValue, oldValue, false))
                    {
#if UNITY_EDITOR
                        _lastDataIndex2D = newValue;
#endif
                        ForceUpdateLoadScope();
                    }
                });
            }
        }

        private void ForceUpdateLoadScope()
        {
            _loadedOrFailedIdLoadScope = SerializableGuid.Empty;
            _loadedOrFailedIndexLoadScope = Grid2DIndex.Empty;

            UpdateLoadScope();
        }

        public LoadScope loadScope
        {
            get => _loadScope;
            private set { SetLoadScope(value); }
        }

        private bool UpdateLoadScope()
        {
            if (initialized)
            {
                LoadScope loadScope = null;

                if (loader != Disposable.NULL)
                {
                    bool createLoadScopeIfMissing = !SceneManager.IsSceneBeingDestroyed() && !LoadingWasAttempted();

                    IdLoader idLoader = loader as IdLoader;
                    if (idLoader != Disposable.NULL)
                    {
                        if (idLoader != Disposable.NULL && idLoader.GetLoadScope(out LoadScope idLoadScope, dataId, createIfMissing: createLoadScopeIfMissing))
                            loadScope = idLoadScope;
                        else
                            loadScope = null;
                    }

                    Index2DLoader index2DLoader = loader as Index2DLoader;
                    if (index2DLoader != Disposable.NULL)
                    {
                        if (index2DLoader != Disposable.NULL && index2DLoader.GetLoadScope(out LoadScope index2DLoadScope, new Grid2DIndex(dataIndex2D.index, dataIndex2D.dimensions), createIfMissing: createLoadScopeIfMissing))
                            loadScope = index2DLoadScope;
                        else
                            loadScope = null;
                    }
                }

                return SetLoadScope(loadScope != Disposable.NULL ? loadScope : null);
            }
            return false;
        }

        private bool SetLoadScope(LoadScope value, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            RegisterCompleteObjectUndo(disposeContext);
#endif

            return SetValue(nameof(loadScope), value, ref _loadScope, (newValue, oldValue) => 
            {
                if (HasChanged(newValue, oldValue, false))
                {
#if UNITY_EDITOR
                    _lastLoadScope = newValue;
#endif
                    if (oldValue is not null)
                    {
                        RemoveLoadScopeDelegate(oldValue);
                        RemoveReferenceFromLoader(oldValue, disposeContext);
                    }
                    if (newValue != Disposable.NULL)
                    {
                        AddReferenceToLoadScope(newValue);
                        AddLoadScopeDelegate(newValue);
                    }

                    if (newValue is IdLoadScope)
                    {
                        IdLoadScope index2DLoadScope = newValue as IdLoadScope;
                        _loadedOrFailedIdLoadScope = index2DLoadScope.scopeId;
                    }
                    if (newValue is Index2DLoadScope)
                    {
                        Index2DLoadScope index2DLoadScope = newValue as Index2DLoadScope;
                        _loadedOrFailedIndexLoadScope = new Grid2DIndex(index2DLoadScope.scopeIndex, index2DLoadScope.scopeDimensions);
                    }

                    UpdateData();
                }
            });
        }

        private void RemoveReferenceFromLoader(LoadScope loadScope, DisposeContext disposeContext)
        {
            if (loadScope is not null && loadScope.loader != Disposable.NULL)
            {
#if UNITY_EDITOR
                if (SceneManager.GetIsUserChangeContext())
                    disposeContext = DisposeContext.Editor_Destroy;
#endif
                loadScope.loader.RemoveReference(loadScope.scopeKey, this, disposeContext);
            }
        }

        private void AddReferenceToLoadScope(LoadScope loadScope)
        {
            if (loadScope != Disposable.NULL && loadScope.loader != Disposable.NULL)
                loadScope.loader.AddReference(loadScope.scopeKey, this);
        }

        private bool UpdateData(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            RegisterCompleteObjectUndo(disposeContext);
#endif

            PersistentScriptableObject data = null;

            if (loadScope != Disposable.NULL)
            {
                SetLoadingState(loadScope.loadingState);

                if (loadingState == DatasourceOperationBase.LoadingState.Loaded)
                    data = loadScope.GetFirstPersistent() as PersistentScriptableObject;
            }
            else
                SetLoadingState(DatasourceOperationBase.LoadingState.None);

            return SetData(data != Disposable.NULL ? data : null, disposeContext);
        }

        /// <summary>
        /// The referenced data (Read Only).
        /// </summary>
        public PersistentScriptableObject data
        {
            get => _data;
            private set => SetData(value);
        }

        private bool SetData(PersistentScriptableObject value, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            RegisterCompleteObjectUndo(disposeContext);
#endif

            return SetValue(nameof(data), value, ref _data, (newValue, oldValue) =>
            {
                if (HasChanged(newValue, oldValue, false))
                {
#if UNITY_EDITOR
                    _lastData = newValue;
#endif
                    RemoveDataDelegates(oldValue);
                    AddDataDelegates(newValue);
                }
            });
        }

        protected virtual void DataChanged(PersistentScriptableObject newValue, PersistentScriptableObject oldValue)
        {

        }

        private bool LoadingWasAttempted()
        {
            if (loader is IdLoader)
                return _loadedOrFailedIdLoadScope != SerializableGuid.Empty && dataId == _loadedOrFailedIdLoadScope;
            if (loader is Index2DLoaderBase)
                return _loadedOrFailedIndexLoadScope != Grid2DIndex.Empty && dataIndex2D == _loadedOrFailedIndexLoadScope;
            
            return false;
        }

        public bool IsLoaded()
        {
            return loader == Disposable.NULL || (loader is Index2DLoaderBase && dataIndex2D == Grid2DIndex.Empty) || (loader is IdLoader && dataId == SerializableGuid.Empty) || loadingState == DatasourceOperationBase.LoadingState.Failed || loadingState == DatasourceOperationBase.LoadingState.Loaded;
        }

        /// <summary>
        /// The referenced data current loading state (Read Only).
        /// </summary>
        public DatasourceOperationBase.LoadingState loadingState
        {
            get => _loadingState;
            private set => SetLoadingState(value);
        }

        private bool SetLoadingState(DatasourceOperationBase.LoadingState value)
        {
            if (_loadingState == value)
                return false;

            _loadingState = value;
            return true;
        }

        public override string ToString()
        {
            string str = "";
            if (loader is IdLoader)
                str = "Id:" + dataId +" ";
            if (loader is Index2DLoaderBase)
                str = "Zoom:"+MathPlus.GetZoomFromGrid2DDimensions(dataIndex2D.dimensions)+", X:"+dataIndex2D.index.x+", Y:"+ dataIndex2D.index.y + " ";
            return str + "(" + GetType() + ")";
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                RemoveReferenceFromLoader(loadScope, disposeContext);

                LoaderPropertyAssignedChangedEvent = null;

                return true;
            }
            return false;
        }
    }
}
