// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{ 
    public interface IProperty : IScriptableBehaviour
    {
        SerializableGuid id { get; }
        Action<IProperty, string, object, object> PropertyAssignedEvent { get; set; }

        bool IsDynamicProperty(int key);

        void ResetId();
    }
}
