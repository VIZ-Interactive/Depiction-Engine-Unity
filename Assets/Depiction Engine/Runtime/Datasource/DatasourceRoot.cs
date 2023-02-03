// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Object required for loaders of certain types.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Container/" + nameof(DatasourceRoot))]
    public class DatasourceRoot : Object
    {
        public override bool IsPhysicsObject()
        {
            return false;
        }

        protected override bool TransformObjectCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformObjectCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (ResetTransform())
                {
                    if (localPositionParam.changed)
                    {
                        if (!localPositionParam.isGeoCoordinate)
                            localPositionParam.SetValue(Vector3Double.zero);
                        else
                        {
                            if (parentGeoAstroObject != Disposable.NULL && this != parentGeoAstroObject)
                                localPositionParam.SetValue(new GeoCoordinate3Double(0.0d, 0.0d, parentGeoAstroObject.IsSpherical() ? -parentGeoAstroObject.radius : 0.0d));
                        }
                    }

                    if (localRotationParam.changed)
                        localRotationParam.SetValue(QuaternionDouble.identity);

                    if (localScaleParam.changed)
                        localScaleParam.SetValue(Vector3Double.one);
                }
                return true;
            }
            return false;
        }

        protected virtual bool ResetTransform()
        {
            return true;
        }

        private bool _wrongParentError;
        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                if (ResetTransform())
                {
                    if (!_wrongParentError && parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject.transform != transform.parent)
                    {
                        _wrongParentError = true;

                        Debug.LogError(nameof(DatasourceRoot) + " can only be added to the root of a " + nameof(GeoAstroObject));

                        transform.SetParent(parentGeoAstroObject.transform, false);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
