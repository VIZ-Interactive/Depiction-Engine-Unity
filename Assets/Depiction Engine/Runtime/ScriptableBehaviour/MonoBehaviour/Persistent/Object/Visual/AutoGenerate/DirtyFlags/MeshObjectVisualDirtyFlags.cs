// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class MeshObjectVisualDirtyFlags : VisualObjectVisualDirtyFlags
    {
        [SerializeField]
        private Mesh _mesh;

        public override void Recycle()
        {
            base.Recycle();

            _mesh = default;
        }

#if UNITY_EDITOR
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.RecoverLostReferencedObject(ref _mesh);
        }
#endif

        public Mesh mesh
        {
            get => _mesh;
            set
            {
                if (_mesh == value)
                    return;

                _mesh = value;

                MeshChanged();
            }
        }

        public void MeshChanged()
        {
            Recreate();
        }
    }
}