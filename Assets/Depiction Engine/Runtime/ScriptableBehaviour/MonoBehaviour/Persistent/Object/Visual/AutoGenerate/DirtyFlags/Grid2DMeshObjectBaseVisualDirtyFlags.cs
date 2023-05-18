// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class Grid2DMeshObjectBaseVisualDirtyFlags : VisualObjectVisualDirtyFlags
    {
        [SerializeField]
        private double _altitude;
        [SerializeField]
        private Vector3 _meshRendererVisualLocalScale;
        [SerializeField]
        private double _size;
        [SerializeField]
        private float _sphericalRatio;
        [SerializeField]
        private Vector2Int _grid2DIndex;
        [SerializeField]
        private Vector2Int _grid2DDimensions;

        public override void Recycle()
        {
            base.Recycle();

            _altitude = default;
            _meshRendererVisualLocalScale = default;
            _size = default;
            _sphericalRatio = default;
            _grid2DIndex = default;
            _grid2DDimensions = default;
        }

        public double altitude
        {
            get => _altitude;
            set => SetAltitude(value);
        }

        protected virtual bool SetAltitude(double value)
        {
            if (_altitude == value)
                return false;

            _altitude = value;

            return true;
        }

        public Vector3 meshRendererVisualLocalScale
        {
            get => _meshRendererVisualLocalScale;
            set => SetMeshRendererVisualLocalScale(value);
        }

        protected virtual bool SetMeshRendererVisualLocalScale(Vector3 value)
        {
            if (_meshRendererVisualLocalScale == value)
                return false;

            _meshRendererVisualLocalScale = value;

            return true;
        }

        public double size
        {
            get => _size;
            set => SetSize(value);
        }

        protected virtual bool SetSize(double value)
        {
            if (_size == value)
                return false;

            _size = value;

            return true;
        }

        public float sphericalRatio
        {
            get => _sphericalRatio;
            set => SetSphericalRatio(value);
        }

        protected virtual bool SetSphericalRatio(float value)
        {
            if (_sphericalRatio == value)
                return false;

            _sphericalRatio = value;

            return true;
        }

        public Vector2Int grid2DIndex
        {
            get => _grid2DIndex;
            set => SetGrid2DIndex(value);
        }

        protected virtual bool SetGrid2DIndex(Vector2Int value)
        {
            if (_grid2DIndex == value)
                return false;

            _grid2DIndex = value;

            return true;
        }

        public Vector2Int grid2DDimensions
        {
            get => _grid2DDimensions;
            set => SetGrid2DDimensions(value);
        }

        protected virtual bool SetGrid2DDimensions(Vector2Int value)
        {
            if (_grid2DDimensions == value)
                return false;

            _grid2DDimensions = value;

            return true;
        }
    }
}
