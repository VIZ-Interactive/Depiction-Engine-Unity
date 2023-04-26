// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
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
        /// <b><see cref="ElevationTerrainRGBPngRaw"/>:</b> <br/>
        /// A .pngraw texture containing elevation in Mapbox TerrainRGB format.<br/><br/>
        /// <b><see cref="ElevationTerrainRGBWebP"/>:</b> <br/>
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
            ElevationTerrainRGBPngRaw,
            ElevationTerrainRGBWebP,
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

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                persistentsDictionary.Clear();

            if (initializingContext == InitializationContext.Existing)
                PerformAddRemovePersistentsChange(persistentsDictionary, persistentsDictionary);

            InitValue(value => datasourceId = value, SerializableGuid.Empty, () => GetDuplicateComponentReferenceId(datasourceId, datasource is DatasourceManager ? null : datasource, initializingContext), initializingContext);
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

        private List<(bool, SerializableGuid, IPersistent)> PerformAddRemovePersistentsChange(PersistentsDictionary persistentsDictionary, PersistentsDictionary lastPersistentsDictionary)
        {
#if UNITY_EDITOR
            SerializationUtility.RecoverLostReferencedObjectsInCollection(persistentsDictionary);
#endif
            return PerformAddRemovePersistentsChange(this, persistentsDictionary, lastPersistentsDictionary);
        }

        public static List<(bool, SerializableGuid, IPersistent)> PerformAddRemovePersistentsChange(IPersistentList persistentList, PersistentsDictionary persistentsDictionary, PersistentsDictionary lastPersistentsDictionary)
        {
            List<(bool, SerializableGuid, IPersistent)> changedPersistents = null;
          
            SerializationUtility.FindAddedRemovedObjectsChange(persistentsDictionary, lastPersistentsDictionary,
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

        protected List<(bool, object, LoadScope)> PerformAddRemoveLoadScopesChange<T, T1>(IDictionary < T, T1> loadScopesDictionary, IDictionary<T, T1> lastLoadScopesDictionary, bool performingUndoRedo = false) where T1 : LoadScope
        {
#if UNITY_EDITOR
            SerializationUtility.RecoverLostReferencedObjectsInCollection(loadScopesDictionary);
#endif

            List<LoadScope> loadQueue = null;

            List<(bool, object, LoadScope)> changedLoadScopes = null;

            //Find loadScopes that were newly destroyed
            for (int i = lastLoadScopesDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T, T1> objectKeyPair = lastLoadScopesDictionary.ElementAt(i);
                T1 lastLoadScope = objectKeyPair.Value;
                if (lastLoadScope == Disposable.NULL)
                {
                    changedLoadScopes ??= new();
                    changedLoadScopes.Add((false, objectKeyPair.Key, null));
                }
                else
                {
                    List<(bool, SerializableGuid, IPersistent)> changedPersistents = lastLoadScope.PerformAddRemovePersistentsChange(performingUndoRedo);

                    bool reload = lastLoadScope.LoadingWasCompromised() && (!loadScopesDictionary.TryGetValue(objectKeyPair.Key, out T1 loadScope) || loadScope.LoadingWasCompromised());

                    if (!reload && changedPersistents != null)
                    {
                        foreach ((bool, SerializableGuid, IPersistent) changedPersistent in changedPersistents)
                        {
                            if (!changedPersistent.Item1)
                            {
                                reload = true;
                                break;
                            }
                        }
                    }

                    if (reload)
                    {
                        loadQueue ??= new();
                        loadQueue.Add(lastLoadScope);
                    }
                }
            }

            if (!Object.ReferenceEquals(loadScopesDictionary, lastLoadScopesDictionary))
            {
                //Find loadScopes that were newly created
                for (int i = loadScopesDictionary.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<T, T1> objectKeyPair = loadScopesDictionary.ElementAt(i);
                    T1 loadScope = objectKeyPair.Value;
                    if (loadScope != Disposable.NULL)
                    {
                        if (lastLoadScopesDictionary.TryGetValue(objectKeyPair.Key, out T1 lastLoadScope))
                        {
                            if (!Object.ReferenceEquals(lastLoadScope, loadScope))
                            {
                                lastLoadScope.Merge(loadScope);

                                DisposeManager.Dispose(loadScope, DisposeContext.Programmatically_Destroy);
                            }
                        }
                        else
                        {
                            changedLoadScopes ??= new();
                            changedLoadScopes.Add((true, objectKeyPair.Key, loadScope));

                            if (loadScope.LoadingWasCompromised())
                            {
                                loadQueue ??= new();
                                loadQueue.Add(loadScope);
                            }
                        }
                    }
                }

                loadScopesDictionary.Clear();
                foreach (KeyValuePair<T, T1> objectKeyPair in lastLoadScopesDictionary)
                    loadScopesDictionary.Add(objectKeyPair);
            }

            if (changedLoadScopes != null)
            {
                foreach ((bool, object, LoadScope) changedLoadScope in changedLoadScopes)
                {
                    bool success = changedLoadScope.Item1 ? AddLoadScope(changedLoadScope.Item3) : RemoveLoadScope(changedLoadScope.Item2, DisposeContext.Programmatically_Destroy);
                }
            }

            if (loadQueue != null)
            {
                float loadInterval = 0.5f;
                foreach (LoadScope loadScope in loadQueue)
                {
                    //The interval is required because creating Assets, such as Texture2D, too quickly after InitializeOnLoadMethod will cause them to become null shortly after creation.
                    Load(loadScope, loadInterval);
                    loadInterval += 0.01f;
                }
            }

            return changedLoadScopes;
        }

        protected List<(bool, object, ReferenceBase)> PerformAddRemoveReferencesChange<T>(IDictionary<T, ReferencesList> referencesDictionary, IDictionary<T, ReferencesList> lastReferencesDictionary)
        {
#if UNITY_EDITOR
            for (int i = referencesDictionary.Count - 1; i >= 0; i--)
                SerializationUtility.RecoverLostReferencedObjectsInCollection(referencesDictionary.ElementAt(i).Value);
#endif
            List<(bool, object, ReferenceBase)> changedReferences = null;

            //Find reference that were newly destroyed
            for (int i = lastReferencesDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T, ReferencesList> objectKeyPair = lastReferencesDictionary.ElementAt(i);
                ReferencesList referenceList = objectKeyPair.Value;
                for (int e = referenceList.Count - 1; e >= 0; e--)
                {
                    ReferenceBase reference = referenceList.ElementAt(e);
                    if (Disposable.IsDisposed(reference))
                    {
                        changedReferences ??= new();
                        changedReferences.Add((false, objectKeyPair.Key, reference));
                    }
                }
            }

            if (!Object.ReferenceEquals(referencesDictionary, lastReferencesDictionary))
            {
                //Find reference that were newly created
                for (int i = referencesDictionary.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<T, ReferencesList> objectKeyPair = referencesDictionary.ElementAt(i);
                    ReferencesList references = objectKeyPair.Value;
                    for (int e = references.Count - 1; e >= 0; e--)
                    {
                        ReferenceBase reference = references.ElementAt(e);
                        if (!lastReferencesDictionary.TryGetValue(objectKeyPair.Key, out ReferencesList lastReferences) || !lastReferences.Contains(reference))
                        {
                            changedReferences ??= new();
                            if (!Disposable.IsDisposed(reference))
                                changedReferences.Add((true, objectKeyPair.Key, reference));
                            else
                            {
                                if (lastReferences is null)
                                {
                                    lastReferences = new ReferencesList();
                                    lastReferencesDictionary.Add(objectKeyPair.Key, lastReferences);
                                }
                                lastReferences.Add(reference);
                                changedReferences.Add((false, objectKeyPair.Key, reference));
                            }
                        }
                    }
                }

                referencesDictionary.Clear();
                foreach (KeyValuePair<T, ReferencesList> objectKeyPair in lastReferencesDictionary)
                {
                    ReferencesList references = new();
                    foreach (ReferenceBase reference in objectKeyPair.Value)
                        references.Add(reference);
                    referencesDictionary.Add(objectKeyPair.Key, references);
                }
            }

            if (changedReferences != null)
            {
                foreach ((bool, object, ReferenceBase) changedReference in changedReferences)
                {
                    bool success = changedReference.Item1 ? AddReference(changedReference.Item2, changedReference.Item3) : RemoveReference(changedReference.Item2, changedReference.Item3, DisposeContext.Programmatically_Destroy);
                }
            }

            return changedReferences;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastDatasource = datasource;

                lastPersistentsDictionary.Clear();
                lastPersistentsDictionary.CopyFrom(persistentsDictionary);

                IterateOverLoadScopes((loadScopeKey, loadScope) => { loadScope.InitializeLastFields(); return true; });
#endif

                return true;
            }
            return false;
        }

        protected virtual void ClearLoadScopes()
        {
        }

        protected override bool LateInitialize(InitializationContext initializingContext)
        {
            if (base.LateInitialize(initializingContext))
            {
                QueueAutoUpdate();

                UpdateLoaderFields(true);
                UpdateLoadScopeFields();

                return true;
            }

            return false;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
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
            if (IsDisposing())
                return;

            if (!Disposable.IsDisposed(persistent))
                persistent.DisposedEvent += PersistentDisposedHandler;
        }

        private void PersistentDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            if (IsDisposing())
                return;

            IPersistent persistent = disposable as IPersistent;
            RemovePersistent(persistent, disposeContext);
            if (GetLoadScope(out LoadScope loadScope, persistent))
                loadScope.RemovePersistent(persistent, disposeContext);
        }

        private void RemoveLoadScopeDelegates(LoadScope loadScope)
        {
            if (loadScope is not null)
                loadScope.LoadingStateChangedEvent -= LoadScopeLoadingStateChangedHandler;
        }

        private void AddLoadScopeDelegates(LoadScope loadScope)
        {
            if (IsDisposing())
                return;

            if (loadScope != Disposable.NULL)
                loadScope.LoadingStateChangedEvent += LoadScopeLoadingStateChangedHandler;
        }

        private void LoadScopeLoadingStateChangedHandler(LoadScope loadScope)
        {
            LoadScopeLoadingStateChangedEvent?.Invoke(loadScope);
        }

