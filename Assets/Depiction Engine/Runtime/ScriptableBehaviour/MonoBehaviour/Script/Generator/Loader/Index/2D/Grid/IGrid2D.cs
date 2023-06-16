// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Implements 2D Grid members compatible with <see cref="DepictionEngine.Grid2DLoaderBase"/>.
    /// </summary>
    public interface IGrid2D : IGrid
    {
        GeoCoordinate2Double geoCenter { get; }

        Grid2D IsInGrid(Vector2Int grid2DIndex, Vector2Int grid2DDimensions);
        void IterateOverIndexes(Action<IGrid2D, GeoCoordinate2Double, Vector2Int, Vector2Int> callback);
    }
}
