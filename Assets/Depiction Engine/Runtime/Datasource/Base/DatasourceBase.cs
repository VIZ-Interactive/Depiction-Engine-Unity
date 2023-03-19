// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Wrapper class to allow the use of Datasource as script.
    /// </summary>
    public class DatasourceBase : Script, ILoadDatasource
    {
        public const string NOTHING_TO_SAVE_MSG = "Nothing to Save";
        public const string NOTHING_TO_SYNCHRONIZE_MSG = "Nothing to Synchronize";
        public const string NOTHING_TO_DELETE_MSG = "Nothing to Delete";

#if UNITY_EDITOR
        [BeginFoldout("Operations")]
        [SerializeField, Button(nameof(SaveAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSaveBtn)), Tooltip("Push scene objects values to the datasource."), BeginHorizontalGroup(true)]
        private bool _saveAll;
        [SerializeField, Button(nameof(SaveSelectedBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSaveBtn)), Tooltip("Push the selected scene objects values to the datasource."), EndHorizontalGroup]
        private bool _saveSelected;

        [SerializeField, Button(nameof(SynchronizeAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSynchronizeBtn)), Tooltip("Pull the values from the datasource and use them to update the scene objects."), BeginHorizontalGroup(true)]
        private bool _synchronizeAll;
        [SerializeField, Button(nameof(SynchronizeSelectedBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSynchronizeBtn)), Tooltip("Pull the values from the datasource and use them to update the selected scene objects."), EndHorizontalGroup]
        private bool _synchronizeSelected;

        [SerializeField, Button(nameof(DeleteAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableDeleteBtn)), Tooltip("Delete all the scene objects from the datasource (The scene objects will not be deleted)."), BeginHorizontalGroup(true)]
        private bool _deleteAll;
        [SerializeField, Button(nameof(DeleteSelectedBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableDeleteBtn)), Tooltip("Delete all the selected scene objects from the datasource (The scene objects will not be deleted)."), EndHorizontalGroup, EndFoldout]
        private bool _deleteSelected;
#endif

        [BeginFoldout("Load Scope")]
        [SerializeField, Tooltip("The interval (in seconds) at which we call the " + nameof(ReloadAll) + " function. Automatically calling " + nameof(ReloadAll) + " can be useful to keep objects in synch. Set to zero to deactivate."), EndFoldout]
        private float _autoReloadInterval;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(ReloadAllBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Create missing " + nameof(LoadScope) + "(s), reload existing ones and dispose those that are no longer required.")]
        private bool _reloadAll;
#endif

        [BeginFoldout("Persistents")]
        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private Datasource _datasource;

        private DatasourceOperationBase _datasourceOperation;

        private Tween _autoReloadIntervalTimer;

#if UNITY_EDITOR
        private bool GetEnableSaveBtn()
        {
            return datasource.SupportsOperationType(Datasource.OperationType.Save);
        }

        private bool GetEnableSynchronizeBtn()
        {
            return datasource.SupportsOperationType(Datasource.OperationType.Synchronize);
        }

        private bool GetEnableDeleteBtn()
        {
            return datasource.SupportsOperationType(Datasource.OperationType.Delete);
        }

        private void SaveAllBtn()
        {
            if (SaveAll() == 0)
                Debug.Log(NOTHING_TO_SAVE_MSG);
        }

        private void SaveSelectedBtn()
        {
            int saving = 0;

            IterateOverSelectedPersistent((persistent) => 
            {
                if (Save(persistent))
                    saving++;
            });

            if (saving == 0)
                Debug.Log(NOTHING_TO_SAVE_MSG);
        }

        private void SynchronizeAllBtn()
        {
            if (SynchronizeAll() == 0)
                Debug.Log(NOTHING_TO_SYNCHRONIZE_MSG);
        }

        private void SynchronizeSelectedBtn()
        {
            int synchronized = 0;

            IterateOverSelectedPersistent((persistent) =>
            {
                if (Synchronize(persistent))
                    synchronized++;
            });

            if (synchronized == 0)
                    Debug.Log(NOTHING_TO_SYNCHRONIZE_MSG);
        }

        private void DeleteAllBtn()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Delete All", "Are you sure you want to Delete All Persistents?", "Ok", "Cancel") && DeleteAll() == 0)
                Debug.Log(NOTHING_TO_DELETE_MSG);
        }

        private void DeleteSelectedBtn()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Delete Selected", "Are you sure you want to Delete Selected Persistent", "Ok", "Cancel"))
            {
                int deleted = 0;

                IterateOverSelectedPersistent((persistent) =>
                {
                    if (Delete(persistent))
                        deleted++;
                });

                if (deleted == 0)
                    Debug.Log(NOTHING_TO_DELETE_MSG);
            }
        }

        private void IterateOverSelectedPersistent(Action<IPersistent> callback)
        {
            IPersistent persistent = Editor.Selection.GetSelectedDatasourcePersistent();
            if (!Disposable.IsDisposed(persistent))
                callback(persistent);
        }

        private void ReloadAllBtn()
        {
            ReloadAll();
        }
