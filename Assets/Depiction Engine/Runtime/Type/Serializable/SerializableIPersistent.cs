// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [Serializable]
    public struct SerializableIPersistent
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
    }
}
