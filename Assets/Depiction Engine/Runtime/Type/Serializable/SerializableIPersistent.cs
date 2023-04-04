// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using JetBrains.Annotations;
using System;
using UnityEngine;

namespace DepictionEngine
{
    [Serializable]
    public struct SerializableIPersistent : IEquatable<SerializableIPersistent>
    {
        [SerializeField]
        private PersistentMonoBehaviour _persistentMonoBehaviour;
        [SerializeField]
        private PersistentScriptableObject _persistentScriptableObject;

        public SerializableIPersistent(IPersistent persistent)
        {
            _persistentScriptableObject = null;
            _persistentMonoBehaviour = null;
            if (persistent is PersistentMonoBehaviour)
                _persistentMonoBehaviour = (PersistentMonoBehaviour)persistent;
            if (persistent is PersistentScriptableObject)
                _persistentScriptableObject = (PersistentScriptableObject)persistent;
        }

        public IPersistent persistent
        {
            get => !Object.ReferenceEquals(_persistentMonoBehaviour, null) ? _persistentMonoBehaviour : _persistentScriptableObject;
        }

        public static implicit operator UnityEngine.Object(SerializableIPersistent d) => (UnityEngine.Object)d.persistent;
        public static explicit operator SerializableIPersistent(UnityEngine.Object b) => new SerializableIPersistent(b is IPersistent ? (IPersistent)b : null);

        public bool Equals(IPersistent obj)
        {
            return !Object.ReferenceEquals(persistent, null) ? persistent.Equals(obj) : Object.ReferenceEquals(obj, null);
        }

        public bool Equals(SerializableIPersistent obj)
        {
            return Equals(obj.persistent);
        }

        public override bool Equals(object obj)
        {
            return !Object.ReferenceEquals(persistent, null) ? persistent.Equals(obj) : Object.ReferenceEquals(obj, null);
        }

        public static bool operator ==(SerializableIPersistent lhs, object rhs)
        {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SerializableIPersistent lhs, object rhs) => !(lhs == rhs);

        public override int GetHashCode() => persistent.GetHashCode();
    }
}
