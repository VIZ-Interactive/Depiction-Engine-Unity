// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class FillGrid2D : Grid2D
    {
        public int zoom;

        public FillGrid2D Init(int zoom)
        {
            this.zoom = zoom;

            return this;
        }

        protected override bool UpdateGrid(Vector2Int gridDimensions)
        {
            if (base.UpdateGrid(gridDimensions))
            {
                for (int y = 0; y < gridDimensions.y; y++)
                    AddRow(y, CreateRow(y, 0, gridDimensions.x - 1));
                return true;
            }
            return false;
        }
    }
}
