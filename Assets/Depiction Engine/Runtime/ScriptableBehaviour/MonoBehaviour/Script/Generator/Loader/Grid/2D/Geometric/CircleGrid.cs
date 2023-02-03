// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;

namespace DepictionEngine
{
    public class CircleGrid : GeometricGrid2D
    {
        private static readonly double CIRCLE_INTERSECTION_THRESHOLD = 0.000000002d;

        private double _scaledRadius;

        public bool UpdateCircleGridProperties(Vector2Int gridDimensions, GeoCoordinate2Double geoCenter, GeoAstroObject parentGeoAstroObject, double radius, bool updateDerivedProperties = true)
        {
            bool changed = UpdateGeometricGridFields(gridDimensions, geoCenter, parentGeoAstroObject, false);

            double newScaledRadius = Math.Max(radius * _scaleFactor, MIN_SIZE);
            if (_scaledRadius != newScaledRadius)
            {
                _scaledRadius = newScaledRadius;
                changed = true;
            }

            if (updateDerivedProperties && changed)
                UpdateDerivedProperties();

            return changed;
        }

        protected override bool GetCornerFromIndex(ref Vector3Double corner, int i, double depthOffset = 0.0d)
        {
            if (i > 0 && i < 4)
            {
                if (i == 0)
                    corner = new Vector3Double(0.0d, depthOffset, _scaledRadius);
                if (i == 1)
                    corner = new Vector3Double(_scaledRadius, depthOffset, 0.0d);
                if (i == 2)
                    corner = new Vector3Double(0.0d, depthOffset, -_scaledRadius);
                if (i == 3)
                    corner = new Vector3Double(-_scaledRadius, depthOffset, 0.0d);
                
                return true;
            }
            
            return base.GetCornerFromIndex(ref corner, i, depthOffset);
        }

        protected override int AddEdgeIntersections(ref List<Vector2Double> intersections, Vector2Int gridDimensions, Vector3Double center, double circleRadius, bool isSpherical)
        {
            int added = base.AddEdgeIntersections(ref intersections, gridDimensions, center, circleRadius, isSpherical);

            Vector3Double[] points;
            if (isSpherical)
            {
                if (MathGeometry.CylinderCircleIntersection(out points, _centerLocalPosition.normalized, _scaledRadius, MathPlus.DOUBLE_RADIUS, circleRadius, center))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, true, false, CIRCLE_INTERSECTION_THRESHOLD);
            }
            else
            {
                center.x = -MathPlus.SIZE / 2.0d;
                if (MathGeometry.CylinderLineIntersection(out points, _centerLocalPosition, Vector3Double.up, _scaledRadius, center, Vector3Double.right))
                    added += AddValidIntersections(ref intersections, gridDimensions, points, false, false, CIRCLE_INTERSECTION_THRESHOLD);
            }

            return added;
        }

        protected override bool PointIsInside(Vector3Double point, bool isSpherical, double intersectionThreshod = 0)
        {
            if (base.PointIsInside(point, isSpherical, intersectionThreshod))
            {
                Vector3Double gridLocalPoint = _geoAstroObjectToGridMatrix.MultiplyPoint3x4(point);
                gridLocalPoint.y = 0.0d;
                
                return gridLocalPoint.magnitude <= _scaledRadius + intersectionThreshod;
            }
            
            return false;
        }
    }
}
