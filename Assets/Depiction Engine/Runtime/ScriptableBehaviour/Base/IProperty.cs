// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{ 
    /// <summary>
    /// Implements members required to read and write property values. 
    /// </summary>
    public interface IProperty : IScriptableBehaviour
    {
        /// <summary>
        /// The <see cref="DepictionEngine.SerializableGuid"/> of the object.
        /// </summary>
        SerializableGuid id { get; }

        /// <summary>
        /// Dispatched when a new value as been assigned to a field using the <see cref="DepictionEngine.PropertyMonoBehaviour.SetValue"/> method.
        /// </summary>
        Action<IProperty, string, object, object> PropertyAssignedEvent { get; set; }

        /// <summary>
        /// A dynamic property is a property which is expected to be modified by the engine directly. For example, physics driven <see cref="DepictionEngine.TransformDouble.localPosition"/> or <see cref="DepictionEngine.TransformDouble.localRotation"/> should be dynamic properties.
        /// </summary>
        /// <param name="key">The property key as returned by <see cref="DepictionEngine.PropertyMonoBehaviour.GetPropertyKey"/>.</param>
        /// <returns>True if the property is dynamic otherwise False</returns>
        bool IsDynamicProperty(int key);

        /// <summary>
        /// Resets the Id to <see cref="DepictionEngine.SerializableGuid.Empty"/>.
        /// </summary>
        void ResetId();
    }
}
