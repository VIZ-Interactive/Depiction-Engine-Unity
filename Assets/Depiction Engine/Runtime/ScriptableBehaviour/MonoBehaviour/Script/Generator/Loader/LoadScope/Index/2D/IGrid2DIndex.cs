// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Implements the members required to be within the scope of an <see cref="DepictionEngine.Index2DLoadScope"/> used by <see cref="DepictionEngine.Index2DLoaderBase"/>.
    /// </summary>
    public interface IGrid2DIndex
    {
        Vector2Int grid2DIndex { get; }
        Vector2Int grid2DDimensions { get; }

        bool IsGridIndexValid();
    }
}
