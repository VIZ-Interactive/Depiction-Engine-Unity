// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Abstracts the complexity of running code Asynchronously(Task or Coroutine) based on platform.
    /// </summary>
    public class Processor : Disposable
    {
        /// <summary>
        /// The different processing state. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// Processing as not started. <br/><br/>
        /// <b><see cref="Processing"/>:</b> <br/>
        /// Currently processing. <br/><br/>
        /// <b><see cref="Completed"/>:</b> <br/>
        /// Processing as completed. <br/><br/>
        /// <b><see cref="Canceled"/>:</b> <br/>
        /// Processing was canceled.
        /// </summary>
        public enum ProcessingState
        {
            None,
            Processing,
            Completed,
            Canceled
        }

        /// <summary>
        /// The different processing type. <br/><br/>
        /// <b><see cref="AsyncTask"/>:</b> <br/>
        /// Asynchronous Task. <br/><br/>
        /// <b><see cref="AsyncCoroutine"/>:</b> <br/>
        /// Asynchronous Coroutine. <br/><br/>
        /// <b><see cref="Sync"/>:</b> <br/>
        /// Synchronized.
        /// </summary>
        public enum ProcessingType
        {
            AsyncTask,
            AsyncCoroutine,
            Sync
        }

        private ProcessingState _processingState;

        private System.Threading.Tasks.Task _task;
        private Coroutine _coroutine;
        private CancellationTokenSource _cancellationTokenSource;

        private MonoBehaviour _monoBehaviour;

        public ProcessingState processingState { get => _processingState; }

        public bool Cancel()
        {
            bool canceled = false;

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel(_monoBehaviour, _coroutine);
                _cancellationTokenSource = null;

                _processingState = ProcessingState.Canceled;

                canceled = true;
            }

            return canceled;
        }

        public bool ProcessingWasCompromised()
        {
            return _processingState == ProcessingState.Processing && _task == null && _coroutine == null;
        }

        public void StartProcessing(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, Type dataType, Type parametersType, Action<ProcessorParameters> parametersCallback = null, Action<ProcessorOutput, string> processingCompleted = null, ProcessingType processingType = ProcessingType.AsyncTask)
        {
            if (func != null)
            {
                Cancel();

#if !UNITY_EDITOR && UNITY_WEBGL
                //Multi-threading is not supported on WebGL
                if (processingType == ProcessingType.AsyncTask)
                    processingType = ProcessingType.AsyncCoroutine;
#endif

                switch (processingType)
                {
                    case ProcessingType.Sync:
                        StartProcessingSync(func, dataType, parametersType, parametersCallback, processingCompleted);
                        break;
                    case ProcessingType.AsyncTask:
                        _task = StartProcessingAsyncTask(func, dataType, parametersType, parametersCallback, processingCompleted);
                        break;
                    case ProcessingType.AsyncCoroutine:
                        _coroutine = StartProcessingCoroutine(func, dataType, parametersType, parametersCallback, processingCompleted);
                        break;
                }
            }
        }

        private void StartProcessingSync(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, Type dataType, Type parametersType, Action<ProcessorParameters> parametersCallback, Action<ProcessorOutput, string> processingCompleted)
        {
            ProcessorOutput data = null;
            ProcessorParameters parameters = null;
            CancellationTokenSource cancellationTokenSource = null;
            Init(ref data, ref parameters, ref cancellationTokenSource, dataType, parametersType, parametersCallback);
    
            parameters.Init(cancellationTokenSource, ProcessingType.Sync);
            CallFuncSync(func, data, parameters);

            Finalize(processingCompleted, data, parameters, cancellationTokenSource);
        }

        private async System.Threading.Tasks.Task StartProcessingAsyncTask(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, Type dataType, Type parametersType, Action<ProcessorParameters> parametersCallback, Action<ProcessorOutput, string> processingCompleted)
        {
            string errorMsg = null;

            ProcessorOutput data = null;
            ProcessorParameters parameters = null;
            CancellationTokenSource cancellationTokenSource = null;
            Init(ref data, ref parameters, ref cancellationTokenSource, dataType, parametersType, parametersCallback);
         
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    cancellationTokenSource.ThrowIfCancellationRequested();
                    parameters.Init(cancellationTokenSource, ProcessingType.AsyncTask);
                    CallFuncSync(func, data, parameters);
                }
                catch(Exception e)
                {
                    errorMsg = GetErrorMsg(e);
                }
            }, cancellationTokenSource.Token);

            Finalize(processingCompleted, data, parameters, cancellationTokenSource, errorMsg);
        }

        private void CallFuncSync(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, ProcessorOutput data, ProcessorParameters parameters)
        {
            IEnumerator enumerator = func(data, parameters);
            while (enumerator.MoveNext()) { };
        }

        private Coroutine StartProcessingCoroutine(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, Type dataType, Type parametersType, Action<ProcessorParameters> parametersCallback, Action<ProcessorOutput, string> processingCompleted)
        {
            ProcessorOutput data = null;
            ProcessorParameters parameters = null;
            CancellationTokenSource cancellationTokenSource = null;
            Init(ref data, ref parameters, ref cancellationTokenSource, dataType, parametersType, parametersCallback);

            parameters.Init(cancellationTokenSource, ProcessingType.AsyncCoroutine);
            return StartCoroutine(CallFuncAsync(func, data, parameters, processingCompleted, cancellationTokenSource));
        }

        private IEnumerator CallFuncAsync(Func<ProcessorOutput, ProcessorParameters, IEnumerator> func, ProcessorOutput data, ProcessorParameters parameters, Action<ProcessorOutput, string> processingCompleted, CancellationTokenSource cancellationTokenSource)
        {
            yield return func(data, parameters);

            Finalize(processingCompleted, data, parameters, cancellationTokenSource);

            yield break;
        }

        private Coroutine StartCoroutine(IEnumerator routine)
        {
            _monoBehaviour = _monoBehaviour != null ? _monoBehaviour : SceneManager.Instance();
            return _monoBehaviour.StartCoroutine(routine);
        }

        private void Init(ref ProcessorOutput data, ref ProcessorParameters parameters, ref CancellationTokenSource cancellationTokenSource, Type dataType, Type parametersType, Action<ProcessorParameters> parametersCallback)
        {
            data = dataType != null ? GetDataInstance(dataType) : null;
            parameters = CreateParametersInstance(parametersType);
            parametersCallback?.Invoke(parameters);

            _processingState = ProcessingState.Processing;

            cancellationTokenSource = _cancellationTokenSource = new CancellationTokenSource();
        }

        private string GetErrorMsg(Exception e)
        {
            if (e is not OperationCanceledException)
                return e + ", " + e.Message + ": " + e.StackTrace;
            return null;
        }

        private bool WasCancelled(CancellationTokenSource cancellationTokenSource)
        {
            bool canceled = false;

            if (cancellationTokenSource != null)
            {
                canceled = cancellationTokenSource.IsCancellationRequested;
                cancellationTokenSource.Dispose();
                if (cancellationTokenSource == _cancellationTokenSource)
                    _cancellationTokenSource = null;
            }
            return canceled;
        }

        private void Finalize(Action<ProcessorOutput, string> processingCompleted, ProcessorOutput data, ProcessorParameters parameters, CancellationTokenSource cancellationTokenSource, string errorMsg = null)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(errorMsg))
                Debug.LogError(data + ", " + parameters + ": " + errorMsg);
