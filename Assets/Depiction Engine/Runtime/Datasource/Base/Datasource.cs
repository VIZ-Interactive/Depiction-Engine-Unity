// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    public class Datasource : PropertyScriptableObject
    {
        /// <summary>
        /// The different types of datasource operation. <br/><br/>
        /// <b><see cref="Save"/>:</b> <br/>
        /// Save to datasource. <br/><br/>
        /// <b><see cref="Synchronize"/>:</b> <br/>
        /// Synchronize with datasource. <br/><br/>
        /// <b><see cref="Delete"/>:</b> <br/>
        /// Delete from datasource. <br/><br/>
        /// <b><see cref="Load"/>:</b> <br/>
        /// Load from datasource.
        /// </summary>
        public enum OperationType
        {
            Save,
            Synchronize,
            Delete,
            Load
        }

        [Serializable]
        private class PersistenceDataDictionary : SerializableDictionary<SerializableGuid, PersistenceData> { };

        [SerializeField]
        private PersistenceDataDictionary _persistenceDataDictionary;

        [SerializeField]
        private bool _supportsSave;
        [SerializeField]
        private bool _supportsSynchronize;
        [SerializeField]
        private bool _supportsDelete;

        private List<LoaderBase> _loaders;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.PersistenceData"/> instance is added to the <see cref="DepictionEngine.Datasource"/>.
        /// </summary>
        public Action<PersistenceData> PersistenceDataAddedEvent;
        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.PersistenceData"/> instance is removed from the <see cref="DepictionEngine.Datasource"/>.
        /// </summary>
        public Action<PersistenceData> PersistenceDataRemovedEvent;

        public override void Recycle()
        {
            base.Recycle();

            _loaders?.Clear();

            _supportsSave = default;
            _supportsSynchronize = default;
            _supportsDelete = default;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<LoaderBase>((loader) =>
                {
                    if (loader.GetDatasource() == this)
                        AddLoader(loader);

                    return true;
                });
            }
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            IterateOverPersistenceData((persistentId, persistenceData) =>
            {
                if (Disposable.IsDisposed(persistenceData.persistent))
                    _persistenceDataDictionary.Remove(persistentId);
                return true;
            });
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                DatasourceManager.DatasourceLoadersChangedEvent -= DatasourceLoadersChangedHandler;
                if (!IsDisposing())
                    DatasourceManager.DatasourceLoadersChangedEvent += DatasourceLoadersChangedHandler;

                IterateOverLoaders((loader) =>
                {
                    RemoveLoaderDelegates(loader);
                    AddLoaderDelegates(loader);

                    return true;
                });

                IterateOverPersistenceData((persistentId, persistenceData) =>
                {
                    RemovePersistenceDataDelegates(persistenceData);
                    AddPersistenceDataDelegates(persistenceData);

                    return true;
                });

                return true;
            }
            return false;
        }

        private void DatasourceLoadersChangedHandler(LoaderBase loader)
        {
            if (loader != Disposable.NULL && loader.GetDatasource() == this)
                AddLoader(loader);
            else
                RemoveLoader(loader);
        }

        private void RemoveLoaderDelegates(LoaderBase loader)
        {
            if (loader is not null)
            {
                loader.LoadScopeDisposingEvent -= LoadScopeDisposingHandler;
                loader.LoadScopeLoadingStateChangedEvent -= LoadScopeLoadingStateChangedHandler;
            }
        }

        private void AddLoaderDelegates(LoaderBase loader)
        {
            if (!IsDisposing() && loader != Disposable.NULL)
            {
                loader.LoadScopeDisposingEvent += LoadScopeDisposingHandler;
                loader.LoadScopeLoadingStateChangedEvent += LoadScopeLoadingStateChangedHandler;
            }
        }

        private void LoadScopeDisposingHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            AutoDisposeLoadScopePersistents(disposable as LoadScope, disposeContext);
        }

        private void LoadScopeLoadingStateChangedHandler(LoadScope loadScope)
        {
            if (_reloadingLoadScopes != null && _reloadingLoadScopes.Remove(loadScope) && _reloadingLoadScopes.Count == 0)
                ReloadCompleted();
        }

        private void RemovePersistenceDataDelegates(PersistenceData persistenceData)
        {
            if (persistenceData is not null)
            {
                persistenceData.DisposedEvent -= PersistenceDataDisposedHandler;
                persistenceData.CanBeAutoDisposedChangedEvent -= PersistenceDataCanBeAutoDisposedChangedHandler;
                persistenceData.PropertyAssignedEvent -= PersistenceDataPropertyAssignedHandler;
            }
        }

        private void AddPersistenceDataDelegates(PersistenceData persistenceData)
        {
            if (!IsDisposing() && !Disposable.IsDisposed(persistenceData))
            {
                persistenceData.DisposedEvent += PersistenceDataDisposedHandler;
                persistenceData.CanBeAutoDisposedChangedEvent += PersistenceDataCanBeAutoDisposedChangedHandler;
                persistenceData.PropertyAssignedEvent += PersistenceDataPropertyAssignedHandler;
            }
        }

        private void PersistenceDataDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
