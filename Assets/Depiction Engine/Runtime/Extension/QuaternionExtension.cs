// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class QuaternionExtension
    {
        public static Quaternion ReflectQuaternionAroundAxes(this Quaternion rotation, Vector3 reflectAxis)
        {
            if (reflectAxis.x == -1)
                rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
            if (reflectAxis.y == -1)
                rotation = new Quaternion(rotation.y, rotation.x, rotation.w, rotation.z);
            if (reflectAxis.z == -1)
                rotation = new Quaternion(rotation.z, -rotation.w, rotation.x, -rotation.y);
            return rotation;
        }

        public static Quaternion FlipQuaternionAroundAxes(this Quaternion rotation, Vector3 flipAxis)
        {
            if (flipAxis == new Vector3(1, -1, 1) || flipAxis == new Vector3(-1, -1, 1))
                rotation = new Quaternion(rotation.y, -rotation.x, rotation.w, -rotation.z);
            if (flipAxis == new Vector3(1, 1, -1) || flipAxis == new Vector3(-1, 1, -1))
                rotation = new Quaternion(rotation.z, -rotation.w, -rotation.x, rotation.y);
            if (flipAxis == new Vector3(1, -1, -1) || flipAxis == new Vector3(-1, -1, -1))
                rotation = new Quaternion(rotation.w, rotation.z, -rotation.y, -rotation.x);
            return rotation;
        }
    }
}
