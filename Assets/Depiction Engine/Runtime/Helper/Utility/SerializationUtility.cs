// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace DepictionEngine
{
    public class SerializationUtility
    {
#if UNITY_EDITOR
        public static bool RecoverLostReferencedObjectsInCollections<T>(IList<T> objectsList, IList<T> lastObjectsList) where T : UnityEngine.Object
        {
            bool referenceRecovered = false;

            if (RecoverLostReferencedObjectsInCollection(objectsList))
                referenceRecovered = true;
            if (RecoverLostReferencedObjectsInCollection(lastObjectsList))
                referenceRecovered = true;

            return referenceRecovered;
        }

        public static bool RecoverLostReferencedObjectsInCollection<T>(IList<T> objectsList) where T : UnityEngine.Object
        {
            bool changed = false;

            for (int i = objectsList.Count - 1; i >= 0; i--)
            {
                T value = objectsList.ElementAt(i);

                if (ProcessValue(objectsList.ElementAt(i), (newValue) => 
                {
                    objectsList[i] = newValue;

                    return true;
                }))
                    changed = true;
            }

            return changed;
        }

        public static bool RecoverLostReferencedObjectsInCollections<T, T1>(IDictionary<T, T1> objectsDictionary, IDictionary<T, T1> lastObjectsDictionary) where T1 : UnityEngine.Object
        {
            bool referenceRecovered = false;

            if (RecoverLostReferencedObjectsInCollection(objectsDictionary))
                referenceRecovered = true;
            if (RecoverLostReferencedObjectsInCollection(lastObjectsDictionary))
                referenceRecovered = true;

            return referenceRecovered;
        }

        public static bool RecoverLostReferencedObjectsInCollection<T,T1>(IDictionary<T,T1> objectsDictionary) where T1 : UnityEngine.Object
        {
            bool changed = false;

            for (int i = objectsDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T,T1> keyValuePair = objectsDictionary.ElementAt(i);

                if (ProcessValue(keyValuePair.Value, (newValue) =>
                {
                    objectsDictionary.Remove(keyValuePair.Key);
                    objectsDictionary.Add(keyValuePair.Key, newValue);

                    return true;
                }))
                    changed = true;
            }

            return changed;
        }

        public static bool RecoverLostReferencedObjectsInCollections(IDictionary<SerializableGuid, SerializableIPersistent> objectsDictionary, IDictionary<SerializableGuid, SerializableIPersistent> lastObjectsDictionary)
        {
            bool referenceRecovered = false;

            if (RecoverLostReferencedObjectsInCollection(objectsDictionary))
                referenceRecovered = true;
            if (RecoverLostReferencedObjectsInCollection(lastObjectsDictionary))
                referenceRecovered = true;

            return referenceRecovered;
        }

        public static bool RecoverLostReferencedObjectsInCollection(IDictionary<SerializableGuid, SerializableIPersistent> objectsDictionary)
        {
            bool changed = false;

            for (int i = objectsDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<SerializableGuid, SerializableIPersistent> keyValuePair = objectsDictionary.ElementAt(i);

                if (ProcessValue(keyValuePair.Value.persistent as UnityEngine.Object, (newValue) =>
                {
                    objectsDictionary.Remove(keyValuePair.Key);
                    objectsDictionary.Add(keyValuePair.Key, new SerializableIPersistent((IPersistent)newValue));

                    return true;
                }, (newValue) =>
                {
                    newValue = InstanceManager.Instance().GetPersistent(keyValuePair.Key) as UnityEngine.Object;
                    return newValue;
                }))
                    changed = true;
            }

            return changed;
        }

        private static bool ProcessValue<T>(T value, Func<T, bool> unityObjectCallback, Func<T, T> recoverLostReferencedObjectCallback = null) where T : UnityEngine.Object
        {
            bool changed = false;
            
            T oldValue = value;

            if (!RecoverLostReferencedObject(ref value) && recoverLostReferencedObjectCallback != null)
                value = recoverLostReferencedObjectCallback.Invoke(value);

            if (oldValue != value)
            {
                changed = true;
                unityObjectCallback?.Invoke(value);
            }

            return changed;
        }

        public static bool RecoverLostReferencedObject<T>(ref T unityObject) where T : UnityEngine.Object
        {
            if (!Object.ReferenceEquals(unityObject, null) && unityObject == null)
                unityObject = (T)UnityEditor.EditorUtility.InstanceIDToObject(unityObject.GetInstanceID());
            return unityObject != null;
        }
#endif

        public static void FindAddedRemovedObjects<T, T1>(IDictionary<T, T1> dictionary, IDictionary<T, T1> lastDictionary, Action<T> removedCallback = null, Action<T, T1> addedCallback = null)
        {
            //Find object that were newly destroyed
            for (int i = lastDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T, T1> objectKeyPair = lastDictionary.ElementAt(i);
                if (Disposable.IsDisposed(objectKeyPair.Value))
                    removedCallback?.Invoke(objectKeyPair.Key);
            }

            //Find object that were newly created
            for (int i = dictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T, T1> objectKeyPair = dictionary.ElementAt(i);
                if (!Disposable.IsDisposed(objectKeyPair.Value) && !lastDictionary.ContainsKey(objectKeyPair.Key))
                    addedCallback?.Invoke(objectKeyPair.Key, objectKeyPair.Value);
            }

            dictionary.Clear();
            foreach (KeyValuePair<T, T1> objectKeyPair in lastDictionary)
                dictionary.Add(objectKeyPair);
        }
    }
}
