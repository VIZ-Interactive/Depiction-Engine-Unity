// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(MeshObject))]
    [RequireScript(typeof(AssetReference))]
    public class MeshObject : MeshObjectBase
    {
        private Mesh _mesh;

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateMesh();

                return true;
            }
            return false;
        }

        private AssetReference meshAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(0), typeof(Mesh).Name) as AssetReference; }
        }

        private void UpdateMesh()
        {
            mesh = meshAssetReference != Disposable.NULL ? meshAssetReference.data as Mesh : null;
        }

        private Mesh mesh
        {
            get { return _mesh; }
            set 
            {
                if (_mesh == value)
                    return;

                _mesh = value;
            }
        }

        //TODO: Add MeshRendererVisual creation code
    }
}
