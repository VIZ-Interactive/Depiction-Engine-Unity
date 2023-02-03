// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public class Grid2DMeshObjectBase : MeshObjectBase, IGridIndexObject
    {
        private const string TEXTURE_INDEX_DIMENSIONS_POSTFIX = "IndexDimensions";

        [BeginFoldout("Altitude")]
        [SerializeField, Tooltip("The altitude at which the object should be positioned."), EndFoldout]
        private double _altitudeOffset;

        [BeginFoldout("Grid 2D")]
        [SerializeField, Tooltip("The horizontal and vertical size of the grid."), EndFoldout]
        private Vector2Int _grid2DDimensions;

        //Needs to be serialized or otherwise some Grid2DMeshObject get recreated when entering Play mode in the Editor
        [SerializeField, HideInInspector]
        private Vector2Int _grid2DIndex;

        private double _size;
        private float _sphericalRatio;
        private Vector3 _meshRendererVisualLocalScale;

        public override void Recycle()
        {
            base.Recycle();

            _grid2DIndex = Vector2Int.zero;
            _size = 0.0d;
            _sphericalRatio = 0.0f;
            _meshRendererVisualLocalScale = Vector3.zero;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => altitudeOffset = value, 0.0d, initializingState);
            InitValue(value => grid2DDimensions = value, Vector2Int.one, initializingState);
        }

        public override bool IsPhysicsObject()
        {
            return false;
        }

        protected override bool TransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            if (base.TransformChanged(changedComponent, capturedComponent))
            {
                if (changedComponent.HasFlag(TransformBase.Component.Position))
                    UpdateGridIndex();
          
                return true;
            }
            return false;
        }

        protected override void ReferenceLoaderPropertyAssignedChangedHandler(ReferenceBase reference, IProperty serializable, string name, object newValue, object oldValue)
        {
            base.ReferenceLoaderPropertyAssignedChangedHandler(reference, serializable, name, newValue, oldValue);

            if (name == nameof(Index2DLoaderBase.minMaxZoom))
                UpdateReferenceDataIndex2D();
        }

        protected override void Initialized(InstanceManager.InitializationContext initializingState)
        {
            base.Initialized(initializingState);

            UpdateReferenceDataIndex2D();
        }

        public bool IsGridIndexValid()
        {
            return transform != Disposable.NULL && transform.parentGeoAstroObject != Disposable.NULL;
        }

        /// <summary>
        /// The altitude at which the object should be positioned.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public double altitudeOffset
        {
            get { return _altitudeOffset; }
            set
            {
                SetValue(nameof(altitudeOffset), value, ref _altitudeOffset, (newValue, oldValue) =>
                {
                    ForceUpdateTransform(true);
                });
            }
        }

        /// <summary>
        /// The horizontal and vertical size of the grid.
        /// </summary>
        [Json(get:false)]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public Vector2Int grid2DDimensions
        {
            get { return _grid2DDimensions; }
            set
            {
                if (value.x < 1)
                    value.x = 1;
                if (value.y < 1)
                    value.y = 1;
                SetValue(nameof(grid2DDimensions), value, ref _grid2DDimensions, (newValue, oldValue) =>
                {
                    UpdateGridIndex();
                });
            }
        }

        private bool UpdateGridIndex()
        {
            bool changed = false;

            if (transform != Disposable.NULL)
                changed = SetGrid2DIndex(GetGrid2DIndex(grid2DDimensions));

            return changed;
        }

        public Vector2Int grid2DIndex
        {
            get { return _grid2DIndex; }
            private set { SetGrid2DIndex(value); }
        }

        private bool SetGrid2DIndex(Vector2Int value)
        {
            return SetValue(nameof(grid2DIndex), value, ref _grid2DIndex, (newValue, oldValue) =>
            {
                UpdateReferenceDataIndex2D();
            });
        }

        private void UpdateReferenceDataIndex2D()
        {
            if (initialized)
            {
                IterateOverReferences<ReferenceBase>((reference) =>
                {
                    Grid2DIndex grid2DIndex = Grid2DIndex.empty;

                    Index2DLoader index2DLoader = reference.loader as Index2DLoader;
                    if (index2DLoader != Disposable.NULL)
                    {
                        int zoom = MathPlus.GetZoomFromGrid2DDimensions(grid2DDimensions);

                        int clampedZoom = Mathf.Clamp(zoom, index2DLoader.minMaxZoom.x, index2DLoader.minMaxZoom.y);
                        Vector2Int loaderGrid2DDimensions = MathPlus.GetGrid2DDimensionsFromZoom(clampedZoom, index2DLoader.xyTilesRatio);
                        grid2DIndex = new Grid2DIndex(GetGrid2DIndex(loaderGrid2DDimensions), loaderGrid2DDimensions);
                    }
         
                    reference.dataIndex2D = grid2DIndex;

                    return true;
                });
            }
        }

        private Vector2Int GetGrid2DIndex(Vector2Int grid2DDimensions)
        {
            return transform != Disposable.NULL ? GetGrid2DIndex(transform.GetGeoCoordinate(), grid2DDimensions) : Vector2Int.minusOne;
        }

        public GeoCoordinate3Double GetSnapToGridIndexGeoCoordinate(GeoCoordinate2Double geoCoordinate)
        {
            return MathPlus.GetGeoCoordinate3FromIndex(GetGrid2DIndex(geoCoordinate, grid2DDimensions) + new Vector2(0.5f, 0.5f), grid2DDimensions);
        }

        private Vector2Int GetGrid2DIndex(GeoCoordinate2Double geoCoordinate, Vector2Int grid2DDimensions)
        {
            if (transform.GetParentGeoAstroObject() != Disposable.NULL)
                return MathPlus.GetIndexFromGeoCoordinate(geoCoordinate, grid2DDimensions);
            return Vector2Int.minusOne;
        }

        public Vector2Double GetGrid2DIndexFromGeoCoordinate(GeoCoordinate3Double geoCoordinate)
        {
            return MathPlus.GetGrid2DIndexFromGeoCoordinate(geoCoordinate, grid2DDimensions);
        }

        protected override bool SetController(ControllerBase value)
        {
            if (value != Disposable.NULL)
            {
                DisposeManager.Dispose(value);
                Debug.LogWarning("Controller not allowed in " + GetType().Name);
            }
            return base.SetController(value);
        }

        protected override bool SetTransform(TransformDouble value)
        {
            if (base.SetTransform(value))
            {
                UpdateGridIndex();

                return true;
            }
            return false;
        }

        protected double size
        {
            get { return _size; }
            private set { SetValue(nameof(size), value, ref _size); }
        }

        public bool IsValidSphericalRatio()
        {
            return IsSpherical() || IsFlat();
        }

        protected bool IsSpherical()
        {
            return sphericalRatio == 1.0f;
        }

        protected bool IsFlat()
        {
            return sphericalRatio == 0.0f;
        }

        protected float sphericalRatio
        {
            get { return _sphericalRatio; }
            private set { SetValue(nameof(sphericalRatio), value, ref _sphericalRatio); }
        }

        protected Vector3 meshRendererVisualLocalScale
        {
            get { return _meshRendererVisualLocalScale; }
            private set { SetValue(nameof(meshRendererVisualLocalScale), value, ref _meshRendererVisualLocalScale); }
        }

        protected override Vector3Double GetClosestGeoAstroObjectCenterOS(GeoAstroObject closestGeoAstroObject)
        {
            return base.GetClosestGeoAstroObjectCenterOS(closestGeoAstroObject) / meshRendererVisualLocalScale;
        }

        protected virtual double GetAltitude(bool addOffset = true)
        {
            return addOffset ? altitudeOffset : 0.0d;
        }

        public static double GetScale(double size, Vector2Int grid2DDimensions)
        {
            return size / grid2DDimensions.y;
        }

        protected virtual Vector3 GetMeshRendererVisualLocalScale()
        {
            return meshRendererVisualLocalScale;
        }

        protected virtual Color GetColor()
        {
            return Color.clear;
        }

        protected virtual Texture GetColorMap()
        {
            return null;
        }

        protected virtual Texture GetAdditionalMap()
        {
            return null;
        }

        public static bool operator <(Grid2DMeshObjectBase a, Grid2DMeshObjectBase b)
        {
            return a.grid2DDimensions.x < b.grid2DDimensions.x;
        }

        public static bool operator >(Grid2DMeshObjectBase a, Grid2DMeshObjectBase b)
        {
            return a.grid2DDimensions.x > b.grid2DDimensions.x;
        }

        protected override void UpdateMeshRendererVisualModifiers(Action<VisualObjectVisualDirtyFlags> completedCallback, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisualModifiers(completedCallback, meshRendererVisualDirtyFlags);

            UpdateGridMeshRendererVisualModifier();
        }

        protected virtual void UpdateGridMeshRendererVisualModifier()
        {
            if (meshRendererVisualModifiers.Count < 1)
                meshRendererVisualModifiers.Add(MeshRendererVisual.CreateMeshRendererVisualModifier());
        }

        protected override void ParentGeoAstroObjectChanged(GeoAstroObject newValue, GeoAstroObject oldValue)
        {
            base.ParentGeoAstroObjectChanged(newValue, oldValue);

            UpdateGridIndex();
        }

        protected override bool TransformObjectCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformObjectCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (localPositionParam.isGeoCoordinate && transform.parentGeoAstroObject != Disposable.NULL)
                {
                    if (localPositionParam.changed)
                    {
                        GeoCoordinate3Double geoCoordinate = localPositionParam.geoCoordinateValue;

                        bool geoCoordinateChanged = false;

                        GeoCoordinate3Double newGeoCoordinate = GetSnapToGridIndexGeoCoordinate(geoCoordinate);
                        newGeoCoordinate.altitude = geoCoordinate.altitude;
                        if (geoCoordinate.latitude != newGeoCoordinate.latitude || geoCoordinate.longitude != newGeoCoordinate.longitude)
                            geoCoordinateChanged = true;

                        double altitude = GetAltitude();

                        if (newGeoCoordinate.altitude != altitude)
                        {
                            newGeoCoordinate.altitude = altitude;
                            geoCoordinateChanged = true;
                        }

                        if (geoCoordinateChanged)
                            localPositionParam.SetValue(newGeoCoordinate);
                    }

                    ApplyAutoAlignToSurface(localPositionParam, localRotationParam, transform.parentGeoAstroObject);
                }
                return true;
            }
            return false;
        }

        protected override void UpdateVisualProperties()
        {
            base.UpdateVisualProperties();

            bool parentGeoAstroObjectNotNull = parentGeoAstroObject != Disposable.NULL;

            size = parentGeoAstroObjectNotNull ? parentGeoAstroObject.size : 100.0d;

            bool transformDirty = PropertyDirty(nameof(transform));

            if (transformDirty || PropertyDirty(nameof(parentGeoAstroObject)) || (parentGeoAstroObjectNotNull && parentGeoAstroObject.PropertyDirty(nameof(sphericalRatio))))
                sphericalRatio = parentGeoAstroObjectNotNull ? parentGeoAstroObject.GetSphericalRatio() : 0.0f;

            UpdateAltitudeOffset();

            if (transformDirty || PropertyDirty(nameof(size)) || PropertyDirty(nameof(sphericalRatio)) || PropertyDirty(nameof(altitudeOffset)) || PropertyDirty(nameof(grid2DDimensions)))
            {
                double altitudeScale = 1.0d;

                if (parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject.IsSpherical())
                    altitudeScale = (parentGeoAstroObject.radius + altitudeOffset) / parentGeoAstroObject.radius;

                meshRendererVisualLocalScale = Vector3.one * (float)(size / grid2DDimensions.y * altitudeScale);
            }
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(Grid2DMeshObjectBaseVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags is Grid2DMeshObjectBaseVisualDirtyFlags)
            {
                Grid2DMeshObjectBaseVisualDirtyFlags grid2DMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as Grid2DMeshObjectBaseVisualDirtyFlags;

                grid2DMeshRendererVisualDirtyFlags.altitude = GetAltitude(false);
                grid2DMeshRendererVisualDirtyFlags.meshRendererVisualLocalScale = meshRendererVisualLocalScale;
                grid2DMeshRendererVisualDirtyFlags.size = size;
                grid2DMeshRendererVisualDirtyFlags.sphericalRatio = GetSphericalRatio();
                grid2DMeshRendererVisualDirtyFlags.grid2DIndex = grid2DIndex;
                grid2DMeshRendererVisualDirtyFlags.grid2DDimensions = grid2DDimensions;
            }
        }

        protected virtual Type GetProcessorParametersType()
        {
            return null;
        }

        protected virtual void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            if (meshRendererVisualDirtyFlags is Grid2DMeshObjectBaseVisualDirtyFlags)
            {
                Grid2DMeshObjectBaseVisualDirtyFlags grid2DMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as Grid2DMeshObjectBaseVisualDirtyFlags;

                (parameters as Grid2DMeshObjectParameters).Init(grid2DMeshRendererVisualDirtyFlags.altitude, grid2DMeshRendererVisualDirtyFlags.size, grid2DMeshRendererVisualDirtyFlags.grid2DDimensions, grid2DMeshRendererVisualDirtyFlags.meshRendererVisualLocalScale, grid2DMeshRendererVisualDirtyFlags.grid2DIndex, grid2DMeshRendererVisualDirtyFlags.sphericalRatio, GetFlipTriangles());
            }
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            if (visualsChanged || PropertyDirty(nameof(size)) || PropertyDirty(nameof(meshRendererVisualLocalScale)) || PropertyDirty(nameof(popupT)))
            {
                transform.IterateOverRootChildren<MeshRendererVisual>((meshRendererVisual) =>
                { 
                    Vector3 meshRendererVisualLocalScale = GetMeshRendererVisualLocalScale();
                    if (meshRendererVisualLocalScale != Vector3.zero)
                        meshRendererVisual.transform.localScale = meshRendererVisualLocalScale;
                    return true;
                });
            }
        }

        protected virtual void UpdateAltitudeOffset()
        {
            
        }

        protected virtual float GetSphericalRatio()
        {
            return sphericalRatio;
        }

        protected virtual bool GetFlipTriangles()
        {
            return false;
        }

        protected override void SetTextureToMaterial(string name, Texture value, Texture2D defaultTexture, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            base.SetTextureToMaterial(name, value, defaultTexture, material, materialPropertyBlock);

            Vector2Int grid2DIndex = this.grid2DIndex;
            Vector2Int grid2DDimensions = this.grid2DDimensions;

            if (value != Disposable.NULL)
            {
                grid2DIndex = value.grid2DIndex;
                grid2DDimensions = value.grid2DDimensions;
            }

            SetVectorToMaterial(name + TEXTURE_INDEX_DIMENSIONS_POSTFIX, new Vector4(grid2DIndex.x, grid2DIndex.y, grid2DDimensions.x, grid2DDimensions.y), material, materialPropertyBlock);
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            SetVectorToMaterial("_" + TEXTURE_INDEX_DIMENSIONS_POSTFIX, new Vector4(grid2DIndex.x, grid2DIndex.y, grid2DDimensions.x, grid2DDimensions.y), material, materialPropertyBlock);
            SetColorToMaterial("_Color", GetColor(), material, materialPropertyBlock);
            SetTextureToMaterial("_ColorMap", GetColorMap(), Texture2D.blackTexture, material, materialPropertyBlock);
            SetTextureToMaterial("_AdditionalMap", GetAdditionalMap(), Texture2D.whiteTexture, material, materialPropertyBlock);
        }

        protected class Grid2DMeshObjectParameters : ProcessorParameters
        {
            private double _size;
            private Vector2Int _grid2DDimensions;
            private Vector2Int _grid2DIndex;
            private float _sphericalRatio;

            private double _normalizedRadiusSize;
            private double _normalizedCircumferenceSize;
            private double _scale;
            private float _inverseHeightScale;
            private double _inverseScale;

            private Vector3Double _centerPoint;
            private QuaternionDouble _centerRotation;
            private QuaternionDouble _inverseCenterRotation;

            private bool _flipTriangles;

            public override void Recycle()
            {
                base.Recycle();

                _size = 0.0d;
                _grid2DDimensions = Vector2Int.zero;
                _inverseHeightScale = 0.0f;
                _grid2DIndex = Vector2Int.zero;
                _sphericalRatio = 0.0f;

                _normalizedRadiusSize = 0.0d;
                _normalizedCircumferenceSize = 0.0d;
                _scale = 0.0d;
                _inverseScale = 0.0d;

                _centerPoint = Vector3Double.zero;
                _centerRotation = QuaternionDouble.identity;
                _inverseCenterRotation = QuaternionDouble.identity;

                _flipTriangles = false;
            }

            public Grid2DMeshObjectParameters Init(double altitude, double size, Vector2Int grid2DDimensions, Vector3 meshRendererVisualLocalScale, Vector2Int grid2DIndex, float sphericalRatio, bool flipTriangles = false)
            {
                _size = size;
                _grid2DDimensions = grid2DDimensions;
                _grid2DIndex = grid2DIndex != Vector2Int.minusOne ? grid2DIndex : Vector2Int.zero;
                _sphericalRatio = sphericalRatio;

                _normalizedRadiusSize = MathPlus.DOUBLE_RADIUS * _grid2DDimensions.y;
                _normalizedCircumferenceSize = MathPlus.SIZE * _grid2DDimensions.y;
                _scale = GetScale(_size, _grid2DDimensions);
                _inverseHeightScale = meshRendererVisualLocalScale.x / meshRendererVisualLocalScale.y;
                _inverseScale = 1.0d / _size * _normalizedCircumferenceSize;

                GeoCoordinate3Double centerGeoCoordinate = MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(_grid2DIndex.x + 0.5d, _grid2DIndex.y + 0.5d), grid2DDimensions);
                if (UseAltitude())
                    centerGeoCoordinate.altitude = altitude * _inverseScale;

                _centerPoint = MathPlus.GetLocalPointFromGeoCoordinate(centerGeoCoordinate, _sphericalRatio, _normalizedRadiusSize, _normalizedCircumferenceSize);
                _centerRotation = MathPlus.GetUpVectorFromGeoCoordinate(centerGeoCoordinate, _sphericalRatio);
                _inverseCenterRotation = Quaternion.Inverse(_centerRotation);

                _flipTriangles = flipTriangles;

                return this;
            }

            protected virtual bool UseAltitude()
            {
                return true;
            }

            public double size
            {
                get { return _size; }
            }

            public Vector2Int grid2DDimensions
            {
                get { return _grid2DDimensions; }
            }

            public Vector2Int grid2DIndex
            {
                get { return _grid2DIndex; }
            }

            public float sphericalRatio
            {
                get { return _sphericalRatio; }
            }

            public double scale
            {
                get { return _scale; }
            }

            public float inverseHeightScale
            {
                get { return _inverseHeightScale; }
            }

            public double inverseScale
            {
                get { return _inverseScale; }
            }

            public double normalizedRadiusSize
            {
                get { return _normalizedRadiusSize; }
            }

            public double normalizedCircumferenceSize
            {
                get { return _normalizedCircumferenceSize; }
            }

            public Vector3Double centerPoint
            {
                get { return _centerPoint; }
            }

            public QuaternionDouble centerRotation
            {
                get { return _centerRotation; }
            }

            public bool flipTriangles
            {
                get { return _flipTriangles; }
            }

            public virtual bool GetElevation(out double value, float x, float y, bool clamp = false)
            {
                value = 0;
                return false;
            }

            public virtual double GetElevation(float x, float y, bool clamp = false)
            {
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3Double TransformGeoCoordinateToVector(double latitude, double longitude, double altitude = 0.0d)
            {
                Vector3Double point = MathPlus.GetLocalPointFromGeoCoordinate(new GeoCoordinate3Double(latitude, longitude, altitude), _sphericalRatio, _normalizedRadiusSize, _normalizedCircumferenceSize);
                Vector3 vector = _inverseCenterRotation * (point - _centerPoint);
                vector.y *= _inverseHeightScale;
                return vector;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public QuaternionDouble GetUpVector(GeoCoordinate2Double geoCoordinate)
            {
                return _inverseCenterRotation * MathPlus.GetUpVectorFromGeoCoordinate(geoCoordinate, _sphericalRatio);
            }
        }
    }
}
