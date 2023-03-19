// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;

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
            IPersistent editorPersistent = UnityEditor.EditorUtility.InstanceIDToObject(persistent.GetInstanceID()) as IPersistent;
            if (!Object.ReferenceEquals(editorPersistent, persistent))
            {
                persistentsDictionary.Remove(persistentId);
                persistentsDictionary.Add(persistentId, new SerializableIPersistent(editorPersistent));
            }
        }
    }
}
#endif
