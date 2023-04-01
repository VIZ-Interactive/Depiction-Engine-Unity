// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)


namespace DepictionEngine
{
    public interface IDatasource
    {
        public bool IsIdMatching(SerializableGuid datasourceId);
    }
}
