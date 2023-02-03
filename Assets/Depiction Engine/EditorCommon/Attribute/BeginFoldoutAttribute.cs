// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    /// <summary>
    /// Begin a foldable group of inspector serialized properties.
    /// </summary>
    public class BeginFoldoutAttribute : CustomAttribute
    {
        public string label;

        public bool foldoutCreated;
        public string serializedPropertyPath;

        public BeginFoldoutAttribute(string label)
        {
            this.label = label;
        }
    }
}