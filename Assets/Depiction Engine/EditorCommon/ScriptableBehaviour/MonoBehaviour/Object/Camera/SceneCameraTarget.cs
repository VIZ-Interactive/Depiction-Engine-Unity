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

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            if (controller == Disposable.NULL)
            {
                SceneCameraTargetGeoCoordinateController controller = gameObject.GetSafeComponent<SceneCameraTargetGeoCoordinateController>();
                if (controller == Disposable.NULL)
                    controller = CreateScript<SceneCameraTargetGeoCoordinateController>();
                controller.preventGroundPenetration = false;
                controller.autoSnapToGround = GeoCoordinateController.SnapType.None;
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

        public void SetTargetAutoSnapToGround(bool value)
        {
            GeoCoordinateController geoCoordinateController = controller as GeoCoordinateController;
            if (geoCoordinateController != Disposable.NULL)
            {
                GeoCoordinateController.SnapType autoSnapToGround = value ? GeoCoordinateController.SnapType.Terrain : GeoCoordinateController.SnapType.None;
                if (autoSnapToGround != geoCoordinateController.autoSnapToGround)
                    geoCoordinateController.autoSnapToGround = autoSnapToGround;
            }
        }

        protected override DisposeContext GetDisposingContext()
        {
            return DisposeContext.Programmatically_Destroy;
        }
    }
}
#endif
