// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Loader used to display Camera centric data.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/Grid/" + nameof(CameraGrid2DLoader))]
    public class CameraGrid2DLoader : Grid2DLoaderBase
    {
        /// <summary>
        /// The different parts of a camera that the <see cref="DepictionEngine.CameraGrid2DLoader"/> can center itself on. <br/><br/>
        /// <b><see cref="Target"/>:</b> <br/>
        /// Center on the camera target. <br/><br/>
        /// <b><see cref="Camera"/>:</b> <br/>
        /// Center on the camera.
        /// </summary>
        public enum CameraCenterOnType
        {
            Target,
            Camera
        };

        [Serializable]
        private class CameraGridLoaderCenterOnLoadTriggerDictionary : SerializableDictionary<int, CameraGridLoaderCenterOnLoadTrigger> { };

        [BeginFoldout("Camera")]
        [SerializeField, Tooltip("The minimum zoom level to display even at long distances. This value should be within the bounds of the 'minMaxZoom' property.")]
        private uint _minZoom;
        [SerializeField, MinMaxRange(0.0f, MAX_ZOOM), Tooltip("Dictates the number of grids and their respective zoom values. For example, a cascade range of 2-6 means that 5 grids will be created with the first one having its zoom value offsetted by 2 then 3,4,5 and 6 for the fifth one.")]
        private Vector2Int _cascades;
        [SerializeField, MinMaxRange(0.0f, MAX_ZOOM), Tooltip("MeshObject whose scope zoom value falls outside of this range will have their "+nameof(MeshObjectBase.useCollider)+" value set to false.")]
        private Vector2Int _collidersRange;
        [SerializeField, Tooltip("The center (Camera or target) from which the grid size will be calculated and tiles will be first loaded."), EndFoldout]
        private CameraCenterOnType _centerOn;

        [BeginFoldout("Size")]
        [SerializeField, Tooltip("A factor by which the grid size will be multiplied. Use to increase or decrease the level of detail.")]
        private float _sizeMultiplier;
        [SerializeField, Tooltip("When enabled the grid size will be automatically adjusted based on latitude to preserve a constant tile / pixel density. Only relevant in spherical mode.")]
        private bool _sizeLatitudeCompensation;
        [SerializeField, Tooltip("A factor by which the grid size will be multiplied with camera delta movement to alter grid size. This helps dynamically lower the number of tiles loaded when the camera is moving fast to help with performance. Set to zero to deactivate.")]
        private float _sizeOffsettingMultiplier;
        [SerializeField, Tooltip("The max number the size offsetting multiplier can reach. Higher values will be clamped.")]
        private float _sizeOffsettingMaximum;
        [SerializeField, Tooltip("The amount of time required for the grid size to return to normal once fast camera movement is no longer detected."), EndFoldout]
        private float _sizeOffsettingDuration;

        private CameraGridLoaderCenterOnLoadTriggerDictionary _cameraGridLoaderCenterOnLoadTriggers;

        private List<CameraGrid2D> _cameraGrids;

#if UNITY_EDITOR
        protected override bool GetShowZoom()
        {
            return false;
        }

        protected override bool GetShowWaitBetweenLoad()
        {
            return true;
        }

        protected override bool GetShowLoadDelay()
        {
            return true;
        }
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => minZoom = value, (uint)3, initializingContext);
            InitValue(value => cascades = value, new Vector2Int(0, MAX_ZOOM), initializingContext);
            InitValue(value => collidersRange = value, new Vector2Int(0, MAX_ZOOM), initializingContext);
            InitValue(value => centerOn = value, CameraCenterOnType.Camera, initializingContext);
            InitValue(value => sizeMultiplier = value, 2.0f, initializingContext);
            InitValue(value => sizeLatitudeCompensation = value, true, initializingContext);
            InitValue(value => sizeOffsettingMultiplier = value, 1.2f, initializingContext);
            InitValue(value => sizeOffsettingMaximum = value, 1.0f, initializingContext);
            InitValue(value => sizeOffsettingDuration = value, 2.0f, initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            QueueAutoUpdate();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                foreach (CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger in cameraGridLoaderCenterOnLoadTriggers.Values)
                {
                    cameraGridLoaderCenterOnLoadTrigger.UpdateAllDelegates(IsDisposing());
                    RemoveCameraGridLoaderCenterOnLoadTriggerDelegates(cameraGridLoaderCenterOnLoadTrigger);
                    if (!IsDisposing())
                        AddCameraGridLoaderCenterOnLoadTriggerDelegates(cameraGridLoaderCenterOnLoadTrigger);
                }

                return true;
            }
            return false;
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            Camera camera = property as Camera;
            if (camera != Disposable.NULL)
                UpdateLoadTriggers();
        }

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceRemovedHandler(property);

            Camera camera = property as Camera;
            if (camera != Disposable.NULL)
                UpdateLoadTriggers();
        }

        private void AddCameraGridLoaderCenterOnLoadTriggerDelegates(CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger)
        {
            if (cameraGridLoaderCenterOnLoadTrigger != null)
            {
                cameraGridLoaderCenterOnLoadTrigger.CameraDisposingEvent += CameraGridLoaderCenterOnLoadTriggerDisposingHandler;
                cameraGridLoaderCenterOnLoadTrigger.QueueAutoUpdateEvent += CameraGridLoaderCenterOnLoadTriggerQueueAutoUpdateHandler;
            }
        }

        private void RemoveCameraGridLoaderCenterOnLoadTriggerDelegates(CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger)
        {
            if (cameraGridLoaderCenterOnLoadTrigger is not null)
            {
                cameraGridLoaderCenterOnLoadTrigger.CameraDisposingEvent -= CameraGridLoaderCenterOnLoadTriggerDisposingHandler;
                cameraGridLoaderCenterOnLoadTrigger.QueueAutoUpdateEvent -= CameraGridLoaderCenterOnLoadTriggerQueueAutoUpdateHandler;
            }
        }

        private void CameraGridLoaderCenterOnLoadTriggerDisposingHandler(CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger)
        {
            RemoveCameraGridLoaderCenterOnLoadTrigger(cameraGridLoaderCenterOnLoadTrigger);
        }

        private void CameraGridLoaderCenterOnLoadTriggerQueueAutoUpdateHandler()
        {
            QueueAutoUpdate();
        }

        protected override bool SetParentGeoAstroObject(GeoAstroObject newValue, GeoAstroObject oldValue)
        {
            if (base.SetParentGeoAstroObject(newValue, oldValue))
            {
                QueueAutoUpdate();

                return true;
            }
            return false;
        }

        /// <summary>
        /// The minimum zoom level to display even at long distances. This value should be within the bounds of the 'minMaxZoom' property.
        /// </summary>
        [Json]
        public uint minZoom
        {
            get => _minZoom;
            set => SetValue(nameof(minZoom), (uint)Mathf.Clamp(value, 0, MAX_ZOOM), ref _minZoom, (newValue, oldValue) => { QueueAutoUpdate(); });
        }

        /// <summary>
        /// Dictates the number of grids and their respective zoom values.
        /// </summary>
        /// <remarks>
        /// For example, a cascade range of 2-6 means that 5 grids will be created with the first one having its zoom value offsetted by 2 then 3,4,5 and 6 for the fifth one.
        /// </remarks>
        [Json]
        public Vector2Int cascades
        {
            get => _cascades;
            set => SetValue(nameof(cascades), value, ref _cascades, (newValue, oldValue) => { QueueAutoUpdate(); });
        }

        /// <summary>
        /// MeshObject whose scope zoom value falls outside of this range will have their <see cref="DepictionEngine.MeshObjectBase.useCollider"/> value set to false.
        /// </summary>
        [Json]
        public Vector2Int collidersRange
        {
            get => _collidersRange;
            set => SetValue(nameof(collidersRange), value, ref _collidersRange);
        }

        /// <summary>
        /// The center (Camera or target) from which the grid size will be calculated and tiles will be first loaded.
        /// </summary>
        [Json]
        public CameraCenterOnType centerOn
        {
            get => _centerOn;
            set => SetValue(nameof(centerOn), value, ref _centerOn, (newValue, oldValue) => { QueueAutoUpdate(); });
        }

        /// <summary>
        /// A factor by which the grid size will be multiplied.
        /// </summary>
        [Json]
        public float sizeMultiplier
        {
            get => _sizeMultiplier;
            set => SetValue(nameof(sizeMultiplier), value, ref _sizeMultiplier, (newValue, oldValue) => { QueueAutoUpdate(); });
        }

        /// <summary>
        /// When enabled the grid size will be automatically adjusted based on latitude to preserve a constant tile / pixel density. Only works in spherical mode. 
        /// </summary>
        [Json]
        public bool sizeLatitudeCompensation
        {
            get => _sizeLatitudeCompensation;
            set => SetValue(nameof(sizeLatitudeCompensation), value, ref _sizeLatitudeCompensation, (newValue, oldValue) => { QueueAutoUpdate(); });
        }

        /// <summary>
        /// A factor by which the grid size will be multiplied with camera delta movement to alter grid size. This helps dynamically lower the number of tiles loaded when the camera is moving fast to help with performance. Set to zero to deactivate.
        /// </summary>
        [Json]
        public float sizeOffsettingMultiplier
        {
            get => _sizeOffsettingMultiplier;
            set => SetValue(nameof(sizeOffsettingMultiplier), value, ref _sizeOffsettingMultiplier);
        }

        /// <summary>
        /// The max number the size offsetting multiplier can reach. Higher values will be clamped.
        /// </summary>
        [Json]
        public float sizeOffsettingMaximum
        {
            get => _sizeOffsettingMaximum;
            set => SetValue(nameof(sizeOffsettingMaximum), value, ref _sizeOffsettingMaximum);
        }

        /// <summary>
        /// The amount of time required for the grid size to return to normal once fast camera movement is no longer detected.
        /// </summary>
        [Json]
        public float sizeOffsettingDuration
        {
            get => _sizeOffsettingDuration;
            set => SetValue(nameof(sizeOffsettingDuration), value, ref _sizeOffsettingDuration);
        }

        private CameraGridLoaderCenterOnLoadTriggerDictionary cameraGridLoaderCenterOnLoadTriggers
        {
            get { _cameraGridLoaderCenterOnLoadTriggers ??= new CameraGridLoaderCenterOnLoadTriggerDictionary(); return _cameraGridLoaderCenterOnLoadTriggers; }
        }

        protected List<CameraGrid2D> cameraGrids
        {
            get { _cameraGrids ??= new List<CameraGrid2D>(); return _cameraGrids; }
        }

        protected override IEnumerable<IGrid2D> GetGrids()
        {
            return cameraGrids;
        }

        private void RemoveCameraGridLoaderCenterOnLoadTrigger(CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger)
        {
            Camera camera = cameraGridLoaderCenterOnLoadTrigger.camera;
            if (camera is not null)
            {
                if (cameraGridLoaderCenterOnLoadTriggers.Remove(camera.GetCameraInstanceID()))
                {
                    RemoveCameraGridLoaderCenterOnLoadTriggerDelegates(cameraGridLoaderCenterOnLoadTrigger);

                    CameraGrid2D cameraGrid2D = cameraGrids.Find((cameraGrid2D) => { return cameraGrid2D.camera == camera; });
                    if (cameraGrid2D is not null)
                        cameraGrids.Remove(cameraGrid2D);

                    QueueAutoUpdate();
                }
            }
        }

        private CameraGridLoaderCenterOnLoadTrigger GetCameraGridLoaderCenterOnLoadTrigger(Camera camera)
        {
            CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger = null;

            if (camera != Disposable.NULL)
            {
                if (!cameraGridLoaderCenterOnLoadTriggers.TryGetValue(camera.GetCameraInstanceID(), out cameraGridLoaderCenterOnLoadTrigger))
                {
                    cameraGridLoaderCenterOnLoadTrigger = new CameraGridLoaderCenterOnLoadTrigger(this, camera);
                    AddCameraGridLoaderCenterOnLoadTriggerDelegates(cameraGridLoaderCenterOnLoadTrigger);

                    cameraGridLoaderCenterOnLoadTriggers[camera.GetCameraInstanceID()] = cameraGridLoaderCenterOnLoadTrigger;

                    cameraGrids.Add(CreateCameraGrid<CameraGrid2D>(camera));

                    QueueAutoUpdate();
                }
            }

            return cameraGridLoaderCenterOnLoadTrigger;
        }

        protected override void UpdateLoadTriggers()
        {
            base.UpdateLoadTriggers();

            instanceManager.IterateOverInstances<Camera>(
                (camera) =>
                {
                    GetCameraGridLoaderCenterOnLoadTrigger(camera).Update();

                    return true;
                });
        }

        protected override void UpdateLoaderFields(bool forceUpdate)
        {
            base.UpdateLoaderFields(forceUpdate);

            foreach (CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger in cameraGridLoaderCenterOnLoadTriggers.Values)
                cameraGridLoaderCenterOnLoadTrigger.UpdateLastLoadedCenterOnPosition();
        }

        protected override Index2DLoadScope CreateIndex2DLoadScope(Vector2Int index, Vector2Int dimensions)
        {
            Index2DLoadScope index2DLoadScope = base.CreateIndex2DLoadScope(index, dimensions);

            UpdateLoadScopeCameras(index2DLoadScope);

            return index2DLoadScope;
        }

        protected override void UpdateLoadScopeFields()
        {
            base.UpdateLoadScopeFields();

            IEnumerable<IGrid2D> grids = GetGrids();
            IterateOverLoadScopes((loadScopeKey, loadScope) =>
            {
                UpdateLoadScopeCameras(loadScope as Index2DLoadScope, grids);
                return true;
            });
        }

        private List<int> _visibleInCameras;
        public void UpdateLoadScopeCameras(Index2DLoadScope indexLoadScope, IEnumerable<IGrid2D> grids = null)
        {
            _visibleInCameras ??= new List<int>();
            _visibleInCameras.Clear();

            grids ??= GetGrids();

            if (grids != null)
            {
                foreach (IGrid2D grid in grids)
                {
                    if (grid != null)
                    {
                        Grid2D grid2D = grid.IsInGrid(indexLoadScope.scopeIndex, indexLoadScope.scopeDimensions);
                        if (grid2D != null && grid2D.cameraGrid2D != null)
                        {
                            _visibleInCameras.Add(grid2D.cameraGrid2D.camera.GetCameraInstanceID());
                            //_camerasCascade.Add(grid2D.cascade);
                        }
                    }
                }
            }

            if (indexLoadScope.visibleInCameras == null || !indexLoadScope.visibleInCameras.SequenceEqual(_visibleInCameras))
                indexLoadScope.visibleInCameras = _visibleInCameras.ToArray();
        }

        protected override bool UpdateGridsFields(IGrid2D grid, bool forceUpdate = false)
        {
            if (forceUpdate)
            {
                if (grid is CameraGrid2D)
                {
                    CameraGrid2D cameraGrid = grid as CameraGrid2D;
                    Camera camera = cameraGrid.camera;
                    if (parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject.transform != Disposable.NULL)
                    {
                        if (camera != Disposable.NULL)
                        {
                            if (cameraGridLoaderCenterOnLoadTriggers.TryGetValue(camera.GetCameraInstanceID(), out CameraGridLoaderCenterOnLoadTrigger cameraGridLoaderCenterOnLoadTrigger))
                            {
                                TransformDouble centerOnTransform = cameraGridLoaderCenterOnLoadTrigger.centerOnTransform;

                                GeoCoordinate3Double geoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromPoint(centerOnTransform.position);

                                parentGeoAstroObject.GetGeoCoordinateElevation(out double elevation, geoCoordinate, null, false);
                                geoCoordinate.altitude = elevation;

                                float sizeMultiplier = this.sizeMultiplier - cameraGridLoaderCenterOnLoadTrigger.dynamicSizeOffset;

                                if (IsSpherical() && sizeLatitudeCompensation)
                                    sizeMultiplier *= (float)(parentGeoAstroObject.GetCircumferenceAtLatitude(geoCoordinate.latitude) / parentGeoAstroObject.size);

                                cameraGrid.UpdateCameraGridProperties(camera.isActiveAndEnabled, parentGeoAstroObject, parentGeoAstroObject.GetPointFromGeoCoordinate(geoCoordinate), camera, minZoom, cascades, minMaxZoom, sizeMultiplier, xyTilesRatio);
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static T CreateCameraGrid<T>(Camera camera) where T : CameraGrid2D
        {
            return ScriptableObject.CreateInstance<T>().Init(camera) as T;
        }

        [Serializable]
        private class CameraGridLoaderCenterOnLoadTrigger
        {
            [SerializeField]
            private CameraGrid2DLoader _cameraGrid2DLoader;
            [SerializeField]
            private Camera _camera;
            [SerializeField]
            private TransformDouble _centerOnTransform;

            private float _sizeOffsettingMultiplier;
            private float _sizeOffsettingMaximum;
            private float _sizeOffsettingDuration;

            private float _dynamicSizeOffset;
            private Tween _dynamicSizeOffsetTimer;

            private GeoCoordinate3Double? _lastCenterOnTransformGeoCoordinate;
            private GeoCoordinate3Double? _lastLoadedCenterOnTransformGeoCoordinate;

            private GeoAstroObject _parentGeoAstroObject;

            /// <summary>
            /// Dispatched when the <see cref="DepictionEngine.Camera"/> <see cref="DepictionEngine.IDisposable.UpdateDisposingContext"/> is triggered.
            /// </summary>
            public Action<CameraGridLoaderCenterOnLoadTrigger> CameraDisposingEvent;
            /// <summary>
            /// Dispatched after changes requiring a <see cref="DepictionEngine.LoaderBase.QueueAutoUpdate"/> in the <see cref="DepictionEngine.CameraGrid2DLoader"/> are detected.
            /// </summary>
            public Action QueueAutoUpdateEvent;

            public CameraGridLoaderCenterOnLoadTrigger(CameraGrid2DLoader cameraGrid2DLoader, Camera camera)
            {
                this.cameraGrid2DLoader = cameraGrid2DLoader;
                this.camera = camera;

                UpdateAllDelegates(false);

                LastCenterOnTransformPositionDirty();
            }

            private void LastCenterOnTransformPositionDirty()
            {
                _lastCenterOnTransformGeoCoordinate = null;
                _lastLoadedCenterOnTransformGeoCoordinate = null;
            }

            public void UpdateAllDelegates(bool isDisposing)
            {
                RemoveCameraDelegates(_camera);
                if (!isDisposing)
                    AddCameraDelegates(_camera);

                RemoveParentGeoAstroObjectDelegates(_parentGeoAstroObject);
                if (!isDisposing)
                    AddParentGeoAstroObjectDelegates(_parentGeoAstroObject);

                RemoveCenterOnTransformDelegates(_centerOnTransform);
                if (!isDisposing)
                    AddCenterOnTransformDelegates(_centerOnTransform);
            }

            private void RemoveCameraDelegates(Camera camera)
            {
                if (camera is not null)
                {
                    camera.DisposedEvent -= CameraDisposedHandler;
                    camera.PropertyAssignedEvent -= CameraPropertyAssignedHandler;
                }
            }

            private void AddCameraDelegates(Camera camera)
            {
                if (camera != Disposable.NULL)
                {
                    camera.DisposedEvent += CameraDisposedHandler;
                    camera.PropertyAssignedEvent += CameraPropertyAssignedHandler;
                }
            }

            private void CameraDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
            {
                dynamicSizeOffsetTimer = null;

                UpdateAllDelegates(true);
                CameraDisposingEvent?.Invoke(this);
            }

            private void CameraPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
            {
                if (name == nameof(Camera.activeAndEnabled))
                    DispatchQueueAutoUpdate();
            }

            private void RemoveParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
            {
                if (parentGeoAstroObject is not null)
                {
                    parentGeoAstroObject.PropertyAssignedEvent -= ParentGeoAstroObjectPropertyAssignedHandler;
                    if (parentGeoAstroObject.transform is not null)
                        parentGeoAstroObject.transform.PropertyAssignedEvent -= ParentGeoAstroObjectTransformPropertyAssignedHandler;
                }
            }

            private void AddParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
            {
                if (parentGeoAstroObject != Disposable.NULL)
                {
                    parentGeoAstroObject.PropertyAssignedEvent += ParentGeoAstroObjectPropertyAssignedHandler;
                    parentGeoAstroObject.transform.PropertyAssignedEvent += ParentGeoAstroObjectTransformPropertyAssignedHandler;
                }
            }

            private void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
            {
                if (name == nameof(GeoAstroObject.size) || name == nameof(GeoAstroObject.sphericalRatio))
                    DispatchQueueAutoUpdate();
            }

            private void ParentGeoAstroObjectTransformPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
            {
                if (TransformDouble.IsTransformProperty(name))
                    DispatchCenterOnPositionChanged();
            }

            private void RemoveCenterOnTransformDelegates(TransformDouble centerOnTransform)
            {
                if (centerOnTransform is not null)
                    centerOnTransform.PropertyAssignedEvent -= CenterOnTransformChangedHandler;
            }

            private void AddCenterOnTransformDelegates(TransformDouble centerOnTransform)
            {
                if (centerOnTransform != Disposable.NULL)
                    centerOnTransform.PropertyAssignedEvent += CenterOnTransformChangedHandler;
            }

            private void CenterOnTransformChangedHandler(IProperty property, string name, object newValue, object oldValue)
            {
                if (name == nameof(TransformDouble.position))
                {
                    DispatchCenterOnPositionChanged();

                    if (parentGeoAstroObject != Disposable.NULL && centerOnTransform != Disposable.NULL)
                    {
                        if (!_lastCenterOnTransformGeoCoordinate.HasValue)
                            UpdateLastCenterOnTransformGeoCoordinate();

                        if (_lastCenterOnTransformGeoCoordinate.HasValue)
                        {
                            GeoCoordinate3Double centerOnTransformGeoCoordinate = CorrectCenterOnTransformGeoCoordinateAltitude(parentGeoAstroObject, parentGeoAstroObject.GetGeoCoordinateFromPoint(centerOnTransform.position));
                            
                            Vector3Double delta = parentGeoAstroObject.GetPointFromGeoCoordinate(centerOnTransformGeoCoordinate) - parentGeoAstroObject.GetPointFromGeoCoordinate(_lastCenterOnTransformGeoCoordinate.Value);

                            UpdateLastCenterOnTransformGeoCoordinate();

                            double motionAmplitude = delta.magnitude - 100.0d;

                            if (motionAmplitude < 0.0d)
                                motionAmplitude = 0.0d;

                            //Dont divide by Zero
                            if (centerOnTransformGeoCoordinate.altitude != 0.0d)
                                motionAmplitude /= centerOnTransformGeoCoordinate.altitude;

                            float newDynamicSizeOffset = (float)motionAmplitude * sizeOffsettingMultiplier;

                            newDynamicSizeOffset = Mathf.Clamp(newDynamicSizeOffset, 0.0f, sizeOffsettingMaximum);

                            newDynamicSizeOffset = Mathf.Max(dynamicSizeOffset, newDynamicSizeOffset.Round(7));
                            
                            if (SetDynamicSizeOffset(newDynamicSizeOffset))
                                UpdateDynamicOffsetTimer();
                        }
                    }
                }
            }

            private void DispatchCenterOnPositionChanged()
            {
                if (parentGeoAstroObject != Disposable.NULL && centerOnTransform != Disposable.NULL)
                {
                    if (_lastLoadedCenterOnTransformGeoCoordinate.HasValue)
                    {
                        GeoCoordinate3Double centerOnTransformGeoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromPoint(centerOnTransform.position);
                        double angleThreshold = 0.0001d;
                        if (Math.Abs(centerOnTransformGeoCoordinate.latitude - _lastLoadedCenterOnTransformGeoCoordinate.Value.latitude) > angleThreshold || Math.Abs(centerOnTransformGeoCoordinate.longitude - _lastLoadedCenterOnTransformGeoCoordinate.Value.longitude) > angleThreshold || Math.Abs(centerOnTransformGeoCoordinate.altitude - _lastLoadedCenterOnTransformGeoCoordinate.Value.altitude) > 1.0d)
                            DispatchQueueAutoUpdate();
                    }
                }
            }

            private void DispatchQueueAutoUpdate()
            {
                QueueAutoUpdateEvent?.Invoke();
            }

            public void Update()
            {
                if (cameraGrid2DLoader != Disposable.NULL)
                {
                    parentGeoAstroObject = cameraGrid2DLoader.objectBase is GeoAstroObject ? cameraGrid2DLoader.objectBase as GeoAstroObject : cameraGrid2DLoader.parentGeoAstroObject;

                    TransformDouble newCenterOnTransform = camera.transform;

                    if (cameraGrid2DLoader.centerOn == CameraCenterOnType.Target)
                    {
                        TargetControllerBase targetController = camera.controller as TargetControllerBase;
                        if (targetController != Disposable.NULL && targetController.target != Disposable.NULL)
                            newCenterOnTransform = targetController.target.transform;
                    }

                    centerOnTransform = newCenterOnTransform;

                    sizeOffsettingMultiplier = cameraGrid2DLoader.sizeOffsettingMultiplier;
                    sizeOffsettingMaximum = cameraGrid2DLoader.sizeOffsettingMaximum;
                    if (!SetSizeOffsettingDuration(cameraGrid2DLoader.sizeOffsettingDuration) && dynamicSizeOffsetTimer == Disposable.NULL)
                        UpdateDynamicOffsetTimer();
                }
            }

            private void UpdateDynamicOffsetTimer()
            {
                dynamicSizeOffsetTimer = TweenManager.Instance().To(dynamicSizeOffset, 0.0f, sizeOffsettingDuration, (value) => { dynamicSizeOffset = value; }, null, () => dynamicSizeOffsetTimer = null);
            }

            private void UpdateLastCenterOnTransformGeoCoordinate()
            {
                if (parentGeoAstroObject != Disposable.NULL && centerOnTransform != Disposable.NULL)
                    _lastCenterOnTransformGeoCoordinate = CorrectCenterOnTransformGeoCoordinateAltitude(parentGeoAstroObject, parentGeoAstroObject.GetGeoCoordinateFromPoint(centerOnTransform.position));
                else
                    _lastCenterOnTransformGeoCoordinate = null;
            }

            private GeoCoordinate3Double CorrectCenterOnTransformGeoCoordinateAltitude(GeoAstroObject parentGeoAstroObject, GeoCoordinate3Double centerOnTransformGeoCoordinate)
            {
                GeoCoordinateController elevationGeoCoordinateController = centerOnTransform.objectBase.controller as GeoCoordinateController;

                if (centerOnTransform == camera.transform)
                {
                    TargetControllerBase cameraTargetControllerBase = camera.controller as TargetControllerBase;
                    if (cameraTargetControllerBase != Disposable.NULL)
                    {
                        Object cameraTarget = cameraTargetControllerBase.target;
                        if (cameraTarget != Disposable.NULL)
                        {
                            if (cameraTarget.controller is GeoCoordinateController)
                                elevationGeoCoordinateController = cameraTarget.controller as GeoCoordinateController;

                            centerOnTransformGeoCoordinate.altitude = parentGeoAstroObject.GetGeoCoordinateFromPoint(cameraTargetControllerBase.GetTargetPosition()).altitude + cameraTargetControllerBase.distance;
                        }
                    }
                }

                if (elevationGeoCoordinateController != Disposable.NULL && elevationGeoCoordinateController.parentGeoAstroObject == parentGeoAstroObject)
                    centerOnTransformGeoCoordinate.altitude -= elevationGeoCoordinateController.elevation;

                return centerOnTransformGeoCoordinate;
            }

            public void UpdateLastLoadedCenterOnPosition()
            {
                if (parentGeoAstroObject != Disposable.NULL && centerOnTransform != Disposable.NULL)
                    _lastLoadedCenterOnTransformGeoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromPoint(centerOnTransform.position);
                else
                    _lastLoadedCenterOnTransformGeoCoordinate = null;
            }

            private CameraGrid2DLoader cameraGrid2DLoader
            {
                get => _cameraGrid2DLoader;
                set
                {
                    if (Object.ReferenceEquals(_cameraGrid2DLoader, value))
                        return;

                    _cameraGrid2DLoader = value;
                }
            }

            public Camera camera
            {
                get => _camera;
                set
                {
                    if (Object.ReferenceEquals(_camera, value))
                        return;

                    RemoveCameraDelegates(_camera);

                    _camera = value;

                    AddCameraDelegates(_camera);
                }
            }

            private GeoAstroObject parentGeoAstroObject
            {
                get => _parentGeoAstroObject;
                set
                {
                    if (Object.ReferenceEquals(_parentGeoAstroObject, value))
                        return;

                    RemoveParentGeoAstroObjectDelegates(_parentGeoAstroObject);

                    _parentGeoAstroObject = value;

                    AddParentGeoAstroObjectDelegates(_parentGeoAstroObject);

                    QueueAutoUpdateEvent();
                }
            }

            public TransformDouble centerOnTransform
            {
                get => _centerOnTransform;
                private set 
                {
                    if (Object.ReferenceEquals(_centerOnTransform, value))
                        return;

                    RemoveCenterOnTransformDelegates(_centerOnTransform);

                    _centerOnTransform = value;

                    AddCenterOnTransformDelegates(_centerOnTransform);

                    LastCenterOnTransformPositionDirty();

                    DispatchQueueAutoUpdate();
                }
            }

            private float sizeOffsettingMultiplier
            {
                get => _sizeOffsettingMultiplier;
                set
                {
                    if (_sizeOffsettingMultiplier == value)
                        return;

                    _sizeOffsettingMultiplier = value;
                }
            }

            private float sizeOffsettingMaximum
            {
                get => _sizeOffsettingMaximum;
                set
                {
                    if (_sizeOffsettingMaximum == value)
                        return;

                    _sizeOffsettingMaximum = value;
                }
            }

            private float sizeOffsettingDuration
            {
                get => _sizeOffsettingDuration;
                set { SetSizeOffsettingDuration(value); }
            }

            private bool SetSizeOffsettingDuration(float value)
            {
                if (_sizeOffsettingDuration == value)
                    return false;

                _sizeOffsettingDuration = value;

                UpdateDynamicOffsetTimer();

                return true;
            }

            private Tween dynamicSizeOffsetTimer
            {
                get => _dynamicSizeOffsetTimer;
                set
                {
                    if (Object.ReferenceEquals(_dynamicSizeOffsetTimer, value))
                        return;

                    
                    DisposeManager.Dispose(_dynamicSizeOffsetTimer);

                    _dynamicSizeOffsetTimer = value;
                }
            }

            public float dynamicSizeOffset
            {
                get => _dynamicSizeOffset;
                private set => SetDynamicSizeOffset(value);
            }

            private bool SetDynamicSizeOffset(float value)
            {
                if (_dynamicSizeOffset == value)
                    return false;

                _dynamicSizeOffset = value;

                DispatchQueueAutoUpdate();

                return true;
            }
        }
    }
}
