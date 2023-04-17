// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [CreateComponent(typeof(AssetReference))]
    public class ElevationGridMeshObjectBase : Grid2DMeshObjectBase
    {
        public const string ELEVATION_REFERENCE_DATATYPE = nameof(Elevation);

        private Elevation _elevation;

        protected override void CreateComponents(InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            InitializeReferenceDataType(ELEVATION_REFERENCE_DATATYPE, typeof(AssetReference));
        }

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
            {
                ElevationChanged();
                (meshRendererVisualDirtyFlags as ElevationGridMeshObjectVisualDirtyFlags).ElevationChanged();
            }
        }

        protected override bool UpdateReferences(bool forceUpdate = false)
        {
            if (base.UpdateReferences(forceUpdate))
            {
                elevation = GetAssetFromAssetReference<Elevation>(elevationAssetReference);

                return true;
            }
            return false;
        }

        protected override bool IterateOverAssetReferences(Func<AssetBase, AssetReference, bool, bool> callback)
        {
            if (base.IterateOverAssetReferences(callback))
            {
                if (!callback.Invoke(elevation, elevationAssetReference, false))
                    return false;

                return true;
            }
            return false;
        }

        private AssetReference elevationAssetReference
        {
            get => GetFirstReferenceOfType(ELEVATION_REFERENCE_DATATYPE) as AssetReference;
        }

        protected Elevation elevation
        {
            get { return _elevation; }
            private set 
            {
                Elevation oldValue = _elevation;
                Elevation newValue = value;

                if (Object.ReferenceEquals(oldValue, newValue))
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
                Datasource.AllowAutoDisposeOnOutOfSynchProperty(() => { ForceUpdateTransform(true); });
        }

        public bool GetGeoCoordinateElevation(out double elevation, GeoCoordinate3Double geoCoordinate, bool raycast = false)
        {
            if (raycast)
            {
                RaycastHitDouble hit = null;

                transform.IterateOverChildren<MeshRendererVisual>((childMeshRendererVisual) =>
                {
                    if (childMeshRendererVisual != Disposable.NULL)
                    {
                        Collider collider = childMeshRendererVisual.GetCollider();
                        if (collider != null)
                        {
                            double maxDistance = parentGeoAstroObject.GetDefaultRaycastMaxDistance();
                            RayDouble ray = parentGeoAstroObject.GetGeoCoordinateRay(geoCoordinate, maxDistance);

                            if (collider.Raycast(ray, out hit, (float)maxDistance * 2.0f))
                                return false;
                        }
                    }
                    return true;
                });

                if (hit != null)
                {
                    elevation = parentGeoAstroObject.GetGeoCoordinateFromPoint(hit.point).altitude;
                    return true;
                }
            }
            else
            {
                Vector2 normalizedGrid2DIndex = GetGrid2DIndexFromGeoCoordinate(geoCoordinate) - grid2DIndex;
                if (GetElevation(out elevation, normalizedGrid2DIndex.x, normalizedGrid2DIndex.y))
                    return true;
            }

            elevation = 0.0f;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetElevation(out double elevation, float x, float y)
        {
            elevation = 0.0f;

            if (this.elevation != Disposable.NULL)
            {
                Vector2 pixel = GetProjectedPixel(this.elevation, x, y);

                elevation = this.elevation.GetElevation(pixel.x, pixel.y);

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAltitude(bool addOffset = true)
        {
            GetElevation(out double elevation, 0.5f, 0.5f);
            return base.GetAltitude(addOffset) + elevation;
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

                _elevation = default;
                _centerElevation = default;
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
