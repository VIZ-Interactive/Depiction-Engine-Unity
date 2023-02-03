// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;

namespace DepictionEngine
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, object> _typesDefaultValue = new Dictionary<Type, object>();

        public static object Default(this Type type)
        {
            object defaultValue = null;

            if (type.IsValueType && !_typesDefaultValue.TryGetValue(type, out defaultValue))
            {
                defaultValue = Activator.CreateInstance(type);
                _typesDefaultValue[type] = defaultValue;
            }

            return defaultValue;
        }
    }
}