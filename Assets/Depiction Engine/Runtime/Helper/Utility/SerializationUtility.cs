// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace DepictionEngine
{
    public class SerializationUtility
    {
#if UNITY_EDITOR
        public static bool RecoverLostReferencedObjectsInCollection<T>(IList<T> objectsList) where T : UnityEngine.Object
        {
            bool changed = false;

            for (int i = objectsList.Count - 1; i >= 0; i--)
            {
                T value = objectsList.ElementAt(i);

                if (RecoverLostReferencedObject(objectsList.ElementAt(i), (newValue) => 
                {
                    objectsList[i] = newValue;

                    return true;
                }))
                    changed = true;
            }

            return changed;
        }

        public static bool RecoverLostReferencedObjectsInCollection<T,T1>(IDictionary<T,T1> objectsDictionary) where T1 : UnityEngine.Object
        {
            bool changed = false;

            for (int i = objectsDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T,T1> keyValuePair = objectsDictionary.ElementAt(i);

                if (RecoverLostReferencedObject(keyValuePair.Value, (newValue) =>
                {
                    objectsDictionary.Remove(keyValuePair.Key);
                    objectsDictionary.Add(keyValuePair.Key, newValue);

                    return true;
                }))
                    changed = true;
            }

            return changed;
        }

        public static bool RecoverLostReferencedObjectsInCollection(IDictionary<SerializableGuid, SerializableIPersistent> objectsDictionary)
        {
            bool changed = false;

            for (int i = objectsDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<SerializableGuid, SerializableIPersistent> keyValuePair = objectsDictionary.ElementAt(i);

                if (RecoverLostReferencedObject(keyValuePair.Value.persistent as UnityEngine.Object, (newValue) =>
                {
                    objectsDictionary.Remove(keyValuePair.Key);
                    objectsDictionary.Add(keyValuePair.Key, new SerializableIPersistent((IPersistent)newValue));
                   
                    return true;
                }, (newValue) =>
                {
                    IPersistent persistent = InstanceManager.Instance().GetPersistent(keyValuePair.Key);
                    if (!Disposable.IsDisposed(persistent))
                        newValue = persistent as UnityEngine.Object;
           
                    return newValue;
                }))
                    changed = true;
            }

            return changed;
        }

        private static bool RecoverLostReferencedObject<T>(T unityObject, Func<T, bool> unityObjectCallback, Func<T, T> recoverLostReferencedObjectCallback = null) where T : UnityEngine.Object
        {
            bool changed = false;
            
            T oldUnityObject = unityObject;

            if (!RecoverLostReferencedObject(ref unityObject) && recoverLostReferencedObjectCallback != null)
                unityObject = recoverLostReferencedObjectCallback.Invoke(unityObject);

            if (!Object.ReferenceEquals(oldUnityObject, unityObject))
            {
                changed = true;
                unityObjectCallback?.Invoke(unityObject);
            }

            return changed;
        }

        public static bool RecoverLostReferencedObject<T>(ref T unityObject) where T : UnityEngine.Object
        {
            if (!Object.ReferenceEquals(unityObject, null) && unityObject == null)
            {
                UnityEngine.Object recoveredObject = UnityEditor.EditorUtility.InstanceIDToObject(unityObject.GetInstanceID());
                if (recoveredObject != null)
                    unityObject = (T)recoveredObject;
            }
            return unityObject != null;
        }

        public static void PerformUndoRedoPropertyChange<T>(Action<T> callback, ref T field, ref T lastField)
        {
            T newValue = field;
            field = lastField;
            callback?.Invoke(newValue);
        }
#endif

        public static void FindAddedRemovedObjectsChange<T, T1>(IDictionary<T, T1> dictionary, IDictionary<T, T1> lastDictionary, Action<T> removedCallback = null, Action<T, T1> addedCallback = null)
        {
            //Find object that were newly destroyed
            for (int i = lastDictionary.Count - 1; i >= 0; i--)
            {
                KeyValuePair<T, T1> objectKeyPair = lastDictionary.ElementAt(i);
                if (Disposable.IsDisposed(objectKeyPair.Value))
                    removedCallback?.Invoke(objectKeyPair.Key);
            }

            if (!Object.ReferenceEquals(dictionary, lastDictionary))
            {
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
}
