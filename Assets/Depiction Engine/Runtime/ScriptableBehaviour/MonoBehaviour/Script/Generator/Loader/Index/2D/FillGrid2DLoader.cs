// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/Grid/" + nameof(FillGrid2DLoader))]
    public class FillGrid2DLoader : Grid2DLoaderBase
    {
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

            _fillGrids = new FillGrid2D[minMaxZoom.y + 1 - minMaxZoom.x];
            int gridCount = 0;
            for (int i = minMaxZoom.x; i <= minMaxZoom.y; i++)
            {
                _fillGrids[gridCount] = CreateGrid<FillGrid2D>().Init(i);
                gridCount++;
            }
        }

        protected override Vector2Int GetDefaultMinMaxZoom()
        {
            return Vector2Int.zero;
        }

        protected override float GetDefaultLoadInterval()
        {
            return 0.01f;
        }

        protected override void MinMaxZoomChanged()
        {
            base.MinMaxZoomChanged();

            InitGrids();
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
