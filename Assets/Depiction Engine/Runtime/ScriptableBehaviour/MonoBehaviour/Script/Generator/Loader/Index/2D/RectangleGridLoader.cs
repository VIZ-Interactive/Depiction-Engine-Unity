// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/Grid/" + nameof(RectangleGridLoader))]
    public class RectangleGridLoader : Grid2DLoaderBase
    {
        [BeginFoldout("Rectangle")]
        [SerializeField, Tooltip("A rotation value indicating the direction towards which the rectangle should face.")]
        private double _angle;
        [SerializeField, Tooltip("Rectangle Grid width, in world units.")]
        private double _width;
        [SerializeField, Tooltip("Rectangle Grid height, in world units."), EndFoldout]
        private double _height;

        private RectangleGrid[] _rectangleGrid;

        protected override void InitGrids()
        {
            base.InitGrids();

            if (_rectangleGrid == null)
                _rectangleGrid = new RectangleGrid[] { CreateGrid<RectangleGrid>() };
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => angle = value, 0.0d, initializingContext);
            InitValue(value => width = value, 100.0d, initializingContext);
            InitValue(value => height = value, 100.0d, initializingContext);
        }

        /// <summary>
        /// A rotation value indicating the direction towards which the rectangle should face. 
        /// </summary>
        [Json]
        public double angle
        {
            get { return _angle; }
            set { SetValue(nameof(angle), value, ref _angle); }
        }

        /// <summary>
        /// Rectangle Grid width, in world units. 
        /// </summary>
        [Json]
        public double width
        {
            get { return _width; }
            set { SetValue(nameof(width), ValidateSize(value), ref _width); }
        }

        /// <summary>
        /// Rectangle Grid height, in world units. 
        /// </summary>
        [Json]
        public double height
        {
            get { return _height; }
            set { SetValue(nameof(height), ValidateSize(value), ref _height); }
        }

        private double ValidateSize(double value)
        {
            if (value < 0)
                value = 0;
            return value;
        }

        protected override IEnumerable<IGrid2D> GetGrids()
        {
            return _rectangleGrid;
        }

        protected override bool UpdateGridsFields(IGrid2D grid, bool forceUpdate = false)
        {
            bool changed = forceUpdate;

            if (grid is RectangleGrid)
            {
                RectangleGrid rectangleGrid = grid as RectangleGrid;
                if (rectangleGrid.UpdateRectangleGridProperties(GetGrid2DDimensionsFromZoom(_zoom), GetGeoCoordinateCenter(), parentGeoAstroObject, _angle, _width, _height))
                    changed = true;
            }

            return changed;
        }
    }
}
