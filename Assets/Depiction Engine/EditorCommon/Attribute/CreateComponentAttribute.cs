// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Automatically adds required scripts as dependencies.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class CreateComponentAttribute : Attribute
    {
        public Type createComponent;
        public Type createComponent2;
        public Type createComponent3;
        public Type createComponent4;

        public CreateComponentAttribute(Type createComponent, Type createComponent2, Type createComponent3, Type createComponent4)
        {
            this.createComponent = createComponent;
            this.createComponent2 = createComponent2;
            this.createComponent3 = createComponent3;
            this.createComponent4 = createComponent4;
        }

        public CreateComponentAttribute(Type createComponent, Type createComponent2, Type createComponent3)
        {
            this.createComponent = createComponent;
            this.createComponent2 = createComponent2;
            this.createComponent3 = createComponent3;
        }

        public CreateComponentAttribute(Type createComponent, Type createComponent2)
        {
            this.createComponent = createComponent;
            this.createComponent2 = createComponent2;
        }

        public CreateComponentAttribute(Type createComponent)
        {
            this.createComponent = createComponent;
        }
    }
}