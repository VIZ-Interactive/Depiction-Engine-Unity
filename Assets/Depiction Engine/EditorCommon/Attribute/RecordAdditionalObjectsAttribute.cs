// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RecordAdditionalObjectsAttribute : CustomAttribute
    {
        public string methodName;

        public RecordAdditionalObjectsAttribute(string methodName)
        {
            this.methodName = methodName;
        }
    }
}