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

            _defaultColor = Color.clear;
            _defaultLevelHeight = 0.0f;
            _defaultHeight = 0.0f;
        }

        public Color defaultColor
        {
            get { return _defaultColor; }
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
            get { return _defaultLevelHeight; }
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
            get { return _defaultHeight; }
            set
            {
                if (_defaultHeight == value)
                    return;

                _defaultHeight = value;
                isDirty = true;
            }
        }

        protected override bool SetSphericalRatio(float value)
        {
            if (base.SetSphericalRatio(value))
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
