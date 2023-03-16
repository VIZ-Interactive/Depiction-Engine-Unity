// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;

namespace DepictionEngine
{
    /// <summary>
    /// Implements methods to identify which components a GameObject should have.
    /// </summary>
    public interface IRequiresComponents
    {
        void GetRequiredComponentTypes(ref List<Type> types);
    }
}
