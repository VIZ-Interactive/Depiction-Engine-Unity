// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;

namespace DepictionEngine
{
	public static class PhysicsManager
	{
        public static IEnumerable<TransformDouble> physicTransforms => _physicObjects != null ? _physicObjects.Values : null;

        private static Dictionary<SerializableGuid, TransformDouble> _physicObjects;

        public static void RemovePhysicObject(SerializableGuid id)
        {
            _physicObjects?.Remove(id);
        }

        public static void AddPhysicObject(SerializableGuid id, TransformDouble transform)
		{
            if (transform != Disposable.NULL)
            {
                _physicObjects ??= new();
                _physicObjects.TryAdd(id, transform);
            }
		}
    }
}