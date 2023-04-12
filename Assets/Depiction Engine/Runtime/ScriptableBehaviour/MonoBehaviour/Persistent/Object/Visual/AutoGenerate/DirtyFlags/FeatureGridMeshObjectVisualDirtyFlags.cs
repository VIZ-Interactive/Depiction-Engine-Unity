// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class FeatureGridMeshObjectVisualDirtyFlags : ElevationGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private Feature _feature;
        [SerializeField]
        private bool _processing;

        private Processor _processor;

        public override void Recycle()
        {
            base.Recycle();

            _feature = default;

            _processing = default;
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
            get { return _feature; }
            set
            {
                if (_feature == value)
                    return;

                _feature = value;

                FeatureChanged();
            }
        }

        public void SetProcessing(bool processing, Processor processor = null)
        {
            _processing = processing;
            _processor = processor;
        }

        public bool ProcessingWasCompromised()
        {
            return _processing && _processor == null;
        }

        public void FeatureChanged()
        {
            isDirty = true;
        }
    }
}