#if UNITY_EDITOR
            InitializationContext initializationContext = disposeContext == DisposeContext.Editor_Destroy ? InitializationContext.Editor : InitializationContext.Programmatically;
            Editor.UndoManager.RecordObject(this, initializationContext);
#endif
            RemovePersistenceData(disposable as PersistenceData);
#if UNITY_EDITOR
            Editor.UndoManager.FlushUndoRecordObjects();
#endif
        }

        private void PersistenceDataCanBeAutoDisposedChangedHandler(PersistenceData persistenceData)
        {
            AutoDisposePersistent(persistenceData);
        }

        private void PersistenceDataPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (property is IGrid2DIndex)
            {
                bool dimensionsChanged = name == nameof(IGrid2DIndex.grid2DDimensions);
                bool indexChanged = name == nameof(IGrid2DIndex.grid2DIndex);
                if (dimensionsChanged || indexChanged)
                {
                    if (GetPersistenceData(property.id, out PersistenceData persistenceData))
                    {
                        IGrid2DIndex gridIndexObject = property as IGrid2DIndex;

                        IterateOverLoaders((loader) =>
                        {
                            if (loader is Index2DLoaderBase && loader.Contains(persistenceData.persistent))
                            {
                                Index2DLoaderBase index2DLoader = loader as Index2DLoaderBase;

                                Vector2Int oldDimensions = gridIndexObject.grid2DDimensions;
                                Vector2Int newDimensions = gridIndexObject.grid2DDimensions;
                                if (dimensionsChanged)
                                {
                                    oldDimensions = (Vector2Int)oldValue;
                                    newDimensions = (Vector2Int)newValue;
                                }

                                Vector2Int oldIndex = gridIndexObject.grid2DIndex;
                                Vector2Int newIndex = gridIndexObject.grid2DIndex;
                                if (indexChanged)
                                {
                                    oldIndex = (Vector2Int)oldValue;
                                    newIndex = (Vector2Int)newValue;
                                }

                                ChangeLoadScope(persistenceData, index2DLoader.GetLoadScope(out Index2DLoadScope newLoadScope, newDimensions, newIndex) ? newLoadScope : null, index2DLoader.GetLoadScope(out Index2DLoadScope oldLoadScope, oldDimensions, oldIndex) ? oldLoadScope : null);
                            }
                            return true;
                        });
                    }
                }
            }
        }

        private void ChangeLoadScope(PersistenceData persistenceData, LoadScope newLoadScope, LoadScope oldLoadScope)
        {
            if (newLoadScope != oldLoadScope)
            {
                if (oldLoadScope != Disposable.NULL)
                    oldLoadScope.RemovePersistent(persistenceData.persistent);
                if (newLoadScope != Disposable.NULL)
                    newLoadScope.AddPersistent(persistenceData.persistent);

                persistenceData.UpdateCanBeAutoDisposed();
            }
        }

        public static bool EnablePersistenceOperations()
        {
#if UNITY_EDITOR
            return !Application.isPlaying;
#else
            return true;
#endif
        }

        public bool GetPersistenceData(SerializableGuid id, out PersistenceData persistentData)
        {
            if (persistenceDataDictionary.TryGetValue(id, out persistentData) && persistentData != Disposable.NULL)
                return true;

            persistentData = null;
            return false;
        }

        public bool supportsSave
        {
            get { return _supportsSave; }
            set 
            {
                if (_supportsSave == value)
                    return;

                _supportsSave = value;

                IterateOverPersistenceData((persistentId, persistenceData) => { persistenceData.supportsSave = value; return true; });
            }
        }

        public bool supportsSynchronize
        {
            get { return _supportsSynchronize; }
            set 
            {
                if (_supportsSynchronize == value)
                    return;

                _supportsSynchronize = value;

                IterateOverPersistenceData((persistentId, persistenceData) => { persistenceData.supportsSynchronize = value; return true; });
            }
        }

        public bool supportsDelete
        {
            get { return _supportsDelete; }
            set 
            {
                if (_supportsDelete == value)
                    return;

                _supportsDelete = value;

                IterateOverPersistenceData((persistentId, persistenceData) => { persistenceData.supportsDelete = value; return true; });
            }
        }

        public int persistentCount
        {
            get { return persistenceDataDictionary.Count; }
        }

        private PersistenceDataDictionary persistenceDataDictionary
        {
            get
            {
                _persistenceDataDictionary ??= new PersistenceDataDictionary();
                return _persistenceDataDictionary;
            }
        }

        public void IterateOverLoaders(Func<LoaderBase, bool> callback)
        {
            if (_loaders != null)
            {
                for (int i = _loaders.Count - 1; i >= 0; i--)
                {
                    if (!callback(_loaders[i]))
                        break;
                }
            }
        }

        public void IterateOverPersistenceData(Func<SerializableGuid, PersistenceData, bool> callback)
        {
            if (_persistenceDataDictionary != null)
            {
                for (int i = _persistenceDataDictionary.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<SerializableGuid, PersistenceData> persistenceDataKey = _persistenceDataDictionary.ElementAt(i);
                    if (!callback(persistenceDataKey.Key, persistenceDataKey.Value))
                        break;
                }
            }
        }

        private void AddPersistenceData(PersistenceData persistenceData, bool allOutOfSync = false)
        {
            SerializableGuid persistentId = persistenceData.persistent.id;
            if (persistenceDataDictionary.TryAdd(persistentId, persistenceData))
            {
                AddPersistenceDataDelegates(persistenceData);

                if (allOutOfSync)
                    persistenceData.SetAllOutOfSync();

                persistenceData.supportsSave = supportsSave;
                persistenceData.supportsSynchronize = supportsSynchronize;
                persistenceData.supportsDelete = supportsDelete;

                PersistenceDataAddedEvent?.Invoke(persistenceData);
            }
        }

        private void RemovePersistenceData(PersistenceData persistenceData)
        {
            if (IsDisposing())
                return;

            SerializableGuid persistentId = persistenceData.persistent.id;
            if (persistenceDataDictionary.Remove(persistentId))
            {
                RemovePersistenceDataDelegates(persistenceData);
                PersistenceDataRemovedEvent?.Invoke(persistenceData);
            }
        }

        private List<LoaderBase> loaders
        {
            get
            {
                _loaders ??= new List<LoaderBase>();
                return _loaders;
            }
        }

        private bool RemoveLoader(LoaderBase loader)
        {
            if (loaders.Remove(loader))
            {
                RemoveLoaderDelegates(loader);

                return true;
            }
            return false;
        }

        private bool AddLoader(LoaderBase loader)
        {
            if (!loaders.Contains(loader))
            {
                loaders.Add(loader);

                if (initialized)
                    AddLoaderDelegates(loader);

                return true;
            }
            return false;
        }

        public bool SupportsOperationType(OperationType operationType)
        {
            if (EnablePersistenceOperations())
            {
                if (operationType == OperationType.Save)
                    return supportsSave;
                if (operationType == OperationType.Synchronize)
                    return supportsSynchronize;
                if (operationType == OperationType.Delete)
                    return supportsDelete;
            }
            return false;
        }

        private List<LoadScope> _reloadingLoadScopes;
        /// <summary>
        /// Create missing <see cref="DepictionEngine.LoadScope"/>(s), reload existing ones and dispose those that are no longer required.
        /// </summary>
        public void ReloadAll()
        {
            if (_reloadingLoadScopes == null)
                _reloadingLoadScopes = new();
            else
                _reloadingLoadScopes.Clear();

            IterateOverLoaders((loader) =>
            {
                List<LoadScope> reloadLoadScope = loader.ReloadAll();
                if (reloadLoadScope != null)
                {
                    foreach (LoadScope loadScope in reloadLoadScope)
                    {
                        if (loadScope.LoadInProgress())
                            _reloadingLoadScopes.Add(loadScope);
                    }
                }
                return true;
            });

            if (_reloadingLoadScopes.Count == 0)
                ReloadCompleted();
        }

        private void ReloadCompleted()
        {
            IterateOverPersistenceData((persistentId, persistenceData) =>
            {
                IPersistent persistent = persistenceData.persistent;

                bool isInScope = false;

                IterateOverLoaders((loader) =>
                {
                    if (loader.GetLoadScope(out LoadScope loadScope, persistent))
                        isInScope = true;
                    else
                        loader.RemovePersistent(persistent);

                    return true;
                });

                if (!isInScope)
                    AutoDisposePersistent(persistenceData);

                return true;
            });
        }

        public void Load(DatasourceOperationBase datasourceOperation, Action<List<IPersistent>> resultCallback, LoadScope loadScope)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        List<IPersistent> persistents = null;

                        if (success)
                            persistents = CreatePersistents(loadScope.loader, operationResult);

                        resultCallback?.Invoke(persistents);
                    });
            }
        }

        public void Save(DatasourceOperationBase datasourceOperation, Action<int> resultCallback)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        int successCount = 0;

                        if (success)
                        {
                            operationResult.IterateOverResultsData<IdResultData>((idResultData, persistent) =>
                            {
                                if (!Disposable.IsDisposed(persistent) && !GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                                    AddPersistenceData(CreatePersistenceData(persistent));
                            });

                            successCount = SyncOperationResult(operationResult);
                        }

                        resultCallback?.Invoke(successCount);
                    });
            }
        }

        public void Synchronize(DatasourceOperationBase datasourceOperation, Action<int> resultCallback)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        int successCount = 0;

                        if (success)
                        {
                            operationResult.IterateOverResultsData<SynchronizeResultData>((synchronizeResultData, persistent) =>
                            {
                                if (!Disposable.IsDisposed(persistent))
                                    persistent.SetJson(synchronizeResultData.json);
                            });

                            successCount = SyncOperationResult(operationResult);
                        }

                        resultCallback?.Invoke(successCount);
                    });
            }
        }

        private int SyncOperationResult(OperationResult operationResult)
        {
            int successCount = operationResult.IterateOverResultsData<IdResultData>((idResultData, persistent) =>
            {
                if(!Disposable.IsDisposed(persistent) && GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                    AutoDisposePersistent(persistenceData);
            });

            ReloadAll();

            return successCount;
        }

        public void Delete(DatasourceOperationBase datasourceOperation, Action<int> resultCallback)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        int successCount = 0;

                        if (success)
                        {
                            successCount = operationResult.IterateOverResultsData<IdResultData>((idResultData, persistent) =>
                            {
                                if (!Disposable.IsDisposed(persistent) && GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                                    Dispose(persistenceData);
                            });
                        }

                        resultCallback?.Invoke(successCount);
                    });
            }
        }

        private List<IPersistent> CreatePersistents(LoaderBase loader, OperationResult operationResult)
        {
            List<IPersistent> persistents = null;

            if (operationResult != Disposable.NULL)
            {
                if (operationResult.resultsData != null && operationResult.resultsData.Count > 0)
                {
                    foreach (LoadResultData loadResultData in operationResult.resultsData.Cast<LoadResultData>())
                    {
                        List<IPersistent> createdPersistents = CreateObjectAndChildren(loader, loadResultData);
                        if (persistents == null)
                            persistents = createdPersistents;
                        else
                            persistents.AddRange(createdPersistents);
                    }
                }
            }

            return persistents;
        }

        private List<IPersistent> CreateObjectAndChildren(LoaderBase loader, LoadResultData loadResultData)
        {
            List<IPersistent> persistents = new();

            MergeJson(loadResultData.jsonResult, loadResultData.jsonFallback);

            FallbackValues persistentFallbackValues = instanceManager.GetFallbackValues(loadResultData.persistentFallbackValuesId);

            if (persistentFallbackValues != Disposable.NULL)
                persistentFallbackValues.ApplyFallbackValuesToJson(loadResultData.jsonResult);

            IPersistent persistent = CreatePersistent(loader, loadResultData.type, loadResultData.jsonResult, loadResultData.propertyModifiers);

            persistents.Add(persistent);

            if (persistent is Object)
            {
                Object objectBase = persistent as Object;
                if (loadResultData.children != null && loadResultData.children.Count > 0)
                {
                    foreach (LoadResultData childLoadData in loadResultData.children)
                    {
                        if (childLoadData.jsonResult[nameof(Object.transform)] == null || childLoadData.jsonResult[nameof(Object.transform)][nameof(TransformBase.parent)] == null)
                            childLoadData.jsonResult[nameof(Object.transform)][nameof(TransformBase.parent)] = objectBase.transform.id.ToString();

                        persistents.Concat(CreateObjectAndChildren(loader, childLoadData));
                    }
                }
            }

            return persistents;
        }

        private IPersistent CreatePersistent(LoaderBase loader, Type type, JSONNode json, List<PropertyModifier> propertyModifiers = null)
        {
            if (json != null && json[nameof(IPersistent.id)] != null)
            {
                if (SerializableGuid.TryParse(json[nameof(IPersistent.id)], out SerializableGuid id))
                {
                    if (GetPersistenceData(id, out PersistenceData persistentData))
                        return persistentData.persistent;
                }
            }

            bool isNewPersitent = loader.GeneratePersistent(out IPersistent persistent, type, json, propertyModifiers);

            if (!Disposable.IsDisposed(persistent))
                AddPersistenceData(CreatePersistenceData(persistent), !isNewPersitent);

            return persistent;
        }

        private PersistenceData CreatePersistenceData(IPersistent persistent)
        {
            Type type = typeof(PersistenceData);
            if (persistent is Object)
                type = typeof(ObjectPersistenceData);

            return (instanceManager.CreateInstance(type) as PersistenceData).Init(this, persistent);
        }

        private void MergeJson(JSONNode json1, JSONObject json2)
        {
            foreach (string key in json2.m_Dict.Keys)
            {
                JSONNode json2Node = json2[key];
                if (json1[key] == null)
                    json1[key] = json2Node;
                else
                {
                    if (json2Node is JSONObject && json2Node.Count > 0)
                        MergeJson(json1[key], json2Node as JSONObject);
                }
            }
        }

        private void AutoDisposeLoadScopePersistents(LoadScope loadScope, DisposeContext disposeContext)
        {
            if (loadScope.loader != Disposable.NULL)
            {
                loadScope.IterateOverPersistents((i, persistent) =>
                {
                    if (GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                        AutoDisposePersistent(persistenceData, disposeContext);

                    return true;
                });
            }
        }

        private bool AutoDisposePersistent(PersistenceData persistenceData, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            IPersistent persistent = persistenceData.persistent;

            if (!Disposable.IsDisposed(persistent))
            {
                bool dispose = persistenceData.canBeAutoDisposed;

                if (!IsDisposing() && dispose)
                {
                    IterateOverLoaders((loader) =>
                    {
                        if (loader.Contains(persistent) && loader.GetLoadScope(out LoadScope loadScope, persistent))
                        {
                            dispose = false;

                            return false;
                        }
                        return true;
                    });
                }

                if (dispose)
                {
                    Dispose(persistent is PersistentMonoBehaviour ? (persistent as PersistentMonoBehaviour).gameObject : persistent, disposeContext);

                    return true;
                }
            }
            
            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                IterateOverPersistenceData((persistentId, persistenceData) => 
                {
                    Dispose(persistenceData, disposeContext);
                    return true;
                });

                PersistenceDataAddedEvent = null;
                PersistenceDataRemovedEvent = null;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Data used to create a persistence operation.
        /// </summary>
        public struct PersistenceOperationData
        {
            public IPersistent persistent;
            public JSONObject data;

            public PersistenceOperationData(IPersistent persistent, JSONObject data = null)
            {
                this.persistent = persistent;
                this.data = data;
            }
        }
    }
}
