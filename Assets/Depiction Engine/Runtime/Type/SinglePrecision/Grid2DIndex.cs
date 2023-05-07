// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// An xy index along with xy(horizontal, vertical) grid dimension.
    /// </summary>
    [Serializable]
    public struct Grid2DIndex
    {
        public static readonly Grid2DIndex Empty = new Grid2DIndex(Vector2Int.minusOne, Vector2Int.zero);

        public Vector2Int index;
        public Vector2Int dimensions;

        public Grid2DIndex(Vector2Int index, Vector2Int dimensions)
        {
            this.index = index;
            this.dimensions = dimensions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Grid2DIndex index, Grid2DIndex otherIndex)
        {
            return index.Equals(otherIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Grid2DIndex index, Grid2DIndex otherIndex)
        {
            return !index.Equals(otherIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is Grid2DIndex)
                return Equals((Grid2DIndex)obj);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Grid2DIndex grid2DIndex)
        {
            return grid2DIndex.index == index && grid2DIndex.dimensions == dimensions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(index.x, index.y, dimensions.x, dimensions.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return "(Dimensions:" + dimensions + " Index:" + index + ")";
        }
    }
}
