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

#if UNITY_EDITOR
        /// <summary>
        /// Resets all serialized fields to their default value.
        /// </summary>
        void InspectorReset();
#endif

        /// <summary>
        /// Resets the Id to <see cref="DepictionEngine.SerializableGuid.Empty"/>.
        /// </summary>
        void ResetId();
    }
}
