// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class GlobalLoader : FillGrid2DLoader
    {
        protected override Vector2Int GetDefaultZoomRange()
        {
            return new Vector2Int(2, 2);
        }

        protected override float GetDefaultLoadInterval()
        {
            return 0.0f;
        }

        protected override bool GetDefaultAutoLoadWhenDisabled()
        {
            return true;
        }
    }
}
