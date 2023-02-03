// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class BeginHorizontalGroupAttribute : CustomAttribute
    {
        public bool hideLabels;

        public bool horizontalGroupCreated;

        public BeginHorizontalGroupAttribute(bool hideLabels = false)
        {
            this.hideLabels = hideLabels;
        }
    }
}