// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Automatically adds required scripts as dependencies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class RequireScriptAttribute : Attribute
    {
        public Type requiredScript;
        public Type requiredScript2;
        public Type requiredScript3;

        public RequireScriptAttribute(Type requiredScript, Type requiredScript2, Type requiredScript3)
        {
            this.requiredScript = requiredScript;
            this.requiredScript2 = requiredScript2;
            this.requiredScript3 = requiredScript3;
        }

        public RequireScriptAttribute(Type requiredScript, Type requiredScript2)
        {
            this.requiredScript = requiredScript;
            this.requiredScript2 = requiredScript2;
        }

        public RequireScriptAttribute(Type requiredScript)
        {
            this.requiredScript = requiredScript;
        }
    }
}