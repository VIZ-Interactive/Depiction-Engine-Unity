// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(MeshObject))]
    [CreateComponent(typeof(AssetReference))]
    public class MeshObject : MeshObjectBase
    {
        private const string MESH_REFERENCE_DATATYPE = nameof(Mesh);

        private Mesh _mesh;

        protected override void CreateComponents(InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            InitializeReferenceDataType(MESH_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveMeshDelegates();
                if (!IsDisposing())
                    AddMeshDelegates();

                return true;
            }
            return false;
        }

        private bool RemoveMeshDelegates()
        {
            if (mesh is not null)
            {
                mesh.PropertyAssignedEvent -= MeshPropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private bool AddMeshDelegates()
        {
            if (mesh != Disposable.NULL)
            {
                mesh.PropertyAssignedEvent += MeshPropertyAssignedHandler;
                return true;
            }
            return false;
        }

        private void MeshPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                (meshRendererVisualDirtyFlags as MeshObjectVisualDirtyFlags).MeshChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                mesh = GetAssetFromAssetReference<Mesh>(meshAssetReference);

                return true;
            }
            return false;
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(MeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is MeshObjectVisualDirtyFlags)
            {
                MeshObjectVisualDirtyFlags meshObjectVisualDirtyFlags = meshRendererVisualDirtyFlags as MeshObjectVisualDirtyFlags;

                meshObjectVisualDirtyFlags.mesh = mesh;
            }
        }

        protected override bool IterateOverAssetReferences(Func<AssetBase, AssetReference, bool, bool> callback)
        {
            if (base.IterateOverAssetReferences(callback))
            {
                if (!callback.Invoke(mesh, meshAssetReference, true))
                    return false;

                return true;
            }
            return false;
        }

        private AssetReference meshAssetReference
        {
            get => GetFirstReferenceOfType(MESH_REFERENCE_DATATYPE) as AssetReference;
        }

        private Mesh mesh
        {
            get => _mesh;
            set 
            {
                if (Object.ReferenceEquals(_mesh, value))
                    return;

                RemoveMeshDelegates();

                _mesh = value;

                AddMeshDelegates();
            }
        }

        //TODO: Add MeshRendererVisual creation code
    }
}
