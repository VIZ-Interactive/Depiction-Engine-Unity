// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class TerrainGridMeshObjectVisualDirtyFlags : ElevationGridMeshObjectVisualDirtyFlags
    {
        [SerializeField]
        private int _subdivision;
        [SerializeField]
        private float _subdivisionSize;
        [SerializeField]
        private float _overlapFactor;
        [SerializeField]
        private bool _generateEdgeInSeperateMesh;
        [SerializeField]
        private float _edgeDepth;
        [SerializeField]
        private TerrainGridMeshObject.NormalsType _normalsType;

        private bool _trianglesDirty;
        private bool _uvsDirty;
        private bool _verticesNormalsDirty;

        public override void Recycle()
        {
            base.Recycle();

            _subdivision = 0;
            _subdivisionSize = 0.0f;
            _overlapFactor = 0.0f;
            _generateEdgeInSeperateMesh = false;
            _edgeDepth = 0.0f;
            _normalsType = TerrainGridMeshObject.NormalsType.DerivedFromElevation;
        }

        public int subdivision
        {
            get { return _subdivision; }
            set
            {
                if (_subdivision == value)
                    return;

                _subdivision = value;

                SubdivisionChanged();
            }
        }

        public float subdivisionSize
        {
            get { return _subdivisionSize; }
            set
            {
                if (_subdivisionSize == value)
                    return;

                _subdivisionSize = value;

                SubdivisionChanged();
            }
        }

        protected void SubdivisionChanged()
        {
            TrianglesDirty();
            UVsDirty();
            VerticesNormalsDirty();
        }

        public float overlapFactor
        {
            get { return _overlapFactor; }
            set
            {
                if (_overlapFactor == value)
                    return;

                _overlapFactor = value;

                VerticesNormalsDirty();
            }
        }

        public bool generateEdgeInSeperateMesh
        {
            get { return _generateEdgeInSeperateMesh; }
            set
            {
                if (_generateEdgeInSeperateMesh == value)
                    return;

                _generateEdgeInSeperateMesh = value;

                EdgeChanged();
            }
        }

        public float edgeDepth
        {
            get { return _edgeDepth; }
            set
            {
                if (_edgeDepth == value)
                    return;

                _edgeDepth = value;

                EdgeChanged();
            }
        }

        public TerrainGridMeshObject.NormalsType normalsType
        {
            get { return _normalsType; }
            set
            {
                if (_normalsType == value)
                    return;

                _normalsType = value;

                ElevationChanged();
            }
        }

        protected void EdgeChanged()
        {
            TrianglesDirty();
            UVsDirty();
            VerticesNormalsDirty();
        }

        protected override bool SetAltitude(double value)
        {
            if (base.SetAltitude(value))
            {
                VerticesNormalsDirty();

                return true;
            }

            return false;
        }

        protected override bool SetMeshRendererVisualLocalScale(Vector3 value)
        {
            if (base.SetMeshRendererVisualLocalScale(value))
            {
                VerticesNormalsDirty();

                return true;
            }

            return false;
        }

        protected override bool SetGrid2DIndex(Vector2Int value)
        {
            if (base.SetGrid2DIndex(value))
            {
                VerticesNormalsDirty();

                return true;
            }
            return false;
        }

        protected override bool SetGrid2DDimensions(Vector2Int value)
        {
            if (base.SetGrid2DDimensions(value))
            {
                VerticesNormalsDirty();

                return true;
            }
            return false;
        }

        protected override bool SetSphericalRatio(float value)
        {
            if (base.SetSphericalRatio(value))
            {
                VerticesNormalsDirty();
                DisableMultithreading();

                return true;
            }
            return false;
        }

        public override void ElevationChanged()
        {
            base.ElevationChanged();

            VerticesNormalsDirty();
        }

        public bool trianglesDirty
        {
            get { return _trianglesDirty; }
        }

        public bool uvsDirty
        {
            get { return _uvsDirty; }
        }

        public bool verticesNormalsDirty
        {
            get { return _verticesNormalsDirty; }
        }

        protected void TrianglesDirty()
        {
            _trianglesDirty = true;
            isDirty = true;
        }

        protected void UVsDirty()
        {
            _uvsDirty = true;
            isDirty = true;
        }

        protected void VerticesNormalsDirty()
        {
            _verticesNormalsDirty = true;
            isDirty = true;
        }

        public override void AllDirty()
        {
            base.AllDirty();

            TrianglesDirty();
            UVsDirty();
            VerticesNormalsDirty();
        }

        public override void ResetDirty()
        {
            base.ResetDirty();

            _trianglesDirty = false;
            _uvsDirty = false;
            _verticesNormalsDirty = false;
        }
    }
}
