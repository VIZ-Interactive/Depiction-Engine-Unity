// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(InstantiatorVisualObject))]
    [RequireScript(typeof(AssetReference))]
    public class InstantiatorVisualObject : VisualObject
    {
        [BeginFoldout("GameObjects")]
        [SerializeField, Tooltip("When enabled '"+nameof(UpdateAllGameObjects)+"' will be called automatically when the object is created or when the assetBundle changes.")]
        private bool _autoInstantiate;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(DisposeAllVisualsBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Dispose all children.")]
        private bool _disposeAllGameObjects;
        [SerializeField, Button(nameof(UpdateAllGameObjectsBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Iterate over all the AssetBundle's GameObject and instantiate them as children."), EndFoldout]
        private bool _updateAllGameObjects;
#endif
        [SerializeField, HideInInspector]
        private List<GameObject> _gameObjects;

        private AssetBundle _assetBundle;

#if UNITY_EDITOR
        private void DisposeAllVisualsBtn()
        {
            IsUserChange(() =>
            {
                Editor.UndoManager.CreateNewGroup("Disposed All '" + name + "' Visuals");
                DisposeAllGameObjects();
            });
        }

        private void UpdateAllGameObjectsBtn()
        {
            IsUserChange(() =>
            {
                Editor.UndoManager.CreateNewGroup("Updated All '" + name + "' GameObjects");
                UpdateAllGameObjects();
            });
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _gameObjects = null;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => autoInstantiate = value, true, initializingState);
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                //Some 'Visuals' will not be persisted in the Scene so we need to recreate them
                if (!GetMeshRenderersInitialized())
                    AutoInstantiate();

                return true;
            }
            return false;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveAssetBundleDelegates();
                AddAssetBundleDelegates();

                return true;
            }
            return false;
        }

        private bool RemoveAssetBundleDelegates()
        {
            if (!Object.ReferenceEquals(assetBundle, null))
            {
                assetBundle.PropertyAssignedEvent -= AssetBundlePropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private bool AddAssetBundleDelegates()
        {
            if (!IsDisposing() && assetBundle != Disposable.NULL)
            {
                assetBundle.PropertyAssignedEvent += AssetBundlePropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private void AssetBundlePropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                AssetBundleChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateAssetBundle();

                return true;
            }
            return false;
        }

        /// <summary>
        /// When enabled <see cref="UpdateAllGameObjects"/> will be called automatically when the object is created or when the assetBundle changes.
        /// </summary>
        [Json]
        public bool autoInstantiate
        {
            get { return _autoInstantiate; }
            set
            {
                SetValue(nameof(autoInstantiate), value, ref _autoInstantiate, (newValue, oldValue) =>
                {
                    AutoInstantiate();
                });
            }
        }

        private AssetReference assetBundleAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(0), typeof(AssetBundle).Name) as AssetReference; }
        }

        private void UpdateAssetBundle()
        {
            assetBundle = assetBundleAssetReference != Disposable.NULL ? assetBundleAssetReference.data as AssetBundle : null;
        }

        private AssetBundle assetBundle
        {
            get { return _assetBundle; }
            set
            {
                AssetBundle oldValue = _assetBundle;
                AssetBundle newValue = value;
                if (newValue == oldValue)
                    return;

                RemoveAssetBundleDelegates();

                _assetBundle = newValue;

                AddAssetBundleDelegates();

                AssetBundleChanged();
            }
        }

        private void AssetBundleChanged()
        {
            if (initialized && activeAndEnabled)
            {
                DisposeAllGameObjects();
                AutoInstantiate();
            }
        }

        public void IterateOverGameObjects(Func<GameObject, bool> callback)
        {
            if (_gameObjects != null)
            {
                for (int i = 0; i < _gameObjects.Count; i++)
                {
                    if (!callback(GetGameObjectAt(i)))
                        break;
                }
            }
        }

        public GameObject GetGameObjectAt(int index)
        {
            GameObject gameObject = null;

            if (_gameObjects != null)
                gameObject = _gameObjects[index];

            return gameObject;
        }

        /// <summary>
        /// Dispose all children.
        /// </summary>
        /// <returns></returns>
        public bool DisposeAllGameObjects()
        {
            if (!IsDisposing() && DisposeAllChildren())
            {
                _gameObjects = null;

                return true;
            }
            return false;
        }

        private void AutoInstantiate()
        {
            if (autoInstantiate)
                UpdateAllGameObjects();
        }

        /// <summary>
        /// Iterate over all the AssetBundle's GameObject and instantiate them as children.
        /// </summary>
        private void UpdateAllGameObjects()
        {
            if (initialized)
            {
                List<GameObject> newGameObjects = new List<GameObject>();

                if (assetBundle != Disposable.NULL)
                {
                    assetBundle.IterateOverGameObjects((gameObject, parent) =>
                    {
                        GameObject newGameObject = null;

                        Object objectBase = gameObject.GetComponent<Object>();

                        if (objectBase == Disposable.NULL)
                        {
                            newGameObject = GetGameObjectAt(newGameObjects.Count);

                            if (newGameObject == null)
                                newGameObject = InstantiateGameObject(gameObject, parent);
                            else
                                newGameObject.transform.parent = parent.transform;

                            newGameObjects.Add(newGameObject);
                        }

                        return newGameObject;

                    }, gameObject);
                }

                _gameObjects = newGameObjects;
            }
        }

        private GameObject InstantiateGameObject(GameObject gameObject, GameObject parent)
        {
            GameObject newGameObject = null;

            Visual visual = gameObject.GetComponent<Visual>();

            if (visual != Disposable.NULL)
            {
                Visual newVisual = instanceManager.CreateInstance(visual.GetType(), parent.transform, null, null) as Visual;

                if (newVisual is MeshRendererVisual)
                {
                    MeshRendererVisual meshRendererVisual = newVisual as MeshRendererVisual;
                    MeshRendererVisual gameObjectMeshRendererVisual = visual as MeshRendererVisual;
                    meshRendererVisual.sharedMesh = gameObjectMeshRendererVisual.sharedMesh;
                    meshRendererVisual.sharedMaterial = gameObjectMeshRendererVisual.sharedMaterial;
                }

                newGameObject = newVisual.gameObject;
            }

            if (newGameObject == null)
                newGameObject = MonoBehaviourBase.Instantiate(gameObject, parent.transform, false);

            newGameObject.name = gameObject.name;

            return newGameObject;
        }
    }
}
