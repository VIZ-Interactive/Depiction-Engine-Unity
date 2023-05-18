﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public interface IElevationGrid : IGrid2DIndex
    {
        Elevation elevation { get; }
    }
}
