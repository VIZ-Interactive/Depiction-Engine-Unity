// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/Grid/" + nameof(FillGrid2DLoader))]
    public class FillGrid2DLoader : Grid2DLoaderBase
    {
        [BeginFoldout("Fill")]
        [SerializeField, MinMaxRange(0.0f, MAX_ZOOM), Tooltip("The range of zoom values for which we want to load all the tiles."), EndFoldout]
        private Vector2Int _zoomRange;

        [SerializeField, HideInInspector]
        private FillGrid2D[] _fillGrids;

#if UNITY_EDITOR
        protected override bool GetShowZoom()
        {
            return false;
        }
#endif

        protected override void InitGrids()
        {
            base.InitGrids();

            _fillGrids = new FillGrid2D[_zoomRange.y + 1 - _zoomRange.x];
            int gridCount = 0;
            for (int i = _zoomRange.x; i <= _zoomRange.y; i++)
            {
                _fillGrids[gridCount] = CreateGrid<FillGrid2D>().Init(i);
                gridCount++;
            }
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => zoomRange = value, GetDefaultZoomRange(), initializingContext);
        }

        protected virtual Vector2Int GetDefaultZoomRange()
        {
            return Vector2Int.zero;
        }

        protected override float GetDefaultLoadInterval()
        {
            return 0.01f;
        }

        /// <summary>
        /// The range of zoom values for which we want to load all the tiles.
        /// </summary>
        [Json]
        public Vector2Int zoomRange
        {
            get { return _zoomRange; }
            set
            {
                if (value.x < 0)
                    value.x = 0;
                if (value.y > MAX_ZOOM)
                    value.y = MAX_ZOOM;
                SetValue(nameof(zoomRange), value, ref _zoomRange, (newValue, oldValue) =>
                {
                    InitGrids();
                });
            }
        }

        protected override IEnumerable<IGrid2D> GetGrids()
        {
            return _fillGrids;
        }

        protected override int GetZoomForGrid(Grid2D grid)
        {
            return grid is FillGrid2D ? (grid as FillGrid2D).zoom : base.GetZoomForGrid(grid);
        }
    }
}