#endif

        public override void Recycle()
        {
            base.Recycle();
        
            datasource?.Recycle();
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            KillTimers();
            StartAutoReloadIntervalTimer();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                datasource = default;
          
            datasource ??= DatasourceManager.CreateDatasource();
            datasource.Initialize(this, initializingContext);

            InitValue(value => autoReloadInterval = value, 0.0f, initializingContext);
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                datasource.UpdateAllDelegates(IsDisposing());

                RemoveDatasourcePersistentDelegates();
                AddDatasourcePersistentDelegates();

                return true;
            }
            return false;
        }

        private void RemoveDatasourcePersistentDelegates()
        {
            if (datasource is not null)
                datasource.PersistenceOperationEvent -= PersistenceOperationHandler;
        }

        private void AddDatasourcePersistentDelegates()
        {
            if (!IsDisposing() && datasource is not null)
                datasource.PersistenceOperationEvent += PersistenceOperationHandler;
        }

        private void PersistenceOperationHandler(Datasource.OperationType type, IPersistent persistent, Action callback)
        {
            switch (type)
            {
                case Datasource.OperationType.Save:
                    if (Save(persistent))
                        callback();
                    break;

                case Datasource.OperationType.Synchronize:
                    if (Synchronize(persistent))
                        callback();
                    break;

                case Datasource.OperationType.Delete:
                    if (Delete(persistent))
                        callback();
                    break;
            }
        }

#if UNITY_EDITOR
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            datasource?.UndoRedoPerformed();
        }
