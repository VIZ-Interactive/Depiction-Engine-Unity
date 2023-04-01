// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public interface IPersistentList
    {
        void IterateOverPersistents(Func<SerializableGuid, IPersistent, bool> callback);
        bool AddPersistent(IPersistent persistent);
        bool RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool);
        bool RemovePersistent(SerializableGuid persistentId, DisposeContext disposeContext = DisposeContext.Programmatically_Pool);
    }
}
