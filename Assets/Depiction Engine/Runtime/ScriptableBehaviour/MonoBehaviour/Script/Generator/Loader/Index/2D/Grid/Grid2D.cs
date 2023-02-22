// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class Grid2D : GridBase, IGrid2D
    {
        [Serializable]
        private class RowDictionary : SerializableDictionary<int, Row> { };

        protected static readonly double MIN_SIZE = 0.00000001d;

        private RowDictionary _rows;

        private Vector2Int _grid2DDimensions;

        protected Vector2Double _centerIndex;
        protected Vector2Int _centerIndexInt;

        protected GeoCoordinate2Double _geoCenter;

        private CameraGrid2D _cameraGrid2D;

        public int rowCount { get { return _rows != null ? _rows.Count : 0; } }
        public Vector2Double centerIndex { get { return _centerIndex; } }
        public Vector2Int grid2DDimensions { get { return _grid2DDimensions; } }
        public GeoCoordinate2Double geoCenter { get { return _geoCenter; } }

        public CameraGrid2D cameraGrid2D { get { return _cameraGrid2D; } }

        public void Init(CameraGrid2D cameraGrid2D)
        {
            _cameraGrid2D = cameraGrid2D;
        }

        public Grid2D IsInGrid(Vector2Int grid2DIndex, Vector2Int grid2DDimensions)
        {
            Row row;
            if (enabled && grid2DDimensions == _grid2DDimensions && _rows != null && _rows.TryGetValue(grid2DIndex.y, out row) && row.IsInside(grid2DIndex.x, grid2DIndex.x))
                return this;
            return null;
        }

        protected Row CreateRow(int y, int start, int end)
        {
            SetRowStaticProperties();
            return new Row(y, start, end);
        }

        protected Row CreateRow(int y)
        {
            SetRowStaticProperties();
            return new Row(y);
        }

        protected void SetRowStaticProperties()
        {
            Row.xNumTiles = _grid2DDimensions.x;
            Row.centerX = _centerIndexInt.x;
        }

        protected int GetRowCount()
        {
            return _rows.Count;
        }

        protected Row GetRow(int y, Vector2Int gridDimensions)
        {
            Row row = null;
            if (y >= 0 && y < gridDimensions.y && !_rows.TryGetValue(y, out row))
            {
                row = CreateRow(y);
                _rows.Add(y, row);
            }
            return row;
        }

        protected void AddRow(int y, Row value)
        {
            _rows.Add(y, value);
        }

        protected void IterateOverRows(Action<Row> callback)
        {
            if (callback != null)
            {
                foreach (Row row in _rows.Values)
                    callback(row);
            }
        }

        public bool UpdateGridFields(Vector2Int gridDimensions, GeoCoordinate2Double geoCenter, bool updateDerivedProperties = true)
        {
            bool changed = !wasFirstUpdated;

            if (_grid2DDimensions != gridDimensions)
            {
                _grid2DDimensions = gridDimensions;
                changed = true;
            }

            if (_geoCenter != geoCenter)
            {
                _geoCenter = geoCenter;
                changed = true;
            }

            if (updateDerivedProperties && changed)
                UpdateDerivedProperties();

            return changed;
        }

        public override bool UpdateGrid()
        {
            if (base.UpdateGrid())
            {
                UpdateGrid(_grid2DDimensions);
                return true;
            }
            return false;
        }

        protected virtual bool UpdateGrid(Vector2Int gridDimensions)
        {
            if (_rows == null)
                _rows = new RowDictionary();
            return true;
        }

        protected override bool UpdateDerivedProperties()
        {
            if (base.UpdateDerivedProperties())
            {
                _centerIndex = MathPlus.GetIndexFromGeoCoordinate(_geoCenter, _grid2DDimensions, false);
                _centerIndexInt.x = (int)MathPlus.ClipIndex(_centerIndex.x);
                _centerIndexInt.y = (int)MathPlus.ClipIndex(_centerIndex.y);
                return true;
            }
            return false;
        }

        public void IterateOverIndexes(Action<IGrid2D, GeoCoordinate2Double, Vector2Int, Vector2Int> callback)
        {
            if (_rows != null)
            {
                Vector2Int index = new Vector2Int(-1, -1);
                foreach (Row row in _rows.Values)
                {
                    foreach (Range range in row)
                    {
                        for (int i = range.start; i <= range.end; i++)
                        {
                            index.x = i;
                            index.y = row.y;
                            callback(this, _geoCenter, index, _grid2DDimensions);
                        }
                    }
                }
            }
        }

        public override void Clear()
        {
            base.Clear();

            if (_rows != null)
                _rows.Clear();
        }

        /// <summary>
        /// A range defined by start and end index.
        /// </summary>
        [Serializable]
        protected struct Range
        {
            public int start;
            public int end;

            public Range(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public bool Overlaps(int start, int end)
            {
                return (start >= this.start && start <= this.end) || (end >= this.start && end <= this.end) || (start < this.start && end > this.end);
            }

            public bool IsInside(int start, int end)
            {
                return this.start <= start && this.end >= end;
            }

            public override string ToString()
            {
                return start + ", " + end;
            }
        }

        /// <summary>
        /// A row defined by a list of <see cref="DepictionEngine.Grid2D.Range"/>.
        /// </summary>
        protected class Row : List<Range>
        {
            public static int xNumTiles;
            public static int centerX;

            private int _y;
            private List<Range> _upperRanges;
            private List<Range> _lowerRanges;
            private List<int> _corners;

            public Row(int y)
            {
                _y = y;
            }

            public Row(int y, int start, int end)
            {
                _y = y;
                Add(start, end);
            }

            public int y
            {
                get { return _y; }
            }

            public bool IsFull()
            {
                if (Count > 0)
                {
                    Range range = this[0];
                    return range.start == 0 && range.end == xNumTiles - 1;
                }
                return false;
            }

            public bool IsInside(int start, int end)
            {
                foreach (Range range in this)
                {
                    if (range.IsInside(start, end))
                        return true;
                }
                return false;
            }

            private bool IsNeighbor(Range range, int start, int end, bool wrap)
            {
                int xNumTiles = wrap ? Row.xNumTiles : 0;
                return start == range.end + 1 || end == range.start - 1 || (xNumTiles != 0 && (end == xNumTiles + range.start - 1));
            }

            public void AddUpperRanges(List<Range> ranges)
            {
                _upperRanges = ranges;
            }

            public void AddLowerRanges(List<Range> ranges)
            {
                _lowerRanges = ranges;
            }

            public void AddCorner(int corner)
            {
                if (_corners == null)
                    _corners = new List<int>();
                _corners.Add(corner);
            }

            public void AddCenter(int center, bool wrap)
            {
                List<Range> largest = null;
                List<Range> smallest = null;
                if (_upperRanges != null && _lowerRanges != null)
                {
                    if (_upperRanges.Count > _lowerRanges.Count)
                    {
                        largest = _upperRanges;
                        smallest = _lowerRanges;
                    }
                    else
                    {
                        largest = _lowerRanges;
                        smallest = _upperRanges;
                    }
                }
                else
                    largest = _upperRanges != null ? _upperRanges : _lowerRanges;

                if (largest != null)
                {
                    foreach (Range range in largest)
                    {
                        if (smallest == null && _corners == null)
                            Add(range.start, range.end, GetClosestGap(range.start, range.end, wrap), wrap);
                        else
                            Add(range.start, range.end);
                    }
                }

                if (smallest != null)
                {
                    int[] gaps = new int[smallest.Count];
                    Range range;
                    for (int i = 0; i < smallest.Count; i++)
                    {
                        range = smallest[i];
                        gaps[i] = GetClosestGap(range.start, range.end, wrap);
                    }
                    for (int i = 0; i < smallest.Count; i++)
                    {
                        range = smallest[i];
                        Add(range.start, range.end, gaps[i], wrap);
                    }
                }

                if (_corners != null)
                {
                    foreach (int x in _corners)
                        Add(x, x, GetClosestGap(x, x, wrap), wrap);
                }

                if (center != -1)
                    Add(center, center, GetGaps(center, center, wrap), wrap);
            }

            private int GetClosestGap(int start, int end, bool wrap)
            {
                int left;
                int right;
                return GetGaps(out left, out right, start, end, wrap) ? left != int.MinValue && Mathf.Abs(left) <= right ? left : right : 0;
            }

            private Gaps GetGaps(int start, int end, bool wrap)
            {
                int left;
                int right;
                return GetGaps(out left, out right, start, end, wrap, false) ? new Gaps(left, right) : Gaps.zero;
            }

            private bool GetGaps(out int leftGap, out int rightGap, int start, int end, bool wrap, bool mergeOnlyWithFirstClosestRange = true)
            {
                leftGap = int.MinValue;
                rightGap = int.MaxValue;

                if (Count > 0)
                {
                    if (IsFull())
                        return false;

                    int delta;
                    foreach (Range range in this)
                    {
                        if (range.IsInside(start, end) || (mergeOnlyWithFirstClosestRange && (range.Overlaps(start, end) || IsNeighbor(range, start, end, wrap))))
                            return false;

                        delta = range.end - start + 1;
                        if (delta <= 0 && delta > leftGap)
                            leftGap = delta;
                        delta = range.start - end - 1;
                        if (delta >= 0 && delta < rightGap)
                            rightGap = delta;
                        if (wrap)
                        {
                            delta = range.end - xNumTiles - start + 1;
                            if (delta <= 0 && delta > leftGap)
                                leftGap = delta;
                            delta = xNumTiles + range.start - end - 1;
                            if (delta >= 0 && delta < rightGap)
                                rightGap = delta;
                        }
                    }
                    return true;
                }
                return false;
            }

            private void Add(int start, int end, Gaps gaps, bool wrap)
            {
                Add(start, end, gaps.left, wrap);
                Add(start, end, gaps.right, wrap);
            }

            private void Add(int start, int end, int gap, bool wrap = false)
            {
                if (gap != 0 && gap != int.MaxValue && gap != int.MinValue && (!wrap || Math.Abs(gap) < xNumTiles / 2 - 1))
                {
                    int lastIndex = xNumTiles - 1;
                    if (gap < 0)
                    {
                        start += gap;
                        if (start < 0)
                        {
                            Add(lastIndex + start, lastIndex);
                            start = 0;
                        }
                    }
                    if (gap > 0)
                    {
                        end += gap;
                        if (end > lastIndex)
                        {
                            Add(0, end - lastIndex - 1);
                            end = lastIndex;
                        }
                    }
                }

                Add(start, end);
            }

            public void Add(int start, int end)
            {
                int maxIndex = xNumTiles - 1;

                end = Mathf.Clamp(end, 0, maxIndex);
                start = Mathf.Clamp(start, 0, maxIndex);

                if (Count > 0)
                {
                    if (IsFull())
                        return;

                    Range range;
                    for (int i = 0; i < Count; i++)
                    {
                        range = this[i];
                        if (range.IsInside(start, end))
                            return;

                        if (range.Overlaps(start, end) || IsNeighbor(range, start, end, false))
                        {
                            start = Mathf.Min(range.start, start);
                            end = Mathf.Max(range.end, end);
                            Remove(range);
                            i--;
                        }
                    }
                }

                if (end - start > 200)
                {
                    start = Mathf.Clamp(centerX - 10, 0, maxIndex);
                    end = Mathf.Clamp(centerX + 10, 0, maxIndex);
                    Debug.LogError("Maximum number of index columns(200) exceeded, columns will be clipped");
                }

                Add(new Range(start, end));
            }

            private struct Gaps
            {
                public static readonly Gaps zero = new Gaps(0, 0);

                public int left;
                public int right;

                public Gaps(int left, int right)
                {
                    this.left = left;
                    this.right = right;
                }
            }
        }
    }
}
