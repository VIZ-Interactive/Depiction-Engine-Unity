// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public static class DoubleExtension
    {
        public static double Clamp01(this double value)
        {
            return Math.Min(Math.Max(value, 0.0d), 1.0d);
        }

        public static double Wrap(this double deg)
        {
            deg = (deg + 180.0d) % 360.0d;
            return deg + (deg > 0.0d ? -180.0d : 180.0d);
        }
    }
}
