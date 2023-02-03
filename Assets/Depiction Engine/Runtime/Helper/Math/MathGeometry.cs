// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    /// <summary>
    /// Math Geometry helper methods.
    /// </summary>
    public class MathGeometry
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineSegmentsIntersection(out Vector2Double intersection, Vector2Double point1, Vector2Double point2, Vector2Double point3, Vector2Double point4)
        {
            intersection = Vector2Double.zero;

            var d = (point2.x - point1.x) * (point4.y - point3.y) - (point2.y - point1.y) * (point4.x - point3.x);

            if (d == 0.0d)
            {
                return false;
            }

            var u = ((point3.x - point1.x) * (point4.y - point3.y) - (point3.y - point1.y) * (point4.x - point3.x)) / d;
            var v = ((point3.x - point1.x) * (point2.y - point1.y) - (point3.y - point1.y) * (point2.x - point1.x)) / d;

            if (u < 0.0d || u > 1.0d || v < 0.0d || v > 1.0d)
            {
                return false;
            }

            intersection.x = point1.x + u * (point2.x - point1.x);
            intersection.y = point1.y + u * (point2.y - point1.y);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CylinderCircleIntersection(out Vector3Double[] intersections, Vector3Double cylinderNormal, double cylinderRadius, double geoAstroObjectRadius, double circleRadius, Vector3Double circleCenter)
        {
            double distanceFromCenter = Math.Sqrt((geoAstroObjectRadius * geoAstroObjectRadius) - (cylinderRadius * cylinderRadius));
            if (PlaneCircleIntersection(out intersections, !double.IsNaN(distanceFromCenter) ? cylinderNormal * distanceFromCenter : Vector3Double.zero, cylinderNormal, circleCenter, circleRadius))
                return true;
            else
            {
                intersections = null;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CylinderLineIntersection(out Vector3Double[] intersections, Vector3Double cylinderCenter, Vector3Double cylinderNormal, double cylinderRadius, Vector3Double lineOrigin, Vector3Double lineDirection)
        {
            Vector3Double oc = lineOrigin - cylinderCenter;
            double card = Vector3Double.Dot(cylinderNormal, lineDirection);
            double caoc = Vector3Double.Dot(cylinderNormal, oc);
            double a = 1.0d - card * card;
            double b = Vector3Double.Dot(oc, lineDirection) - caoc * card;
            double c = Vector3Double.Dot(oc, oc) - caoc * caoc - cylinderRadius * cylinderRadius;
            double h = b * b - a * c;
            if (h < 0.0d)
            {
                intersections = null;
                return false;
            }
            h = Math.Sqrt(h);
            intersections = new Vector3Double[] { lineOrigin + (lineDirection * ((-b - h) / a)), lineOrigin + (lineDirection * ((-b + h) / a)) };
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LinePlaneIntersection(out Vector3Double intersection, Vector3Double planeCenter, Vector3Double planeNormal, RayDouble ray, bool castLineBothDirection = true)
        {
            PlaneDouble plane = new PlaneDouble(planeNormal, planeCenter);
            double enter;
            if (plane.Raycast(ray, out enter))
            {
                intersection = ray.origin + (ray.direction * enter);
                return true;
            }
            if (castLineBothDirection)
            {
                ray.direction = -ray.direction;
                if (plane.Raycast(ray, out enter))
                {
                    intersection = ray.origin + (ray.direction * enter);
                    return true;
                }
            }
            intersection = Vector3Double.negativeInfinity;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineSphereIntersection(out Vector3Double intersection, double radius, RayDouble ray)
        {
            Vector3Double oc = ray.origin;
            double a = Vector3Double.Dot(ray.direction, ray.direction);
            double b = 2.0 * Vector3Double.Dot(oc, ray.direction);
            double c = Vector3Double.Dot(oc, oc) - radius * radius;
            double discriminant = b * b - 4 * a * c;
            if (discriminant >= 0)
            {
                intersection = ray.origin + (ray.direction * (-b - Math.Sqrt(discriminant)) / (2.0 * a));
                return true;
            }
            intersection = Vector3Double.negativeInfinity;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PlaneCircleIntersection(out Vector3Double[] intersections, Vector3Double planeCenter, Vector3Double planeNormal, Vector3Double circleCenter, double circleRadius)
        {
            double tolerance = 0.000000000000000001d;

            Vector3Double circleNormal = Vector3Double.forward;

            //We can get the direction of the line of intersection of the two planes by calculating the
            //cross product of the normals of the two planes. Note that this is just a direction and the line
            //is not fixed in space yet.
            Vector3Double lineVec = Vector3Double.Cross(circleNormal, planeNormal);

            //Next is to calculate a point on the line to fix it's position. This is done by finding a vector from
            //the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
            //errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
            //the cross product of the normal of plane2 and the lineDirection.      
            Vector3Double ldir = Vector3Double.Cross(planeNormal, lineVec);

            double numerator = Vector3Double.Dot(circleNormal, ldir);

            intersections = null;
            //Prevent divide by zero.
            if (Math.Abs(numerator) > tolerance)
            {
                double t2 = Vector3Double.Dot(circleNormal, circleCenter - planeCenter) / numerator;
                Vector3Double point1 = planeCenter + ldir * t2;
                Vector3Double point2 = point1 + lineVec;

                double dx = point2.x - point1.x;
                double dy = point2.y - point1.y;

                double a = dx * dx + dy * dy;
                double b = 2.0d * (dx * (point1.x - circleCenter.x) + dy * (point1.y - circleCenter.y));
                double c = (point1.x - circleCenter.x) * (point1.x - circleCenter.x) + (point1.y - circleCenter.y) * (point1.y - circleCenter.y) - circleRadius * circleRadius;

                double determinate = b * b - 4.0d * a * c;
                double t;

                if ((a <= tolerance) || (determinate < -tolerance))
                {
                    intersections = null;
                    return false;
                }
                else if (determinate < tolerance && determinate > -tolerance)
                {
                    t = -b / (2.0d * a);
                    Vector3Double result1 = new Vector3Double(point1.x + t * dx, point1.y + t * dy, point1.z);
                    Vector3Double result2 = result1;
                    result1.x -= 0.0000000001d;
                    result2.x += 0.0000000001d;
                    intersections = new Vector3Double[] { result1, result2 };
                    return true;
                }
                else
                {
                    t = (-b + Math.Sqrt(determinate)) / (2.0d * a);
                    Vector3Double result1 = new Vector3Double(point1.x + t * dx, point1.y + t * dy, point1.z);
                    t = (-b - Math.Sqrt(determinate)) / (2.0d * a);
                    Vector3Double result2 = new Vector3Double(point1.x + t * dx, point1.y + t * dy, point1.z);
                    intersections = new Vector3Double[] { result1, result2 };
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineCircleIntersection(out Vector2Double intersection1, double radius, Vector2Double point1, Vector2Double point2)
        {
            if (IsIntersecting(radius, point1, point2))
            {
                //Calculate terms of the linear and quadratic equations
                var M = (point2.y - point1.y) / (point2.x - point1.x);
                var B = point1.y - M * point1.x;
                var a = 1 + M * M;
                var b = 2 * (M * B - M);
                var c = B * B - radius * radius - 2 * B;
                // solve quadratic equation
                var sqRtTerm = Math.Sqrt(b * b - 4 * a * c);
                var x = ((-b) + sqRtTerm) / (2 * a);
                // make sure we have the correct root for our line segment
                if ((x < Math.Min(point1.x, point2.x) ||
                    (x > Math.Max(point1.x, point2.x))))
                { x = ((-b) - sqRtTerm) / (2 * a); }
                //solve for the y-component
                var y = M * x + B;
                // Intersection Calculated
                intersection1 = new Vector2Double(x, y);
                return true;
            }
            else
            {
                // Line segment does not intersect at one point.  It is either 
                // fully outside, fully inside, intersects at two points, is 
                // tangential to, or one or more points is exactly on the 
                // circle radius.
                intersection1 = Vector2Double.zero;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LineCircleIntersections(out Vector2Double intersection1, out Vector2Double intersection2, double radius, Vector2Double point1, Vector2Double point2)
        {
            // Precalculate this value. We use it often
            Vector2Double dir = point2 - point1;

            double a = dir.x * dir.x + dir.y * dir.y;
            double b = 2.0d * ((dir.x * point1.x) + (dir.y * point1.y));

            double c = (point1.x * point1.x) + (point1.y * point1.y) - (radius * radius);

            double delta = b * b - (4.0d * a * c);

            intersection1 = intersection2 = Vector2Double.zero;

            if (delta < 0.0d) // No intersection
            {
                return;
            }
            else if (delta == 0.0d) // One intersection
            {
                double u = -b / (2.0d * a);

                intersection1 = point1 + (dir * u);
                return;
                /* Use LineP1 instead of LocalP1 because we want our answer in global
                space, not the circle's local space */
            }
            else if (delta > 0.0d) // Two intersections
            {
                double SquareRootDelta = Math.Sqrt(delta);

                double u1 = (-b + SquareRootDelta) / (2.0d * a);

                double u2 = (-b - SquareRootDelta) / (2.0d * a);

                intersection1 = point1 + (dir * u1);
                intersection2 = point1 + (dir * u2);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetCircleToCircleIntersections(out Vector2Double intersection1, out Vector2Double intersection2, double radius1, Vector2Double circleCenter, double radius2)
        {
            double a, dx, dy, d, h, rx, ry;
            double x2, y2;

            /* dx and dy are the vertical and horizontal distances between
             * the circle centers.
             */
            dx = circleCenter.x;
            dy = circleCenter.y;

            /* Determine the straight-line distance between the centers. */
            d = Math.Sqrt((dy * dy) + (dx * dx));

            /* Check for solvability. */
            if (d > (radius1 + radius2))
            {
                /* no solution. circles do not intersect. */
                intersection1 = Vector2Double.zero;
                intersection2 = Vector2Double.zero;
                return;
            }
            if (d < Math.Abs(radius1 - radius2))
            {
                /* no solution. one circle is contained in the other */
                intersection1 = Vector2Double.negativeInfinity;
                intersection2 = Vector2Double.positiveInfinity;
                return;
            }

            /* 'point 2' is the point where the line through the circle
             * intersection points crosses the line between the circle
             * centers.  
             */

            /* Determine the distance from point 0 to point 2. */
            a = ((radius1 * radius1) - (radius2 * radius2) + (d * d)) / (2.0 * d);

            /* Determine the coordinates of point 2. */
            x2 = dx * a / d;
            y2 = dy * a / d;

            /* Determine the distance from point 2 to either of the
             * intersection points.
             */
            h = Math.Sqrt((radius1 * radius1) - (a * a));

            /* Now determine the offsets of the intersection points from
             * point 2.
             */
            rx = -dy * (h / d);
            ry = dx * (h / d);

            /* Determine the absolute intersection points. */
            double xi = x2 + rx;
            double xi_prime = x2 - rx;
            double yi = y2 + ry;
            double yi_prime = y2 - ry;

            intersection1 = new Vector2Double(xi, yi);
            intersection2 = new Vector2Double(xi_prime, yi_prime);
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIntersecting(double radius, Vector2Double point1, Vector2Double point2)
        {
            return IsInsideCircle(radius, point1) ^ IsInsideCircle(radius, point2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInsideCircle(double radius, Vector2Double checkPoint)
        {
            return Math.Sqrt(Math.Pow(checkPoint.x, 2.0d) + Math.Pow(checkPoint.y, 2.0d)) < radius;
        }
    }
}
