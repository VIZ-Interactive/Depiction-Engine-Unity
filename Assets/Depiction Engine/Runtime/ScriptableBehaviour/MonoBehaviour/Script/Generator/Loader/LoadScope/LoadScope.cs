// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    public class LoadScope : ScriptableObjectDisposable, IPersistentList
    {
        [SerializeField]
        private DatasourceOperationBase.LoadingState _loadingState;
        [SerializeField]
        private DatasourceOperationBase _datasourceOperation;

        [SerializeField]
        protected PersistentsDictionary _persistentsDictionary;

        [SerializeField, HideInInspector]
        private LoaderBase _loader;

        private Tween _loadIntervalTween;

        /// <summary>
        /// Dispatched when the loading state changed.
        /// </summary>
        public Action<LoadScope> LoadingStateChangedEvent;
        /// <summary>
        /// Dispatched when an <see cref="DepictionEngine.IPersistent"/> loaded from a <see cref="DepictionEngine.ILoadDatasource"/> is added to the <see cref="DepictionEngine.LoadScope"/>.
        /// </summary>
        public Action<LoadScope> PersistentAddedEvent;
        /// <summary>
        /// Dispatched when an <see cref="DepictionEngine.IPersistent"/> loaded from a <see cref="DepictionEngine.ILoadDatasource"/> is removed from the <see cref="DepictionEngine.LoadScope"/>.
        /// </summary>
        public Action<LoadScope> PersistentRemovedEvent;

        public override void Recycle()
        {
            base.Recycle();

            _loadingState = default;
            _datasourceOperation = null;

            _persistentsDictionary?.Clear();

            _loader = default;
        }

        public LoadScope Init(LoaderBase loader)
        {
            this.loader = loader;

            return this;
        }

        public List<(bool, SerializableGuid, IPersistent)> PerformAddRemovePersistents(bool compareWithLastDictionary = false)
        {
            PersistentsDictionary lastPersistentsDictionary = persistentsDictionary;
#if UNITY_EDITOR
            if (compareWithLastDictionary)
                lastPersistentsDictionary = this.lastPersistentsDictionary;
#endif
            return LoaderBase.PerformAddRemovePersistentsChange(this, persistentsDictionary, lastPersistentsDictionary);
        }

        public void InitializeLastFields()
        {
#if UNITY_EDITOR
            lastPersistentsDictionary.Clear();
            lastPersistentsDictionary.CopyFrom(persistentsDictionary);
#endif
        }

#if UNITY_EDITOR
        protected PersistentsDictionary _lastPersistentsDictionary;
        private PersistentsDictionary lastPersistentsDictionary
        {
            get => _lastPersistentsDictionary ??= new ();
        }

        public void RecoverLostReferencedObjects()
        {
            UnityEngine.Object loaderUnityObject = _loader;
            if (SerializationUtility.RecoverLostReferencedObject(ref loaderUnityObject))
                _loader = loaderUnityObject as LoaderBase;
            
            SerializationUtility.RecoverLostReferencedObjectsInCollection(persistentsDictionary);
        }
#endif

        private PersistentsDictionary persistentsDictionary
        {
            get => _persistentsDictionary ??= new ();
        }

        public IPersistent GetFirstPersistent()
        {
            IPersistent persistent = null;

            if (persistentsDictionary.Count > 0)
                persistent = persistentsDictionary.ElementAt(0).Value.persistent;

            return persistent;
        }

        public virtual bool AddPersistent(IPersistent persistent)
        {
            SerializableIPersistent serializableIPersistent = new SerializableIPersistent(persistent);
            if (persistentsDictionary.TryAdd(persistent.id, serializableIPersistent))
            {
#if UNITY_EDITOR
                lastPersistentsDictionary.Add(persistent.id, serializableIPersistent);
#endif

                PersistentAddedEvent?.Invoke(this);

                return true;
            }

            return false;
        }

        public virtual bool RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            return RemovePersistent(persistent.id, disposeContext);
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
#endif
                PersistentRemovedEvent?.Invoke(this);

                return true;
            }

            return false;
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

        public bool ContainsPersistent(SerializableGuid persistentId)
        {
            return persistentsDictionary.ContainsKey(persistentId);
        }

        public void DatasourceChanged(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
#if UNITY_EDITOR
            RegisterCompleteObjectUndo(disposeContext);
#endif

            SetDatasourceOperation(null, disposeContext);

            KillLoading();

            if (persistentsDictionary.Count > 0)
            {
                persistentsDictionary.Clear();

#if UNITY_EDITOR
                lastPersistentsDictionary.Clear();
#endif

                PersistentRemovedEvent?.Invoke(this);
            }
        }

        public bool LoadingWasCompromised()
        {
            //Problem: Loading was interrupted before finishing(Often because the scene is Played while LoadScopes are still loading)
            return initialized && (datasourceOperation == null || datasourceOperation.LoadingWasCompromised());
        }

        public void KillLoading()
        {
            if (LoadInProgress())
            {
                if (loadIntervalTween != null)
                    loadIntervalTween = null;
                if (datasourceOperation != null && datasourceOperation.loadingState == DatasourceOperationBase.LoadingState.Loading)
                    datasourceOperation = null;
                loadingState = DatasourceOperationBase.LoadingState.Interrupted;
            }
        }

        public virtual object scopeKey { get => null; }

        public LoaderBase loader
        {
            get { return _loader; }
            private set
            {
                if (Object.ReferenceEquals(value, _loader))
                    return;

                _loader = value;
            }
        }

        private Tween loadIntervalTween
        {
            get { return _loadIntervalTween; }
            set
            {
                if (Object.ReferenceEquals(_loadIntervalTween, value))
                    return;

                DisposeManager.Dispose(_loadIntervalTween);

                _loadIntervalTween = value;
            }
        }

        private DatasourceOperationBase datasourceOperation
        {
            get { return _datasourceOperation; }
            set { SetDatasourceOperation(value); }
        }

        private bool SetDatasourceOperation(DatasourceOperationBase value, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (Object.ReferenceEquals(_datasourceOperation, value))
                return false;

            DisposeManager.Dispose(_datasourceOperation, disposeContext);

            _datasourceOperation = value;

            return true;
        }

        public DatasourceOperationBase.LoadingState loadingState
        {
            get{ return _loadingState; }
            private set
            {
                if (_loadingState == value)
                    return;

                _loadingState = value;

                LoadingStateChangedEvent?.Invoke(this);
            }
        }

        public virtual object[] GetURLParams()
        {
            return null;
        }

        public JSONArray GetPersistentFallbackValuesJson()
        {
            JSONArray persistentFallbackValues = new();

            foreach (SerializableGuid persistentFallbackValuesId in loader.fallbackValuesId)
            {
                FallbackValues persistentFallbackValue = instanceManager.GetFallbackValues(persistentFallbackValuesId);

                if (persistentFallbackValue != Disposable.NULL)
                {
                    Type type = persistentFallbackValue.GetFallbackValuesType();
                    if (type != null && (typeof(PersistentMonoBehaviour).IsAssignableFrom(type) || typeof(PersistentScriptableObject).IsAssignableFrom(type)))
                    {
                        JSONObject persistentFallbackValueJson = new();

                        string idName = nameof(PropertyMonoBehaviour.id);
                        persistentFallbackValueJson[idName] = JsonUtility.ToJson(persistentFallbackValue.id);

                        string createPersistentIfMissingName = nameof(PersistentMonoBehaviour.createPersistentIfMissing);
                        persistentFallbackValueJson[createPersistentIfMissingName] = persistentFallbackValue.fallbackValuesJson[createPersistentIfMissingName];

                        string typeName = nameof(PropertyMonoBehaviour.type);
                        persistentFallbackValueJson[typeName] = persistentFallbackValue.fallbackValuesJson[typeName];

                        persistentFallbackValues.Add(persistentFallbackValueJson);
                    }
                }
            }

            return persistentFallbackValues;
        }

        public virtual JSONObject GetLoadScopeFallbackValuesJson()
        {
            JSONObject loadScopeFallbackValuesJson = new();

            loadScopeFallbackValuesJson[nameof(Object.name)] += " " + ToString();

            string transformName = nameof(Object.transform);
            loadScopeFallbackValuesJson[transformName][nameof(TransformDouble.type)] = JsonUtility.ToJson(typeof(TransformDouble));
            loadScopeFallbackValuesJson[transformName][nameof(TransformDouble.parent)] = JsonUtility.ToJson(loader.transform.id);

            RestDatasource restDatasource = loader.datasource as RestDatasource;
            if (restDatasource != Disposable.NULL && restDatasource.containsCopyrightedMaterial)
                loadScopeFallbackValuesJson[nameof(RestDatasource.containsCopyrightedMaterial)] = true;

            return loadScopeFallbackValuesJson;
        }

        public FallbackValues GetFirstPersistentFallbackValues()
        {
            FallbackValues persistentFallbackValues = null;

            loader.IterateOverFallbackValues<PersistentMonoBehaviour, PersistentScriptableObject>((fallbackValues) =>
            {
                if (fallbackValues.GetProperty(out bool createPersistentIfMissing, nameof(PersistentMonoBehaviour.createPersistentIfMissing)) && createPersistentIfMissing)
                {
                    persistentFallbackValues = fallbackValues;
                    return false;
                }
                return true;
            });

            return persistentFallbackValues;
        }

        public bool LoadInProgress()
        {
            return loadingState == DatasourceOperationBase.LoadingState.Interval || loadingState == DatasourceOperationBase.LoadingState.Loading;
        }

        public void Load(float loadInterval = 0.0f)
        {
            datasourceOperation = null;

            loadingState = DatasourceOperationBase.LoadingState.Interval;

            loadIntervalTween = tweenManager.DelayedCall(loadInterval, null, () =>
            {
                loadingState = DatasourceOperationBase.LoadingState.None;

                ILoadDatasource datasource = loader.datasource as ILoadDatasource;
                if (!Disposable.IsDisposed(datasource))
                {
                    loadingState = DatasourceOperationBase.LoadingState.Loading;
                    datasourceOperation = datasource.Load((persistents, loadingResult) =>
                    {
                        IterateOverPersistents((persistentId, persistent) =>
                        {
                            if (persistents == null || !persistents.Contains(persistent))
                                RemovePersistent(persistent);
                            return true;
                        });

                        if (persistents != null)
                        {
                            foreach (IPersistent persistent in persistents)
                            {
                                if (loader.AddPersistent(persistent))
                                    AddPersistent(persistent);
                            }
                        }

                        loadingState = loadingResult;
                    }, this);
                }
            }, () => loadIntervalTween = null);
        }

        public virtual bool IsInScope(IPersistent persistent)
        {
            return false;
        }

        public void Merge(LoadScope loadScope)
        {
            bool addedPersistent = false;

            loadScope.IterateOverPersistents((persistentId, persistent) =>
            {
                if (!Disposable.IsDisposed(persistent) && AddPersistent(persistent))
                    addedPersistent = true;
                return true;
            });

            if (addedPersistent)
            {
                if (datasourceOperation == null)
                {
                    datasourceOperation = loadScope.datasourceOperation;
                    loadScope._datasourceOperation = null;
                }
                loadingState = DatasourceOperationBase.LoadingState.Loaded;
            }
        }

        public override string ToString()
        {
            return PropertiesToString() + " (" + GetType().FullName + ")";
        }

        protected virtual string PropertiesToString()
        {
            return "";
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                //Remove the delegate before we stop the datasourceOperation so that we do not throw a loading state change.
                LoadingStateChangedEvent = null;

                loadIntervalTween = null;
                SetDatasourceOperation(null, disposeContext);

                PersistentAddedEvent = null;
                PersistentRemovedEvent = null;

                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public override void MarkAsNotPoolable()
        {
            base.MarkAsNotPoolable();

            if (datasourceOperation != Disposable.NULL)
                datasourceOperation.MarkAsNotPoolable();
        }

        private bool _registeredCompleteObjectUndo;
        private void RegisterCompleteObjectUndo(DisposeContext disposeContext = DisposeContext.Editor_Destroy)
        {
            if (disposeContext == DisposeContext.Editor_Destroy)
            {
                MarkAsNotPoolable();
                if (!_registeredCompleteObjectUndo)
                {
                    _registeredCompleteObjectUndo = true;
                    Editor.UndoManager.RegisterCompleteObjectUndo(this);
                }
            }
        }

        public void ResetRegisterCompleteUndo()
        {
            _registeredCompleteObjectUndo = false;
        }
#endif
    }
}
