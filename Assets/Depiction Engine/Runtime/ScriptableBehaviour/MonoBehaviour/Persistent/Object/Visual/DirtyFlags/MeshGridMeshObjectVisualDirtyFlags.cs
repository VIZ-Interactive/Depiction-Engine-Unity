// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class MeshGridMeshObjectVisualDirtyFlags : FeatureGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private UnityEngine.Mesh[] _unityMeshes;

        public override void Recycle()
        {
            base.Recycle();

            _unityMeshes = null;
        }

        public UnityEngine.Mesh[] unityMeshes
        {
            get { return _unityMeshes; }
            set
            {
                if (_unityMeshes == value)
                    return;

                _unityMeshes = value;
             
                UnityMeshesChanged();
            }
        }

        public void UnityMeshesChanged()
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
