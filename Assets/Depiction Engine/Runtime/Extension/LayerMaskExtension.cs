// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class LayerMaskExtension
    {
        public static bool Includes(this LayerMask mask, int index)
        {
            return (mask.value & LayerUtility.GetLayerMaskFromLayerIndex(index)) > 0;
        }
    }
}
