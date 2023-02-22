// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    /// <summary>
    /// Implements basic Grid members.
    /// </summary>
    public interface IGrid
    {
        bool wasFirstUpdated { get; }

        bool UpdateGrid();
    }
}
