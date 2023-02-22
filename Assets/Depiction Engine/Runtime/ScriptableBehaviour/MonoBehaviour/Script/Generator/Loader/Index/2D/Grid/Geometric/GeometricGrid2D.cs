// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class GeometricGrid2D : Grid2D
    {
        private GeoAstroObject _parentGeoAstroObject;

        protected double _scaleFactor;

        protected Vector3Double _centerLocalPosition;
        protected Matrix4x4Double _geoAstroObjectToGridMatrix;
        protected Matrix4x4Double _gridToGeoAstroObjectMatrix;
        protected QuaternionDouble _gridRotation;

        protected virtual double GetRotation()
        {
            return 0.0d;
        }

        private bool IsSpherical()
        {
            return _parentGeoAstroObject != Disposable.NULL ? _parentGeoAstroObject.IsSpherical() : true;
        }

        protected virtual bool GetCornerFromIndex(ref Vector3Double corner, int i, double depthOffset = 0.0d)
        {
            return false;
        }

        public bool UpdateGeometricGridFields(Vector2Int gridDimensions, GeoCoordinate2Double geoCenter, GeoAstroObject parentGeoAstroObject, bool updateDerivedProperties = true)
        {
            bool changed = UpdateGridFields(gridDimensions, geoCenter, false);

            if (!Object.ReferenceEquals(_parentGeoAstroObject, parentGeoAstroObject))
            {
                _parentGeoAstroObject = parentGeoAstroObject;
                changed = true;
            }

            double newScaleFactor = _parentGeoAstroObject != Disposable.NULL ? MathPlus.DOUBLE_RADIUS / _parentGeoAstroObject.radius : 0.0d;

            if (_scaleFactor != newScaleFactor)
            {
                _scaleFactor = newScaleFactor;
                changed = true;
            }

            if (updateDerivedProperties && changed)
                UpdateDerivedProperties();

            return changed;
        }

        protected override bool UpdateDerivedProperties()
        {
            if (base.UpdateDerivedProperties() && _parentGeoAstroObject != Disposable.NULL)
            {
                float isSphericalOrFlat = IsSpherical() ? 1.0f : 0.0f;
                _centerLocalPosition = MathPlus.GetLocalPointFromGeoCoordinate(_geoCenter, isSphericalOrFlat, MathPlus.DOUBLE_RADIUS, MathPlus.SIZE);
                _gridRotation = MathPlus.GetUpVectorFromGeoCoordinate(_geoCenter, isSphericalOrFlat) * QuaternionDouble.AngleAxis(GetRotation(), Vector3Double.up);
                _gridToGeoAstroObjectMatrix = Matrix4x4Double.TRS(_centerLocalPosition, _gridRotation, Vector3Double.one);
                _geoAstroObjectToGridMatrix = _gridToGeoAstroObjectMatrix.fastinverse;
                return true;
            }
            return false;
        }

        protected override bool UpdateGrid(Vector2Int gridDimensions)
        {
            if (base.UpdateGrid(gridDimensions))
            {
                bool wrap = IsSpherical();

                //Add upper and lower horizontal intersections
                int topY;
                int bottomY;
                for (topY = _centerIndexInt.y; topY >= 0; topY--)
                {
                    if (!AddRangesToRow(topY, gridDimensions, wrap))
                        break;
                }
                for (bottomY = _centerIndexInt.y + 1; bottomY <= gridDimensions.y; bottomY++)
                {
                    if (!AddRangesToRow(bottomY, gridDimensions, wrap))
                        break;
                }

                //Add corners
                Vector3Double corner = Vector3Double.zero;
                Vector3Double intersection;
                Vector3Double cornerGeoAstroObjectLocalPosition;
                Vector2Double cornerIndex;
                for (int i = 0; i < 4; i++)
                {
                    if (GetCornerFromIndex(ref corner, i, wrap ? -MathPlus.DOUBLE_RADIUS : 0.0d))
                    {
                        cornerIndex = Vector2Double.negativeInfinity;
                        cornerGeoAstroObjectLocalPosition = _gridToGeoAstroObjectMatrix.MultiplyPoint3x4(corner);
                        if (wrap)
                        {
                            if (MathGeometry.LineSphereIntersection(out intersection, MathPlus.DOUBLE_RADIUS, new RayDouble(cornerGeoAstroObjectLocalPosition, _gridRotation * Vector3Double.down)))
                                cornerIndex = MathPlus.GetSphericalIndexFromLocalPoint(intersection, gridDimensions, MathPlus.DOUBLE_RADIUS);
                            else if (MathGeometry.LineSphereIntersection(out intersection, MathPlus.DOUBLE_RADIUS, new RayDouble(cornerGeoAstroObjectLocalPosition, _gridRotation * (i > 1 ? Vector3Double.forward : Vector3Double.back))))
                                cornerIndex = MathPlus.GetSphericalIndexFromLocalPoint(intersection, gridDimensions, MathPlus.DOUBLE_RADIUS);
                            else if (MathGeometry.LineSphereIntersection(out intersection, MathPlus.DOUBLE_RADIUS, new RayDouble(cornerGeoAstroObjectLocalPosition, _gridRotation * (i > 1 ? Vector3Double.left : Vector3Double.right))))
                                cornerIndex = MathPlus.GetSphericalIndexFromLocalPoint(intersection, gridDimensions, MathPlus.DOUBLE_RADIUS);
                        }
                        else
                            cornerIndex = MathPlus.GetFlatIndexFromLocalPoint(cornerGeoAstroObjectLocalPosition, gridDimensions, MathPlus.SIZE, false);

                        if (cornerIndex != Vector2Double.negativeInfinity)
                        {
                            cornerIndex.x = MathPlus.Clamp(cornerIndex.x, 0.0d, gridDimensions.x);
                            double delta = cornerIndex.x - _centerIndex.x;
                            double wrapDeltaCW = gridDimensions.x + cornerIndex.x - _centerIndex.x;
                            double wrapDeltaCCW = cornerIndex.x - gridDimensions.x - _centerIndex.x;
                            double wrapDelta = wrap ? Math.Abs(wrapDeltaCW) < Math.Abs(wrapDeltaCCW) ? wrapDeltaCW : wrapDeltaCCW : -1.0d;
                            if ((Math.Abs(delta) < Math.Abs(wrapDelta) ? delta : wrapDelta) > 0.0d)
                                cornerIndex.x = MathPlus.ClipIndex(cornerIndex.x);
                            Row row = GetRow(Mathf.Clamp((int)Math.Floor(cornerIndex.y), topY, bottomY - 1), gridDimensions);
                            if (row != null)
                                row.AddCorner((int)cornerIndex.x);
                        }
                    }
                }

                //Add center
                IterateOverRows((row) =>
                {
                    row.AddCenter(_centerIndexInt.y == row.y ? _centerIndexInt.x : -1, wrap);
                });

                return true;
            }
            return false;
        }

        private List<Vector2Double> _intersections;
        protected virtual bool AddRangesToRow(int y, Vector2Int gridDimensions, bool isSpherical)
        {
            List<Range> newRanges = null;

            bool maxRowsReached = GetRowCount() > 200;

            if (!maxRowsReached)
            {
                float isSphericalOrFlat = isSpherical ? 1.0f : 0.0f;

                if (_intersections == null)
                    _intersections = new List<Vector2Double>();
                else
                    _intersections.Clear();

                _intersections.Add(new Vector2Double(0.0d, y));
                _intersections.Add(new Vector2Double(gridDimensions.x, y));

                Vector3Double gridYLocalPosition = MathPlus.GetLocalPointFromGeoCoordinate(MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(0.0d, y), gridDimensions), isSpherical ? 1.0f : 0.0f, MathPlus.DOUBLE_RADIUS, MathPlus.SIZE);
                if (AddEdgeIntersections(ref _intersections, gridDimensions, new Vector3Double(0.0d, 0.0d, gridYLocalPosition.z), Math.Abs(gridYLocalPosition.y), isSpherical) > 0)
                    _intersections.Sort((intersection1, intersection2) => intersection1.x.CompareTo(intersection2.x));

                double start, end;
                for (int i = 0; i < _intersections.Count - 1; i++)
                {
                    start = _intersections[i].x;
                    end = _intersections[i + 1].x;
                    if (IndexIsInside(new Vector2Double(start + ((end - start) / 2.0d), y), gridDimensions, isSphericalOrFlat, isSpherical))
                    {
                        end = MathPlus.ClipIndex(end);
                        if (newRanges == null)
                            newRanges = new List<Range>();
                        newRanges.Add(new Range((int)start, (int)end));
                    }
                }

                if (newRanges != null && newRanges.Count > 0)
                {
                    Row row = GetRow(y, gridDimensions);
                    if (row != null)
                        row.AddUpperRanges(newRanges);
                    row = GetRow(y - 1, gridDimensions);
                    if (row != null)
                        row.AddLowerRanges(newRanges);
                }
            }

            if (maxRowsReached)
                Debug.LogError("Maximum number of index rows(200) exceeded");

            return newRanges != null;
        }

        protected virtual int AddInnerIntersections(ref List<Vector2Double> intersections, Vector3Double gridYLocalPosition, bool isSpherical)
        {
            return 0;
        }

        protected virtual int AddEdgeIntersections(ref List<Vector2Double> intersections, Vector2Int gridDimensions, Vector3Double center, double circleRadius, bool isSpherical)
        {
            int added = 0;

            Vector3Double[] points;
            if (isSpherical)
            {
                if (MathGeometry.PlaneCircleIntersection(out points, _centerLocalPosition.normalized * 0.000000000000002d, _centerLocalPosition.normalized, center, circleRadius))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false);
            }

            return added;
        }

        protected int AddValidIntersections(ref List<Vector2Double> intersections, Vector2Int gridDimensions, Vector3Double[] points, bool isSpherical, bool inner = true, double intersectionThreshold = 0.0d)
        {
            int added = 0;
            if (points != null)
            {
                foreach (Vector3Double point in points)
                    added += AddValidIntersection(ref intersections, gridDimensions, point, isSpherical, intersectionThreshold);
            }
            return added;
        }

        protected int AddValidIntersection(ref List<Vector2Double> intersections, Vector2Int gridDimensions, Vector3Double point, bool isSpherical, double intersectionThreshold = 0.0d)
        {
            if (PointIsInside(point, isSpherical, intersectionThreshold))
            {
                intersections.Add(MathPlus.GetIndexFromLocalPoint(point, gridDimensions, isSpherical, MathPlus.DOUBLE_RADIUS, MathPlus.SIZE, false));
                return 1;
            }
            return 0;
        }

        private bool IndexIsInside(Vector2Double index, Vector2Int gridDimensions, float sphericalRatio, bool isSpherical, double intersectionThreshod = 0.0d)
        {
            return PointIsInside(MathPlus.GetLocalPointFromGeoCoordinate(MathPlus.GetGeoCoordinate3FromIndex(index, gridDimensions), sphericalRatio, MathPlus.DOUBLE_RADIUS, MathPlus.SIZE), isSpherical, intersectionThreshod);
        }

        protected virtual bool PointIsInside(Vector3Double point, bool isSpherical, double intersectionThreshod = 0.0d)
        {
            if (isSpherical)
                return Math.Abs(_geoAstroObjectToGridMatrix.MultiplyPoint3x4(point).y) <= MathPlus.DOUBLE_RADIUS - 0.000000000000001d;
            else
                return Math.Abs(point.x) <= MathPlus.SIZE / 2.0d;
        }
    }
}
