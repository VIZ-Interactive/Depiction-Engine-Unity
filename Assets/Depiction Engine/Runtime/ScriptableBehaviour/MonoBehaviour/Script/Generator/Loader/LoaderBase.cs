// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    public class LoaderBase : GeneratorBase, IPersistentList
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

        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private PersistentsDictionary _persistentsDictionary;

        [BeginFoldout("Load Scope")]
        [SerializeField, ConditionalShow(nameof(IsNotFallbackValues)), BeginHorizontalGroup]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLoadingLabels))]
#endif
        private int _loadingCount;

        [SerializeField, ConditionalShow(nameof(IsNotFallbackValues)), EndHorizontalGroup]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLoadingLabels))]
#endif
        private int _loadedCount;

#if UNITY_EDITOR
        [SerializeField, Button(nameof(LoadAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Create missing "+nameof(LoadScope)+ "(s) and dispose those that are no longer required."), BeginHorizontalGroup(true)]
        private bool _loadAll;
        [SerializeField, Button(nameof(DisposeAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Dispose all the "+nameof(LoadScope)+ "(s)."), EndHorizontalGroup]
        private bool _disposeAll;
#endif

        [SerializeField, Tooltip("When enabled the " + nameof(LoadScope) + "'s will be automatically disposed when their last reference is removed."), EndFoldout]
        private bool _autoDisposeUnused;

        [SerializeField, HideInInspector]
        private PropertyMonoBehaviour _datasource;

        private bool _autoUpdate;
        private Tween _waitBetweenLoadTimer;
        private Tween _loadDelayTimer;

        /// <summary>
        /// Dispatched after a <see cref="DepictionEngine.LoadScope"/> is Disposed.
        /// </summary>
        public Action<LoadScope, DisposeContext> LoadScopeDisposedEvent;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.LoadScope"/> LoadingState changes.
        /// </summary>
        public Action<LoadScope> LoadScopeLoadingStateChangedEvent;

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

        private void DisposeAllBtn()
        {
            DisposeAllLoadScopes();
        }

        protected override bool GetEnableSeed()
        {
            return datasourceId == SerializableGuid.Empty;
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

            _persistentsDictionary?.Clear();

            _datasource = default;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            KillTimers();

            UpdateLoadTriggers();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Existing)
            {
                PerformAddRemovePersistents(persistentsDictionary, persistentsDictionary);

                PerformAddRemoveReferences();
            }

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
        }

        private void PerformAddRemovePersistents(PersistentsDictionary persistentsDictionary, PersistentsDictionary lastPersistentsDictionary)
        {
            PerformAddRemovePersistents(this, persistentsDictionary, lastPersistentsDictionary);
        }

        public static List<(bool, SerializableGuid, IPersistent)> PerformAddRemovePersistents(IPersistentList persistentList, PersistentsDictionary persistentsDictionary, PersistentsDictionary lastPersistentsDictionary)
        {
            List<(bool, SerializableGuid, IPersistent)> changedPersistents = null;

            SerializationUtility.FindAddedRemovedObjects(persistentsDictionary, lastPersistentsDictionary,
            (persistentId) =>
            {
                changedPersistents ??= new();
                changedPersistents.Add((false, persistentId, null));
            },
            (persistentId, serializableIPersistent) =>
            {
                changedPersistents ??= new();
                changedPersistents.Add((true, persistentId, serializableIPersistent.persistent));
            });

            if (changedPersistents != null)
            {
                foreach ((bool, SerializableGuid, IPersistent) changedPersistent in changedPersistents)
                {
                    bool success = changedPersistent.Item1 ? persistentList.AddPersistent(changedPersistent.Item3) : persistentList.RemovePersistent(changedPersistent.Item2, DisposeContext.Programmatically_Destroy);
                }
            }

            return changedPersistents;
        }

        protected void PerformAddRemoveReferences()
        {
            IterateOverLoadScopeKeys((i, loadScopeKey, references) =>
            {
                if (references.RemoveNullReferences() && RemoveReference(loadScopeKey, null, DisposeContext.Programmatically_Destroy))
                {
                    if (GetLoadScope(out LoadScope loadScope, loadScopeKey))
                        DisposeLoadScope(loadScope, DisposeContext.Programmatically_Destroy);
                }
            });
        }

        protected void PerformAddRemoveAnFixBrokenLoadScopes<T, T1>(IDictionary<T, T1> loadScopesDictionary, IDictionary<T, T1> lastLoadScopesDictionary) where T1 : LoadScope
        {
            PerformAddRemoveLoadScopes(loadScopesDictionary, lastLoadScopesDictionary);
            FixBrokenLoadScopes();
        }

        //Problem: When undoing a "AddComponent"(Loader) action done in the inspector and redoing it, some null loadScopes can be found in the LoadScope Dictionary
        //Fix: If this initialization is the result of an Undo/Redo operation, we look for null LoadScope in the Dictionary and Clear it all if we find some
        private List<(bool, object, LoadScope)> PerformAddRemoveLoadScopes<T, T1>(IDictionary<T, T1> loadScopesDictionary, IDictionary<T, T1> lastLoadScopesDictionary) where T1 : LoadScope
        {
            List<(bool, object, LoadScope)> changedLoadScopes = null;

            SerializationUtility.FindAddedRemovedObjects(loadScopesDictionary, lastLoadScopesDictionary,
            (loadScopeKey) =>
            {
                changedLoadScopes ??= new();
                changedLoadScopes.Add((false, loadScopeKey, null));
            },
            (loadScopeKey, loadScope) =>
            {
                changedLoadScopes ??= new();
                changedLoadScopes.Add((true, loadScopeKey, loadScope));
            });

            if (changedLoadScopes != null)
            {
                foreach ((bool, object, LoadScope) changedLoadScope in changedLoadScopes)
                {
                    bool success = changedLoadScope.Item1 ? AddLoadScope(changedLoadScope.Item3) : RemoveLoadScope(changedLoadScope.Item2, DisposeContext.Programmatically_Destroy);
                }
            }
            return changedLoadScopes;
        }

        protected void FixBrokenLoadScopes(bool compareWithLastDictionary = false)
        {
            float loadInterval = 0.5f;

            IterateOverLoadScopes((loadScopeKey, loadScope) =>
            {
                bool reload = false;

                if (loadScope.PerformAddRemovePersistents(compareWithLastDictionary))
                    reload = true;
                if (loadScope.LoadingWasCompromised())
                    reload = true;

                //The interval is required because creating Assets, such as Texture2D, too quickly after InitializeOnLoadMethod will cause them to become null shortly after creation.
                if (CanAutoLoad() && reload)
                {
                    Load(loadScope, loadInterval);
                    loadInterval += 0.01f;
                }

                return true;
            });
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastDatasource = datasource;

                lastPersistentsDictionary.Clear();
                lastPersistentsDictionary.CopyFrom(persistentsDictionary);

                IterateOverLoadScopes((loadScopeKey, loadScope) => 
                {
                    loadScope.InitializeLastFields();
                    return true;
                });
#endif

                return true;
            }
            return false;
        }

        protected virtual void ClearLoadScopes()
        {
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                QueueAutoUpdate();

                UpdateLoaderFields(true);
                UpdateLoadScopeFields();

                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void AfterAssemblyReloadHandler()
        {
            InstanceManager.Instance(false)?.IterateOverInstances<LoaderBase>((loader) =>
            {
                loader.FixBrokenLoadScopes();
                return true;
            });
        }

        private void BeforeAssemblyReloadHandler()
        {
            IterateOverLoadScopes((loadScopeKey, loadScope) =>
            {
                loadScope.KillLoading();
                return true;
            });
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
#if UNITY_EDITOR
                SceneManager.BeforeAssemblyReloadEvent -= BeforeAssemblyReloadHandler;
                if (!IsDisposing())
                    SceneManager.BeforeAssemblyReloadEvent += BeforeAssemblyReloadHandler;
#endif
                UpdatePersistentsDelegates();

                IterateOverLoadScopes((loadScopeKey, loadScope) => 
                {
                    RemoveLoadScopeDelegates(loadScope);
                    AddLoadScopeDelegates(loadScope);

                    return true;
                });

                return true;
            }
            return false;
        }

        private void UpdatePersistentsDelegates()
        {
            IterateOverPersistents((persistentId, persistent) =>
            {
                RemovePersistentDelegates(persistent);
                AddPersistentDelegates(persistent);

                return true;
            });
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

        private void PersistentDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            IPersistent persistent = disposable as IPersistent;
            if (RemovePersistent(persistent, disposeContext) && GetLoadScope(out LoadScope loadScope, persistent))
                loadScope.RemovePersistent(persistent, disposeContext);
        }

        private void RemoveLoadScopeDelegates(LoadScope loadScope)
        {
            if (loadScope is not null)
                loadScope.LoadingStateChangedEvent -= LoadScopeLoadingStateChangedHandler;
        }

        private void AddLoadScopeDelegates(LoadScope loadScope)
        {
            if (!IsDisposing() && loadScope != Disposable.NULL)
                loadScope.LoadingStateChangedEvent += LoadScopeLoadingStateChangedHandler;
        }

        private void LoadScopeLoadingStateChangedHandler(LoadScope loadScope)
        {
            LoadScopeLoadingStateChangedEvent?.Invoke(loadScope);
        }

#if UNITY_EDITOR
        private PropertyMonoBehaviour _lastDatasource;

        private PersistentsDictionary _lastPersistentsDictionary;
        private PersistentsDictionary lastPersistentsDictionary
        {
            get => _lastPersistentsDictionary ??= new PersistentsDictionary();
        }

        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            //Undos trigger OnEnable which queues an auto update.
            _autoUpdate = false;

            UnityEngine.Object datasourceUnityObject = _datasource;
            if (SerializationUtility.RecoverLostReferencedObject(ref datasourceUnityObject))
                _datasource = (PropertyMonoBehaviour)datasourceUnityObject;

            if (SerializationUtility.RecoverLostReferencedObjectsInCollections(persistentsDictionary, lastPersistentsDictionary))
                PerformAddRemovePersistents(persistentsDictionary, lastPersistentsDictionary);
        }

        protected void UndoRedoPerformedReferencesLoadScopes<T, T1, T2>(IDictionary<T, ReferencesList> referencesDictionary, IDictionary<T, ReferencesList> lastReferencesDictionary, IDictionary<T1, T2> loadScopesDictionary, IDictionary<T1, T2> lastLoadScopesDictionary) where T2 : LoadScope
        {
            bool referencesUndoRedoDetected = false;
            for(int i = referencesDictionary.Count - 1; i >= 0; i--)
            {
                if (SerializationUtility.RecoverLostReferencedObjectsInCollection(referencesDictionary.ElementAt(i).Value))
                    referencesUndoRedoDetected = true;
            }
            for (int i = lastReferencesDictionary.Count - 1; i >= 0; i--)
            {
                if (SerializationUtility.RecoverLostReferencedObjectsInCollection(lastReferencesDictionary.ElementAt(i).Value))
                    referencesUndoRedoDetected = true;
            }
            if (referencesUndoRedoDetected)
                PerformAddRemoveReferences();

            bool loadScopesPersistentsUndoRedoDetected = false;

            if (SerializationUtility.RecoverLostReferencedObjectsInCollections(loadScopesDictionary, lastLoadScopesDictionary))
                loadScopesPersistentsUndoRedoDetected = PerformAddRemoveLoadScopes(loadScopesDictionary, lastLoadScopesDictionary)?.Count > 0;

            IterateOverLoadScopes((loadScopeKey, loadScope) =>
            {
                if (SerializationUtility.RecoverLostReferencedObjectsInCollections(loadScope.persistentsDictionary, loadScope.lastPersistentsDictionary))
                    loadScopesPersistentsUndoRedoDetected = true;
                return true;
            });
            if (loadScopesPersistentsUndoRedoDetected)
                FixBrokenLoadScopes(true);
        }
#endif

        private PersistentsDictionary persistentsDictionary
        {
            get => _persistentsDictionary ??= new();
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

        protected virtual bool GetDefaultAutoDisposeUnused()
        {
            return true;
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
            IterateOverLoadScopes((loadScopeKey, loadScope) =>
            {
                DatasourceOperationBase.LoadingState loadScopeLoadingState = loadScope.loadingState;
                if (loadScopeLoadingState == DatasourceOperationBase.LoadingState.Interval || loadScopeLoadingState == DatasourceOperationBase.LoadingState.Loading)
                    _loadingCount++;
                else
                    _loadedCount++;

                return true;
            });
        }

        protected virtual int GetLoadScopeCount()
        {
            return 0;
        }

        public PropertyMonoBehaviour datasource
        {
            get { return _datasource; }
            set { datasourceId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        private bool SetDatasource(PropertyMonoBehaviour value)
        {
#if UNITY_EDITOR
            if (SceneManager.IsUserChangeContext())
                Editor.UndoManager.RegisterCompleteObjectUndo(this);
#endif

            return SetValue(nameof(datasource), value, ref _datasource, (newValue, oldValue) =>
            {
                if (HasChanged(newValue, oldValue, false))
                {
#if UNITY_EDITOR
                    _lastDatasource = newValue;
#endif
                    DisposeContext disposeContext = SceneManager.IsUserChangeContext() ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Pool;

                    RemoveAllPersistents(disposeContext);

                    float loadInterval = 0.5f;
                    IterateOverLoadScopes((loadScopeKey, loadScope) =>
                    {
                        loadScope.DatasourceChanged(disposeContext);

                        Load(loadScope, loadInterval);
                        loadInterval += 0.01f;
                        return true;
                    });
                }
            });
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
            SetDatasource(datasourceId != SerializableGuid.Empty ? GetComponentFromId<DatasourceBase>(datasourceId) : datasourceManager);
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

        /// <summary>
        /// Create missing <see cref="DepictionEngine.LoadScope"/>(s) and dispose those that are no longer required.
        /// </summary>
        /// <returns></returns>
        public List<LoadScope> LoadAll()
        {
            return UpdateLoadScopes(true);
        }

        /// <summary>
        /// Internal method, to reload data use the <see cref="DepictionEngine.DatasourceBase.ReloadAll"/> instead.
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

        /// <summary>
        /// Returns true if a loadScope exists or a new one was created.
        /// </summary>
        /// <param name="loadScope">Will be set to an existing or new loadScope.</param>
        /// <param name="loadScopeKey">The key used to find the loadScope in the dictionary.</param>
        /// <param name="loadInterval"></param>
        /// <param name="reload">If true any pre-existing loadScope will be reloaded before being returned.</param>
        /// <param name="createIfMissing">Create a new load scope if none exists.</param>
        /// <returns></returns>
        public virtual bool GetLoadScope(out LoadScope loadScope, object loadScopeKey, bool reload = false, bool createIfMissing = false, float loadInterval = 0.0f)
        {
            loadScope = null;
            return false;
        }

        public virtual bool IterateOverLoadScopes(Func<object, LoadScope, bool> callback)
        {
            return true;
        }

        protected virtual void IterateOverLoadScopeKeys(Action<int, object, ReferencesList> callback)
        {
        }

        public virtual bool AddReference(object loadScopeKey, ReferenceBase reference)
        {
            return true;
        }

        public virtual bool RemoveReference(object loadScopeKey, ReferenceBase reference = null, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            return true;
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
                IterateOverLoadScopes((loadScopeKey, loadScope) =>
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

        public void IterateOverPersistents(Func<SerializableGuid, IPersistent, bool> callback)
        {
            if (_persistentsDictionary != null)
            {
                for (int i = _persistentsDictionary.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<SerializableGuid, SerializableIPersistent> persistentKey = _persistentsDictionary.ElementAt(i);
                    if (!callback(persistentKey.Key, persistentKey.Value.persistent))
                        break;
                }
            }
        }

        public bool Contains(IPersistent persistent)
        {
            return persistentsDictionary.ContainsKey(persistent.id);
        }

        public int GetPersistenCount()
        {
            return persistentsDictionary.Count;
        }

        public bool AddPersistent(IPersistent persistent)
        {
            SerializableIPersistent serializableIPersistent = new(persistent);
            if (persistentsDictionary.TryAdd(persistent.id, serializableIPersistent))
            {
                AddPersistentDelegates(persistent);
#if UNITY_EDITOR
                lastPersistentsDictionary.Add(persistent.id, serializableIPersistent);
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
                return true;
            }

            return false;
        }

        public bool RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (RemovePersistent(persistent.id, disposeContext))
            {
                RemovePersistentDelegates(persistent);

                return true;
            }
            return false;
        }

        public bool RemovePersistent(SerializableGuid persistentId, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            if (disposeContext == DisposeContext.Editor_Destroy)
                Editor.UndoManager.RecordObject(this);
#endif

            if (persistentsDictionary.Remove(persistentId))
            {
#if UNITY_EDITOR
                lastPersistentsDictionary.Remove(persistentId);

                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                if (disposeContext == DisposeContext.Editor_Destroy)
                    MarkAsNotPoolable();
#endif
                return true;
            }
#if UNITY_EDITOR
            if (disposeContext == DisposeContext.Editor_Destroy)
                Editor.UndoManager.FlushUndoRecordObjects();
#endif
            return false;
        }

        protected void RemoveAllPersistents(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (persistentsDictionary.Count > 0)
            {
#if UNITY_EDITOR
                if (disposeContext == DisposeContext.Editor_Destroy)
                    Editor.UndoManager.RegisterCompleteObjectUndo(this);
#endif

                persistentsDictionary.Clear();

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                if (disposeContext == DisposeContext.Editor_Destroy)
                    MarkAsNotPoolable();
#endif
            }
        } 

        protected LoadScope CreateLoadScope(Type type)
        {
            LoadScope loadScope = instanceManager.CreateInstance(type) as LoadScope;
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

        protected bool RemoveLoadScope(object loadScopeKey, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            if (disposeContext == DisposeContext.Editor_Destroy)
                Editor.UndoManager.RecordObject(this);
#endif

            bool removed = false;

            if (RemoveLoadScopeInternal(loadScopeKey, out LoadScope loadScope) && loadScope is not null)
            {
                RemoveLoadScopeDelegates(loadScope);

                removed = true;
            }

#if UNITY_EDITOR
            if (disposeContext == DisposeContext.Editor_Destroy)
            {
                Editor.UndoManager.FlushUndoRecordObjects();
                if (removed)
                    MarkAsNotPoolable();
            }
#endif

            return removed;
        }

        protected virtual bool RemoveLoadScopeInternal(object loadScopeKey, out LoadScope loadScope)
        {
            loadScope = null;
            return true;
        }

        private void KillTimers()
        {
            waitBetweenLoadTimer = null;
            loadDelayTimer = null;
        }

        public virtual bool IsInList(LoadScope loadScope)
        {
            return true;
        }

        /// <summary>
        /// Dispose all the <see cref="DepictionEngine.LoadScope"/>(s).
        /// </summary>
        public void DisposeAllLoadScopes(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (GetLoadScopeCount() != 0)
            {
#if UNITY_EDITOR
                if (disposeContext == DisposeContext.Editor_Destroy)
                    Editor.UndoManager.RecordObject(this);
#endif

                IterateOverLoadScopes((loadScopeKey, loadScope) =>
                {
                    DisposeLoadScope(loadScope, disposeContext);

                    return true;
                });

#if UNITY_EDITOR
                if (disposeContext == DisposeContext.Editor_Destroy)
                {
                    Editor.UndoManager.FlushUndoRecordObjects();
                    MarkAsNotPoolable();
                }
#endif
            }
        }

        /// <summary>
        /// Dispose the <see cref="DepictionEngine.LoadScope"/>.
        /// </summary>
        protected void DisposeLoadScope(LoadScope loadScope, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (loadScope != Disposable.NULL)
            {
                RemoveLoadScope(loadScope.scopeKey, disposeContext);

                DisposeManager.Dispose(loadScope, disposeContext);

                LoadScopeDisposedEvent?.Invoke(loadScope, disposeContext);
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                KillTimers();

                DisposeAllLoadScopes(disposeContext);

                LoadScopeDisposedEvent = null;
                LoadScopeLoadingStateChangedEvent = null;

                return true;
            }
            return false;
        }

        [Serializable]
        protected class ReferencesList : List<ReferenceBase>
        {
            public bool IsEmpty()
            {
                return Count == 0;
            }

            public bool RemoveNullReferences()
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    if (this[i] == Disposable.NULL)
                        RemoveAt(i);
                }

                return IsEmpty();
            }
        }
    }
}
