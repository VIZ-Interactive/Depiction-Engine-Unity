// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Used to modify the properties of a <see cref="IScriptableBehaviour"/> object.
    /// </summary>
    [Serializable]
    public class PropertyModifier : Disposable
    {
        /// <summary>
        /// Modifies the properties of the injected object.
        /// </summary>
        /// <param name="scriptableBehaviour">The object to be modified.</param>
        public virtual void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            
        }
    }
}
