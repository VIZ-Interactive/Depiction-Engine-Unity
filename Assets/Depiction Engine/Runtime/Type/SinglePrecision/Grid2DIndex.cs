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

        [SerializeField]
        private int _hashCode;

        public Grid2DIndex(Vector2Int index, Vector2Int dimensions)
        {
            this.index = index;
            this.dimensions = dimensions;
            
            IEnumerable<int> hashCodes = new int[] { index.x, index.y, dimensions.x, dimensions.y };
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            int i = 0;
            foreach (var hashCode in hashCodes)
            {
                if (i % 2 == 0)
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                else
                    hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

                ++i;
            }
            _hashCode = hash1 + (hash2 * 1566083941);
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
            return _hashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return "(Dimensions:" + dimensions + " Index:" + index + ")";
        }
    }
}
