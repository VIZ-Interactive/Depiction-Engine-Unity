// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class BuildingGridMeshObjectVisualDirtyFlags : FeatureGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private Color _defaultColor;
        [SerializeField]
        private float _defaultLevelHeight;
        [SerializeField]
        private float _defaultHeight;

        public override void Recycle()
        {
            base.Recycle();

            _defaultColor = default;
            _defaultLevelHeight = default;
            _defaultHeight = default;
        }

        public Color defaultColor
        {
            get => _defaultColor;
            set
            {
                if (_defaultColor == value)
                    return;

                _defaultColor = value;
                isDirty = true;
            }
        }

        public float defaultLevelHeight
        {
            get => _defaultLevelHeight;
            set
            {
                if (_defaultLevelHeight == value)
                    return;

                _defaultLevelHeight = value;
                isDirty = true;
            }
        }

        public float defaultHeight
        {
            get => _defaultHeight;
            set
            {
                if (_defaultHeight == value)
                    return;

                _defaultHeight = value;
                isDirty = true;
            }
        }

        public override bool SetSphericalRatio(float value, bool disableMultiThreading = true)
        {
            if (base.SetSphericalRatio(value, disableMultiThreading))
            {
                isDirty = true;

                return true;
            }
            return false;
        }

        protected override bool SetAltitude(double value)
        {
            if (base.SetAltitude(value))
            {
                isDirty = true;

                return true;
            }

            return false;
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
