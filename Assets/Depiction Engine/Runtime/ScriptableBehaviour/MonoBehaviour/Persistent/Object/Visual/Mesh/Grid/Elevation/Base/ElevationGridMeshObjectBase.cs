// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [RequireScript(typeof(AssetReference))]
    public class ElevationGridMeshObjectBase : Grid2DMeshObjectBase
    {
        private Elevation _elevation;

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveElevationDelgates(elevation);
                AddElevationDelegates(elevation);

                return true;
            }
            return false;
        }

        private void RemoveElevationDelgates(Elevation elevation)
        {
            if (!Object.ReferenceEquals(elevation, null))
                elevation.PropertyAssignedEvent -= ElevationPropertyAssignedHandler;
        }

        private void AddElevationDelegates(Elevation  elevation)
        {
            if (!IsDisposing() && elevation != Disposable.NULL)
                elevation.PropertyAssignedEvent += ElevationPropertyAssignedHandler;
        }

        private void ElevationPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(AssetBase.data))
                ElevationChanged();
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                UpdateElevation();

                return true;
            }
            return false;
        }

        private AssetReference elevationAssetReference
        {
            get { return AppendToReferenceComponentName(GetReferenceAt(0), typeof(Elevation).Name) as AssetReference; }
        }

        private void UpdateElevation()
        {
            elevation = elevationAssetReference != Disposable.NULL ? elevationAssetReference.data as Elevation : null;
        }

        protected Elevation elevation
        {
            get { return _elevation; }
            private set 
            {
                Elevation oldValue = _elevation;
                Elevation newValue = value;

                if (oldValue == newValue)
                    return;

                RemoveElevationDelgates(oldValue);
                AddElevationDelegates(newValue);

                _elevation = newValue;

                ElevationChanged();
            }
        }

        private void ElevationChanged()
        {
            if (initialized)
            {
                ForceUpdateTransform(true);

                if (meshRendererVisualDirtyFlags is ElevationGridMeshObjectVisualDirtyFlags)
                {
                    ElevationGridMeshObjectVisualDirtyFlags elevationMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as ElevationGridMeshObjectVisualDirtyFlags;

                    elevationMeshRendererVisualDirtyFlags.ElevationChanged();
                }
            }
        }

        public bool GetIsElevationLoaded()
        {
            return elevationAssetReference == Disposable.NULL || elevationAssetReference.IsLoaded();
        }

        public bool GetGeoCoordinateElevation(out double altitude, GeoCoordinate3Double geoCoordinate, bool raycast = false)
        {
            double newAltitude = Double.NaN;

            if (raycast)
            {
                transform.IterateOverRootChildren<MeshRendererVisual>((childMeshRendererVisual) =>
                {
                    if (childMeshRendererVisual != Disposable.NULL)
                    {
                        double maxDistance = parentGeoAstroObject.GetDefaultRaycastMaxDistance();
                        RayDouble ray = parentGeoAstroObject.GetGeoCoordinateRay(geoCoordinate, maxDistance);

                        RaycastHit hitSingle;
                        if (childMeshRendererVisual.GetCollider().Raycast(ray, out hitSingle, (float)maxDistance * 2.0f))
                        {
                            RaycastHitDouble hit = hitSingle;
                            newAltitude = parentGeoAstroObject.GetGeoCoordinateFromPoint(hit.point).altitude;
                            return false;
                        }
                    }
                    return true;
                });
            }
            else
            {
                Vector2 normalizedGrid2DIndex = GetGrid2DIndexFromGeoCoordinate(geoCoordinate) - grid2DIndex;
                newAltitude = GetElevation(normalizedGrid2DIndex.x, normalizedGrid2DIndex.y);
            }

            if (!Double.IsNaN(newAltitude))
            {
                altitude = newAltitude;
                return true;
            }

            altitude = 0.0f;
            return false;
        }

        public float GetElevation(float x, float y)
        {
            float value = 0.0f;

            if (elevation != Disposable.NULL)
            {
                if (grid2DDimensions != elevation.grid2DDimensions)
                {
                    Vector2 projectedGrid2DIndex = MathPlus.ProjectGrid2DIndex(x, y, grid2DIndex, grid2DDimensions, elevation.grid2DIndex, elevation.grid2DDimensions);
                    x = projectedGrid2DIndex.x;
                    y = projectedGrid2DIndex.y;
                }

                value = elevation.GetElevation(x, y);
            }

            return value;
        }

        protected override double GetAltitude(bool addOffset = true)
        {
            return base.GetAltitude(addOffset) + GetElevation(0.5f, 0.5f);
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(ElevationGridMeshObjectVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is ElevationGridMeshObjectVisualDirtyFlags)
            {
                ElevationGridMeshObjectVisualDirtyFlags elevationMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as ElevationGridMeshObjectVisualDirtyFlags;

                elevationMeshRendererVisualDirtyFlags.elevation = elevation;
                elevationMeshRendererVisualDirtyFlags.elevationMultiplier = elevation != Disposable.NULL ? elevation.elevationMultiplier : 0.0f;
            }
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is ElevationGridMeshObjectVisualDirtyFlags)
            {
                ElevationGridMeshObjectVisualDirtyFlags elevationMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as ElevationGridMeshObjectVisualDirtyFlags;

                (parameters as ElevationGridMeshObjectParameters).Init(elevationMeshRendererVisualDirtyFlags.elevation);
            }
        }

        protected override bool AssetLoaded()
        {
            return base.AssetLoaded() && GetIsElevationLoaded();
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            SetTextureToMaterial("_ElevationMap", elevation, Texture2D.blackTexture, material, materialPropertyBlock);
            SetFloatToMaterial("_MinElevation", elevation != null ? elevation.minElevation : 0.0f, material, materialPropertyBlock);
        }

        protected class ElevationGridMeshObjectParameters : Grid2DMeshObjectParameters
        {
            private Elevation _elevation;

            private double _centerElevation;

            public override void Recycle()
            {
                base.Recycle();

                _elevation = null;
                _centerElevation = 0.0d;
            }

            public ElevationGridMeshObjectParameters Init(Elevation elevation = null)
            {
                if (elevation != Disposable.NULL)
                {
                    Lock(elevation);
                    _elevation = elevation;
                }

                if (GetElevation(out double centerElevation, 0.5f, 0.5f))
                    _centerElevation = centerElevation;

                return this;
            }

            public Elevation elevation
            {
                get { return _elevation; }
            }

            public double centerElevation
            {
                get { return _centerElevation; }
            }

            public override bool GetElevation(out double value, float x, float y, bool clamp = false)
            {
                value = 0.0d;
                if (_elevation != Disposable.NULL)
                {
                    value = GetElevation(x, y, clamp);
                    return true;
                }
                return false;
            }

            public override double GetElevation(float x, float y, bool clamp = false)
            {
                if (grid2DDimensions != _elevation.grid2DDimensions)
                {
                    Vector2 projectedGrid2DIndex = MathPlus.ProjectGrid2DIndex(x, y, grid2DIndex, grid2DDimensions, _elevation.grid2DIndex, _elevation.grid2DDimensions);
                    x = projectedGrid2DIndex.x;
                    y = projectedGrid2DIndex.y;
                }

                return _elevation.GetElevation(x, y, clamp) * inverseScale;
            }
        }
    }
}