#endif

            if (!WasCancelled(cancellationTokenSource))
            {
                _processingState = ProcessingState.Completed;

                processingCompleted?.Invoke(data, errorMsg);
            }

            DisposeManager.Dispose(parameters);
            DisposeManager.Dispose(data);
        }

        private ProcessorOutput GetDataInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(ProcessorOutput)))
            {
                Debug.LogError("Processor Data Type '" + type.Name + "' is not valid");
                return null;
            }
            InstanceManager instanceManager = InstanceManager.Instance(false);
            return instanceManager != Disposable.NULL ? instanceManager.CreateInstance(type) as ProcessorOutput : null;
        }

        public static ProcessorParameters CreateParametersInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(ProcessorParameters)))
            {
                Debug.LogError("Processor Parameters Type '" + type.Name + "' is not valid");
                return null;
            }
            InstanceManager instanceManager = InstanceManager.Instance(false);
            return instanceManager != Disposable.NULL ? instanceManager.CreateInstance(type) as ProcessorParameters : null;
        }

        public void Dispose()
        {
            Cancel();
            _processingState = ProcessingState.None;
        }
    }

    public class CancellationTokenSource
    {
        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        public CancellationTokenSource()
        {
            _cancellationTokenSource = new System.Threading.CancellationTokenSource();
        }

        public System.Threading.CancellationToken Token
        {
            get { return _cancellationTokenSource.Token; }
        }

        public bool IsCancellationRequested
        {
            get { return _cancellationTokenSource.IsCancellationRequested; }
        }

        public void Cancel(MonoBehaviour monoBehaviour, Coroutine coroutine)
        {
            _cancellationTokenSource.Cancel();
            if (monoBehaviour != null && coroutine != null)
                monoBehaviour.StopCoroutine(coroutine);
        }

        public void ThrowIfCancellationRequested()
        {
            Token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
