// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public class Grid2DMeshObjectBase : MeshObjectBase, IGrid2DIndex
    {
        private const string TEXTURE_INDEX_DIMENSIONS_POSTFIX = "IndexDimensions";

        [BeginFoldout("Altitude")]
        [SerializeField, Tooltip("The altitude at which the object should be positioned."), EndFoldout]
        private float _altitudeOffset;

        [BeginFoldout("Grid 2D")]
        [SerializeField, Tooltip("The horizontal and vertical size of the grid.")]
        private Vector2Int _grid2DDimensions;

        //Needs to be serialized or otherwise some Grid2DMeshObject get recreated when entering Play mode in the Editor
        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private Vector2Int _grid2DIndex;

        private Vector3 _meshRendererVisualLocalScale;

        public override void Recycle()
        {
            base.Recycle();

            _grid2DIndex = default;
            _grid2DDimensions = default;
            _meshRendererVisualLocalScale = default;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => altitudeOffset = value, 0.0f, initializingContext);
            InitValue(value => grid2DDimensions = value, Vector2Int.one, initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateReferenceDataIndex2D();

            UpdateMeshRendererVisualLocalScale();

            UpdateChildrenMeshRendererVisualLocalScale();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
#if UNITY_EDITOR
                UpdateLeftMouseUpInSceneOrInspectorDelegate();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void UpdateLeftMouseUpInSceneOrInspectorDelegate()
        {
            SceneManager.LeftMouseUpInSceneOrInspectorEvent -= LeftMouseUpInSceneOrInspectorHandler;
            if (isBeingMovedByUser && !IsDisposing())
                SceneManager.LeftMouseUpInSceneOrInspectorEvent += LeftMouseUpInSceneOrInspectorHandler;
        }

        protected override void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            base.ParentGeoAstroObjectPropertyAssignedHandler(property, name, newValue, oldValue);

            if (initialized)
            {
                if (name == nameof(GeoAstroObject.size) || name == nameof(GeoAstroObject.sphericalRatio))
                    UpdateMeshRendererVisualLocalScale();
            }
        }

        private void LeftMouseUpInSceneOrInspectorHandler()
        {
            snapToGridTimer = tweenManager.DelayedCall(0.1f, null, () => { ForceUpdateTransform(true, true); }, () => { snapToGridTimer = null; });
        }

        private Tween _snapToGridTimer;
        private Tween snapToGridTimer
        {
            get => _snapToGridTimer;
            set
            {
                if (Object.ReferenceEquals(_snapToGridTimer, value))
                    return;

                DisposeManager.Dispose(_snapToGridTimer);

                _snapToGridTimer = value;
            }
        }

        private bool _isBeingMovedByUser;
        private bool isBeingMovedByUser
        {
            get => _isBeingMovedByUser;
            set
            {
                if (_isBeingMovedByUser == value)
                    return;

                _isBeingMovedByUser = value;

                UpdateLeftMouseUpInSceneOrInspectorDelegate();
            }
        }
#endif

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

            if (name == nameof(Index2DLoaderBase.minMaxZoom) || name == nameof(Index2DLoaderBase.xyTilesRatio))
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
        public float altitudeOffset
        {
            get => _altitudeOffset;
            set
            {
                SetValue(nameof(altitudeOffset), value, ref _altitudeOffset, (newValue, oldValue) =>
                {
                    ForceUpdateTransform(true);

                    if (initialized)
                        UpdateMeshRendererVisualLocalScale();
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
            get => _grid2DDimensions;
            set
            {
                if (value.x < 1)
                    value.x = 1;
                if (value.y < 1)
                    value.y = 1;
                SetValue(nameof(grid2DDimensions), value, ref _grid2DDimensions, (newValue, oldValue) =>
                {
                    if (!UpdateGridIndex() && initialized)
                        ForceUpdateTransform(true, true);

                    if (initialized)
                        UpdateMeshRendererVisualLocalScale();
                });
            }
        }

        private bool UpdateGridIndex()
        {
            return transform != Disposable.NULL && SetGrid2DIndex(GetGrid2DIndex(grid2DDimensions));
        }

        public Vector2Int grid2DIndex
        {
            get => _grid2DIndex;
            private set => SetGrid2DIndex(value);
        }

        private bool SetGrid2DIndex(Vector2Int value)
        {
            return SetValue(nameof(grid2DIndex), value, ref _grid2DIndex, (newValue, oldValue) =>
            {
                UpdateReferenceDataIndex2D();
                if (initialized)
                    ForceUpdateTransform(true, true);
            });
        }

        private void UpdateReferenceDataIndex2D()
        {
            if (initialized)
            {
                IterateOverReferences<ReferenceBase>((reference) =>
                {
                    Grid2DIndex grid2DIndex = Grid2DIndex.Empty;

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
            get => parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject.size : 100.0f;
        }

        protected virtual float GetSphericalRatio()
        {
            return parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject.sphericalRatio : -1.0f;
        }

        public bool IsValidSphericalRatio()
        {
            return IsSpherical() || IsFlat();
        }

        protected bool IsSpherical()
        {
            return GetSphericalRatio() == 1.0f;
        }

        protected bool IsFlat()
        {
            return GetSphericalRatio() == 0.0f;
        }

        protected override Vector3 GetMeshRendererVisualLocalScale()
        {
            return meshRendererVisualLocalScale;
        }

        protected Vector3 meshRendererVisualLocalScale
        {
            get => _meshRendererVisualLocalScale;
            private set 
            { 
                SetValue(nameof(meshRendererVisualLocalScale), value, ref _meshRendererVisualLocalScale, (newValue, oldValue) => 
                {
                    if (initialized)
                        UpdateChildrenMeshRendererVisualLocalScale();
                }); 
            }
        }

        protected override double GetAltitude(bool addOffset = true)
        {
            double altitude = base.GetAltitude(addOffset);

            if (addOffset)
                altitude += altitudeOffset;

            return altitude;
        }

        public static float GetScale(double size, Vector2Int grid2DDimensions)
        {
            return (float)(size / grid2DDimensions.y);
        }

        protected virtual Texture GetColorMap()
        {
            return null;
        }

        protected virtual Texture GetAdditionalMap()
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetTexturePixel(Texture texture, Vector2 normalizedCoordinate, out Color color)
        {
            if (texture != Disposable.NULL)
                return texture.GetPixel(texture.GetPixelFromNormalizedCoordinate(GetProjectedNormalizedCoordinate(this, texture, normalizedCoordinate)), out color);

            color = Color.clear;
            return false;
        }

        public static Vector2 GetProjectedNormalizedCoordinate(IGrid2DIndex iGrid2DIndex,Texture texture, Vector2 normalizedCoordinate)
        {
            if (iGrid2DIndex.grid2DDimensions != texture.grid2DDimensions)
            {
                Vector2 projectedGrid2DIndex = MathPlus.ProjectGrid2DIndex(normalizedCoordinate.x, normalizedCoordinate.y, iGrid2DIndex.grid2DIndex, iGrid2DIndex.grid2DDimensions, texture.grid2DIndex, texture.grid2DDimensions);
                normalizedCoordinate.x = projectedGrid2DIndex.x;
                normalizedCoordinate.y = projectedGrid2DIndex.y;
            }

            return normalizedCoordinate;
        }

        public static bool operator <(Grid2DMeshObjectBase a, Grid2DMeshObjectBase b)
        {
            return a.grid2DDimensions.x < b.grid2DDimensions.x;
        }

        public static bool operator >(Grid2DMeshObjectBase a, Grid2DMeshObjectBase b)
        {
            return a.grid2DDimensions.x > b.grid2DDimensions.x;
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
                if (!IsDisposing() && localPositionParam.isGeoCoordinate && transform.parentGeoAstroObject != Disposable.NULL)
                {
                    if (localPositionParam.changed)
                    {
                        GeoCoordinate3Double geoCoordinate = localPositionParam.geoCoordinateValue;

                        bool geoCoordinateChanged = false;

                        GeoCoordinate3Double newGeoCoordinate = GetSnapToGridIndexGeoCoordinate(geoCoordinate);

#if UNITY_EDITOR
                        if (!SceneManager.IsEditorNamespace(GetType()))
                        {
                            isBeingMovedByUser = Editor.SceneViewDouble.lastActiveSceneViewDouble != null && Editor.SceneViewDouble.lastActiveSceneViewDouble.positionHandleDragging;
                            if (isBeingMovedByUser) 
                                newGeoCoordinate = geoCoordinate;
                        }
#endif

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

            UpdateAltitudeOffset();
        }

        private void UpdateMeshRendererVisualLocalScale()
        {
            double altitudeScale = 1.0d;

            if (parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject.IsSpherical())
                altitudeScale = (parentGeoAstroObject.radius + altitudeOffset) / parentGeoAstroObject.radius;

            meshRendererVisualLocalScale = Vector3.one * (float)(size / grid2DDimensions.y * altitudeScale);
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
                grid2DMeshRendererVisualDirtyFlags.SetSphericalRatio(GetSphericalRatio(), wasFirstUpdated);
                grid2DMeshRendererVisualDirtyFlags.grid2DIndex = grid2DIndex;
                grid2DMeshRendererVisualDirtyFlags.grid2DDimensions = grid2DDimensions;
            }
        }

        protected override void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            base.InitializeProcessorParameters(parameters);

            if (meshRendererVisualDirtyFlags is Grid2DMeshObjectBaseVisualDirtyFlags)
            {
                Grid2DMeshObjectBaseVisualDirtyFlags grid2DMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as Grid2DMeshObjectBaseVisualDirtyFlags;

                (parameters as Grid2DMeshObjectParameters).Init(grid2DMeshRendererVisualDirtyFlags.altitude, grid2DMeshRendererVisualDirtyFlags.size, grid2DMeshRendererVisualDirtyFlags.grid2DDimensions, grid2DMeshRendererVisualDirtyFlags.meshRendererVisualLocalScale, grid2DMeshRendererVisualDirtyFlags.grid2DIndex, grid2DMeshRendererVisualDirtyFlags.sphericalRatio, GetFlipTriangles());
            }
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            if (visualsChanged)
                UpdateChildrenMeshRendererVisualLocalScale();
        }

        protected void UpdateChildrenMeshRendererVisualLocalScale()
        {
            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                Vector3 meshRendererVisualLocalScale = GetMeshRendererVisualLocalScale();
                if (meshRendererVisualLocalScale != Vector3.zero)
                    meshRendererVisual.transform.localScale = meshRendererVisualLocalScale;
                return true;
            });
        }

        protected virtual void UpdateAltitudeOffset()
        {
            
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
            SetTextureToMaterial("_ColorMap", GetColorMap(), Texture2D.blackTexture, material, materialPropertyBlock);
            SetTextureToMaterial("_AdditionalMap", GetAdditionalMap(), Texture2D.whiteTexture, material, materialPropertyBlock);
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
#if UNITY_EDITOR
                snapToGridTimer = null;
#endif
                return true;
            }
            return false;
        }

        protected class Grid2DMeshObjectParameters : ProcessorParameters, IGrid2DIndex
        {
            private double _size;
            private Vector2Int _grid2DDimensions;
            private Vector2Int _grid2DIndex;
            private float _sphericalRatio;

            private double _normalizedRadiusSize;
            private double _normalizedCircumferenceSize;
            private float _scale;
            private float _inverseHeightScale;
            private float _inverseScale;

            private Vector3Double _centerPoint;
            private QuaternionDouble _centerRotation;
            private QuaternionDouble _inverseCenterRotation;

            private bool _flipTriangles;

            public override void Recycle()
            {
                base.Recycle();

                _size = default;
                _grid2DDimensions = default;
                _inverseHeightScale = default;
                _grid2DIndex = default;
                _sphericalRatio = default;

                _normalizedRadiusSize = default;
                _normalizedCircumferenceSize = default;
                _scale = default;
                _inverseScale = default;

                _centerPoint = default;
                _centerRotation = default;
                _inverseCenterRotation = default;

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
                _inverseScale = (float)(1.0d / _size * _normalizedCircumferenceSize);

                GeoCoordinate3Double centerGeoCoordinate = MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(_grid2DIndex.x + 0.5d, _grid2DIndex.y + 0.5d), grid2DDimensions);
                if (UseAltitude())
                    centerGeoCoordinate.altitude = altitude * inverseScale;

                _centerPoint = MathPlus.GetLocalPointFromGeoCoordinate(centerGeoCoordinate, _sphericalRatio, _normalizedRadiusSize, _normalizedCircumferenceSize);
                _centerRotation = MathPlus.GetUpVectorFromGeoCoordinate(centerGeoCoordinate, _sphericalRatio);
                _inverseCenterRotation = QuaternionDouble.Inverse(_centerRotation);

                _flipTriangles = flipTriangles;

                return this;
            }

            protected virtual bool UseAltitude()
            {
                return true;
            }

            public double size
            {
                get => _size;
            }

            public Vector2Int grid2DDimensions
            {
                get => _grid2DDimensions;
            }

            public Vector2Int grid2DIndex
            {
                get => _grid2DIndex;
            }

            public float sphericalRatio
            {
                get => _sphericalRatio;
            }

            public float scale
            {
                get => _scale;
            }

            public float inverseHeightScale
            {
                get => _inverseHeightScale;
            }

            public float inverseScale
            {
                get => _inverseScale;
            }

            public double normalizedRadiusSize
            {
                get => _normalizedRadiusSize;
            }

            public double normalizedCircumferenceSize
            {
                get => _normalizedCircumferenceSize;
            }

            public Vector3Double centerPoint
            {
                get => _centerPoint;
            }

            public QuaternionDouble centerRotation
            {
                get => _centerRotation;
            }

            public bool flipTriangles
            {
                get => _flipTriangles;
            }

            public bool IsGridIndexValid()
            {
                return true;
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
