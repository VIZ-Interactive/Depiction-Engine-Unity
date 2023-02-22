// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;

namespace DepictionEngine
{
    /// <summary>
    /// Implements persistence features mostly required by the <see cref="DepictionEngine.DatasourceBase"/>.
    /// </summary>
    public interface IPersistent : IJson
    {
        bool autoDispose { get; }
        bool dontSaveToScene { get; set; }
        bool containsCopyrightedMaterial { get; }

        bool createPersistentIfMissing { get; }

        Action<IJson, PropertyInfo> UserPropertyAssignedEvent { get; set; }
        Action<IPersistent, Action> PersistenceSaveOperationEvent { get; set; }
        Action<IPersistent, Action> PersistenceSynchronizeOperationEvent { get; set; }
        Action<IPersistent, Action> PersistenceDeleteOperationEvent { get; set; }
    }
}
