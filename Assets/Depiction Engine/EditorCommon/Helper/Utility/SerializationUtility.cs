// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using UnityEditor;

namespace DepictionEngine.Editor
{
    public class SerializationUtility
    {
        public static void FixBrokenPersistentsDictionary(Action<Func<SerializableGuid, IPersistent, bool>> persistentsIteration, Datasource.PersistentDictionary persistentsDictionary)
        {
            persistentsIteration.Invoke((persistentId, persistent) =>
            {
                FixBrokenPersistentDictionary(persistentsDictionary, persistent, persistentId);
                return true;
            });
        }

        public static void FixBrokenPersistentDictionary(Datasource.PersistentDictionary persistentsDictionary, IPersistent persistent, SerializableGuid persistentId)
        {
            IPersistent editorPersistent = (IPersistent)FindLostReferencedObject(persistent as UnityEngine.Object);
            if (!Object.ReferenceEquals(editorPersistent, persistent))
            {
                persistentsDictionary.Remove(persistentId);
                persistentsDictionary.Add(persistentId, new SerializableIPersistent(editorPersistent));
            }
        }

        public static UnityEngine.Object FindLostReferencedObject(UnityEngine.Object unityObject)
        {
            if (!Object.ReferenceEquals(unityObject, null))
                unityObject = EditorUtility.InstanceIDToObject(unityObject.GetInstanceID());
            return unityObject;
        }
    }
}
#endif
