// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class MeshGridMeshObjectVisualDirtyFlags : FeatureGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private AssetBase _asset;

        public override void Recycle()
        {
            base.Recycle();

            _asset = default;
        }

        public AssetBase asset
        {
            get { return _asset; }
            set
            {
                if (_asset == value)
                    return;

                _asset = value;
             
                AssetChanged();
            }
        }

        public void AssetChanged()
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
