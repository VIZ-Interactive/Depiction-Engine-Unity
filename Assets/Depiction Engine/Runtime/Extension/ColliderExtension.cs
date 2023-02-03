// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class ColliderExtension
    {
        public static bool Raycast(this Collider collider, RayDouble ray, out RaycastHitDouble hitInfo, float maxDistance)
        {
            hitInfo = null;

            RaycastHit hitSingle;
            if (collider.Raycast(ray, out hitSingle, maxDistance))
            {
                hitInfo = hitSingle;
                return true;
            }

            return false;
        }
    }
}
