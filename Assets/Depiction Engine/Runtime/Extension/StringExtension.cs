// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public static class StringExtension
    {
        public static string ToCamelCase(this string text)
        {
            if (text != null && text.Length > 0)
                return Char.ToUpperInvariant(text[0]) + text.Substring(1);
            return text;
        }
    }
}
