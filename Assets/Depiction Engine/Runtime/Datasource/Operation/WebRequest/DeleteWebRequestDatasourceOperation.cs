// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace DepictionEngine
{
    public class DeleteWebRequestDatasourceOperation : WebRequestDatasourceOperationBase
    {
        protected override UnityWebRequest CreateUnityWebRequest(string uri = null, int timeout = 60, List<string> headers = null, byte[] bodyData = null, HTTPMethod httpMethod = HTTPMethod.Get)
        {
            return base.CreateUnityWebRequest(uri, timeout, headers, bodyData, HTTPMethod.Post);
        }

        protected override IEnumerator WebRequestDataProcessorFunction(ProcessorOutput data, ProcessorParameters parameters)
        {
            return DeleteWebRequestProcessingFunctions.PopulateOperationResult(data, parameters);
        }

        public class DeleteWebRequestProcessingFunctions : DatasourceOperationProcessingFunctions
        {
            public static IEnumerator PopulateOperationResult(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in PopulateOperationResult(data as OperationResult, parameters as WebRequestProcessorParameters))
                    yield return enumeration;
            }

            private static IEnumerable PopulateOperationResult(OperationResult operationResult, WebRequestProcessorParameters webRequestProcessorParameters)
            {
                if (operationResult is OperationResult)
                    IterateOverJsonItem(webRequestProcessorParameters.text, (jsonItem) => { AddResponseDataToOperationResult(CreateIdResponseData(jsonItem), operationResult); });

                yield break;
            }
        }
    }
}

