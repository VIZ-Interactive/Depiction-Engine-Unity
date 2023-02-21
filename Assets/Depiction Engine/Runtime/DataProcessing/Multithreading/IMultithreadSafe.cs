// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    /// <summary>
    /// Can be safely passed as a parameter to <see cref="Processor"/> for asynchronous processing.
    /// </summary>
    public interface IMultithreadSafe
    {
        bool locked { get; set; }
    }
}
