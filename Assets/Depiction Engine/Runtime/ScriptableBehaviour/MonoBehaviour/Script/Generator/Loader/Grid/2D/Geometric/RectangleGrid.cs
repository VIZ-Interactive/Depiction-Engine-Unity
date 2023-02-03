// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;

namespace DepictionEngine
{
    public class RectangleGrid : GeometricGrid2D
    {
        private static readonly double RECTANGLE_INTERSECTION_THRESHOLD = 0.0000000000001d;

        private double _rotation;
        private double _scaledHalfWidth;
        private double _scaledHalfHeight;

        private Vector3Double _topCenter;
        private Vector3Double _rightCenter;
        private Vector3Double _bottomCenter;
        private Vector3Double _leftCenter;

        private Vector3Double _edgeTopCenterNormal;
        private Vector3Double _edgeRightCenterNormal;
        private Vector3Double _edgeBottomCenterNormal;
        private Vector3Double _edgeLeftCenterNormal;

        public bool UpdateRectangleGridProperties(Vector2Int gridDimensions, GeoCoordinate2Double geoCenter, GeoAstroObject parentGeoAstroObject, double rotation, double width, double height, bool updateDerivedProperties = true)
        {
            bool changed = UpdateGeometricGridFields(gridDimensions, geoCenter, parentGeoAstroObject, false);

            if (_rotation != rotation)
            {
                _rotation = rotation;
                changed = true;
            }

            double newHalfWidth = Math.Max(width * _scaleFactor, MIN_SIZE) / 2.0d;
            if (_scaledHalfWidth != newHalfWidth)
            {
                _scaledHalfWidth = newHalfWidth;
                changed = true;
            }

            double newHalfHeight = Math.Max(height * _scaleFactor, MIN_SIZE) / 2.0d;
            if (_scaledHalfHeight != newHalfHeight)
            {
                _scaledHalfHeight = newHalfHeight;
                changed = true;
            }

            if (updateDerivedProperties && (!wasFirstUpdated || changed))
                UpdateDerivedProperties();

            return changed;
        }

        private Vector3Double _edgeTop = Vector3Double.forward;
        private Vector3Double _edgeRight = QuaternionDouble.AngleAxis(90.0d, Vector3Double.up) * Vector3Double.forward;
        private Vector3Double _edgeBottom = QuaternionDouble.AngleAxis(180.0d, Vector3Double.up) * Vector3Double.forward;
        private Vector3Double _edgeLeft = QuaternionDouble.AngleAxis(-90.0d, Vector3Double.up) * Vector3Double.forward;
        protected override bool UpdateDerivedProperties()
        {
            if (base.UpdateDerivedProperties())
            {
                _topCenter = _gridToGeoAstroObjectMatrix.MultiplyPoint3x4(new Vector3Double(0.0d, 0.0d, _scaledHalfHeight));
                _rightCenter = _gridToGeoAstroObjectMatrix.MultiplyPoint3x4(new Vector3Double(_scaledHalfWidth, 0.0d, 0.0d));
                _bottomCenter = _gridToGeoAstroObjectMatrix.MultiplyPoint3x4(new Vector3Double(0.0d, 0.0d, -_scaledHalfHeight));
                _leftCenter = _gridToGeoAstroObjectMatrix.MultiplyPoint3x4(new Vector3Double(-_scaledHalfWidth, 0.0d, 0.0d));

                _edgeTopCenterNormal = _gridRotation * _edgeTop;
                _edgeRightCenterNormal = _gridRotation * _edgeRight;
                _edgeBottomCenterNormal = _gridRotation * _edgeBottom;
                _edgeLeftCenterNormal = _gridRotation * _edgeLeft;
                
                return true;
            }

            return false;
        }

        protected override double GetRotation()
        {
            return _rotation;
        }

        protected override bool GetCornerFromIndex(ref Vector3Double corner, int i, double depthOffset = 0.0d)
        {
            if (i >= 0 && i < 4)
            {
                if (i == 0)
                    corner = new Vector3Double(-_scaledHalfWidth, depthOffset, _scaledHalfHeight);
                if (i == 1)
                    corner = new Vector3Double(_scaledHalfWidth, depthOffset, _scaledHalfHeight);
                if (i == 2)
                    corner = new Vector3Double(_scaledHalfWidth, depthOffset, -_scaledHalfHeight);
                if (i == 3)
                    corner = new Vector3Double(-_scaledHalfWidth, depthOffset, -_scaledHalfHeight);
                
                return true;
            }

            return base.GetCornerFromIndex(ref corner, i, depthOffset);
        }

        protected override int AddEdgeIntersections(ref List<Vector2Double> intersections, Vector2Int gridDimensions, Vector3Double center, double circleRadius, bool isSpherical)
        {
            int added = base.AddEdgeIntersections(ref intersections, gridDimensions, center, circleRadius, isSpherical);

            double intersectionThreshold = RECTANGLE_INTERSECTION_THRESHOLD;

            if (isSpherical)
            {
                Vector3Double[] points;
                if (MathGeometry.PlaneCircleIntersection(out points, _topCenter, _edgeTopCenterNormal, center, circleRadius))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false, intersectionThreshold);
                if (MathGeometry.PlaneCircleIntersection(out points, _rightCenter, _edgeRightCenterNormal, center, circleRadius))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false, intersectionThreshold);
                if (MathGeometry.PlaneCircleIntersection(out points, _bottomCenter, _edgeBottomCenterNormal, center, circleRadius))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false, intersectionThreshold);
                if (MathGeometry.PlaneCircleIntersection(out points, _leftCenter, _edgeLeftCenterNormal, center, circleRadius))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false, intersectionThreshold);
            }
            else
            {
                Vector3Double point;
                RayDouble ray = new RayDouble(center, Vector3Double.right);
                if (MathGeometry.LinePlaneIntersection(out point, _topCenter, _edgeTopCenterNormal, ray))
                    added += AddValidIntersection(ref intersections, gridDimensions, point, false, intersectionThreshold);
                if (MathGeometry.LinePlaneIntersection(out point, _rightCenter, _edgeRightCenterNormal, ray))
                    added += AddValidIntersection(ref intersections, gridDimensions, point, false, intersectionThreshold);
                if (MathGeometry.LinePlaneIntersection(out point, _bottomCenter, _edgeBottomCenterNormal, ray))
                    added += AddValidIntersection(ref intersections, gridDimensions, point, false, intersectionThreshold);
                if (MathGeometry.LinePlaneIntersection(out point, _leftCenter, _edgeLeftCenterNormal, ray))
                    added += AddValidIntersection(ref intersections, gridDimensions, point, false, intersectionThreshold);
            }

            return added;
        }

        protected override bool PointIsInside(Vector3Double point, bool isSpherical, double intersectionThreshod = 0.0d)
        {
            if (base.PointIsInside(point, isSpherical, intersectionThreshod))
            {
                Vector3Double gridLocalPoint = _geoAstroObjectToGridMatrix.MultiplyPoint3x4(point);
                return Math.Abs(gridLocalPoint.x) <= _scaledHalfWidth + intersectionThreshod && Math.Abs(gridLocalPoint.z) <= _scaledHalfHeight + intersectionThreshod;
            }
            return false;
        }
    }
}
