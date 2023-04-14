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
    public class PersistentsDictionary : SerializableDictionary<SerializableGuid, SerializableIPersistent> { };

    [Serializable]
    public class Datasource : IPersistentList
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
        private class PersistentsComponentsOutOfSyncDictionary : SerializableDictionary<SerializableGuid, ComponentsOutOfSyncDictionary> { };
        [Serializable]
        private class ComponentsOutOfSyncDictionary : SerializableDictionary<SerializableGuid, ComponentOutOfSyncKeysDictionary> { };
        [Serializable]
        private class ComponentOutOfSyncKeysDictionary : SerializableDictionary<int, bool> { };

        [Serializable]
        private class SerializableGuidHashSet : SerializableHashSet<SerializableGuid> { };

        [Serializable]
        private class LoadersDictionary : SerializableDictionary<SerializableGuid, LoaderBase> { };

        [SerializeField, HideInInspector]
        private PersistentsDictionary _persistentsDictionary;
        [SerializeField, HideInInspector]
        private PersistentsComponentsOutOfSyncDictionary _persistentsComponentsOutOfSyncDictionary;
        [SerializeField, HideInInspector]
        private SerializableGuidHashSet _persistentsIsOutOfSync;
        [SerializeField, HideInInspector]
        private SerializableGuidHashSet _persistentsCanBeAutoDisposed;

        [SerializeField, HideInInspector]
        private bool _supportsSave;
        [SerializeField, HideInInspector]
        private bool _supportsSynchronize;
        [SerializeField, HideInInspector]
        private bool _supportsDelete;

        [SerializeField, HideInInspector]
        private JsonMonoBehaviour _datasourceWrapper;

        private LoadersDictionary _loaders;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.IPersistent.PersistenceSaveOperationEvent"/>, <see cref="DepictionEngine.IPersistent.PersistenceSynchronizeOperationEvent"/> or <see cref="DepictionEngine.IPersistent.PersistenceDeleteOperationEvent"/> is dispatched by the encapsulated <see cref="DepictionEngine.IPersistent"/>.
        /// </summary>
        public Action<OperationType, IPersistent, Action> PersistenceOperationEvent;

        public void Recycle()
        {
            _persistentsDictionary?.Clear();

            _persistentsComponentsOutOfSyncDictionary?.Clear();
            _persistentsIsOutOfSync.Clear();
            _persistentsCanBeAutoDisposed.Clear();

            _supportsSave = default;
            _supportsSynchronize = default;
            _supportsDelete = default;

            _loaders?.Clear();
        }

        public Datasource Initialize(JsonMonoBehaviour datasourceWrapper, InitializationContext initializingContext)
        {
            this.datasourceWrapper = datasourceWrapper;

            //When undoing a Destroy the loaders might not be initialized yet therefore we cannot find them in the instanceManager yet, so we use UnityEngine.Object.FindObjectsOfType instead.
            UnityEngine.Object[] loaders = UnityEngine.Object.FindObjectsOfType(typeof(LoaderBase));
            foreach (LoaderBase loader in loaders)
            {
                if ((this.datasourceWrapper as IDatasource).IsIdMatching(loader.datasourceId))
                    AddLoader(loader);
            }

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                persistentsDictionary.Clear();
                persistentsComponentsOutOfSyncDictionary.Clear();
                persistentsIsOutOfSync.Clear();
                persistentsCanBeAutoDisposed.Clear();
            }

            if (initializingContext == InitializationContext.Existing)
            {
                PerformAddRemovePersistentsChange(persistentsDictionary, persistentsDictionary);

                InitializeAutoDisposePersistents();
            }

            return this;
        }

        private void InitializeAutoDisposePersistents()
        {
            IterateOverPersistents((persistentId, persistent) =>
            {
                AutoDisposePersistent(persistent);
                return true;
            });
        }

        private List<(bool, SerializableGuid, IPersistent, ComponentsOutOfSyncDictionary)> PerformAddRemovePersistentsChange(PersistentsDictionary persistentsDictionary, PersistentsDictionary lastPersistentsDictionary)
        {
#if UNITY_EDITOR
            SerializationUtility.RecoverLostReferencedObjectsInCollection(persistentsDictionary);
#endif
            List<(bool, SerializableGuid, IPersistent, ComponentsOutOfSyncDictionary)> changedPersistents = null;

            SerializationUtility.FindAddedRemovedObjectsChange(persistentsDictionary, lastPersistentsDictionary,
            (persistentId) =>
            {
                changedPersistents ??= new();
                changedPersistents.Add((false, persistentId, null, null));
            },
            (persistentId, serializableIPersistent) =>
            {
                changedPersistents ??= new();
                changedPersistents.Add((true, persistentId, serializableIPersistent.persistent, persistentsComponentsOutOfSyncDictionary[persistentId]));
            });

#if UNITY_EDITOR
            if (!Object.ReferenceEquals(persistentsDictionary, lastPersistentsDictionary))
            {
                persistentsComponentsOutOfSyncDictionary.Clear();
                persistentsComponentsOutOfSyncDictionary.CopyFrom(lastPersistentsComponentsOutOfSyncDictionary);

                persistentsIsOutOfSync.Clear();
                persistentsIsOutOfSync.CopyFrom(lastPersistentsIsOutOfSync);

                persistentsCanBeAutoDisposed.Clear();
                persistentsCanBeAutoDisposed.CopyFrom(lastPersistentsCanBeAutoDisposed);
            }
#endif
            if (changedPersistents != null)
            {
                foreach ((bool, SerializableGuid, IPersistent, ComponentsOutOfSyncDictionary) changedPersistent in changedPersistents)
                {
                    bool success = changedPersistent.Item1 ? AddPersistent(changedPersistent.Item3, false, changedPersistent.Item4) : RemovePersistent(changedPersistent.Item2, DisposeContext.Programmatically_Destroy);
                }
            }

            return changedPersistents;
        }

        public void InitializeLastFields()
        {
#if UNITY_EDITOR
            lastPersistentsDictionary.Clear();
            lastPersistentsDictionary.CopyFrom(persistentsDictionary);

            lastPersistentsComponentsOutOfSyncDictionary.Clear();
            lastPersistentsComponentsOutOfSyncDictionary.CopyFrom(persistentsComponentsOutOfSyncDictionary);

            lastPersistentsIsOutOfSync.Clear();
            lastPersistentsIsOutOfSync.CopyFrom(persistentsIsOutOfSync);

            lastPersistentsCanBeAutoDisposed.Clear();
            lastPersistentsCanBeAutoDisposed.CopyFrom(persistentsCanBeAutoDisposed);
#endif
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

            UpdatePersistentsDelegates(isDisposing);

            return true;
        }

        private void UpdatePersistentsDelegates(bool isDisposing = false)
        {
            IterateOverPersistents((persistentId, persistent) =>
            {
                RemovePersistentDelegates(persistent);
                AddPersistentDelegates(persistent, isDisposing);

                return true;
            });
        }

        private void DatasourceLoadersChangedHandler(LoaderBase loader)
        {
            if (loader != Disposable.NULL && (datasourceWrapper as IDatasource).IsIdMatching(loader.datasourceId))
                AddLoader(loader);
            else
                RemoveLoader(loader);
        }

        private void RemoveLoaderDelegates(LoaderBase loader)
        {
            if (loader is not null)
            {
                loader.LoadScopeDisposedEvent -= LoaderLoadScopeDisposedHandler;
                loader.LoadScopeLoadingStateChangedEvent -= LoaderLoadScopeLoadingStateChangedHandler;
                loader.PropertyAssignedEvent -= LoaderPropertyAssignedHandler;
            }
        }

        private void AddLoaderDelegates(LoaderBase loader, bool isDisposing = false)
        {
            if (!isDisposing && loader != Disposable.NULL)
            {
                loader.LoadScopeDisposedEvent += LoaderLoadScopeDisposedHandler;
                loader.LoadScopeLoadingStateChangedEvent += LoaderLoadScopeLoadingStateChangedHandler;
                loader.PropertyAssignedEvent += LoaderPropertyAssignedHandler;
            }
        }

        private void LoaderLoadScopeDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            AutoDisposeLoadScopePersistents(disposable as LoadScope, disposeContext);
        }

        private void LoaderLoadScopeLoadingStateChangedHandler(LoadScope loadScope)
        {
            if (_reloadingLoadScopes != null && _reloadingLoadScopes.Remove(loadScope) && _reloadingLoadScopes.Count == 0)
                ReloadCompleted();
        }

        private void LoaderPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(LoaderBase.datasource))
                LoaderChanged();
        }

        private void LoaderChanged()
        {
            DisposeContext disposeContext = SceneManager.IsUserChangeContext() ? DisposeContext.Editor_Destroy : DisposeContext.Programmatically_Pool;
            IterateOverPersistents((persistentId, persistent) =>
            {
                AutoDisposePersistent(persistent, disposeContext);
                return true;
            });
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

            SetComponentPropertyOutOfSynch(persistent, persistent, name);

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

                            ChangeLoadScope(persistent, index2DLoader.GetLoadScope(out LoadScope newLoadScope, new Grid2DIndex(newIndex, newDimensions)) ? newLoadScope : null, index2DLoader.GetLoadScope(out LoadScope oldLoadScope, new Grid2DIndex(oldIndex, oldDimensions)) ? oldLoadScope : null);
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
            IPersistent persistent = objectBase as IPersistent;
            SetComponentPropertyOutOfSynch(persistent, component, name);
        }

        private void SetComponentPropertyOutOfSynch(IPersistent persistent, IJson component, string propertyName)
        {
            if (SceneManager.IsUserChangeContext() && component.GetJsonAttribute(propertyName, out JsonAttribute _, out PropertyInfo propertyInfo) && SetPersistentComponentPropertyOutOfSync(persistent, component, propertyInfo, allowAutoDispose))
                SetPersistentIsOutOfSynch(persistent, true);
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
        private PersistentsDictionary _lastPersistentsDictionary;
        private PersistentsDictionary lastPersistentsDictionary
        {
            get { _lastPersistentsDictionary ??= new(); return _lastPersistentsDictionary; }
        }

        private PersistentsComponentsOutOfSyncDictionary _lastPersistentsComponentsOutOfSyncDictionary;
        private PersistentsComponentsOutOfSyncDictionary lastPersistentsComponentsOutOfSyncDictionary
        {
            get { _lastPersistentsComponentsOutOfSyncDictionary ??= new(); return _lastPersistentsComponentsOutOfSyncDictionary; }
        }

        private SerializableGuidHashSet _lastPersistentsIsOutOfSync;
        private SerializableGuidHashSet lastPersistentsIsOutOfSync
        {
            get { _lastPersistentsIsOutOfSync ??= new(); return _lastPersistentsIsOutOfSync; }
        }

        private SerializableGuidHashSet _lastPersistentsCanBeAutoDisposed;
        private SerializableGuidHashSet lastPersistentsCanBeAutoDisposed
        {
            get { _lastPersistentsCanBeAutoDisposed ??= new(); return _lastPersistentsCanBeAutoDisposed; }
        }

        public void UndoRedoPerformed()
        {
            PerformAddRemovePersistentsChange(persistentsDictionary, lastPersistentsDictionary);
        }
#endif

        private PersistentsDictionary persistentsDictionary
        {
            get { _persistentsDictionary ??= new(); return _persistentsDictionary; }
        }

        private static bool _allowAutoDispose;
        public static bool allowAutoDispose { get => _allowAutoDispose; }
        public static void AllowAutoDisposeOnOutOfSynchProperty(Action callback, bool allowAutoDispose = true)
        {
            if (callback != null)
            {
                bool lastAllowAutoDispose = _allowAutoDispose;
                _allowAutoDispose = lastAllowAutoDispose || allowAutoDispose;
                callback();
                _allowAutoDispose = lastAllowAutoDispose;
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

        private PersistentsComponentsOutOfSyncDictionary persistentsComponentsOutOfSyncDictionary
        {
            get { _persistentsComponentsOutOfSyncDictionary ??= new PersistentsComponentsOutOfSyncDictionary(); return _persistentsComponentsOutOfSyncDictionary; }
        }

        private bool GetPersistentComponentOutOfSyncDictionary(IPersistent persistent, out ComponentsOutOfSyncDictionary componentOutOfSynchDictionary)
        {
            return persistentsComponentsOutOfSyncDictionary.TryGetValue(persistent.id, out componentOutOfSynchDictionary);
        }

        private SerializableGuidHashSet persistentsIsOutOfSync
        {
            get { _persistentsIsOutOfSync ??= new SerializableGuidHashSet(); return _persistentsIsOutOfSync; }
        }

        private bool SetPersistentIsOutOfSynch(IPersistent persistent, bool outOfSynch, bool autoDispose = true)
        {
            if (outOfSynch ? persistentsIsOutOfSync.Add(persistent.id) : persistentsIsOutOfSync.Remove(persistent.id))
            {
#if UNITY_EDITOR
                if (outOfSynch)
                    lastPersistentsIsOutOfSync.Add(persistent.id);
                else
                    lastPersistentsIsOutOfSync.Remove(persistent.id);
#endif
                UpdateCanBeAutoDisposed(persistent, autoDispose);
                return true;
            }
            return false;
        }

        private SerializableGuidHashSet persistentsCanBeAutoDisposed
        {
            get { _persistentsCanBeAutoDisposed ??= new SerializableGuidHashSet(); return _persistentsCanBeAutoDisposed; }
        }

        private bool GetPersistentCanBeAutoDisposed(IPersistent persistent)
        {
            return persistentsCanBeAutoDisposed.Contains(persistent.id);
        }

        private bool SetPersistentCanBeAutoDisposed(IPersistent persistent, bool canBeDisposed, bool autoDispose = true)
        {
            if (canBeDisposed ? persistentsCanBeAutoDisposed.Add(persistent.id) : persistentsCanBeAutoDisposed.Remove(persistent.id))
            {
#if UNITY_EDITOR
                if (canBeDisposed)
                    lastPersistentsCanBeAutoDisposed.Add(persistent.id);
                else
                    lastPersistentsCanBeAutoDisposed.Remove(persistent.id);
#endif
                if (autoDispose)
                    AutoDisposePersistent(persistent);

                if (persistent is Object)
                {
                    //If the parent Object also comes from this datasource, update its 'canBeAutoDisposed' value
                    Object parentObject = (persistent as Object).GetParentObject();
                    if (parentObject != Disposable.NULL && GetPersistent(parentObject.id, out IPersistent parentPersistent))
                        UpdateCanBeAutoDisposed(parentPersistent);
                }

                return true;
            }
            return false;
        }

        private bool UpdateCanBeAutoDisposed(IPersistent persistent, bool autoDispose = true)
        {
            if (Disposable.IsDisposed(persistent))
                return false;

            bool canBeDisposed = persistent.autoDispose && (persistent is Interior || !IsPersistentOutOfSync(persistent, true));

            if (canBeDisposed)
            {
                Object objectBase = persistent as Object;
                if (objectBase != Disposable.NULL)
                {
                    objectBase.IterateOverChildrenObject((childObject) => 
                    { 
                        if (childObject != Disposable.NULL && !GetPersistentCanBeAutoDisposed(childObject))
                        {
                            canBeDisposed = false;
                            return false;
                        }
                        return true; 
                    });
                }
            }

            return SetPersistentCanBeAutoDisposed(persistent, canBeDisposed, autoDispose);
        }

        public bool IsPersistentOutOfSync(IPersistent persistent, bool autoDispose = false)
        {
            bool isOutOfSync = false;

            IteratePersistentComponentOverOutOfSync(persistent , (iJson, key, allowAutoDispose) =>
            {
                if (!autoDispose || !allowAutoDispose)
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
#if UNITY_EDITOR
                if (SceneManager.IsUserChangeContext())
                    RegisterCompleteObjectUndo();
#endif
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
        public bool SetPersistentAllOutOfSync(IPersistent persistent, IJson component = null, bool allowAutoDispose = false, bool autoDispose = true)
        {
            bool outOfSynchChanged = false;

            MemberUtility.IterateOverJsonAttribute(component, (component, accessor, name, jsonAttribute, propertyInfo) =>
            {
                if (SetPersistentComponentPropertyOutOfSync(persistent, component, propertyInfo, allowAutoDispose))
                    outOfSynchChanged = true;
            });

            if (outOfSynchChanged)
                return UpdateIsOutOfSync(persistent, autoDispose);
            return false;
        }

        private bool UpdateIsOutOfSync(IPersistent persistent, bool autoDispose = true)
        {
            return SetPersistentIsOutOfSynch(persistent, IsPersistentOutOfSync(persistent), autoDispose);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetPersistentComponentPropertyOutOfSync(IPersistent persistent, IJson component, PropertyInfo propertyInfo, bool allowAutoDispose = false)
        {
            bool changed = false;

            int key = PropertyMonoBehaviour.GetPropertyKey(propertyInfo.Name);
            if (GetPersistentComponentOutOfSyncDictionary(persistent, out ComponentsOutOfSyncDictionary componentsOutOfSynchDictionary))
            {
#if UNITY_EDITOR
                if (SceneManager.IsUserChangeContext())
                    RegisterCompleteObjectUndo();
#endif
                SerializableGuid id = component.id;
                if (!componentsOutOfSynchDictionary.TryGetValue(id, out ComponentOutOfSyncKeysDictionary componentOutOfSyncKeys))
                {
                    componentOutOfSyncKeys = new ComponentOutOfSyncKeysDictionary();
                    componentsOutOfSynchDictionary.Add(id, componentOutOfSyncKeys);
                }

                if (!componentOutOfSyncKeys.ContainsKey(key))
                {
                    componentOutOfSyncKeys.Add(key, allowAutoDispose);
                    changed = true;
                }
                else
                {
                    if (componentOutOfSyncKeys[key] && !allowAutoDispose)
                    {
                        componentOutOfSyncKeys[key] = allowAutoDispose;
                        changed = true;
                    }
                }
            }

            if (changed)
                Debug.Log(key+", "+ allowAutoDispose);

            return changed;
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
                    if (!callback(_loaders.ElementAt(i).Value))
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

        public bool AddPersistent(IPersistent persistent)
        {
            return AddPersistent(persistent, false);
        }

        private bool AddPersistent(IPersistent persistent, bool allOutOfSync = false, ComponentsOutOfSyncDictionary outOfSynch = null)
        {
            SerializableGuid persistentId = persistent.id;
            SerializableIPersistent serializableIPersistent = new SerializableIPersistent(persistent);
            if (persistentsDictionary.TryAdd(persistentId, serializableIPersistent))
            {
                outOfSynch ??= new();
                persistentsComponentsOutOfSyncDictionary.Add(persistentId, outOfSynch);

                if (!(allOutOfSync ? SetPersistentAllOutOfSync(persistent, persistent, false, false) : UpdateIsOutOfSync(persistent, false)))
                    UpdateCanBeAutoDisposed(persistent, false);

                AddPersistentDelegates(persistent);

#if UNITY_EDITOR
                lastPersistentsDictionary.Add(persistentId, serializableIPersistent);
                lastPersistentsComponentsOutOfSyncDictionary.Add(persistentId, outOfSynch);
                UnityEditor.EditorUtility.SetDirty(datasourceWrapper);
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
                persistentsComponentsOutOfSyncDictionary.Remove(persistentId);
                persistentsIsOutOfSync.Remove(persistentId);
                persistentsCanBeAutoDisposed.Remove(persistentId);

#if UNITY_EDITOR
                lastPersistentsDictionary.Remove(persistentId);
                lastPersistentsComponentsOutOfSyncDictionary.Remove(persistentId);
                lastPersistentsIsOutOfSync.Remove(persistentId);
                lastPersistentsCanBeAutoDisposed.Remove(persistentId);

                if (datasourceWrapper != Disposable.NULL)
                    UnityEditor.EditorUtility.SetDirty(datasourceWrapper);
#endif
                return true;
            }

            return false;
        }

        private LoadersDictionary loaders
        {
            get { _loaders ??= new LoadersDictionary(); return _loaders; }
        }

        private bool RemoveLoader(LoaderBase loader)
        {
            if (loaders.Remove(loader.id))
            {
                RemoveLoaderDelegates(loader);

                LoaderChanged();

                return true;
            }
            return false;
        }

        private bool AddLoader(LoaderBase loader)
        {
            if (loaders.TryAdd(loader.id, loader))
            {
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

        public void Load(DatasourceOperationBase datasourceOperation, Action<List<IPersistent>, DatasourceOperationBase.LoadingState> resultCallback, LoadScope loadScope)
        {
            if (datasourceOperation != Disposable.NULL)
            {
                datasourceOperation.Execute((success, operationResult) =>
                    {
                        List<IPersistent> persistents = null;

                        if (success)
                            persistents = CreatePersistents(loadScope.loader, operationResult);

                        resultCallback?.Invoke(persistents, datasourceOperation.loadingState);
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
                datasourceOperation.Execute((success, operationResult) =>
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
                loadScope.IterateOverPersistents((persistentId, persistent) =>
                {
                    if (GetPersistent(persistentId, out persistent))
                        AutoDisposePersistent(persistent, disposeContext);

                    return true;
                });
            }
        }

        private bool AutoDisposePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            return !Disposable.IsDisposed(persistent) && DisposePersistent(persistent, disposeContext , () => 
            {
                bool dispose = true;

                IterateOverLoaders((loader) =>
                {
                    if (loader != Disposable.NULL && (datasourceWrapper as IDatasource).IsIdMatching(loader.datasourceId) && loader.Contains(persistent) && loader.GetLoadScope(out LoadScope loadScope, persistent) && loadScope != Disposable.NULL)
                    {
                        dispose = false;

                        return false;
                    }
                    return true;
                });

                return dispose;
            });
        }

        private bool DisposePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool, Func<bool> validateCallback = null)
        {
            if (GetPersistentCanBeAutoDisposed(persistent) && (validateCallback == null || validateCallback()))
            {
                //Null check is required for when the scene destroys before Play and the persistent as already been destroyed.
                if (!Disposable.IsDisposed(persistent))
                    DisposeManager.Dispose(persistent is PersistentMonoBehaviour ? (persistent as PersistentMonoBehaviour).gameObject : persistent, disposeContext);
             
                return true;
            }
            return false;
        }

        public void OnDispose(DisposeContext disposeContext)
        {
            IterateOverPersistents((persistentId, persistent) => 
            {
                DisposePersistent(persistent, disposeContext);

                return true;
            });
        }

#if UNITY_EDITOR
        private void RegisterCompleteObjectUndo(DisposeContext disposeContext = DisposeContext.Editor_Destroy)
        {
            if (datasourceWrapper != Disposable.NULL)
                datasourceWrapper.RegisterCompleteObjectUndo(disposeContext);
        }
#endif

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
