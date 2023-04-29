// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the manipulation of colors.
    /// </summary>
    public class ColorUtility
    {
        /// <summary>
        /// Convert string to color.
        /// </summary>
        /// <param name="color">A color value.</param>
        /// <param name="colorStr">A string representing a color.</param>
        /// <returns>True if the conversion was successful.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ColorFromString(out Color color, string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr))
            {
                color = Color.clear;
                return false;
            }

            if (colorStr.Contains("#"))
                return UnityEngine.ColorUtility.TryParseHtmlString(colorStr, out color);
            else
            {
                color = UnityEngine.JsonUtility.FromJson<Color>(colorStr);
                return true;
            }
        }
    }
}
