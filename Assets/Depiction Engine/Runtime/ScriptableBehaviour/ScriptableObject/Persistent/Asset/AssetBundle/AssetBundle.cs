// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class AssetBundle : AssetBase, IUnityMeshAsset
    {
        [SerializeField, HideInInspector]
        private byte[] _bytes;

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            SetData(value as byte[]);
        }

        public void SetData(byte[] bytes)
        {
            _bytes = bytes;

            DataPropertyAssigned();
        }

        protected override byte[] GetDataBytes(LoaderBase.DataType dataType)
        {
            return _bytes;
        }

        protected override string GetFileExtension()
        {
            return "assetbundle";
        }

        public T[] LoadAllAssets<T>() where T : UnityEngine.Object
        {
            UnityEngine.AssetBundle assetBundle = UnityEngine.AssetBundle.LoadFromMemory(_bytes);

            T[] unityAsset = null;

            if (assetBundle != null)
            {
                unityAsset = assetBundle.LoadAllAssets<T>();

                assetBundle.Unload(false);
            }

            return unityAsset;
        }

        public void IterateOverUnityMesh(Action<UnityEngine.Mesh> callback)
        {
            IterateOverGameObjects((gameObject, parent) =>
            {
                MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

                if (meshFilter != null && meshFilter.sharedMesh != null)
                    callback(meshFilter.sharedMesh);

                return gameObject;
            }, null);
        }

        public void IterateOverGameObjects(Func<GameObject, GameObject, GameObject> callback, GameObject parent)
        {
            GameObject[] gameObjects = LoadAllAssets<GameObject>();

            if (gameObjects != null)
            {
                foreach (GameObject gameObject in gameObjects)
                    IterateOverGameObjectHierarchy(callback, gameObject, parent);
            }
        }

        private void IterateOverGameObjectHierarchy(Func<GameObject, GameObject, GameObject> callback, GameObject gameObject, GameObject parent)
        {
            if (gameObject != null)
            {
                parent = callback(gameObject, parent);

                if (parent != null)
                {
                    foreach (Transform child in gameObject.transform)
                        IterateOverGameObjectHierarchy(callback, child.gameObject, parent);
                }
            }
        }
    }
}
