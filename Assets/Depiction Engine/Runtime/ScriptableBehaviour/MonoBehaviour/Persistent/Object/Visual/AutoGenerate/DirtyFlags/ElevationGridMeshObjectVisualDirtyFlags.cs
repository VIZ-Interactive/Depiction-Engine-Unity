// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class ElevationGridMeshObjectVisualDirtyFlags : Grid2DMeshObjectBaseVisualDirtyFlags
    {
        [SerializeField]
        private Elevation _elevation;
        [SerializeField]
        private float _elevationMultiplier;

        public override void Recycle()
        {
            base.Recycle();

            _elevation = default;
            _elevationMultiplier = default;
        }

#if UNITY_EDITOR
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.RecoverLostReferencedObject(ref _elevation);
        }
#endif

        public Elevation elevation
        {
            get => _elevation;
            set
            {
                if (_elevation == value)
                    return;

                _elevation = value;

                ElevationChanged();
            }
        }

        public virtual void ElevationChanged()
        {
            isDirty = true;
        }

        public float elevationMultiplier
        {
            get => _elevationMultiplier;
            set
            {
                if (_elevationMultiplier == value)
                    return;

                _elevationMultiplier = value;

                ElevationChanged();
            }
        }
    }
}