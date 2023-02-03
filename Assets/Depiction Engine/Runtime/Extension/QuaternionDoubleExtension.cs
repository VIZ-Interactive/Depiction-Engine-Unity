// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class QuaternionDoubleExtension
    {
        public static QuaternionDouble ReflectQuaternionAroundAxes(this QuaternionDouble rotation, Vector3Double reflectAxis)
        {
            if (reflectAxis.x == -1.0d)
                rotation = new QuaternionDouble(rotation.x, -rotation.y, -rotation.z, rotation.w);
            if (reflectAxis.y == -1.0d)
                rotation = new QuaternionDouble(rotation.y, rotation.x, rotation.w, rotation.z);
            if (reflectAxis.z == -1.0d)
                rotation = new QuaternionDouble(rotation.z, -rotation.w, rotation.x, -rotation.y);
            return rotation;
        }

        public static QuaternionDouble FlipQuaternionAroundAxes(this QuaternionDouble rotation, Vector3Double flipAxis)
        {
            if (flipAxis == new Vector3(1, -1, 1) || flipAxis == new Vector3(-1, -1, 1))
                rotation = new QuaternionDouble(rotation.y, -rotation.x, rotation.w, -rotation.z);
            if (flipAxis == new Vector3(1, 1, -1) || flipAxis == new Vector3(-1, 1, -1))
                rotation = new QuaternionDouble(rotation.z, -rotation.w, -rotation.x, rotation.y);
            if (flipAxis == new Vector3(1, -1, -1) || flipAxis == new Vector3(-1, -1, -1))
                rotation = new QuaternionDouble(rotation.w, rotation.z, -rotation.y, -rotation.x);
            return rotation;
        }
    }
}
