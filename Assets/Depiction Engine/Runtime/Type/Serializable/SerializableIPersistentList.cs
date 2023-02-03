// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    [Serializable]
    public class SerializableIPersistentList : IEnumerable
    {
        [SerializeField]
        private List<PersistentMonoBehaviour> _persistentMonoBehaviours;
        [SerializeField]
        private List<PersistentScriptableObject> _persistentScriptableObjects;

        public SerializableIPersistentList()
        {
            _persistentMonoBehaviours = new List<PersistentMonoBehaviour>();
            _persistentScriptableObjects = new List<PersistentScriptableObject>();
        }

        public int Count
        {
            get { return _persistentMonoBehaviours.Count + _persistentScriptableObjects.Count; }
        }

        public void Add(IPersistent item)
        {
            if (item is PersistentMonoBehaviour)
                _persistentMonoBehaviours.Add(item as PersistentMonoBehaviour);
            if (item is PersistentScriptableObject)
                _persistentScriptableObjects.Add(item as PersistentScriptableObject);
        }

        public bool Remove(IPersistent item)
        {
            if (item is PersistentMonoBehaviour)
                return _persistentMonoBehaviours.Remove(item as PersistentMonoBehaviour);
            if (item is PersistentScriptableObject)
                return _persistentScriptableObjects.Remove(item as PersistentScriptableObject);
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < _persistentMonoBehaviours.Count)
                _persistentMonoBehaviours.RemoveAt(index);
            else
                _persistentScriptableObjects.RemoveAt(index - _persistentMonoBehaviours.Count);
        }

        public IPersistent this[int i]
        {
            get { return i < _persistentMonoBehaviours.Count ? _persistentMonoBehaviours[i] : _persistentScriptableObjects[i - _persistentMonoBehaviours.Count]; }
        }

        public void Clear()
        {
            _persistentMonoBehaviours.Clear();
            _persistentScriptableObjects.Clear();
        }

        public bool Contains(IPersistent item)
        {
            if (item is PersistentMonoBehaviour)
                return _persistentMonoBehaviours.Contains(item);
            if (item is PersistentScriptableObject)
                return _persistentScriptableObjects.Contains(item);
            return false;
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < _persistentMonoBehaviours.Count; ++i)
                yield return _persistentMonoBehaviours[i];
            for (int i = 0; i < _persistentScriptableObjects.Count; ++i)
                yield return _persistentMonoBehaviours[i];
        }
    }
}
