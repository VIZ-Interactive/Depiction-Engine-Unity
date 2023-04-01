// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public static class TypeExtensions
    {
        public static bool IsAssignableFrom(this Type type, Type c, bool recursive)
        {
            bool isAssignableFrom = false;

            while (!isAssignableFrom && c != null)
            {
                isAssignableFrom = type.IsAssignableFrom(c);
                c = recursive ? c.BaseType : null;
            }

            return isAssignableFrom;
        }
    }
}