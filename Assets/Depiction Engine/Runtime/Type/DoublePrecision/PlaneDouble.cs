// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    /// <summary>
    /// Representation of a plane in 3D space.
    /// </summary>
    public class PlaneDouble
    {
        private Vector3Double _normal;
        private double _distance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlaneDouble(Vector3Double inNormal, Vector3Double inPoint)
        {
            _normal = Vector3Double.Normalize(inNormal);
            _distance = -Vector3Double.Dot(_normal, inPoint);
        }

        /// <summary>
        /// Intersects a ray with the plane.
        /// </summary>
        /// <remarks>This function sets enter to the distance along the ray, where it intersects the plane. If the ray is parallel to the plane, function returns false and sets enter to zero. If the ray is pointing in the opposite direction than the plane, function returns false and sets enter to the distance along the ray (negative value).</remarks>
        /// <param name="ray"></param>
        /// <param name="enter"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Raycast(RayDouble ray, out double enter)
        {
            double vdot = Vector3Double.Dot(ray.direction, _normal);
            double ndot = -Vector3Double.Dot(ray.origin, _normal) - _distance;

            if (vdot <= double.Epsilon && vdot >= -double.Epsilon)
            {
                enter = 0.0d;
                return false;
            }

            enter = ndot / vdot;

            return enter > 0.0d;
        }

    }
}