#endif

        protected override bool AddInstanceToManager()
        {
            return true;
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

        private void KillTimers()
        {
            autoReloadIntervalTimer = null;
        }

        public Datasource datasource
        {
            get { return _datasource; }
            private set { _datasource = value; }
        }

        private DatasourceOperationBase datasourceOperation
        {
            get { return _datasourceOperation; }
            set
            {
                if (Object.ReferenceEquals(_datasourceOperation, value))
                    return;

                DisposeManager.Dispose(_datasourceOperation);

                _datasourceOperation = value;
            }
        }

        public virtual string GetDatasourceName()
        {
            return null;
        }

        /// <summary>
        /// Create missing <see cref="DepictionEngine.LoadScope"/>(s), reload existing ones and dispose those that are no longer required.
        /// </summary>
        /// <returns></returns>
        public void ReloadAll()
        {
            datasource.ReloadAll();
        }

        /// <summary>
        /// Add all the <see cref="DepictionEngine.IPersistent"/> found in the datasource to the save operation queue.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> succesfully added to the queue.</returns>
        public int SaveAll()
        {
            int saving = 0;

            datasource.IterateOverPersistents((persistentId, persistent) =>
            {
                if (Save(persistent))
                    saving++;
                return true;
            });

            return saving;
        }

        private Dictionary<Guid, Datasource.PersistenceOperationData> _savePersistenceOperationDatas;
        /// <summary>
        /// Add the the <see cref="DepictionEngine.IPersistent"/> to the save operation queue.
        /// </summary>
        /// <returns>True if the <see cref="DepictionEngine.IPersistent"/> was succesfully added to the queue otherwise False.</returns>
        public bool Save(IPersistent persistent)
        {
            if (datasource.SupportsOperationType(Datasource.OperationType.Save))
            {
                if (!persistent.containsCopyrightedMaterial)
                {
                    _savePersistenceOperationDatas ??= new Dictionary<Guid, Datasource.PersistenceOperationData>();

                    if (!_savePersistenceOperationDatas.ContainsKey(id))
                    {
                        SerializableGuid id = persistent.id;

                        bool isInDatasource = datasource.GetPersistent(id, out persistent);
                        if (!isInDatasource || datasource.IsPersistentOutOfSync(persistent))
                        {
                            JSONObject json = persistent.GetJson(isInDatasource ? datasource : null);
                            if (json != null)
                            {
                                json.Remove(nameof(IPersistent.dontSaveToScene));
                                if (persistent is AutoGenerateVisualObject)
                                    json.Remove(nameof(AutoGenerateVisualObject.dontSaveVisualsToScene));

                                _savePersistenceOperationDatas[id] = new Datasource.PersistenceOperationData(persistent, json);
                                return true;
                            }
                        }
                    }
                }
                else
                    Debug.LogWarning(persistent + " contains Copyrighted material and therefore cannot be saved to a different Datasource");
            }
            return false;
        }

        /// <summary>
        /// Add all the <see cref="DepictionEngine.IPersistent"/> found in the datasource to the synchronize operation queue.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> succesfully added to the queue.</returns>
        public int SynchronizeAll()
        {
            int synchronizing = 0;

            datasource.IterateOverPersistents((persistentId, persistent) =>
            {
                if (Synchronize(persistent))
                    synchronizing++;
                return true;
            });

            return synchronizing;
        }

        private Dictionary<Guid, Datasource.PersistenceOperationData> _synchronizePersistenceOperationDatas;
        /// <summary>
        /// Add the the <see cref="DepictionEngine.IPersistent"/> to the synchronize operation queue.
        /// </summary>
        /// <returns>True if the <see cref="DepictionEngine.IPersistent"/> was succesfully added to the queue otherwise False.</returns>
        public bool Synchronize(IPersistent persistent)
        {
            if (datasource.SupportsOperationType(Datasource.OperationType.Synchronize))
            {
                _synchronizePersistenceOperationDatas ??= new Dictionary<Guid, Datasource.PersistenceOperationData>();

                if (!_synchronizePersistenceOperationDatas.ContainsKey(id))
                {
                    SerializableGuid id = persistent.id;
                    bool isInDatasource = datasource.GetPersistent(id, out _);
                    if (isInDatasource)
                    {
                        _synchronizePersistenceOperationDatas[id] = new Datasource.PersistenceOperationData(persistent);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Add all the <see cref="DepictionEngine.IPersistent"/> found in the datasource to the delete operation queue.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> succesfully added to the queue.</returns>
        public int DeleteAll()
        {
            int deleting = 0;

            datasource.IterateOverPersistents((persistentId, persistent) =>
            {
                if (Delete(persistent))
                    deleting++;
                return true;
            });

            return deleting;
        }

        private Dictionary<Guid, Datasource.PersistenceOperationData> _deletePersistenceOperationDatas;
        /// <summary>
        /// Add the the <see cref="DepictionEngine.IPersistent"/> to the delete operation queue.
        /// </summary>
        /// <returns>True if the <see cref="DepictionEngine.IPersistent"/> was succesfully added to the queue otherwise False.</returns>
        public bool Delete(IPersistent persistent)
        {
            if (datasource.SupportsOperationType(Datasource.OperationType.Delete))
            {
                _deletePersistenceOperationDatas ??= new Dictionary<Guid, Datasource.PersistenceOperationData>();

                if (!_deletePersistenceOperationDatas.ContainsKey(id))
                {
                    SerializableGuid id = persistent.id;
                    bool isInDatasource = datasource.GetPersistent(id, out _);
                    if (isInDatasource)
                    {
                        _deletePersistenceOperationDatas[id] = new Datasource.PersistenceOperationData(persistent);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Process the queued operations.
        /// </summary>
        public void ProcessPersistenceOperations()
        {
            if (datasourceOperation == Disposable.NULL)
                datasourceOperation = ProcessSaveOperations();
            if (datasourceOperation == Disposable.NULL)
                datasourceOperation = ProcessSynchronizeOperations();
            if (datasourceOperation == Disposable.NULL)
                datasourceOperation = ProcessDeleteOperations();
        }

        /// <summary>
        /// Execute a load operation based off of the information found in the <see cref="DepictionEngine.LoadScope"/>.
        /// </summary>
        /// <param name="operationResult"></param>
        /// <param name="loadScope"></param>
        /// <returns>A <see cref="DepictionEngine.DatasourceOperationBase"/> containing the synch or async result of the operation.</returns>
        public DatasourceOperationBase Load(Action<List<IPersistent>> operationResult, LoadScope loadScope)
        {
            DatasourceOperationBase datasourceOperation = CreateLoadDatasourceOperation(loadScope);

            datasource.Load(datasourceOperation, operationResult, loadScope);

            return datasourceOperation;
        }

        protected virtual DatasourceOperationBase CreateLoadDatasourceOperation(LoadScope loadScope)
        {
            return null;
        }

        private DatasourceOperationBase ProcessSaveOperations()
        {
            DatasourceOperationBase datasourceOperation = null;

            if (_savePersistenceOperationDatas != null && _savePersistenceOperationDatas.Count > 0)
            {
                datasourceOperation = CreateSaveDatasourceOperation(_savePersistenceOperationDatas);
                datasource.Save(datasourceOperation, (successCount) => 
                { 
                    DebugOperationResult(Datasource.OperationType.Save, successCount, _savePersistenceOperationDatas.Count - successCount);

                    _savePersistenceOperationDatas.Clear();

                    OperationCompleted();
                });
            }

            return datasourceOperation;
        }

        protected virtual DatasourceOperationBase CreateSaveDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> savePersistenceOperationDatas)
        {
            return null;
        }

        private DatasourceOperationBase ProcessSynchronizeOperations()
        {
            DatasourceOperationBase datasourceOperation = null;

            if (_synchronizePersistenceOperationDatas != null && _synchronizePersistenceOperationDatas.Count > 0)
            {
                datasourceOperation = CreateSynchronizeDatasourceOperation(_synchronizePersistenceOperationDatas);
                datasource.Synchronize(datasourceOperation, (successCount) => 
                { 
                    DebugOperationResult(Datasource.OperationType.Synchronize, successCount, _synchronizePersistenceOperationDatas.Count - successCount);

                    _synchronizePersistenceOperationDatas.Clear();

                    OperationCompleted();
                });
            }

            return datasourceOperation;
        }

        protected virtual DatasourceOperationBase CreateSynchronizeDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> synchronizePersistenceOperationDatas)
        {
            return null;
        }

        private DatasourceOperationBase ProcessDeleteOperations()
        {
            DatasourceOperationBase datasourceOperation = null;

            if (_deletePersistenceOperationDatas != null && _deletePersistenceOperationDatas.Count > 0)
            {
                datasourceOperation = CreateDeleteDatasourceOperation(_deletePersistenceOperationDatas);
                datasource.Delete(datasourceOperation, (successCount) => 
                { 
                    DebugOperationResult(Datasource.OperationType.Delete, successCount, _deletePersistenceOperationDatas.Count - successCount);

                    _deletePersistenceOperationDatas.Clear();

                    OperationCompleted();
                });
            }

            return datasourceOperation;
        }

        protected virtual DatasourceOperationBase CreateDeleteDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> deletePersistenceOperationDatas)
        {
            return null;
        }

        private void DebugOperationResult(Datasource.OperationType operationType, int success, int failed)
        {
            string operationTypeName = operationType.ToString();

            if (failed > 0)
                Debug.LogError(name + ": " + operationTypeName + "(" + success + "), Failed(" + failed + ")");
            else
                Debug.Log(name + ": " + operationTypeName + "(" + success + ")");
        }

        private void OperationCompleted()
        {
            datasourceOperation = null;
        }

        protected T CreateDatasourceOperation<T>() where T : DatasourceOperationBase
        {
            return instanceManager.CreateInstance<T>();
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                KillTimers();

                if (disposeContext != DisposeContext.Programmatically_Pool)
                    DisposeManager.Dispose(_datasource, disposeContext);

                return true;
            }
            return false;
        }
    }
}
