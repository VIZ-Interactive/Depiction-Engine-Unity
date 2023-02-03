// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public static class FloatExtension
    {
        public static float Round(this float value, int decimalPlaces = 2)
        {
            float multiplier = 1;
            for (int i = 0; i < decimalPlaces; i++)
                multiplier *= 10.0f;
            return Mathf.Round(value * multiplier) / multiplier;
        }

        public static float InverseSafe(this float f)
        {
            if (Mathf.Abs(f) > Vector3.kEpsilon)
                return 1.0f / f;
            else
                return 0.0f;
        }

        public static float Wrap(this float deg)
        {
            deg = (deg + 180.0f) % 360.0f;
            return deg + (deg > 0.0f ? -180.0f : 180.0f);
        }
    }
}
