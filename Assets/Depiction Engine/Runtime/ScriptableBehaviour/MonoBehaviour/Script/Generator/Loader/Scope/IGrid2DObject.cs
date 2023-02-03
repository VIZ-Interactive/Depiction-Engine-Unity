// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public interface IGridIndexObject
    {
        Vector2Int grid2DIndex { get; }
        Vector2Int grid2DDimensions { get; }

        bool IsGridIndexValid();
    }
}
