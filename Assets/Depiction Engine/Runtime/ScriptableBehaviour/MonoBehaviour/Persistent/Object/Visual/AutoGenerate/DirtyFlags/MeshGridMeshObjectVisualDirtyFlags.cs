// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class MeshGridMeshObjectVisualDirtyFlags : FeatureGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private AssetBase _meshAsset;

        public override void Recycle()
        {
            base.Recycle();

            _meshAsset = default;
        }

#if UNITY_EDITOR
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.RecoverLostReferencedObject(ref _meshAsset);
        }
#endif

        public AssetBase asset
        {
            get => _meshAsset;
            set
            {
                if (_meshAsset == value)
                    return;

                _meshAsset = value;
             
                MeshAssetChanged();
            }
        }

        public void MeshAssetChanged()
        {
            isDirty = true;
        }

        protected override bool SetMeshRendererVisualLocalScale(Vector3 value)
        {
            if (base.SetMeshRendererVisualLocalScale(value))
            {
                isDirty = true;

                return true;
            }

            return false;
        }
    }
}
