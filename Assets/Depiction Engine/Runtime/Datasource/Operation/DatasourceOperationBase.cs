// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Operations to be executed by the <see cref="DepictionEngine.Datasource"/>.
    /// </summary>
    public class DatasourceOperationBase : ScriptableObjectDisposable
    {
        /// <summary>
        /// The different types of loading state. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// Not loading. <br/><br/>
        /// <b><see cref="Interval"/>:</b> <br/>
        /// A delay before the loading. <br/><br/>
        /// <b><see cref="Loading"/>:</b> <br/>
        /// The Loading is ongoing. <br/><br/>
        /// <b><see cref="Loaded"/>:</b> <br/>
        /// The loading as complete. <br/><br/>
        /// <b><see cref="Failed"/>:</b> <br/>
        /// The Loading failed. <br/><br/>
        /// <b><see cref="Interrupted "/>:</b> <br/>
        /// The Loading was interrupted.
        /// </summary> 
        public enum LoadingState
        {
            None,
            Interval,
            Loading,
            Loaded,
            Failed,
            Interrupted
        };

        [SerializeField]
        private LoadingState _loadingState;

        private Action<bool, OperationResult> _operationResultCallback;

        public override void Recycle()
        {
            base.Recycle();

            loadingState = default;
        }

        public virtual bool LoadingWasCompromised()
        {
            return loadingState == LoadingState.Loading;
        }

        public LoadingState loadingState
        {
            get { return _loadingState; }
            private set
            {
                if (_loadingState == value)
                    return;

                _loadingState = value;
            }
        }

        public virtual DatasourceOperationBase Execute(Action<bool, OperationResult> operationResultCallback)
        {
            _operationResultCallback = operationResultCallback;
            loadingState = LoadingState.Loading;
            return this;
        }

        protected void OperationDone(OperationDoneResult loadingResults)
        {
            if (loadingState != LoadingState.Failed && loadingState != LoadingState.Interrupted && loadingState != LoadingState.Loaded)
            { 
                if (loadingResults != null && !string.IsNullOrEmpty(loadingResults.errorMsg))
                    loadingState = LoadingState.Failed;
                else if (loadingResults.interrupted)
                    loadingState = LoadingState.Interrupted;
                else
                    loadingState = LoadingState.Loaded;

#if UNITY_EDITOR
                //Filter out common error messages
                if (loadingResults.HasDebugErrorMsg())
                    Debug.LogError(loadingResults.errorMsg);
#endif
                _operationResultCallback?.Invoke(loadingResults.success, loadingResults.operationResult);

                KillLoading();
            }
        }

        protected T CreateOperationResult<T>() where T : OperationResult
        {
            return InstanceManager.Instance(false)?.CreateInstance<T>();
        }

        protected T CreateResultData<T>() where T : ResultData
        {
            return InstanceManager.Instance(false)?.CreateInstance<T>();
        }

        protected virtual void KillLoading()
        {
            _operationResultCallback = null;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                KillLoading();

                OperationDone(new OperationDoneResult(true));

                return true;
            }
            return false;
        }

        public class OperationDoneResult
        {
            public bool interrupted;
            public bool success;
            public OperationResult operationResult;
            public string errorMsg;

            public OperationDoneResult(bool success, OperationResult operationResult, string errorMsg = null)
            {
                this.interrupted = false;
                this.success = success;
                this.operationResult = operationResult;
                this.errorMsg = errorMsg;
            }

            public OperationDoneResult(bool interrupted)
            {
                this.interrupted = interrupted;
            }

            public bool HasDebugErrorMsg()
            {
                return !string.IsNullOrEmpty(errorMsg) && errorMsg.IndexOf("Request timeout", StringComparison.CurrentCultureIgnoreCase) == -1 && errorMsg.IndexOf("no data", StringComparison.CurrentCultureIgnoreCase) == -1 && errorMsg.IndexOf("not found", StringComparison.CurrentCultureIgnoreCase) == -1;
            }
        }
    }

    public class DatasourceOperationProcessingFunctions : ProcessingFunctions
    {
        protected static void IterateOverJsonItem(string text, Action<JSONNode> callback)
        {
            if (callback != null)
            {
                JSONNode json = ParseJSON(text);

                if (json != null)
                {
                    if (json.IsArray)
                    {
                        foreach (JSONNode jsonItem in json.AsArray)
                            callback(jsonItem);
                    }
                    else
                        callback(json);
                }
            }
        }

        protected static JSONNode ParseJSON(string text)
        {
            return JSONNode.Parse(text);
        }

        protected static void AddResponseDataToOperationResult(ResultData responseData, OperationResult requestResponse)
        {
            if (responseData != Disposable.NULL)
                requestResponse.Add(responseData);
        }

        protected static IdResultData CreateIdResponseData(JSONNode jsonItem)
        {
            return GetInstance<IdResultData>().Init(SerializableGuid.Parse(jsonItem[nameof(IPersistent.id)]));
        }
    }

    public class OperationResult : ProcessorOutput
    {
        private List<ResultData> _resultsData;

        public override void Recycle()
        {
            base.Recycle();

            _resultsData?.Clear();
        }

        public List<ResultData> resultsData
        {
            get { return _resultsData; }
            private set { _resultsData = value; }
        }

        public void Add(ResultData responseData)
        {
            resultsData ??= new List<ResultData>();
            resultsData.Add(responseData);
        }

        public int IterateOverResultsData<T>(Action<T, IPersistent> callback) where T : ResultData
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);

            if (instanceManager != Disposable.NULL)
            {
                foreach (ResultData resultData in resultsData)
                {
                    if (resultData is T)
                    {
                        IPersistent persistent = null;
                        
                        if (resultData is IdResultData)
                            persistent = instanceManager.GetPersistent((resultData as IdResultData).id);

                        callback((T)resultData, persistent);
                    }
                }
            }

            return resultsData.Count;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (resultsData != null)
                {
                    foreach (ResultData responseData in resultsData)
                        DisposeManager.Dispose(responseData);
                }

                return true;
            }
            return false;
        }
    }

    public class LoadResultData : ResultData
    {
        private Type _type;
        private JSONObject _jsonResult;
        private JSONObject _jsonFallback;
        private SerializableGuid _persistentFallbackValuesId;
        private List<PropertyModifier> _propertyModifiers;
        private List<LoadResultData> _children;

        public LoadResultData Init(Type type, JSONObject jsonResult, JSONObject jsonFallback, SerializableGuid persistentFallbackValuesId, List<PropertyModifier> propertyModifiers = null, List<LoadResultData> children = null)
        {
            _type = type;
            _jsonResult = jsonResult;
            _jsonFallback = jsonFallback;
            _persistentFallbackValuesId = persistentFallbackValuesId;
            _propertyModifiers = propertyModifiers;
            _children = children;

            return this;
        }

        public Type type
        {
            get { return _type; }
        }

        public JSONObject jsonResult
        {
            get { return _jsonResult; }
        }

        public JSONObject jsonFallback
        {
            get { return _jsonFallback; }
        }

        public SerializableGuid persistentFallbackValuesId
        {
            get { return _persistentFallbackValuesId; }
        }

        public List<PropertyModifier> propertyModifiers
        {
            get { return _propertyModifiers; }
        }

        public List<LoadResultData> children
        {
            get { return _children; }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (_propertyModifiers != null)
                {
                    foreach (PropertyModifier propertyModifier in _propertyModifiers)
                        DisposeManager.Dispose(propertyModifier);

                    _propertyModifiers.Clear();
                }

                if (_children != null)
                {
                    foreach (LoadResultData loadData in _children)
                        DisposeManager.Dispose(loadData);

                    _children.Clear();
                }

                return true;
            }
            return false;
        }
    }

    public class SynchronizeResultData : IdResultData
    {
        private JSONNode _json;

        public SynchronizeResultData Init(SerializableGuid id, JSONNode json)
        {
            base.Init(id);

            _json = json;

            return this;
        }

        public JSONNode json
        {
            get { return _json; }
        }
    }

    public class IdResultData : ResultData
    {
        private SerializableGuid _id;

        public IdResultData Init(SerializableGuid id)
        {
            _id = id;

            return this;
        }

        public SerializableGuid id
        {
            get { return _id; }
        }
    }

    public class ResultData : Disposable
    {
    }
}
