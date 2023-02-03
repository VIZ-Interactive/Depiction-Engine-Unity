// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class AssetManager : AssetPostprocessor
    {
        void OnPostprocessPrefab(GameObject gameObject)
        {
            //Debug.Log(PrefabUtility.GetCorrespondingObjectFromSource(gameObject));
            //Object obj = gameObject.GetComponent<Object>();
            //gameObject.transform.hideFlags |= HideFlags.HideInInspector;
            //InstanceManager.Initialize(gameObject, InitializationState.Programmatically_Duplicate);
            //PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
            //DisposeManager.Dispose(g);
        }

        public static void InitListeners()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= SceneChanged;
            EditorSceneManager.activeSceneChanged -= SceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += SceneChanged;
            EditorSceneManager.activeSceneChanged += SceneChanged;
        }

        private static void SceneChanged(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.Scene scene2)
        {
            SaveAssets();
        }

        public static void SaveAssets()
        {
            //TODO: Add back? is this nessesary for the SceneViewProperties?
            //AssetDatabase.SaveAssets();
        }

        public static void Refresh()
        {
            AssetDatabase.Refresh();
        }
    }
}
#endif