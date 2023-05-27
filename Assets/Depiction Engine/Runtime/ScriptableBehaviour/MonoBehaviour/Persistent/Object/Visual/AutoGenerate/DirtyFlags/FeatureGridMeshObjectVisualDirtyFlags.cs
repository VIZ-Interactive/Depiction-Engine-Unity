// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class FeatureGridMeshObjectVisualDirtyFlags : ElevationGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private Feature _feature;

        public override void Recycle()
        {
            base.Recycle();

            _feature = default;
        }

#if UNITY_EDITOR
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.RecoverLostReferencedObject(ref _feature);
        }
#endif

        public Feature feature
        {
            get => _feature;
            set
            {
                if (_feature == value)
                    return;

                _feature = value;

                FeatureChanged();
            }
        }

        public void FeatureChanged()
        {
            Recreate();
        }
    }
}
