// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class Vector3Extension
    {
        public static bool CompareTo(this Vector3 vec1, Vector3 vec2, int decimals)
        {
            return vec1.Round(decimals) == vec2.Round(decimals);
        }

        public static Vector3 Round(this Vector3 vec, int decimals = 2)
        {
            float mul = Mathf.Pow(10.0f, decimals);
            return new Vector3(Mathf.Round(vec.x * mul) / mul, Mathf.Round(vec.y * mul) / mul, Mathf.Round(vec.z * mul) / mul);
        }

        public static Vector3 Invert(this Vector3 vec)
        {
            return new Vector3(vec.x != 0.0f ? 1.0f / vec.x : 0.0f, vec.y != 0.0f ? 1.0f / vec.y : 0.0f, vec.z != 0.0f ? 1.0f / vec.z : 0.0f);
        }

        public static Vector3 Divide(this Vector3 vec, Vector3 by)
        {
            return new Vector3(vec.x / by.x, vec.y / by.y, vec.z / by.z);
        }

        public static Vector3 Absolute(this Vector3 vec)
        {
            return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        }
    }
}