#if UNITY_EDITOR
        private PersistentsDictionary _lastPersistentsDictionary;
        private PersistentsDictionary lastPersistentsDictionary
        {
            get { _lastPersistentsDictionary ??= new PersistentsDictionary(); return _lastPersistentsDictionary; }
        }

        private PropertyMonoBehaviour _lastDatasource;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                SerializationUtility.RecoverLostReferencedObject(ref _datasource);
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { SetDatasource(value); }, ref _datasource, ref _lastDatasource);

                PerformAddRemovePersistentsChange(persistentsDictionary, lastPersistentsDictionary);

                IterateOverLoadScopes((loadScopeKey, loadScope) => { loadScope.LoaderUndoRedoPerformed(); return true; });

                return true;
            }
            return false;
        }
#endif

        protected PersistentsDictionary persistentsDictionary
        {
            get { _persistentsDictionary ??= new(); return _persistentsDictionary; }
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
            get { UpdateLoadCount(); return _loadingCount; }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.LoadScope"/>(s) currently loaded.
        /// </summary>
        public int loadedCount
        {
            get { UpdateLoadCount(); return _loadedCount; }
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
            get => _datasource;
            set => datasourceId = value != Disposable.NULL ? value.id : SerializableGuid.Empty;
        }

        private bool SetDatasource(PropertyMonoBehaviour value)
        {
            return SetValue(nameof(datasource), value, ref _datasource, (newValue, oldValue) => 
            {
#if UNITY_EDITOR
                _lastDatasource = newValue;
#endif
            }, true);
        }

        /// <summary>
        /// The id of the datasource from which we will be loading data.
        /// </summary>
        [Json]
        public SerializableGuid datasourceId
        {
            get => _datasourceId;
            set
            {
                DisposeContext disposeContext = SceneManager.GetIsUserChangeContext() ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Pool;

#if UNITY_EDITOR
                RegisterCompleteObjectUndo(disposeContext);
#endif

                SetValue(nameof(datasourceId), value, ref _datasourceId, (newValue, oldValue) =>
                {
                    if (UpdateDatasourceFromDatasourceId() && initialized && HasChanged(newValue, oldValue, false))
                    {
                        RemoveAllPersistents();

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
        }

        private void UpdateDatasource()
        {
            UpdateDatasourceFromDatasourceId();
        }

        private bool UpdateDatasourceFromDatasourceId()
        {
            return SetDatasource(datasourceId != SerializableGuid.Empty ? GetComponentFromId<PropertyMonoBehaviour>(datasourceId) : datasourceManager);
        }

        /// <summary>
        /// The endpoint that will be used by the <see cref="DepictionEngine.RestDatasource"/> when loading.
        /// </summary>
        [Json]
        public string loadEndpoint
        {
            get => _loadEndpoint;
            set => SetValue(nameof(loadEndpoint), value, ref _loadEndpoint);
        }

        /// <summary>
        /// The type of data we expect the loading operation to return.
        /// </summary>
        [Json]
        public DataType dataType
        {
            get => _dataType;
            set => SetValue(nameof(dataType), value, ref _dataType);
        }

        [Json]
        public int depth
        {
            get => _depth;
            set => SetValue(nameof(depth), value, ref _depth);
        }

        /// <summary>
        /// The amount of time (in seconds) to wait for a '<see cref="DepictionEngine.RestDatasource"/> loading operation' before canceling, if it applies. 
        /// </summary>
        [Json]
        public int timeout
        {
            get => _timeout;
            set => SetValue(nameof(timeout), value, ref _timeout);
        }

        /// <summary>
        /// Values to send as web request headers during a '<see cref="DepictionEngine.RestDatasource"/> loading operation', if it applies. 
        /// </summary>
        [Json]
        public List<string> headers
        {
            get => _headers;
            set => SetValue(nameof(headers), value, ref _headers);
        }

        /// <summary>
        /// When enabled the loader will automatically update loadScopes even if the Script or GameObject is not activated.
        /// </summary>
        [Json]
        public bool autoUpdateWhenDisabled
        {
            get => _autoUpdateWhenDisabled;
            set => SetValue(nameof(autoUpdateWhenDisabled), value, ref _autoUpdateWhenDisabled, (newValue, oldValue) => {  QueueAutoUpdate(); });
        }

        /// <summary>
        /// The interval (in seconds) at which we evaluate whether an auto update request should be made.
        /// </summary>
        [Json]
        public float autoUpdateInterval
        {
            get => _autoUpdateInterval;
            set => SetValue(nameof(autoUpdateInterval), value, ref _autoUpdateInterval);
        }

        /// <summary>
        /// The amount of time (in seconds) to wait before we update following an auto update request.
        /// </summary>
        [Json]
        public float autoUpdateDelay
        {
            get => _autoUpdateDelay;
            set => SetValue(nameof(autoUpdateDelay), value, ref _autoUpdateDelay);
        }

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.LoadScope"/>'s will be automatically disposed when their last reference is removed.
        /// </summary>
        [Json]
        public bool autoDisposeUnused
        {
            get => _autoDisposeUnused;
            set => SetValue(nameof(autoDisposeUnused), value, ref _autoDisposeUnused);
        }

        private Tween waitBetweenLoadTimer
        {
            get => _waitBetweenLoadTimer;
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
            get => _loadDelayTimer;
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

        public virtual bool RemoveReference(object loadScopeKey, ReferenceBase reference, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            return true;
        }

        protected override void ObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            base.ObjectPropertyAssignedHandler(property, name, newValue, oldValue);

            if (name == nameof(Object.gameObjectActiveSelf) && (bool)newValue)
                QueueAutoUpdate();
        }

        protected override void EnabledChanged(bool newValue, bool oldValue)
        {
            base.EnabledChanged(newValue, oldValue);

            if (newValue)
                QueueAutoUpdate();
        }

        /// <summary>
        /// Queue a call to <see cref="DepictionEngine.LoaderBase.UpdateLoadScopes"/> if and when possible.
        /// </summary>
        protected void QueueAutoUpdate()
        {
            if (initialized)
                _autoUpdate = true;
        }

        protected bool CanAutoLoad()
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
                            if (!IsDisposing())
                            {
                                waitBetweenLoadTimer = null;
                                if (_autoUpdate && (loadDelayTimer == Disposable.NULL || !loadDelayTimer.playing))
                                {
                                    loadDelayTimer = tweenManager.DelayedCall(autoUpdateDelay, null, () =>
                                    {
                                        if (!IsDisposing() && CanAutoLoad())
                                            UpdateLoadScopes(true);
                                    }, () => loadDelayTimer = null);
                                }
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

        public bool IsPersistentInScope(IPersistent persistent)
        {
            bool isInScope = false;

            IterateOverLoadScopes((loadScopeKey, loadScope) => 
            {
                if (loadScope.ContainsPersistent(persistent.id))
                {
                    isInScope = true;
                    return false;
                }
                return true;
            });

            return isInScope;
        }

        public bool ContainsPersistent(IPersistent persistent)
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

                SceneManager.MarkSceneDirty();
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
            RegisterCompleteObjectUndo(disposeContext);
#endif

            if (persistentsDictionary.Remove(persistentId))
            {
#if UNITY_EDITOR
                lastPersistentsDictionary.Remove(persistentId);

                SceneManager.MarkSceneDirty();
#endif
                return true;
            }

            return false;
        }

        protected void RemoveAllPersistents(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (persistentsDictionary.Count > 0)
            {
#if UNITY_EDITOR
                RegisterCompleteObjectUndo(disposeContext);
#endif

                persistentsDictionary.Clear();

#if UNITY_EDITOR
                lastPersistentsDictionary.Clear();

                SceneManager.MarkSceneDirty();
#endif
            }
        } 

        protected LoadScope CreateLoadScope(Type type, string name)
        {
            LoadScope loadScope = instanceManager.CreateInstance(type) as LoadScope;
            if (loadScope != Disposable.NULL)
            {
                loadScope.name = name;
                return loadScope;
            }
            return null;
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
            RegisterCompleteObjectUndo(disposeContext);
#endif

            if (RemoveLoadScopeInternal(loadScopeKey, out LoadScope loadScope) && loadScope is not null)
            {
                RemoveLoadScopeDelegates(loadScope);

                loadScope.IterateOverPersistents((persistentId, persistent) => 
                {
                    if (!GetLoadScope(out LoadScope loadScope, persistent))
                        RemovePersistent(persistent, disposeContext);
                    return true;
                });

                return true;
            }

            return false;
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
                RegisterCompleteObjectUndo(disposeContext);
#endif

                IterateOverLoadScopes((loadScopeKey, loadScope) =>
                {
                    DisposeLoadScope(loadScope, disposeContext);

                    return true;
                });
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
        protected class ReferencesList : IList<ReferenceBase>
        {
            [SerializeField]
            private List<ReferenceBase> _references;

            public int Count => _references.Count;

            public bool IsReadOnly => false;

            public ReferenceBase this[int index] { get => _references[index]; set => _references[index] = value; }

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

            public int IndexOf(ReferenceBase item)
            {
                return _references.IndexOf(item);
            }

            public void Insert(int index, ReferenceBase item)
            {
                _references.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                _references.RemoveAt(index);
            }

            public void Clear()
            {
                _references.Clear();
            }

            public void CopyTo(ReferenceBase[] array, int arrayIndex)
            {
                _references.CopyTo(array, arrayIndex);
            }

            public IEnumerator<ReferenceBase> GetEnumerator()
            {
                return _references.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _references.GetEnumerator();
            }
        }
    }
}
