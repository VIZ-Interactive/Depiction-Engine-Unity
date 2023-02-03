// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the manipulation of strings.
    /// </summary>
    public class StringUtility
    {
        /// <summary>
        /// Returns a new line compatible with the targeted build platform.
        /// </summary>
        public static string NewLine
        {
            get
            {
                string newLine = Environment.NewLine;

#if UNITY_EDITOR_WIN
                if (newLine == "\r\n")
                    newLine = "\n";
#endif

                return newLine;
            }
        }
    }
}
