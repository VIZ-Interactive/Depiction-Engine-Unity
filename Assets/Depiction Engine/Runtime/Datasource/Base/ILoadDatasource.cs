// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using System;
using Unity.Loading;

namespace DepictionEngine
{
    /// <summary>
    /// A datasource that supports loading.
    /// </summary>
    public interface ILoadDatasource
    {
        DatasourceOperationBase Load(Action<List<IPersistent>, DatasourceOperationBase.LoadingState> operationResult, LoadScope loadScope);
    }
}
