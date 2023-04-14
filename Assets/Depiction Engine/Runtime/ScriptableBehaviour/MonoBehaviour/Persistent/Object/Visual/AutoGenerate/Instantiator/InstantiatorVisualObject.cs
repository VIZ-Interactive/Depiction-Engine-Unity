// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Instantiate the <see cref="DepictionEngine.Visual"/> found in an <see cref="DepictionEngine.AssetBundle"/> as children.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(InstantiatorVisualObject))]
    [CreateComponent(typeof(AssetReference))]
    public class InstantiatorVisualObject : AutoGenerateVisualObject
    {
        private const string ASSETBUNDLE_REFERENCE_DATATYPE = nameof(AssetBundle);

        [BeginFoldout("GameObjects")]
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
        protected override bool GetShowAutoInstantiate()
        {
            return true;
        }

        private void DisposeAllVisualsBtn()
        {
            SceneManager.UserContext(() =>
            {
                Editor.UndoManager.CreateNewGroup("Disposed All '" + name + "' Visuals");
                DisposeAllChildren();
            });
        }

        private void UpdateAllGameObjectsBtn()
        {
            SceneManager.UserContext(() =>
            {
                Editor.UndoManager.CreateNewGroup("Updated All '" + name + "' GameObjects");
                UpdateAllGameObjects();
            });
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _gameObjects = default;
        }

        protected override void CreateComponents(InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            InitializeReferenceDataType(ASSETBUNDLE_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateAllDelegates() 
        { 
            if (base.UpdateAllDelegates())
            { 
                RemoveAssetBundleDelegates();
                if (!IsDisposing())
                    AddAssetBundleDelegates();
               
                return true;
            }
            return false;
        }

        private bool RemoveAssetBundleDelegates()
        {
            if (assetBundle is not null)
            {
                assetBundle.PropertyAssignedEvent -= AssetBundlePropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private bool AddAssetBundleDelegates()
        {
            if (assetBundle != Disposable.NULL)
            {
                assetBundle.PropertyAssignedEvent += AssetBundlePropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private void AssetBundlePropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                (meshRendererVisualDirtyFlags as InstantiatorVisualObjectVisualDirtyFlags).AssetBundleChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                assetBundle = GetAssetFromAssetReference<AssetBundle>(assetBundleAssetReference);

                return true;
            }
            return false;
        }

        protected override bool IterateOverAssetReferences(Func<AssetBase, AssetReference, bool, bool> callback)
        {
            if (base.IterateOverAssetReferences(callback))
            {
                if (!callback.Invoke(assetBundle, assetBundleAssetReference, true))
                    return false;

                return true;
            }
            return false;
        }

        private AssetReference assetBundleAssetReference
        {
            get => GetFirstReferenceOfType(ASSETBUNDLE_REFERENCE_DATATYPE) as AssetReference;
        }

        private AssetBundle assetBundle
        {
            get => _assetBundle;
            set
            {
                if (Object.ReferenceEquals(_assetBundle, value))
                    return;

                RemoveAssetBundleDelegates();

                _assetBundle = value;

                AddAssetBundleDelegates();
            }
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(InstantiatorVisualObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is InstantiatorVisualObjectVisualDirtyFlags)
            {
                InstantiatorVisualObjectVisualDirtyFlags instantiatorVisualObjectVisualDirtyFlags = meshRendererVisualDirtyFlags as InstantiatorVisualObjectVisualDirtyFlags;

                instantiatorVisualObjectVisualDirtyFlags.assetBundle = assetBundle;
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

        protected override void UpdateMeshRendererVisuals(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisuals(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags.isDirty)
                UpdateAllGameObjects();
        }

        protected override bool DisposeAllChildren(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.DisposeAllChildren(disposeContext))
            {
                _gameObjects = null;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Iterate over all the AssetBundle's GameObject and instantiate them as children.
        /// </summary>
        private void UpdateAllGameObjects()
        {
            if (initialized)
            {
                List<GameObject> newGameObjects = new();

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
                Visual newVisual = instanceManager.CreateInstance(visual.GetType(), parent.transform, null, null, InitializationContext.Programmatically) as Visual;

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
            {
                newGameObject = MonoBehaviourDisposable.Instantiate(gameObject, parent.transform, false);
                MeshRenderer meshRenderer = newGameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    AddMeshRenderer(meshRenderer);
            }

            newGameObject.name = gameObject.name;

            return newGameObject;
        }
    }
}
