// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class LabelOverrideAttribute : CustomAttribute
    {
        public string label;

        public LabelOverrideAttribute(string label)
        {
            this.label = label;
        }
    }
}
