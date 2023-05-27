// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DepictionEngine
{
    public class WebRequestDatasourceOperationBase : DatasourceOperationBase
    {
        /// <summary>
        /// The different types of HTTP method. <br/><br/>
        /// <b><see cref="Get"/>:</b> <br/>
        /// Requests a representation of the specified resource. <br/><br/>
        /// <b><see cref="GetTexture"/>:</b> <br/>
        /// Requests a representation of the specified texture. <br/><br/>
        /// <b><see cref="Put"/>:</b> <br/>
        /// Replaces all current representations of the target resource with the request payload. <br/><br/>
        /// <b><see cref="Post"/>:</b> <br/>
        /// Submits an entity to the specified resource. <br/><br/>
        /// <b><see cref="Delete"/>:</b> <br/>
        /// Deletes the specified resource.
        /// </summary> 
        protected enum HTTPMethod
        {
            Get, 
            GetTexture, 
            Put, 
            Post, 
            Delete
        };

        [SerializeField]
        private string _uri;
        [SerializeField]
        private int _timeout;
        [SerializeField]
        private List<string> _headers;
        [SerializeField]
        private byte[] _bodyData;

        private Processor _webRequestDataProcessor;

        private UnityWebRequest _www;
        private Coroutine _coroutine;
        private MonoBehaviour _monoBehaviour;

        public void Init(string uri, int timeout, List<string> headers, byte[] bodyData)
        {
            _uri = uri;
            _timeout = timeout;
            _headers = headers;
            _bodyData = bodyData;
        }

        protected Processor webRequestDataProcessor
        {
            get => _webRequestDataProcessor;
            set
            {
                if (Object.ReferenceEquals(_webRequestDataProcessor, value))
                    return;

                _webRequestDataProcessor?.Cancel();

                _webRequestDataProcessor = value;
            }
        }

        protected override void KillLoading()
        {
            base.KillLoading();
          
            _webRequestDataProcessor?.Dispose();

            if (_monoBehaviour != null && _coroutine != null)
                _monoBehaviour.StopCoroutine(_coroutine);
            _coroutine = null;

            if (_www != null)
            {
                _www.Dispose();
                _www = null;
            }
        }

        public override DatasourceOperationBase Execute(Action<bool, OperationResult> operationResultCallback)
        {
            base.Execute(operationResultCallback);

            UnityWebRequest request = CreateUnityWebRequest(_uri, _timeout, _headers, _bodyData);

            if (request != null)
            {
                try
                {
                    if (Disposable.IsDisposed(_monoBehaviour))
                        _monoBehaviour = datasourceManager;
                    _coroutine = _monoBehaviour.StartCoroutine(SendWebRequest(request));
                }
                catch (Exception e)
                {
                    OperationDone(new OperationDoneResult(false, null, e.Message));
                }
            }

            return this;
        }

        protected virtual UnityWebRequest CreateUnityWebRequest(string uri, int timeout = 60, List<string> headers = null, byte[] bodyData = null, HTTPMethod httpMethod = HTTPMethod.Get)
        {
            UnityWebRequest request = null;

            if (httpMethod == HTTPMethod.Get || httpMethod == HTTPMethod.GetTexture)
                request = httpMethod == HTTPMethod.GetTexture ? UnityWebRequestTexture.GetTexture(uri) : UnityWebRequest.Get(uri);
            else if (httpMethod == HTTPMethod.Put)
                request = UnityWebRequest.Put(uri, bodyData);
            else if (httpMethod == HTTPMethod.Post || httpMethod == HTTPMethod.Delete)
            {
#if UNITY_2022_2_OR_NEWER
                request = UnityWebRequest.PostWwwForm(uri, "");
#else
                request = UnityWebRequest.Post(uri, "");
#endif
                request.uploadHandler = new UploadHandlerRaw(bodyData);
            }

            request.timeout = timeout;

            if (headers != null)
            {
                foreach (string header in headers)
                {
                    string[] headerNameValuePair = header.Split('#');
                    if (headerNameValuePair.Length == 2)
                        request.SetRequestHeader(headerNameValuePair[0], headerNameValuePair[1]);
                }
            }

            return request;
        }

        private IEnumerator SendWebRequest(UnityWebRequest request)
        {
            using (_www = request)
            {
                yield return _www.SendWebRequest();

                if (_www.result != UnityWebRequest.Result.ConnectionError && _www.result != UnityWebRequest.Result.ProtocolError)
                {
                    webRequestDataProcessor ??= InstanceManager.Instance(false)?.CreateInstance<Processor>();

                    webRequestDataProcessor.StartProcessing(WebRequestDataProcessorFunction, typeof(OperationResult), InitWebRequestProcessorParametersType(), InitWebRequestProcessorParameters, (data, errorMsg) =>
                    {
                        OperationDone(new OperationDoneResult(true, data as OperationResult));
                    }, sceneManager.enableMultithreading ? Processor.ProcessingType.AsyncTask : Processor.ProcessingType.Sync);
                }
                else
                    OperationDone(new OperationDoneResult(false, null, "ERROR: " + _www.error + ", CONTENT: " + _www.downloadHandler.text + ", URI: " + _www.url));
            }
        }

        protected virtual IEnumerator WebRequestDataProcessorFunction(ProcessorOutput data, ProcessorParameters parameters)
        {
            return null;
        }

        protected virtual Type InitWebRequestProcessorParametersType()
        {
            return typeof(WebRequestProcessorParameters);
        }

        protected virtual void InitWebRequestProcessorParameters(ProcessorParameters parameters)
        {
            WebRequestProcessorParameters webRequestProcessorParameters = parameters as WebRequestProcessorParameters;
            
            string text = null;
            byte[] data = null;
            Texture2D texture = null;

            if (_www != null)
            {
                text = _www.downloadHandler.text;
                texture = _www.downloadHandler is DownloadHandlerTexture ? DownloadHandlerTexture.GetContent(_www) : null;
                data = _www.downloadHandler.data;
            }

            webRequestProcessorParameters.Init(text, data, texture);

        }

        public override bool LoadingWasCompromised()
        {
            return base.LoadingWasCompromised() && (webRequestDataProcessor == null || webRequestDataProcessor.ProcessingWasCompromised());
        }
    }

    public class WebRequestProcessorParameters : ProcessorParameters
    {
        private string _text;
        private byte[] _data;
        private Texture2D _texture;

        public WebRequestProcessorParameters Init(string text, byte[] data, Texture2D texture)
        {
            _text = text;
            _data = data;
            _texture = texture;
            
            return this;
        }

        public string text
        {
            get => _text;
            private set => _text = value;
        }

        public byte[] data
        {
            get => _data;
            private set => _data = value;
        }

        public Texture2D texture
        {
            get => _texture;
            protected set => SetTexture(value);
        }

        public bool SetTexture(Texture2D value, bool destroyLastTexture = true)
        {
            if (Object.ReferenceEquals(_texture, value))
                return false;

            if (destroyLastTexture)
                DisposeManager.Destroy(_texture);

            _texture = value;

            return true;
        }
    }
}

