// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public static class Vector3DoubleExtension
    {
        public static bool CompareTo(this Vector3Double vec1, Vector3Double vec2, int decimals)
        {
            return vec1.Round(decimals) == vec2.Round(decimals);
        }

        public static Vector3Double Round(this Vector3Double vec, int decimals = 2)
        {
            double mul = Math.Pow(10.0d, decimals);
            return new Vector3Double(Math.Round(vec.x * mul) / mul, Math.Round(vec.y * mul) / mul, Math.Round(vec.z * mul) / mul);
        }

        public static Vector3Double Invert(this Vector3Double vec)
        {
            return new Vector3Double(vec.x != 0.0d ? 1.0d / vec.x : 0.0d, vec.y != 0.0d ? 1.0d / vec.y : 0.0d, vec.z != 0.0d ? 1.0d / vec.z : 0.0d);
        }

        public static Vector3Double Divide(this Vector3Double vec, Vector3Double by)
        {
            return new Vector3Double(vec.x / by.x, vec.y / by.y, vec.z / by.z);
        }

        public static Vector3Double Absolute(this Vector3Double vec)
        {
            return new Vector3Double(Math.Abs(vec.x), Math.Abs(vec.y), Math.Abs(vec.z));
        }

        public static bool IsUniform(this Vector3Double vec)
        {
            return MathPlus.Approximately(vec.x, vec.y) && MathPlus.Approximately(vec.x, vec.z) && MathPlus.Approximately(vec.y, vec.z);
        }
    }
}
