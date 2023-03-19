// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [Serializable]
    public class Datasource
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
        public class PersistentDictionary : SerializableDictionary<SerializableGuid, SerializableIPersistent> { };

        [Serializable]
        private class PersistentsComponentsOutOfSyncDictionary : SerializableDictionary<SerializableGuid, ComponentsOutOfSyncDictionary> { };
        [Serializable]
        private class ComponentsOutOfSyncDictionary : SerializableDictionary<SerializableGuid, ComponentOutOfSyncKeysDictionary> { };
        [Serializable]
        private class ComponentOutOfSyncKeysDictionary : SerializableDictionary<int, bool> { };

        [Serializable]
        private class SerializableGuidHashSet : SerializableHashSet<SerializableGuid> { };

        [SerializeField]
        private PersistentDictionary _persistentsDictionary;
        [SerializeField]
        private PersistentsComponentsOutOfSyncDictionary _persistentComponentOutOfSyncDictionary;
        [SerializeField]
        private SerializableGuidHashSet _persistentIsOutOfSync;
        [SerializeField]
        private SerializableGuidHashSet _persistentCanBeAutoDisposed;

        [SerializeField]
        private bool _supportsSave;
        [SerializeField]
        private bool _supportsSynchronize;
        [SerializeField]
        private bool _supportsDelete;

        [SerializeField]
        private JsonMonoBehaviour _datasourceWrapper;

        private List<LoaderBase> _loaders;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.IPersistent.PersistenceSaveOperationEvent"/>, <see cref="DepictionEngine.IPersistent.PersistenceSynchronizeOperationEvent"/> or <see cref="DepictionEngine.IPersistent.PersistenceDeleteOperationEvent"/> is dispatched by the encapsulated <see cref="DepictionEngine.IPersistent"/>.
        /// </summary>
        public Action<OperationType, IPersistent, Action> PersistenceOperationEvent;

        public void Recycle()
        {
            _persistentsDictionary?.Clear();

            _persistentComponentOutOfSyncDictionary?.Clear();
            _persistentIsOutOfSync.Clear();
            _persistentCanBeAutoDisposed.Clear();

            _supportsSave = default;
            _supportsSynchronize = default;
            _supportsDelete = default;

            _loaders?.Clear();
        }

        public Datasource Initialize(JsonMonoBehaviour datasourceWrapper, InitializationContext initializingContext)
        {
            this.datasourceWrapper = datasourceWrapper;

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

            if (initializingContext == InitializationContext.Existing)
            {
                IterateOverPersistents((persistentId, persistent) =>
                {
                    if (Disposable.IsDisposed(persistent))
                        RemovePersistent(persistent);
                    return true;
                });
            }

            return this;
        }

        public bool UpdateAllDelegates(bool isDisposing)
        {
            DatasourceManager.DatasourceLoadersChangedEvent -= DatasourceLoadersChangedHandler;
            if (!isDisposing)
                DatasourceManager.DatasourceLoadersChangedEvent += DatasourceLoadersChangedHandler;

            IterateOverLoaders((loader) =>
            {
                RemoveLoaderDelegates(loader);
                AddLoaderDelegates(loader, isDisposing);

                return true;
            });

            IterateOverPersistents((persistentId, persistent) =>
            {
                RemovePersistentDelegates(persistent);
                AddPersistentDelegates(persistent, isDisposing);

                return true;
            });

            return true;
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
                loader.LoadScopeDisposedEvent -= LoadScopeDisposedHandler;
                loader.LoadScopeLoadingStateChangedEvent -= LoadScopeLoadingStateChangedHandler;
            }
        }

        private void AddLoaderDelegates(LoaderBase loader, bool isDisposing = false)
        {
            if (!isDisposing && loader != Disposable.NULL)
            {
                loader.LoadScopeDisposedEvent += LoadScopeDisposedHandler;
                loader.LoadScopeLoadingStateChangedEvent += LoadScopeLoadingStateChangedHandler;
            }
        }

        private void LoadScopeDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            AutoDisposeLoadScopePersistents(disposable as LoadScope, disposeContext);
        }

        private void LoadScopeLoadingStateChangedHandler(LoadScope loadScope)
        {
            if (_reloadingLoadScopes != null && _reloadingLoadScopes.Remove(loadScope) && _reloadingLoadScopes.Count == 0)
                ReloadCompleted();
        }

        private bool RemovePersistentDelegates(IPersistent persistent)
        {
            if (persistent is not null)
            {
                persistent.DisposedEvent -= PersistentDisposedHandler;
                persistent.PropertyAssignedEvent -= PersistentPropertyAssignedHandler;
                if (persistent is Object)
                {
                    Object objectBase = persistent as Object;
                    objectBase.ComponentPropertyAssignedEvent -= ObjectComponentPropertyAssignedHandler;
                    objectBase.ChildRemovedEvent -= ObjectChildRemovedHandler;
                    objectBase.ChildAddedEvent -= ObjectChildAddedHandler;
                    objectBase.ScriptRemovedEvent -= ObjectScriptRemovedHandler;
                    objectBase.ScriptAddedEvent -= ObjectScriptAddedHandler;
                }

                RemovePersistentOperationDelegates(persistent);

                return true;
            }
            return false;
        }

        private void RemovePersistentOperationDelegates(IPersistent persistent)
        {
            persistent.PersistenceSaveOperationEvent -= PersistenceSaveOperationHandler;
            persistent.PersistenceSynchronizeOperationEvent -= PersistenceSynchronizeOperationHandler;
            persistent.PersistenceDeleteOperationEvent -= PersistenceDeleteOperationHandler;
        }

        private bool AddPersistentDelegates(IPersistent persistent, bool isDisposing = false)
        {
            if (!isDisposing && !Disposable.IsDisposed(persistent))
            {
                persistent.DisposedEvent += PersistentDisposedHandler;
                persistent.PropertyAssignedEvent += PersistentPropertyAssignedHandler;
                if (persistent is Object)
                {
                    Object objectBase = persistent as Object;
                    objectBase.ComponentPropertyAssignedEvent += ObjectComponentPropertyAssignedHandler;
                    objectBase.ChildRemovedEvent += ObjectChildRemovedHandler;
                    objectBase.ChildAddedEvent += ObjectChildAddedHandler;
                    objectBase.ScriptRemovedEvent += ObjectScriptRemovedHandler;
                    objectBase.ScriptAddedEvent += ObjectScriptAddedHandler;
                }

                AddPersistentOperationDelegates(persistent);

                return true;
            }
            return false;
        }

        private void AddPersistentOperationDelegates(IPersistent persistent)
        {
            if (supportsSave)
                persistent.PersistenceSaveOperationEvent += PersistenceSaveOperationHandler;
            if (supportsSynchronize)
                persistent.PersistenceSynchronizeOperationEvent += PersistenceSynchronizeOperationHandler;
            if (supportsDelete)
                persistent.PersistenceDeleteOperationEvent += PersistenceDeleteOperationHandler;
        }

        private void PersistentDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemovePersistent(disposable as IPersistent, disposeContext);
        }

        private void PersistentPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            IPersistent persistent = property as IPersistent;

            if (name == nameof(PersistentMonoBehaviour.autoDispose))
                UpdateCanBeAutoDisposed(persistent);

            if (SceneManager.IsUserChangeContext() && persistent.GetJsonAttribute(name, out JsonAttribute _, out PropertyInfo propertyInfo) && SetPersistentComponentPropertyOutOfSync(persistent, persistent, propertyInfo))
                SetPersistentIsOutOfSynch(persistent, true);

            if (property is IGrid2DIndex)
            {
                bool dimensionsChanged = name == nameof(IGrid2DIndex.grid2DDimensions);
                bool indexChanged = name == nameof(IGrid2DIndex.grid2DIndex);
                if (dimensionsChanged || indexChanged)
                {
                    IGrid2DIndex gridIndexObject = property as IGrid2DIndex;

                    IterateOverLoaders((loader) =>
                    {
                        if (loader is Index2DLoaderBase && loader.Contains(persistent))
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

                            ChangeLoadScope(persistent, index2DLoader.GetLoadScope(out Index2DLoadScope newLoadScope, newDimensions, newIndex) ? newLoadScope : null, index2DLoader.GetLoadScope(out Index2DLoadScope oldLoadScope, oldDimensions, oldIndex) ? oldLoadScope : null);
                        }
                        return true;
                    });
                }
            }
        }

        private void ChangeLoadScope(IPersistent persistent, LoadScope newLoadScope, LoadScope oldLoadScope)
        {
            if (newLoadScope != oldLoadScope)
            {
                if (oldLoadScope != Disposable.NULL)
                    oldLoadScope.RemovePersistent(persistent);
                if (newLoadScope != Disposable.NULL)
                    newLoadScope.AddPersistent(persistent);

                UpdateCanBeAutoDisposed(persistent);
            }
        }

        private void ObjectComponentPropertyAssignedHandler(Object objectBase, IJson component, string name, object newValue, object oldValue)
        {
            if (SceneManager.IsUserChangeContext())
            {
                if (component.GetJsonAttribute(name, out JsonAttribute _, out PropertyInfo propertyInfo))
                    SetPersistentComponentPropertyOutOfSync(objectBase, component, propertyInfo);
            }
        }

        private void ObjectChildRemovedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
                UpdateCanBeAutoDisposed(objectBase);
        }

        private void ObjectChildAddedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
                UpdateCanBeAutoDisposed(objectBase);
        }

        private void ObjectScriptRemovedHandler(Object objectBase, Script script)
        {
            if (SceneManager.IsUserChangeContext())
                SetPersistentAllSync(objectBase, script);
        }

        private void ObjectScriptAddedHandler(Object objectBase, Script script)
        {
            if (SceneManager.IsUserChangeContext())
                SetPersistentAllOutOfSync(objectBase, script);
        }

        private void PersistenceSaveOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Save, persistent, callback);
        }

        private void PersistenceSynchronizeOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Synchronize, persistent, callback);
        }

        private void PersistenceDeleteOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Delete, persistent, callback);
        }

