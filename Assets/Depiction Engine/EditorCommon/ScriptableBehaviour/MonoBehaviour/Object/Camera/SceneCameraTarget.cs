// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class SceneCameraTarget : Object
    {
        protected override bool CanBeDuplicated()
        {
            return false;
        }

        protected override bool IsValidInitialization(InitializationContext initializingContext)
        {
            //When undoing a GeoAstroObject destroy, the target will also be recreated and we dont want to have two targets in the scene. 
            if (base.IsValidInitialization(initializingContext))
                return initializingContext != InitializationContext.Existing;
            return false;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            if (controller == Disposable.NULL)
            {
                SceneCameraTargetGeoCoordinateController controller = gameObject.GetSafeComponent<SceneCameraTargetGeoCoordinateController>();
                if (controller == Disposable.NULL)
                    controller = CreateScript<SceneCameraTargetGeoCoordinateController>();
                controller.autoSnapToAltitude = GeoCoordinateController.SnapType.None;
            }
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        protected override bool GetDefaultDontSaveToScene()
        {
            return true;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                gameObject.hideFlags |= HideFlags.DontSave;

                return true;
            }
            return false;
        }

        public void SetTargetParent(TransformDouble parent)
        {
            if (transform != Disposable.NULL && !Object.ReferenceEquals(transform.parent, parent))
            {
                SetParent(parent);
                if (parent == Disposable.NULL)
                    transform.rotation = QuaternionDouble.identity;
            }
        }

        public void SetTargetAutoSnapToAltitude(bool value)
        {
            GeoCoordinateController geoCoordinateController = controller as GeoCoordinateController;
            if (geoCoordinateController != Disposable.NULL)
                geoCoordinateController.autoSnapToAltitude = value ? GeoCoordinateController.SnapType.Highest_Surface_Elevation : GeoCoordinateController.SnapType.None;
        }

        protected override DisposeContext GetDisposingContext()
        {
            return DisposeContext.Programmatically_Destroy;
        }
    }
}
#endif
