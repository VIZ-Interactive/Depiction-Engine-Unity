// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
namespace DepictionEngine.Editor
{
    public class SceneCameraTargetGeoCoordinateController : GeoCoordinateController
    {
        protected override bool AddInstanceToManager()
        {
            return false;
        }
    }
}
#endif
