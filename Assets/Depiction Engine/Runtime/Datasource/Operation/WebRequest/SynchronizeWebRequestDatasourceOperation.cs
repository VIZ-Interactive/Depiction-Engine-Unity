// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections;

namespace DepictionEngine
{
    public class SynchronizeWebRequestDatasourceOperation : WebRequestDatasourceOperationBase
    {
        protected override IEnumerator WebRequestDataProcessorFunction(ProcessorOutput data, ProcessorParameters parameters)
        {
            return SynchronizeWebRequestProcessingFunctions.PopulateOperationResult(data, parameters);
        }

        public class SynchronizeWebRequestProcessingFunctions : DatasourceOperationProcessingFunctions
        {
            public static IEnumerator PopulateOperationResult(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in PopulateOperationResult(data as OperationResult, parameters as WebRequestProcessorParameters))
                    yield return enumeration;
            }

            private static IEnumerable PopulateOperationResult(OperationResult operationResult, WebRequestProcessorParameters webRequestProcessorParameters)
            {
                if (operationResult is OperationResult)
                    IterateOverJsonItem(webRequestProcessorParameters.text, (jsonItem) => { AddResponseDataToOperationResult(CreateSynchronizeResponseData(jsonItem), operationResult); });

                yield break;
            }

            private static SynchronizeResultData CreateSynchronizeResponseData(JSONNode jsonItem)
            {
                return GetInstance<SynchronizeResultData>().Init(SerializableGuid.Parse(jsonItem[nameof(IPersistent.id)]), jsonItem);
            }
        }
    }
}

