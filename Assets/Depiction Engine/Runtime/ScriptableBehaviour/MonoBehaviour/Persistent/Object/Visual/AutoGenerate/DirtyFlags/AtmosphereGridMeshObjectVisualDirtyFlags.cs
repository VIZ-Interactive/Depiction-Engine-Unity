// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class AtmosphereGridMeshObjectVisualDirtyFlags : Grid2DMeshObjectBaseVisualDirtyFlags
    {
        [SerializeField]
        private int _subdivision;
        [SerializeField]
        private float _subdivisionSize;

        [SerializeField]
        private bool _trianglesDirty;
        [SerializeField]
        private bool _uvsDirty;
        [SerializeField]
        private bool _verticesNormalsDirty;

        public override void Recycle()
        {
            base.Recycle();

            _subdivision = default;
            _subdivisionSize = default;
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
