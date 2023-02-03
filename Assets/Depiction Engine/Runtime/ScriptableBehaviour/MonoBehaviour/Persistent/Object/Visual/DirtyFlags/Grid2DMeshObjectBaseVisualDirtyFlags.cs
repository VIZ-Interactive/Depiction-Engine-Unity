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

            _altitude = 0.0d;
            _meshRendererVisualLocalScale = Vector3.zero;
            _size = 0.0d;
            _sphericalRatio = 0.0f;
            _grid2DIndex = Vector2Int.zero;
            _grid2DDimensions = Vector2Int.zero;
        }

        public double altitude
        {
            get { return _altitude; }
            set { SetAltitude(value); }
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
            get { return _meshRendererVisualLocalScale; }
            set { SetMeshRendererVisualLocalScale(value); }
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
            get { return _size; }
            set { SetSize(value); }
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
            get { return _sphericalRatio; }
            set { SetSphericalRatio(value); }
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
            get { return _grid2DIndex; }
            set { SetGrid2DIndex(value); }
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
            get { return _grid2DDimensions; }
            set { SetGrid2DDimensions(value); }
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
