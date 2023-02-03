// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    public class Datasource : ScriptableObjectBase
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
        private List<LoaderBase> _loaders;

        [SerializeField]
        private bool _supportsSave;
        [SerializeField]
        private bool _supportsSynchronize;
        [SerializeField]
        private bool _supportsDelete;

        public Action<PersistenceData> PersistenceDataAddedEvent;
        public Action<PersistenceData> PersistenceDataRemovedEvent;

        public override void Recycle()
        {
            base.Recycle();

            DisposeAllPersistenceData();

            if (_loaders != null)
                _loaders.Clear();

            _supportsSave = false;
            _supportsSynchronize = false;
            _supportsDelete = false;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            if (_loaders != null)
            {
                for (int i = _loaders.Count - 1; i >= 0;i--)
                {
                    if (_loaders[i] == Disposable.NULL)
                        _loaders.RemoveAt(i);
                }
            }

            List<SerializableGuid> nullPersistenceData = null;

            foreach (SerializableGuid key in persistenceDataDictionary.Keys)
            {
                if (persistenceDataDictionary.TryGetValue(key, out PersistenceData persistenceData) && Disposable.IsDisposed(persistenceData.persistent))
                {
                    if (nullPersistenceData == null)
                        nullPersistenceData = new List<SerializableGuid>();
                    nullPersistenceData.Add(key);
                }
            }

            if (nullPersistenceData != null)
            {
                for (int i = nullPersistenceData.Count - 1; i >= 0; i--)
                    persistenceDataDictionary.Remove(nullPersistenceData[i]);
            }
        }

        public Datasource Init()
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<LoaderBase>(
                   (loader) =>
                   {
                        if (loader.GetDatasource() == this)
                            AddLoader(loader);

                       return true;
                   });
            }

            return this;
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

                IterateOverPersistenceData((persistenceData) =>
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
            if (!Object.ReferenceEquals(loader, null))
                loader.LoadScopeDisposingEvent -= LoadScopeDisposingHandler;
        }

        private void AddLoaderDelegates(LoaderBase loader)
        {
            if (!IsDisposing() && loader != Disposable.NULL)
                loader.LoadScopeDisposingEvent += LoadScopeDisposingHandler;
        }

        private void LoadScopeDisposingHandler(IDisposable disposable)
        {
            AutoDisposePersistents(disposable as LoadScope);
        }

        private void RemovePersistenceDataDelegates(PersistenceData persistenceData)
        {
            if (!Object.ReferenceEquals(persistenceData, null))
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

        private void PersistenceDataDisposedHandler(IDisposable disposable)
        {
            RemovePersistenceData(disposable as PersistenceData);
        }

        private void PersistenceDataCanBeAutoDisposedChangedHandler(PersistenceData persistenceData)
        {
            AutoDispose(persistenceData);
        }

        private void PersistenceDataPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (property is IGridIndexObject)
            {
                bool dimensionsChanged = name == nameof(IGridIndexObject.grid2DDimensions);
                bool indexChanged = name == nameof(IGridIndexObject.grid2DIndex);
                if (dimensionsChanged || indexChanged)
                {
                    if (GetPersistenceData(property.id, out PersistenceData persistenceData))
                    {
                        IGridIndexObject gridIndexObject = property as IGridIndexObject;

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

                IterateOverPersistenceData((persistenceData) => { persistenceData.supportsSave = value; return true; });
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

                IterateOverPersistenceData((persistenceData) => { persistenceData.supportsSynchronize = value; return true; });
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

                IterateOverPersistenceData((persistenceData) => { persistenceData.supportsDelete = value; return true; });
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
                if (_persistenceDataDictionary == null)
                    _persistenceDataDictionary = new PersistenceDataDictionary();
                return _persistenceDataDictionary;
            }
        }

        public void IterateOverPersistenceData(Func<PersistenceData, bool> callback)
        {
            foreach (PersistenceData persistenceData in persistenceDataDictionary.Values)
            {
                if (!callback(persistenceData))
                    break;
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

                if (PersistenceDataAddedEvent != null)
                    PersistenceDataAddedEvent(persistenceData);
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

                if (PersistenceDataRemovedEvent != null)
                    PersistenceDataRemovedEvent(persistenceData);
            }
        }

        private List<LoaderBase> loaders
        {
            get
            {
                if (_loaders == null)
                    _loaders = new List<LoaderBase>();
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

        public void Load(DatasourceOperationBase datasourceOperation, Action<List<IPersistent>> resultCallback, LoadScope loadScope)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        List<IPersistent> persistents = null;

                        if (success)
                            persistents = CreatePersistents(loadScope.loader, operationResult);

                        if (resultCallback != null)
                            resultCallback(persistents);
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

                        if (resultCallback != null)
                            resultCallback(successCount);
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

                        if (resultCallback != null)
                            resultCallback(successCount);
                    });
            }
        }

        private int SyncOperationResult(OperationResult operationResult)
        {
            int successCount = operationResult.IterateOverResultsData<IdResultData>((idResultData, persistent) =>
            {
                if(!Disposable.IsDisposed(persistent) && GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                    AutoDispose(persistenceData);
            });

            IterateOverLoaders((loader) =>
            {
                loader.ReloadAll();
                return true;
            });

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

                        if (resultCallback != null)
                            resultCallback(successCount);
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
                    foreach (LoadResultData loadResultData in operationResult.resultsData)
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
            List<IPersistent> persistents = new List<IPersistent>();

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
            PersistenceData persistentData;

            if (json != null && json[nameof(IPersistent.id)] != null)
            {
                if (SerializableGuid.TryParse(json[nameof(IPersistent.id)], out SerializableGuid id))
                {
                    if (GetPersistenceData(id, out persistentData))
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

        private bool IterateOverLoaders(Func<LoaderBase, bool> callback)
        {
            for (int i = loaders.Count - 1; i >= 0; i--)
            {
                if (!callback(loaders[i]))
                    return false;
            }

            return true;
        }

        private void AutoDisposePersistents(LoadScope loadScope)
        {
            if (loadScope.loader != Disposable.NULL)
            {
                loadScope.IterateOverPersistents((persistent) =>
                {
                    if (GetPersistenceData(persistent.id, out PersistenceData persistenceData))
                        AutoDispose(persistenceData);

                    return true;
                });
            }
        }

        private bool AutoDispose(PersistenceData persistenceData)
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
                    Dispose(persistent is PersistentMonoBehaviour ? (persistent as PersistentMonoBehaviour).gameObject : persistent, DisposeManager.DestroyDelay.Delayed);

                    return true;
                }
            }
            
            return false;
        }

        private void DisposeAllPersistenceData()
        {
            if (_persistenceDataDictionary != null)
            {
                foreach (PersistenceData persistenceData in _persistenceDataDictionary.Values)
                    Dispose(persistenceData);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            DisposeAllPersistenceData();
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
