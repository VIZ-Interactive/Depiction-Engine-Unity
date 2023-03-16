// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class LoadScope : ScriptableObjectDisposable
    {
        [SerializeField]
        private DatasourceOperationBase.LoadingState _loadingState;
        [SerializeField]
        private DatasourceOperationBase _datasourceOperation;

        [SerializeField]
        protected SerializableIPersistentList _persistents;

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

        public override void Recycle()
        {
            base.Recycle();

            _persistents?.Clear();

            _loadingState = default;
            _loader = default;
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);
        }

        public bool LoadingWasCompromised()
        {
            //Problem: Loading was interrupted before finishing(Often because the scene is Played while LoadScopes are still loading)
            return initialized && (datasourceOperation == null || datasourceOperation.LoadingWasCompromised());
        }

        public LoadScope Init(LoaderBase loader)
        {
            this.loader = loader;

            return this;
        }

        public bool RemoveNullPersistents()
        {
            bool removed = false;

            IterateOverPersistents((i, persistent) =>
            {
                if (Disposable.IsDisposed(persistent))
                {
                    _persistents.RemoveAt(i);
                    removed = true;
                }
                return true;
            });

            return removed;
        }

        private SerializableIPersistentList persistents
        {
            get
            {
                _persistents ??= new SerializableIPersistentList();
                return _persistents;
            }
        }

        public IPersistent GetFirstPersistent()
        {
            IPersistent persistent = null;

            if (persistents.Count > 0)
                persistent = persistents[0];

            return persistent;
        }

        public virtual bool AddPersistent(IPersistent persistent)
        {
            bool added = false;

            if (!persistents.Contains(persistent))
            {
                persistents.Add(persistent);
                added = true;
            }

            if (added)
                PersistentAddedEvent?.Invoke(this);

            return added;
        }

        public virtual bool RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (IsDisposing())
                return false;

            return persistents.Remove(persistent);
        }

        public void IterateOverPersistents(Func<int, IPersistent, bool> callback)
        {
            if (_persistents != null)
            {
                for (int i = _persistents.Count - 1; i >= 0; i--)
                {
                    if (!callback(i, _persistents[i]))
                        break;
                }
            }
        }

        public int seed { get { return loader.seed; } }
        public LoaderBase.DataType dataType { get { return loader.dataType; } }
        public int depth { get { return loader.depth; } }
        public int timeout { get { return loader.timeout; } }
        public List<string> headers { get { return loader.headers; } }
        public string loadEndpoint { get { return loader.loadEndpoint; } }

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
            set 
            {
                if (Object.ReferenceEquals(_datasourceOperation, value))
                    return;

                DisposeManager.Dispose(_datasourceOperation);

                _datasourceOperation = value;

                UpdateLoadingState();
            }
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

            loadIntervalTween = tweenManager.DelayedCall(loadInterval, null, () =>
            {
                ILoadDatasource loadDatasource = loader.datasource;

                if (loader.datasource == Disposable.NULL)
                    loadDatasource = datasourceManager;

                datasourceOperation = loadDatasource.Load((persistents) =>
                {
                    IterateOverPersistents((i, persistent) => 
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

                    UpdateLoadingState();

                }, this);
            }, () => loadIntervalTween = null);
        }

        private void UpdateLoadingState()
        {
            loadingState = datasourceOperation != Disposable.NULL ? datasourceOperation.loadingState : DatasourceOperationBase.LoadingState.Interval;
        }

        public virtual bool IsInScope(IPersistent persistent)
        {
            return false;
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
                DisposeManager.Dispose(_datasourceOperation, disposeContext);

                PersistentAddedEvent = null;

                return true;
            }
            return false;
        }
    }
}