#if UNITY_EDITOR
        public void UndoRedoPerformed()
        {
            Editor.SerializationUtility.FixBrokenPersistentsDictionary(IterateOverPersistents, persistentsDictionary);
        }
#endif

        public static bool EnablePersistenceOperations()
        {
#if UNITY_EDITOR
            return !Application.isPlaying;
#else
            return true;
#endif
        }

        public bool GetPersistent(SerializableGuid id, out IPersistent persistent)
        {
            if (persistentsDictionary.TryGetValue(id, out SerializableIPersistent serializableIPersistent) && !Disposable.IsDisposed(serializableIPersistent.persistent))
            {
                persistent = serializableIPersistent.persistent;
                return true;
            }

            persistent = null;
            return false;
        }

        private JsonMonoBehaviour datasourceWrapper
        {
            get => _datasourceWrapper;
            set => _datasourceWrapper = value;
        }

        public bool supportsSave
        {
            get { return _supportsSave; }
            set
            {
                if (_supportsSave == value)
                    return;

                IterateOverPersistents((persistentId, persistent) => { RemovePersistentOperationDelegates(persistent); return true; });

                _supportsSave = value;

                IterateOverPersistents((persistentId, persistent) => { AddPersistentOperationDelegates(persistent); return true; });
            }
        }

        public bool supportsSynchronize
        {
            get { return _supportsSynchronize; }
            set
            {
                if (_supportsSynchronize == value)
                    return;

                IterateOverPersistents((persistentId, persistent) => { RemovePersistentOperationDelegates(persistent); return true; });

                _supportsSynchronize = value;

                IterateOverPersistents((persistentId, persistent) => { AddPersistentOperationDelegates(persistent); return true; });
            }
        }

        public bool supportsDelete
        {
            get { return _supportsDelete; }
            set
            {
                if (_supportsDelete == value)
                    return;

                IterateOverPersistents((persistentId, persistent) => { RemovePersistentOperationDelegates(persistent); return true; });

                _supportsDelete = value;

                IterateOverPersistents((persistentId, persistent) => { AddPersistentOperationDelegates(persistent); return true; });
            }
        }

        public int persistentCount
        {
            get { return persistentsDictionary.Count; }
        }

        private PersistentDictionary persistentsDictionary
        {
            get => _persistentsDictionary ??= new PersistentDictionary();
        }

        private PersistentsComponentsOutOfSyncDictionary persistentsComponentsOutOfSyncDictionary
        {
            get => _persistentComponentOutOfSyncDictionary ??= new PersistentsComponentsOutOfSyncDictionary();
        }

        private bool GetPersistentComponentOutOfSyncDictionary(IPersistent persistent, out ComponentsOutOfSyncDictionary componentOutOfSynchDictionary)
        {
            return persistentsComponentsOutOfSyncDictionary.TryGetValue(persistent.id, out componentOutOfSynchDictionary);
        }

        private SerializableGuidHashSet persistentIsOutOfSync
        {
            get => _persistentIsOutOfSync ??= new SerializableGuidHashSet();
        }

        private bool SetPersistentIsOutOfSynch(IPersistent persistent, bool outOfSynch)
        {
            if (outOfSynch ? persistentIsOutOfSync.Add(persistent.id) : persistentIsOutOfSync.Remove(persistent.id))
            {
                UpdateCanBeAutoDisposed(persistent);
                return true;
            }
            return false;
        }

        private SerializableGuidHashSet persistentCanBeAutoDisposed
        {        
            get => _persistentCanBeAutoDisposed ??= new SerializableGuidHashSet();
        }

        private bool GetPersistentCanBeAutoDisposed(IPersistent persistent)
        {
            return persistentCanBeAutoDisposed.Contains(persistent.id);
        }

        private bool SetPersistentCanBeAutoDisposed(IPersistent persistent, bool outOfSynch, bool autoDispose = true)
        {
            if (outOfSynch ? persistentCanBeAutoDisposed.Add(persistent.id) : persistentCanBeAutoDisposed.Remove(persistent.id))
            {
                if (autoDispose)
                    AutoDisposePersistent(persistent);

                if (persistent is Object)
                    UpdateCanBeAutoDisposed((persistent as Object).transform.objectBase);
                
                return true;
            }
            return false;
        }

        private void UpdateCanBeAutoDisposed(IPersistent persistent, bool autoDispose = true)
        {
            if (Disposable.IsDisposed(persistent))
                return;

            bool canBeDisposed = persistent.autoDispose && (persistent is Interior || !IsPersistentOutOfSync(persistent, true));

            if (canBeDisposed && persistent is Object)
            {
                Object objectBase = persistent as Object;
                objectBase.transform.IterateOverChildrenObject<Object>((objectBase) =>
                {
                    if (!GetPersistentCanBeAutoDisposed(persistent))
                    {
                        canBeDisposed = false;
                        return false;
                    }
                    return true;
                });
            }

            SetPersistentCanBeAutoDisposed(persistent, canBeDisposed, autoDispose);
        }

        public bool IsPersistentOutOfSync(IPersistent persistent, bool autoDispose = false)
        {
            bool isOutOfSync = false;

            IteratePersistentComponentOverOutOfSync(persistent , (iJson, key, preventAutoDispse) =>
            {
                if (!autoDispose || !preventAutoDispse)
                {
                    isOutOfSync = true;
                    return false;
                }
                return true;
            });

            return isOutOfSync;
        }

        public bool IsPersistentComponentPropertyOutOfSync(IPersistent persistent, IJson component, string name)
        {
            if (GetPersistentComponentOutOfSyncDictionary(persistent, out ComponentsOutOfSyncDictionary componentsOutOfSynchDictionary))
            {
                if (componentsOutOfSynchDictionary.TryGetValue(component.id, out ComponentOutOfSyncKeysDictionary componentOutOfSyncKeys))
                    return componentOutOfSyncKeys.ContainsKey(PropertyMonoBehaviour.GetPropertyKey(name));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPersistentAllSync(IPersistent persistent, IJson component = null)
        {
            if (GetPersistentComponentOutOfSyncDictionary(persistent, out ComponentsOutOfSyncDictionary componentsOutOfSynchDictionary))
            {
                if (component is null)
                {
                    componentsOutOfSynchDictionary.Clear();

                    UpdateIsOutOfSync(persistent);
                }
                else if (componentsOutOfSynchDictionary.TryGetValue(component.id, out ComponentOutOfSyncKeysDictionary componentOutOfSyncKey))
                    componentOutOfSyncKey.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPersistentAllOutOfSync(IPersistent persistent, IJson component = null, bool allowAutoDispose = false)
        {
            bool outOfSynchChanged = false;

            MemberUtility.IterateOverJsonAttribute(component, (component, accessor, name, jsonAttribute, propertyInfo) =>
            {
                if (SetPersistentComponentPropertyOutOfSync(persistent, component, propertyInfo, allowAutoDispose))
                    outOfSynchChanged = true;
            });

            if (outOfSynchChanged)
                UpdateIsOutOfSync(persistent);
        }

        private void UpdateIsOutOfSync(IPersistent persistent)
        {
            SetPersistentIsOutOfSynch(persistent, IsPersistentOutOfSync(persistent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetPersistentComponentPropertyOutOfSync(IPersistent persistent, IJson component, PropertyInfo propertyInfo, bool allowAutoDispose = false)
        {
            int key = PropertyMonoBehaviour.GetPropertyKey(propertyInfo.Name);
            if (!component.IsDynamicProperty(key) && GetPersistentComponentOutOfSyncDictionary(persistent, out ComponentsOutOfSyncDictionary componentsOutOfSynchDictionary))
            {
                SerializableGuid id = component.id;
                if (!componentsOutOfSynchDictionary.TryGetValue(id, out ComponentOutOfSyncKeysDictionary componentOutOfSyncKeys))
                {
                    componentOutOfSyncKeys = new ComponentOutOfSyncKeysDictionary();
                    componentsOutOfSynchDictionary.Add(id, componentOutOfSyncKeys);
                }

                if (!componentOutOfSyncKeys.ContainsKey(key))
                {
                    componentOutOfSyncKeys.Add(key, allowAutoDispose);
                    return true;
                }
                else
                    componentOutOfSyncKeys[key] = allowAutoDispose;
            }
            return false;
        }

        private void IteratePersistentComponentOverOutOfSync(IPersistent persistent, Func<IJson, int, bool, bool> callback)
        {
            if (GetPersistentComponentOutOfSyncDictionary(persistent, out ComponentsOutOfSyncDictionary componentOutOfSynchDictionary))
            {
                foreach (SerializableGuid id in componentOutOfSynchDictionary.Keys)
                {
                    ComponentOutOfSyncKeysDictionary outOfSyncKeys = componentOutOfSynchDictionary[id];
                    IJson iJson = InstanceManager.Instance().GetIJson(id);
                    if (iJson != null)
                    {
                        foreach (int key in outOfSyncKeys.Keys)
                        {
                            if (!callback(iJson, key, outOfSyncKeys[key]))
                                return;
                        }
                    }
                }
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

        private void AddPersistent(IPersistent persistent, bool allOutOfSync = false)
        {
            SerializableGuid persistentId = persistent.id;
            if (!persistentsDictionary.TryGetValue(persistentId, out SerializableIPersistent serializableIPersistent))
            {
                serializableIPersistent = new SerializableIPersistent(persistent);
                persistentsDictionary.Add(persistentId, serializableIPersistent);
                persistentsComponentsOutOfSyncDictionary.Add(persistentId, new ComponentsOutOfSyncDictionary());

                UpdateCanBeAutoDisposed(persistent, false);

                AddPersistentDelegates(persistent);

                if (allOutOfSync)
                    SetPersistentAllOutOfSync(persistent, persistent);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(datasourceWrapper);
#endif
            }
        }

        private void RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            if (disposeContext == DisposeContext.Editor_Destroy)
                Editor.UndoManager.RegisterCompleteObjectUndo(datasourceWrapper);
#endif

            SerializableGuid persistentId = persistent.id;
            if (persistentsDictionary.Remove(persistentId))
            {
                persistentsComponentsOutOfSyncDictionary.Remove(persistentId);
                persistentIsOutOfSync.Remove(persistentId);
                persistentCanBeAutoDisposed.Remove(persistentId);

                RemovePersistentDelegates(persistent);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(datasourceWrapper);
#endif
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
            IterateOverPersistents((persistentId, persistent) =>
            {
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
                    AutoDisposePersistent(persistent);

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
                                if (!Disposable.IsDisposed(persistent) && !GetPersistent(persistent.id, out persistent))
                                    AddPersistent(persistent);
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
                if(!Disposable.IsDisposed(persistent) && GetPersistent(persistent.id, out persistent))
                    AutoDisposePersistent(persistent);
            });

            ReloadAll();

            return successCount;
        }

        public void Delete(DatasourceOperationBase datasourceOperation, Action<int> resultCallback)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((Action<bool, OperationResult>)((success, operationResult) =>
                    {
                        int successCount = 0;

                        if (success)
                        {
                            successCount = operationResult.IterateOverResultsData<IdResultData>((idResultData, persistent) =>
                            {
                                if (!Disposable.IsDisposed(persistent) && GetPersistent(persistent.id, out persistent))
                                    RemovePersistent(persistent);
                            });
                        }
                        resultCallback?.Invoke(successCount);
                    }));
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

            FallbackValues persistentFallbackValues = InstanceManager.Instance().GetFallbackValues(loadResultData.persistentFallbackValuesId);

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
            IPersistent persistent;

            if (json != null && json[nameof(IPersistent.id)] != null)
            {
                if (SerializableGuid.TryParse(json[nameof(IPersistent.id)], out SerializableGuid id))
                {
                    if (GetPersistent(id, out persistent))
                        return persistent;
                }
            }

            bool isNewPersitent = loader.GeneratePersistent(out persistent, type, json, propertyModifiers);

            if (!Disposable.IsDisposed(persistent))
                AddPersistent(persistent, !isNewPersitent);

            return persistent;
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
                    if (GetPersistent(persistent.id, out persistent))
                        AutoDisposePersistent(persistent, disposeContext);

                    return true;
                });
            }
        }

        private bool AutoDisposePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (!Disposable.IsDisposed(persistent))
            {
                bool dispose = GetPersistentCanBeAutoDisposed(persistent);

                if (dispose)
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
                    DisposeManager.Dispose(persistent is PersistentMonoBehaviour ? (persistent as PersistentMonoBehaviour).gameObject : persistent, disposeContext);

                    return true;
                }
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
