// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Expose a property to the <see cref="DepictionEngine.JsonInterface"/> and make available to be persisted in a <see cref="Datasource"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonAttribute : CustomAttribute
    {
        /// <summary>
        /// If true the property will be write only and wont be included when calling  <see cref="DepictionEngine.IJson.GetJson"/>.
        /// </summary>
        public bool get;
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string propertyName;
        /// <summary>
        /// The name of a method which returns a boolean. If it returns true the property will be included when calling <see cref="DepictionEngine.IJson.GetJson"/> otherwise it wont.
        /// </summary>
        public string conditionalMethod;

        /// <summary>
        /// Expose a property to the <see cref="DepictionEngine.JsonInterface"/> and make available to be persisted in a <see cref="DepictionEngine.Datasource"/>.
        /// </summary>
        /// <param name="get">If true the property will be write only and wont be included when calling  <see cref="DepictionEngine.IJson.GetJson"/>.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="conditionalMethod">The name of a method which returns a boolean. If it returns true the property will be included when calling <see cref="DepictionEngine.IJson.GetJson"/> otherwise it wont.</param>
        public JsonAttribute(bool get = true, string propertyName = null, string conditionalMethod = "")
        {
            this.get = get;
            this.propertyName = propertyName;
            this.conditionalMethod = conditionalMethod;
        }
    }
}