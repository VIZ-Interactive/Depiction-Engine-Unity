// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Supports having children.
    /// </summary>
    interface IHasChildren
    {
        int childCount { get; }

        void IterateOverChildren<T>(Func<T, bool> callback) where T : PropertyMonoBehaviour;
    }
}
