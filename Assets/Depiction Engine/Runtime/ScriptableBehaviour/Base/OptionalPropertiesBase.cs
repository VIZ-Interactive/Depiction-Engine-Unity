// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class OptionalPropertiesBase : ScriptableObject
    {
        [HideInInspector]
        public UnityEngine.Object parent;
    }
}
