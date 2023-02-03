// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    /// <summary>
    /// End a foldable group of inspector serialized properties.
    /// </summary>
    public class EndFoldoutAttribute : CustomAttribute
    {
        public float space;

        public EndFoldoutAttribute(float space = 10.0f)
        {
            this.space = space;
        }
    }
}