// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Reflection;

namespace DepictionEngine
{
    /// <summary>
    /// Supports reading or writing properties in Json format.
    /// </summary>
    public interface IJson : IProperty
    {
#if UNITY_EDITOR
        bool PasteComponentAllowed();
#endif
    }
}
