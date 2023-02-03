// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class ConditionalAttribute : CustomAttribute
    {
        public string methodName;

        public ConditionalAttribute(string conditionalMethod) 
        {
            this.methodName = conditionalMethod;
        }
    }
}