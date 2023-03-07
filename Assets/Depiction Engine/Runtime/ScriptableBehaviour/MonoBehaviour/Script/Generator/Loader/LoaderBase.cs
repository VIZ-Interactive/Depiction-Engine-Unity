// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class LoaderBase : GeneratorBase
    {
        /// <summary>
        /// Different types of data. <br/><br/>
		/// <b><see cref="Unknown"/>:</b> <br/>
        /// The Data type is Unknown. <br/><br/>
		/// <b><see cref="Json"/>:</b> <br/>
        /// JSON format. <br/><br/>
		/// <b><see cref="TexturePngJpg"/>:</b> <br/>
        /// A .png or .jpg texture.<br/><br/>
        /// <b><see cref="TextureWebP"/>:</b> <br/>
        /// A .webp texture.<br/><br/>
        /// <b><see cref="ElevationMapboxTerrainRGBPngRaw"/>:</b> <br/>
        /// A .pngraw texture containing elevation in Mapbox TerrainRGB format.<br/><br/>
        /// <b><see cref="ElevationMapboxTerrainRGBWebP"/>:</b> <br/>
        /// A .webp texture containing elevation in Mapbox TerrainRGB format.<br/><br/>
        /// <b><see cref="ElevationEsriLimitedErrorRasterCompression"/>:</b> <br/>
        /// Elevation data in Esri LimitedErrorRasterCompression (LERC) format.
        /// </summary>
        public enum DataType
        {
            Unknown,
            Json,
            TexturePngJpg,
            TextureWebP,
            ElevationMapboxTerrainRGBPngRaw,
            ElevationMapboxTerrainRGBWebP,
            ElevationEsriLimitedErrorRasterCompression
        }

        [BeginFoldout("Datasource")]
        [SerializeField, ComponentReference, Tooltip("The id of the datasource from which we will be loading data.")]
        private SerializableGuid _datasourceId;
        [SerializeField, Tooltip("The endpoint that will be used by the "+nameof(RestDatasource)+" when loading.")]
        private string _loadEndpoint;
        [SerializeField, Tooltip("The type of data we expect the loading operation to return.")]
        private DataType _dataType;
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableDepth))]
#endif
        private int _depth;
        [SerializeField, Tooltip("The amount of time (in seconds) to wait for a '"+nameof(RestDatasource)+" loading operation' before canceling, if it applies.")]
        private int _timeout;
        [SerializeField, Tooltip("Values to send as web request headers during a '"+nameof(RestDatasource)+" loading operation', if it applies."), EndFoldout]
        private List<string> _headers;

        [BeginFoldout("Loader")]
        [SerializeField, Tooltip("When enabled the loader will automatically update loadScopes even if the Script or GameObject is not activated.")]
        private bool _autoUpdateWhenDisabled;
        [SerializeField, Tooltip("The interval (in seconds) at which we evaluate whether an auto update request should be made.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowWaitBetweenLoad))]
#endif
        private float _autoUpdateInterval;
        [SerializeField, Tooltip("The amount of time (in seconds) to wait before we update following an auto update request.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowLoadDelay))]
#endif
        private float _autoUpdateDelay;
        [SerializeField, Tooltip("The interval (in seconds) at which we call the "+nameof(ReloadAll)+ " function. Automatically calling "+nameof(ReloadAll)+" can be useful to keep objects in synch with a datasource. Set to zero to deactivate."), EndFoldout]
        private float _autoReloadInterval;

        [BeginFoldout("Load Scope")]
#if UNITY_EDITOR
        [SerializeField, Button(nameof(LoadAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Create missing "+nameof(LoadScope)+ "(s) and dispose those that are no longer required."), BeginHorizontalGroup(true)]
        private bool _loadAll;
        [SerializeField, Button(nameof(ReloadAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Create missing "+nameof(LoadScope)+ "(s), reload existing ones and dispose those that are no longer required.")]
        private bool _reloadAll;
        [SerializeField, Button(nameof(DisposeAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Dispose all the "+nameof(LoadScope)+ "(s)."), EndHorizontalGroup]
        private bool _disposeAll;
#endif

        [SerializeField, Tooltip("When enabled the "+nameof(LoadScope)+"'s will be automatically disposed when their last reference is removed.")]
        private bool _autoDisposeUnused;

        [SerializeField, ConditionalShow(nameof(IsNotFallbackValues)), BeginHorizontalGroup]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLoadingLabels))]
#endif
        private int _loadingCount;

        [SerializeField, ConditionalShow(nameof(IsNotFallbackValues)), EndHorizontalGroup, EndFoldout]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLoadingLabels))]
#endif
        private int _loadedCount;

        [SerializeField, HideInInspector]
        private SerializableIPersistentList _persistents;

        [SerializeField, HideInInspector]
        private DatasourceBase _datasource;

        private bool _autoUpdate;
        private Tween _waitBetweenLoadTimer;
        private Tween _loadDelayTimer;
        private Tween _autoReloadIntervalTimer;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.LoadScope"/> is Disposing.
        /// </summary>
        public Action<LoadScope> LoadScopeDisposingEvent;

#if UNITY_EDITOR
        protected virtual bool GetShowWaitBetweenLoad()
        {
            return false;
        }

        protected virtual bool GetShowLoadDelay()
        {
            return false;
        }

        protected bool GetEnableDepth()
        {
            return dataType == DataType.Json;
        }

        private bool GetEnableLoadingLabels()
        {
            return false;
        }

        private void LoadAllBtn()
        {
            LoadAll();
        }

        private void ReloadAllBtn()
        {
            ReloadAll();
        }

        private void DisposeAllBtn()
        {
            DisposeLoadScopes();
        }

        protected override bool GetEnableSeed()
        {
            return GetDatasource() == datasourceManager.sceneDatasource;
        }
#endif

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (_datasourceId != null)
                callback(_datasourceId, UpdateDatasource);
        }

        public override void Recycle()
        {
            base.Recycle();

            if (_persistents != null)
                _persistents.Clear();

            _datasource = null;

            ClearLoadScopes();
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            KillTimers();
            StartAutoReloadIntervalTimer();

            UpdateLoadTriggers();
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            bool clearLoadScopes = initializingContext == InstanceManager.InitializationContext.Editor_Duplicate || initializingContext == InstanceManager.InitializationContext.Programmatically_Duplicate;

            //Problem: When undoing a "Add Component"(Loader) action done in the inspector and redoing it, some null loadScopes can be found in the LoadScope Dictionary for some reason
            //Fix: If this initialization is the result of an Undo/Redo operation, we look for null LoadScope in the Dictionary and Clear it all if we find some
            if (!clearLoadScopes && initializingContext == InstanceManager.InitializationContext.Existing)
            {
                if (DetectNullLoadScope())
                {
                    clearLoadScopes = true;
                    Debug.LogError("Detected null LoadScopes in:" + this);
                }
            }

            if (clearLoadScopes)
                ClearLoadScopes();

            IterateOverPersistents((persistent) =>
            {
                if (Disposable.IsDisposed(persistent))
                    RemovePersistent(persistent);

                return true;
            });

            InitValue(value => datasourceId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(datasourceId, datasource, initializingContext); }, initializingContext);
            InitValue(value => loadEndpoint = value, "", initializingContext);
            InitValue(value => dataType = value, DataType.Json, initializingContext);
            InitValue(value => depth = value, 0, initializingContext);
            InitValue(value => autoDisposeUnused = value, GetDefaultAutoDisposeUnused(), initializingContext);
            InitValue(value => timeout = value, 60, initializingContext);
            InitValue(value => headers = value, new List<string>(), initializingContext);
            InitValue(value => autoUpdateWhenDisabled = value, GetDefaultAutoLoadWhenDisabled(), initializingContext);
            InitValue(value => autoUpdateInterval = value, GetDefaultWaitBetweenLoad(), initializingContext);
            InitValue(value => autoUpdateDelay = value, GetDefaultLoadDelay(), initializingContext);
            InitValue(value => autoReloadInterval = value, GetDefaultAutoReloadInterval(), initializingContext);
        }

        protected virtual void ClearLoadScopes()
        {

        }

        protected virtual bool DetectNullLoadScope()
        {
            return false;
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                InitializeLoadScopes();

                QueueAutoUpdate();

                UpdateLoaderFields(true);
                UpdateLoadScopeFields();

                return true;
            }

            return false;
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            InitializeLoadScopes();
        }

        private void InitializeLoadScopes()
        {
            IterateOverLoadScopes((loadScope) =>
            {
                bool reload = false;

                if (loadScope.RemoveNullPersistents())
                    reload = true;
                if (loadScope.LoadingWasCompromised())
                    reload = true;

                if (CanAutoLoad() && reload)
                    Load(loadScope);

                return true;
            });
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                IterateOverPersistents((persistent) =>
                {
                    RemovePersistentDelegates(persistent);
                    AddPersistentDelegates(persistent);

                    return true;
                });

                IterateOverLoadScopes((loadScope) => 
                {
                    RemoveLoadScopeDelegates(loadScope);
                    AddLoadScopeDelegates(loadScope);

                    return true;
                });

                return true;
            }
            return false;
        }

        private void RemovePersistentDelegates(IPersistent persistent)
        {
            if (persistent is not null)
                persistent.DisposedEvent -= PersistentDisposedHandler;
        }

        private void AddPersistentDelegates(IPersistent persistent)
        {
            if (!IsDisposing() && !Disposable.IsDisposed(persistent))
                persistent.DisposedEvent += PersistentDisposedHandler;
        }

        private void PersistentDisposedHandler(IDisposable disposable)
        {
            RemovePersistent(disposable as IPersistent);
        }

        private void RemoveLoadScopeDelegates(LoadScope loadScope)
        {
            if (loadScope is not null)
            {
                loadScope.DisposingEvent -= LoadScopeDisposingHandler;
                loadScope.DisposedEvent -= LoadScopeDisposedHandler;
            }
        }

        private void AddLoadScopeDelegates(LoadScope loadScope)
        {
            if (!IsDisposing() && loadScope != Disposable.NULL)
            {
                loadScope.DisposingEvent += LoadScopeDisposingHandler;
                loadScope.DisposedEvent += LoadScopeDisposedHandler;
            }
        }

        private void LoadScopeDisposingHandler(IDisposable disposable)
        {
            LoadScope loadScope = disposable as LoadScope;

            LoadScopeDisposingEvent?.Invoke(loadScope);
        }

        private void LoadScopeDisposedHandler(IDisposable disposable)
        {
            LoadScope loadScope = disposable as LoadScope;

            RemoveLoadScope(loadScope);
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected virtual bool GetDefaultAutoLoadWhenDisabled()
        {
            return false;
        }

        protected virtual float GetDefaultWaitBetweenLoad()
        {
            return 0.0f;
        }

        protected virtual float GetDefaultLoadDelay()
        {
            return 0.0f;
        }

        protected virtual float GetDefaultAutoReloadInterval()
        {
            return 0.0f;
        }

        protected virtual bool GetDefaultAutoDisposeUnused()
        {
            return true;
        }

        private SerializableIPersistentList persistents
        {
            get
            {
                _persistents ??= new SerializableIPersistentList();
                return _persistents;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.LoadScope"/>(s) currently loading.
        /// </summary>
        public int loadingCount
        {
            get
            {
                UpdateLoadCount();
                return _loadingCount;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.LoadScope"/>(s) currently loaded.
        /// </summary>
        public int loadedCount
        {
            get
            {
                UpdateLoadCount();
                return _loadedCount;
            }
        }

        private void UpdateLoadCount()
        {
            _loadingCount = _loadedCount = 0;
            IterateOverLoadScopes((loadScope) =>
            {
                DatasourceOperationBase.LoadingState loadScopeLoadingState = loadScope.loadingState;
                if (loadScopeLoadingState == DatasourceOperationBase.LoadingState.Interval || loadScopeLoadingState == DatasourceOperationBase.LoadingState.Loading)
                    _loadingCount++;
                else
                    _loadedCount++;

                return true;
            });
        }

        public bool Contains(IPersistent persistent)
        {
            return persistents.Contains(persistent);
        }

        public void IterateOverPersistents(Func<IPersistent, bool> callback)
        {
            for (int i = persistents.Count - 1; i >= 0; i--)
            {
                if (!callback(persistents[i]))
                    break;
            }
        }

        public bool AddPersistent(IPersistent persistent)
        {
            bool added = false;

            if (!persistents.Contains(persistent))
            {
                persistents.Add(persistent);
                added = true;
            }

            if (added)
            {
                AddPersistentDelegates(persistent);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
            }

            return added;
        }

        private void RemoveAllPersistents()
        {
            IterateOverPersistents((persistent) =>
            {
                RemovePersistent(persistent);
                return true;
            });
        }

        public bool RemovePersistent(IPersistent persistent)
        {
            bool removed = false;

            if (!IsDisposing())
            {
                if (persistents.Remove(persistent))
                    removed = true;

                if (removed)
                {
                    RemovePersistentDelegates(persistent);

                    if (GetLoadScope(out LoadScope loadScope, persistent))
                        loadScope.RemovePersistent(persistent);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
                }
            }

            return removed;
        }


        public Datasource GetDatasource()
        {
            if (datasource != Disposable.NULL)
                return datasource.datasource;

            DatasourceManager datasourceManager = DatasourceManager.Instance(false);
            if (datasourceManager != Disposable.NULL)
                return datasourceManager.sceneDatasource;

            return null;
        }

        public DatasourceBase datasource
        {
            get { return _datasource; }
            set { datasourceId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        /// <summary>
        /// The id of the datasource from which we will be loading data.
        /// </summary>
        [Json]
        public SerializableGuid datasourceId
        {
            get { return _datasourceId; }
            set
            {
                SetValue(nameof(datasourceId), value, ref _datasourceId, (newValue, oldValue) =>
                {
                    UpdateDatasource();
                });
            }
        }

        private void UpdateDatasource()
        {
            SetValue(nameof(datasource), GetComponentFromId<DatasourceBase>(datasourceId), ref _datasource);
        }

        /// <summary>
        /// The endpoint that will be used by the <see cref="DepictionEngine.RestDatasource"/> when loading.
        /// </summary>
        [Json]
        public string loadEndpoint
        {
            get { return _loadEndpoint; }
            set { SetValue(nameof(loadEndpoint), value, ref _loadEndpoint); }
        }

        /// <summary>
        /// The type of data we expect the loading operation to return.
        /// </summary>
        [Json]
        public DataType dataType
        {
            get { return _dataType; }
            set { SetValue(nameof(dataType), value, ref _dataType); }
        }

        [Json]
        public int depth
        {
            get { return _depth; }
            set { SetValue(nameof(depth), value, ref _depth); }
        }

        /// <summary>
        /// The amount of time (in seconds) to wait for a '<see cref="DepictionEngine.RestDatasource"/> loading operation' before canceling, if it applies. 
        /// </summary>
        [Json]
        public int timeout
        {
            get { return _timeout; }
            set { SetValue(nameof(timeout), value, ref _timeout); }
        }

        /// <summary>
        /// Values to send as web request headers during a '<see cref="DepictionEngine.RestDatasource"/> loading operation', if it applies. 
        /// </summary>
        [Json]
        public List<string> headers
        {
            get { return _headers; }
            set { SetValue(nameof(headers), value, ref _headers); }
        }

        /// <summary>
        /// When enabled the loader will automatically update loadScopes even if the Script or GameObject is not activated.
        /// </summary>
        [Json]
        public bool autoUpdateWhenDisabled
        {
            get { return _autoUpdateWhenDisabled; }
            set { SetValue(nameof(autoUpdateWhenDisabled), value, ref _autoUpdateWhenDisabled); }
        }

        /// <summary>
        /// The interval (in seconds) at which we evaluate whether an auto update request should be made.
        /// </summary>
        [Json]
        public float autoUpdateInterval
        {
            get { return _autoUpdateInterval; }
            set { SetValue(nameof(autoUpdateInterval), value, ref _autoUpdateInterval); }
        }

        /// <summary>
        /// The amount of time (in seconds) to wait before we update following an auto update request.
        /// </summary>
        [Json]
        public float autoUpdateDelay
        {
            get { return _autoUpdateDelay; }
            set { SetValue(nameof(autoUpdateDelay), value, ref _autoUpdateDelay); }
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="DepictionEngine.LoaderBase.ReloadAll"/> function. Automatically calling <see cref="DepictionEngine.LoaderBase.ReloadAll"/> can be useful to keep objects in synch with a datasource. Set to zero to deactivate.
        /// </summary>
        [Json]
        public float autoReloadInterval
        {
            get { return _autoReloadInterval; }
            set
            {
                SetValue(nameof(autoReloadInterval), value, ref _autoReloadInterval, (newValue, oldValue) =>
                {
                    StartAutoReloadIntervalTimer();
                });
            }
        }

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.LoadScope"/>'s will be automatically disposed when their last reference is removed.
        /// </summary>
        [Json]
        public bool autoDisposeUnused
        {
            get { return _autoDisposeUnused; }
            set { SetValue(nameof(autoDisposeUnused), value, ref _autoDisposeUnused); }
        }

        private Tween waitBetweenLoadTimer
        {
            get { return _waitBetweenLoadTimer; }
            set
            {
                if (Object.ReferenceEquals(_waitBetweenLoadTimer, value))
                    return;

                DisposeManager.Dispose(_waitBetweenLoadTimer);

                _waitBetweenLoadTimer = value;
            }
        }

        private Tween loadDelayTimer
        {
            get { return _loadDelayTimer; }
            set
            {
                if (Object.ReferenceEquals(_loadDelayTimer, value))
                    return;

                DisposeManager.Dispose(_loadDelayTimer);

                _loadDelayTimer = value;
            }
        }

        private void StartAutoReloadIntervalTimer()
        {
            if (initialized)
            {
                autoReloadIntervalTimer = autoReloadInterval != 0.0f ? tweenManager.DelayedCall(autoReloadInterval, null, () =>
                {
                    ReloadAll();
                    StartAutoReloadIntervalTimer();
                }, () => autoReloadIntervalTimer = null) : null;
            }
        }

        private Tween autoReloadIntervalTimer
        {
            get { return _autoReloadIntervalTimer; }
            set
            {
                if (Object.ReferenceEquals(_autoReloadIntervalTimer, value))
                    return;

                DisposeManager.Dispose(_autoReloadIntervalTimer);
                
                _autoReloadIntervalTimer = value;
            }
        }

        /// <summary>
        /// Create missing <see cref="DepictionEngine.LoadScope"/>(s) and dispose those that are no longer required.
        /// </summary>
        /// <returns></returns>
        public List<LoadScope> LoadAll()
        {
            return UpdateLoadScopes(true);
        }

        /// <summary>
        /// Create missing <see cref="DepictionEngine.LoadScope"/>(s), reload existing ones and dispose those that are no longer required.
        /// </summary>
        /// <returns></returns>
        public List<LoadScope> ReloadAll()
        {
            return UpdateLoadScopes(true, true);
        }

        /// <summary>
        /// Returns true if a loadScope is found for the specified <see cref="DepictionEngine.IPersistent"/>.
        /// </summary>
        /// <param name="loadScope"></param>
        /// <param name="persistent"></param>
        /// <returns></returns>
        public virtual bool GetLoadScope(out LoadScope loadScope, IPersistent persistent)
        {
            loadScope = null;
            return false;
        }

        public virtual bool IterateOverLoadScopes(Func<LoadScope, bool> callback)
        {
            return true;
        }

        public virtual bool AddReference(LoadScope loadScope, ReferenceBase reference)
        {
            return false;
        }

        public virtual bool RemoveReference(LoadScope loadScope, ReferenceBase reference)
        {
            return false;
        }

        /// <summary>
        /// Queue a call to <see cref="DepictionEngine.LoaderBase.UpdateLoadScopes"/> if and when possible.
        /// </summary>
        protected void QueueAutoUpdate()
        {
            if (initialized)
                _autoUpdate = true;
        }

        private bool CanAutoLoad()
        {
            return isActiveAndEnabled || autoUpdateWhenDisabled;
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                UpdateLoadTriggers();

                if (wasFirstUpdated && CanAutoLoad())
                {
                    if (waitBetweenLoadTimer == Disposable.NULL || !waitBetweenLoadTimer.playing)
                    {
                        //Required if object was Diposed during Undo and we can no longer make calls to accessors
                        TweenManager tweenManager = this.tweenManager;
                        waitBetweenLoadTimer = tweenManager.DelayedCall(autoUpdateInterval, null, () =>
                        {
                            waitBetweenLoadTimer = null;
                            if (_autoUpdate && (loadDelayTimer == Disposable.NULL || !loadDelayTimer.playing))
                            {
                                loadDelayTimer = tweenManager.DelayedCall(autoUpdateDelay, null, () =>
                                {
                                    UpdateLoadScopes(true);
                                }, () => loadDelayTimer = null);
                            }
                        }, () => waitBetweenLoadTimer = null);
                    }
                }

                return true;
            }
            return false;
        }

        protected virtual void UpdateLoadTriggers()
        {
        }

        protected virtual List<LoadScope> UpdateLoadScopes(bool forceUpdate, bool reload = false)
        {
            _autoUpdate = false;

            UpdateLoaderFields(forceUpdate);

            if (autoDisposeUnused)
            {
                IterateOverLoadScopes((loadScope) =>
                {
                    if (!IsInList(loadScope))
                        DisposeLoadScope(loadScope);

                    return true;
                });
            }

            List <LoadScope> loadScopes = GetListedLoadScopes(reload);

            UpdateLoadScopeFields();

            return loadScopes;
        }

        protected virtual void UpdateLoaderFields(bool forceUpdate)
        {
        }

        protected virtual List<LoadScope> GetListedLoadScopes(bool reload)
        {
            return null;
        }

        protected virtual void UpdateLoadScopeFields()
        {
        }

        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                UpdateLoadCount();

                return true;
            }
            return false;
        }

        public void Load(LoadScope loadScope, float loadInterval = 0.0f)
        {
            if (loadScope != Disposable.NULL)
                loadScope.Load(loadInterval);
        }

        protected LoadScope CreateLoadScope(Type type)
        {
            LoadScope loadScope = instanceManager.CreateInstance(type, initializingContext: GetInitializeContext()) as LoadScope;
            return loadScope != Disposable.NULL ? loadScope : null;
        }

        protected bool AddLoadScope(LoadScope loadScope)
        {
            if (AddLoadScopeInternal(loadScope))
            {
                AddLoadScopeDelegates(loadScope);

                return true;
            }
            return false;
        }

        protected virtual bool AddLoadScopeInternal(LoadScope loadScope)
        {
            return loadScope != Disposable.NULL;
        }

        protected bool RemoveLoadScope(LoadScope loadScope)
        {
            if (RemoveLoadScopeInternal(loadScope))
            {
                RemoveLoadScopeDelegates(loadScope);

                return true;
            }
            return false;
        }

        protected virtual bool RemoveLoadScopeInternal(LoadScope loadScope)
        {
            return true;
        }

        private void KillTimers()
        {
            waitBetweenLoadTimer = null;
            loadDelayTimer = null;
            autoReloadIntervalTimer = null;
        }

        public virtual bool IsInList(LoadScope loadScope)
        {
            return true;
        }

        /// <summary>
        /// Dispose all the <see cref="DepictionEngine.LoadScope"/>(s).
        /// </summary>
        public void DisposeLoadScopes(DisposeManager.DisposeContext disposeContext = DisposeManager.DisposeContext.Programmatically, DisposeManager.DisposeDelay disposeDelay = DisposeManager.DisposeDelay.None)
        {
            IterateOverLoadScopes((loadScope) =>
            {
                DisposeLoadScope(loadScope, disposeContext, disposeDelay);

                return true;
            });
        }

        /// <summary>
        /// Dispose the <see cref="DepictionEngine.LoadScope"/>.
        /// </summary>
        protected void DisposeLoadScope(LoadScope loadScope, DisposeManager.DisposeContext disposeContext = DisposeManager.DisposeContext.Programmatically, DisposeManager.DisposeDelay disposeDelay = DisposeManager.DisposeDelay.None)
        {
            Dispose(loadScope, disposeContext, disposeDelay);
        }

        public override bool OnDisposing(DisposeManager.DisposeContext disposeContext)
        {
            if (base.OnDisposing(disposeContext))
            {
                KillTimers();

                DisposeLoadScopes(disposeContext, DisposeManager.DisposeDelay.Delayed);

                return true;
            }
            return false;
        }

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled)
        {
            if (base.OnDisposed(disposeContext, pooled))
            {
                LoadScopeDisposingEvent = null;

                return true;
            }
            return false;
        }

        [Serializable]
        protected class ReferencesList
        {
            [SerializeField]
            private List<ReferenceBase> _references;

            public ReferencesList()
            {
                _references = new List<ReferenceBase>();
            }

            public bool Contains(ReferenceBase reference)
            {
                return _references.Contains(reference);
            }

            public bool IsEmpty()
            {
                return _references.Count == 0;
            }

            public void Add(ReferenceBase reference)
            {
                _references.Add(reference);
            }

            public bool Remove(ReferenceBase reference)
            {
                return _references.Remove(reference);
            }

            public bool RemoveNullReferences()
            {
                for (int i = _references.Count - 1; i >= 0; i--)
                {
                    if (_references[i] == Disposable.NULL)
                        _references.RemoveAt(i);
                }

                return IsEmpty();
            }
        }
    }
}
