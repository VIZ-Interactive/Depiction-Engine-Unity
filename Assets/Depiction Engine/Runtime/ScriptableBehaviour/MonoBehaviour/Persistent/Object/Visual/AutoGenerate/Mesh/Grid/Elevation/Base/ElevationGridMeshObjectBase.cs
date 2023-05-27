// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    [CreateComponent(typeof(AssetReference))]
    public class ElevationGridMeshObjectBase : Grid2DMeshObjectBase, IElevationGrid
    {
        public const string ELEVATION_REFERENCE_DATATYPE = nameof(Elevation);

        private Elevation _elevation;

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            InitializeReferenceDataType(ELEVATION_REFERENCE_DATATYPE, typeof(AssetReference));
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveElevationDelegates(elevation);
                AddElevationDelegates(elevation);

                return true;
            }
            return false;
        }

        private void RemoveElevationDelegates(Elevation elevation)
        {
            if (elevation is not null)
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
                if (!callback.Invoke(elevation, elevationAssetReference, elevationAssetReference != Disposable.NULL && elevationAssetReference.dataIndex2D != Grid2DIndex.Empty))
                    return false;

                return true;
            }
            return false;
        }

        private AssetReference elevationAssetReference
        {
            get => GetFirstReferenceOfType(ELEVATION_REFERENCE_DATATYPE) as AssetReference;
        }

        public Elevation elevation
        {
            get => _elevation;
            private set 
            {
                SetValue(nameof(elevation), value, ref _elevation, (newValue, oldValue) => 
                {
                    if (initialized & HasChanged(newValue, oldValue, false))
                    {
                        RemoveElevationDelegates(oldValue);
                        AddElevationDelegates(newValue);

                        ElevationChanged();
                    }
                });
            }
        }

        private void ElevationChanged()
        {
            if (initialized)
            {
                Datasource.StartAllowAutoDisposeOnOutOfSynchProperty();

                ForceUpdateTransform(true);

                Datasource.EndAllowAutoDisposeOnOutOfSynchProperty();
            }
        }

        public bool GetGeoCoordinateElevation(out float elevation, GeoCoordinate3Double geoCoordinate, bool raycast = false)
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
                    elevation = (float)parentGeoAstroObject.GetGeoCoordinateFromPoint(hit.point).altitude;
                    return true;
                }
            }
            else
            {
                Vector2 normalizedGrid2DIndex = GetGrid2DIndexFromGeoCoordinate(geoCoordinate) - grid2DIndex;
                normalizedGrid2DIndex.y = 1.0f - normalizedGrid2DIndex.y;
                if (GetElevation(normalizedGrid2DIndex, out elevation))
                    return true;
            }

            elevation = 0.0f;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetElevation(Vector2 normalizedCoordinate, out float elevation)
        {
            if (elevationAssetReference.IsLoaded() && this.elevation == Disposable.NULL)
            {
                elevation = 0.0f;
                return true;
            }

            return GetElevation(this, normalizedCoordinate, out elevation, altitudeOffset);
        }

        [ThreadStatic]
        private static List<float> _elevationSamples;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetElevation(IElevationGrid elevationGrid, Vector2 normalizedCoordinate, out float elevation, float altitudeOffset = 0.0f)
        {
            if (elevationGrid.elevation != Disposable.NULL)
            {
                normalizedCoordinate.x = Mathf.Clamp01(normalizedCoordinate.x);
                normalizedCoordinate.y = Mathf.Clamp01(normalizedCoordinate.y);

                if (elevationGrid.elevation.GetElevation(elevationGrid.elevation.GetPixelFromNormalizedCoordinate(GetProjectedNormalizedCoordinate(elevationGrid, elevationGrid.elevation, normalizedCoordinate)), out elevation))
                {
                    elevation += altitudeOffset;
                    return true;
                }
            }

            elevation = 0.0f;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAltitude(bool addOffset = true)
        {
            double altitude = base.GetAltitude(addOffset);

            if (GetElevation(new Vector2(0.5f, 0.5f), out float elevation))
                altitude += elevation;

            return altitude;
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
                if (elevation != Disposable.NULL)
                    elevationMeshRendererVisualDirtyFlags.elevationMultiplier = elevation.elevationMultiplier;
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

            bool hasElevation = elevation != null;
            SetTextureToMaterial("_ElevationMap", elevation, Texture2D.blackTexture, material, materialPropertyBlock);
            SetFloatToMaterial("_MinElevation", hasElevation ? elevation.minElevation : 0.0f, material, materialPropertyBlock);
            SetFloatToMaterial("_ElevationMultiplier", hasElevation ? elevation.elevationMultiplier : 1.0f, material, materialPropertyBlock);
            SetIntToMaterial("_ElevationPixelBuffer", hasElevation && elevation.pixelBuffer ? 1 : 0, material, materialPropertyBlock);
        }

        protected class ElevationGridMeshObjectParameters : Grid2DMeshObjectParameters, IElevationGrid
        {
            private Elevation _elevation;

            private float _centerElevation;

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

                if (GetElevation(new Vector2(0.5f, 0.5f), out float centerElevation))
                    _centerElevation = centerElevation;

                return this;
            }

            public Elevation elevation
            {
                get => _elevation;
            }

            public float centerElevation
            {
                get => _centerElevation;
            }

            public bool GetElevation(Vector2 normalizedCoordinate, out float elevation)
            {
                if (ElevationGridMeshObjectBase.GetElevation(this, normalizedCoordinate, out elevation))
                {
                    elevation *= inverseScale;
                    return true;
                }
                return false;
            }
        }
    }
}
