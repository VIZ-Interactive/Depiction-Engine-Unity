// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

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

        /// <summary>
        /// Dispatched as a signal to any <see cref="DepictionEngine.DatasourceBase"/> containing this <see cref="DepictionEngine.IPersistent"/> to let them know that they need to add it to their save operation queue.
        /// </summary>
        Action<IPersistent, Action> PersistenceSaveOperationEvent { get; set; }
        /// <summary>
        /// Dispatched as a signal to any <see cref="DepictionEngine.DatasourceBase"/> containing this <see cref="DepictionEngine.IPersistent"/> to let them know that they need to add it to their synchronize operation queue.
        /// </summary>
        Action<IPersistent, Action> PersistenceSynchronizeOperationEvent { get; set; }
        /// <summary>
        /// Dispatched as a signal to any <see cref="DepictionEngine.DatasourceBase"/> containing this <see cref="DepictionEngine.IPersistent"/> to let them know that they need to add it to their delete operation queue.
        /// </summary>
        Action<IPersistent, Action> PersistenceDeleteOperationEvent { get; set; }

        /// <summary>
        /// Trigger the dispatch of a <see cref="DepictionEngine.IPersistent.PersistenceSaveOperationEvent"/>.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> successfully added to the queue.</returns>
        int Save();

        /// <summary>
        /// Trigger the dispatch of a <see cref="DepictionEngine.IPersistent.PersistenceSynchronizeOperationEvent"/>.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> successfully added to the queue.</returns>
        int Synchronize();

        /// <summary>
        /// Trigger the dispatch of a <see cref="DepictionEngine.IPersistent.PersistenceDeleteOperationEvent"/>.
        /// </summary>
        /// <returns>The number of <see cref="DepictionEngine.IPersistent"/> successfully added to the queue.</returns>
        int Delete();
    }
}
