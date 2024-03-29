﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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

#if UNITY_EDITOR
        [SerializeField, Button(nameof(LoadBtn))]
        private bool _load;
#endif

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

#if UNITY_EDITOR
        private void LoadBtn()
        {
            Load();
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _loadingState = default;
            _datasourceOperation = null;

            _persistentsDictionary?.Clear();

            _loader = default;
        }

        public bool InitializeLastFields()
        {
#if UNITY_EDITOR
            lastPersistentsDictionary.Clear();
            lastPersistentsDictionary.CopyFrom(persistentsDictionary);
#endif
            return true;
        }

        public LoadScope Init(LoaderBase loader)
        {
            this.loader = loader;

            InitializeLastFields();

            return this;
        }

        public List<(bool, SerializableGuid, IPersistent)> PerformAddRemovePersistentsChange(bool compareWithLastDictionary = false)
        {
            PersistentsDictionary lastPersistentsDictionary = persistentsDictionary;
#if UNITY_EDITOR
            if (compareWithLastDictionary)
                lastPersistentsDictionary = this.lastPersistentsDictionary;

            SerializationUtility.RecoverLostReferencedObjectsInCollection(persistentsDictionary);
#endif
            return LoaderBase.PerformAddRemovePersistentsChange(this, persistentsDictionary, lastPersistentsDictionary);
        }

#if UNITY_EDITOR
        protected PersistentsDictionary _lastPersistentsDictionary;
        private PersistentsDictionary lastPersistentsDictionary
        {
            get { _lastPersistentsDictionary ??= new(); return _lastPersistentsDictionary; }
        }

        public void LoaderUndoRedoPerformed()
        {
            SerializationUtility.RecoverLostReferencedObject(ref _loader);
        }
#endif

        private PersistentsDictionary persistentsDictionary
        {
            get { _persistentsDictionary ??= new(); return _persistentsDictionary; }
        }

        public IPersistent GetFirstPersistent()
        {
            return persistentsDictionary.Count > 0 ? persistentsDictionary.ElementAt(0).Value.persistent : null;
        }

        public virtual bool AddPersistent(IPersistent persistent)
        {
            SerializableIPersistent serializableIPersistent = new(persistent);
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

        public int persistentCount { get => persistentsDictionary.Count; }

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
            return initialized && (loadingState == DatasourceOperationBase.LoadingState.Interval && loadIntervalTween == null) || (datasourceOperation == null || datasourceOperation.LoadingWasCompromised());
        }

        public void KillLoading()
        {
            if (LoadInProgress())
            {
                loadIntervalTween = null;
                datasourceOperation = null;
                loadingState = DatasourceOperationBase.LoadingState.Interrupted;
            }
        }

        public virtual object scopeKey { get => null; }

        public LoaderBase loader
        {
            get => _loader;
            private set => _loader = value;
        }

        private Tween loadIntervalTween
        {
            get => _loadIntervalTween;
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
            get => _datasourceOperation;
            set => SetDatasourceOperation(value);
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
            get => _loadingState;
            private set
            {
                if (_loadingState == value)
                    return;

                _loadingState = value;

                LoadingStateChangedEvent?.Invoke(this);
            }
        }

        public bool LoadInProgress()
        {
            return loadIntervalTween != Disposable.NULL || (datasourceOperation != null && datasourceOperation.loadingState == DatasourceOperationBase.LoadingState.Loading);
        }

        public void Load(float loadInterval = 0.0f)
        {
            datasourceOperation = null;

            TweenManager tweenManager = this.tweenManager;
            if (tweenManager != Disposable.NULL)
            {
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
                                {
                                    RemovePersistent(persistent);
                                    if (!loader.IsPersistentInScope(persistent))
                                        loader.RemovePersistent(persistent);
                                }
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

                        if (datasourceOperation == Disposable.NULL)
                            loadingState = DatasourceOperationBase.LoadingState.Failed;

                    }
                }, () => loadIntervalTween = null);
            }
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
            JSONObject loadScopeFallbackValuesJson = new()
            {
                [nameof(Object.name)] = name
            };

            string transformName = nameof(Object.transform);
            loadScopeFallbackValuesJson[transformName][nameof(TransformDouble.type)] = JsonUtility.ToJson(typeof(TransformDouble));
            loadScopeFallbackValuesJson[transformName][nameof(TransformDouble.parentId)] = JsonUtility.ToJson(loader.transform.id);

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

        public virtual bool IsInScope(IPersistent persistent)
        {
            return false;
        }

        public virtual object[] GetURLParams()
        {
            return null;
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
#endif
    }
}
