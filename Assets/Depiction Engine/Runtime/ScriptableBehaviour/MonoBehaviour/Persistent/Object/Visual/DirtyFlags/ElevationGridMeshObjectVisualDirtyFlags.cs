﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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

            _elevation = null;
            _elevationMultiplier = 0.0f;
        }

        public Elevation elevation
        {
            get { return _elevation; }
            set
            {
                if (_elevation == value)
                    return;

                _elevation = value;

                ElevationChanged();
            }
        }

        public float elevationMultiplier
        {
            get { return _elevationMultiplier; }
            set
            {
                if (_elevationMultiplier == value)
                    return;

                _elevationMultiplier = value;

                ElevationChanged();
            }
        }

        public virtual void ElevationChanged()
        {
            isDirty = true;
        }
    }
}