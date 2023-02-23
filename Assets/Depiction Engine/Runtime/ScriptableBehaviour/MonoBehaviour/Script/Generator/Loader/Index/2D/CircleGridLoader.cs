// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/Grid/" + nameof(CircleGridLoader))]
    public class CircleGridLoader : Grid2DLoaderBase
    {
        [BeginFoldout("Circle")]
        [SerializeField, Tooltip("The grid radius size over which tiles should be loaded, in world units."), EndFoldout]
        private float _radius;

        private CircleGrid[] _circleGrid;

        protected override void InitGrids()
        {
            base.InitGrids();

            if (_circleGrid == null)
                _circleGrid = new CircleGrid[] { CreateGrid<CircleGrid>() };
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => radius = value, 100.0f, initializingContext);
        }

        /// <summary>
        /// The grid radius size over which tiles should be loaded, in world units.
        /// </summary>
        [Json]
        public float radius
        {
            get { return _radius; }
            set 
            {
                if (value < 0.0f)
                    value = 0.0f;
                SetValue(nameof(radius), value, ref _radius); 
            }
        }

        protected override IEnumerable<IGrid2D> GetGrids()
        {
            return _circleGrid;
        }

        protected override bool UpdateGridsFields(IGrid2D grid, bool forceUpdate = false)
        {
            bool changed = forceUpdate;
            if (grid is CircleGrid && (grid as CircleGrid).UpdateCircleGridProperties(GetGrid2DDimensionsFromZoom(_zoom), GetGeoCoordinateCenter(), parentGeoAstroObject, _radius))
                changed = true;
            return changed;
        }
    }
}
