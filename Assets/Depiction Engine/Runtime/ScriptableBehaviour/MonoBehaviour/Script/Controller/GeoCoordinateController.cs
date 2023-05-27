// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Controller/" + nameof(GeoCoordinateController))]
	public class GeoCoordinateController : ControllerBase
	{
        /// <summary>
        /// Different types of ground position snapping. <br/><br/>
		/// <b><see cref="None"/>:</b> <br/>
        /// Do not snap. <br/><br/>
		/// <b><see cref="Zero"/>:</b> <br/>
        /// Snap to altitude level zero. <br/><br/>
        /// <b><see cref="Prevent_Surface_Penetration_Raycast"/>:</b> <br/>
        /// Snap to the surface if the object penetrates below the surface level using Raycasting (Precise).
        /// <b><see cref="Prevent_Surface_Penetration_Elevation"/>:</b> <br/>
        /// Snap to the surface if the object penetrates below the surface level using Elevation (Faster).
		/// <b><see cref="Highest_Surface_Raycast"/>:</b> <br/>
        /// Snap to the surface using Raycasting (Precise).
        /// <b><see cref="Highest_Surface_Elevation"/>:</b> <br/>
        /// Snap to the surface using Elevation (Faster).
        /// </summary>
        public enum SnapType
        {
            None,
            Zero,
            Prevent_Surface_Penetration_Raycast,
            Prevent_Surface_Penetration_Elevation,
            Highest_Surface_Raycast,
            Highest_Surface_Elevation
        };

        public const double DEFAULT_SURFACE_SNAP_OFFSET_VALUE = 0.0d;

		[BeginFoldout("Altitude")]
		[SerializeField, Tooltip("Controls the object altitude. Raycast is more Precise while Elevation will be Faster.")]
		private SnapType _autoSnapToAltitude;
		[SerializeField, Tooltip("This value represents the distance from the surface at which the object will be positioned. Requires '"+nameof(autoSnapToAltitude)+"' enabled to take effect."), EndFoldout]
		private double _surfaceSnapOffset;

		[BeginFoldout("Rotation")]
		[SerializeField, Tooltip("When enabled the '"+nameof(GeoCoordinateController)+"' will always rotate the object so that it is pointing upwards.")]
		private bool _autoAlignUpwards;
		[SerializeField, Tooltip("An offset vector represented in degrees which is added to the surface up vector used by the '"+nameof(autoAlignUpwards)+"'."), EndFoldout]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableUpVector))]
#endif
		private Vector3 _upVector;

        [SerializeField, HideInInspector]
        private double _elevation;

        private double _radius;

        private Camera _filterTerrainCamera;

#if UNITY_EDITOR
		protected virtual bool GetEnableUpVector()
		{
			return autoAlignUpwards;
		}
#endif

        public override void Recycle()
        {
            base.Recycle();

            _elevation = 0.0d;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

            InitValue(value => autoSnapToAltitude = value, GetDefaultAutoSnapToAltitude(), initializingContext);
			InitValue(value => surfaceSnapOffset = value, DEFAULT_SURFACE_SNAP_OFFSET_VALUE, initializingContext);
			InitValue(value => autoAlignUpwards = value, true, initializingContext);
			InitValue(value => upVector = value, Vector3.zero, initializingContext);
		}

        protected virtual SnapType GetDefaultAutoSnapToAltitude()
        {
            return SnapType.Highest_Surface_Elevation;
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

        private void LeftMouseUpInSceneOrInspectorHandler()
        {
            snapToAltitudeTimer = tweenManager.DelayedCall(0.1f, null, () => { ForceUpdateTransform(true, true); }, () => { snapToAltitudeTimer = null; });
        }

        private Tween _snapToAltitudeTimer;
        private Tween snapToAltitudeTimer
        {
            get => _snapToAltitudeTimer;
            set
            {
                if (Object.ReferenceEquals(_snapToAltitudeTimer, value))
                    return;

                DisposeManager.Dispose(_snapToAltitudeTimer);

                _snapToAltitudeTimer = value;
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

        protected override bool RemoveParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
		{
			if (base.RemoveParentGeoAstroObjectDelegates(parentGeoAstroObject))
			{
				parentGeoAstroObject.TerrainGridMeshObjectAltitudeOffsetChangedEvent -= TerrainGridMeshObjectChanged;
                parentGeoAstroObject.TerrainGridMeshObjectElevationChangedEvent -= TerrainGridMeshObjectChanged;
                parentGeoAstroObject.TerrainGridMeshObjectMeshModifiedEvent -= TerrainGridMeshObjectChanged;

                return true;
			}
			return false;
		}

		protected override bool AddParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
		{
			if (base.AddParentGeoAstroObjectDelegates(parentGeoAstroObject))
			{
                if (RequiresElevation())
                    parentGeoAstroObject.TerrainGridMeshObjectAltitudeOffsetChangedEvent += TerrainGridMeshObjectChanged;
                if (autoSnapToAltitude == SnapType.Highest_Surface_Elevation || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Elevation)
                    parentGeoAstroObject.TerrainGridMeshObjectElevationChangedEvent += TerrainGridMeshObjectChanged;
                if (autoSnapToAltitude == SnapType.Highest_Surface_Raycast || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Raycast)
                    parentGeoAstroObject.TerrainGridMeshObjectMeshModifiedEvent += TerrainGridMeshObjectChanged;
                return true;
			}
			return false;
		}

        private void TerrainGridMeshObjectChanged(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject)
        {
            if (transform != Disposable.NULL && !forceUpdateTransformPending.localPositionChanged && grid2DIndexTerrainGridMeshObject.grid2DIndex == (Vector2Int)MathPlus.GetIndexFromGeoCoordinate(transform.GetGeoCoordinate(), grid2DIndexTerrainGridMeshObject.grid2DDimensions))
                ForceUpdateTransformPending(true);
        }

        protected override bool TransformPropertyAssigned(IProperty property, string name, object newValue, object oldValue)
		{
			if (base.TransformPropertyAssigned(property, name, newValue, oldValue))
			{
				if (name == nameof(TransformDouble.position))
                    ForceUpdateTransformPending(true);

                return true;
			}
			return false;
		}

        protected override void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
			base.ParentGeoAstroObjectPropertyAssignedHandler(property, name, newValue, oldValue);	

			if (name == nameof(GeoAstroObject.size))
                ForceUpdateTransform(true);
        }

        protected override bool SetParentGeoAstroObject(GeoAstroObject newValue, GeoAstroObject oldValue)
		{
			if (base.SetParentGeoAstroObject(newValue, oldValue))
			{
				ForceUpdateTransform(true, true, true);

				return true;
			}
			return false;
		}

		private double GetRadius()
        {
			return parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject.radius : 0.0d;
		}

        public double elevation
        {
            get => _elevation;
            private set => _elevation = value;
        }

        public Camera filterTerrainCamera
		{
            get => _filterTerrainCamera;
            set => _filterTerrainCamera = value;
		}

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.GeoCoordinateController"/> will always position the object above the ground. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public SnapType autoSnapToAltitude
		{
            get => _autoSnapToAltitude;
			set 
			{
				SetValue(nameof(autoSnapToAltitude), value, ref _autoSnapToAltitude, (newValue, oldValue) => 
				{
                    if (initialized)
                    {
                        UpdateParentGeoAstroObjectDelegates();
                        ForceUpdateTransform(true);
                    }
				});
			}
		}

        /// <summary>
        /// This value represents the distance from the ground at which the object will be positioned. Requires <see cref="DepictionEngine.GeoCoordinateController.autoSnapToAltitude"/> enabled to take effect.
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public double surfaceSnapOffset
		{
            get => _surfaceSnapOffset;
			set
			{
				SetValue(nameof(surfaceSnapOffset), value, ref _surfaceSnapOffset, (newValue, oldValue) =>
				{
                    if (initialized)
                        ForceUpdateTransform(true);
				});
			}
		}

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.GeoCoordinateController"/> will always rotate the object so that it is pointing upwards. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public bool autoAlignUpwards
		{
            get => _autoAlignUpwards;
			set
			{
				SetValue(nameof(autoAlignUpwards), value, ref _autoAlignUpwards, (newValue, oldValue) =>
				{
                    if (initialized)
                        ForceUpdateTransform(true);
                });
			}
		}

        /// <summary>
        /// An offset vector represented in degrees which is added to the surface up vector used by the <see cref="DepictionEngine.GeoCoordinateController.autoAlignUpwards"/>.
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public Vector3 upVector
		{
            get => _upVector;
			set
			{
				SetValue(nameof(upVector), value, ref _upVector, (newValue, oldValue) =>
				{
                    if (initialized)
                        ForceUpdateTransform(true);
                });
			}
		}

		private bool RequiresElevation()
		{
			return autoSnapToAltitude == SnapType.Highest_Surface_Raycast || autoSnapToAltitude == SnapType.Highest_Surface_Elevation || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Raycast || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Elevation;
        }

        protected override bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (enabled && localPositionParam.isGeoCoordinate && parentGeoAstroObject != Disposable.NULL)
                {
                    if (localPositionParam.changed)
                    {
                        GeoCoordinate3Double geoCoordinate = localPositionParam.geoCoordinateValue;
                        if (!localPositionParam.isGeoCoordinate)
                            geoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromPoint(transform.parent != Disposable.NULL ? transform.parent.TransformPoint(localPositionParam.vector3DoubleValue) : localPositionParam.vector3DoubleValue);

                        double altitude = autoSnapToAltitude == SnapType.Zero ? surfaceSnapOffset : geoCoordinate.altitude;

                        bool updateElevation = true;

#if UNITY_EDITOR
                        if (!SceneManager.IsEditorNamespace(GetType()))
                        {
                            isBeingMovedByUser = Editor.SceneViewDouble.lastActiveSceneViewDouble != null && Editor.SceneViewDouble.lastActiveSceneViewDouble.positionHandleDragging;
                            if (isBeingMovedByUser)
                                updateElevation = false;
                        }
#endif

                        if (updateElevation)
                        {
                            if (RequiresElevation())
                            {
                                if (parentGeoAstroObject.GetGeoCoordinateElevation(out float newElevation, geoCoordinate, filterTerrainCamera, autoSnapToAltitude == SnapType.Highest_Surface_Raycast || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Raycast))
                                    elevation = newElevation;

                                double newAltitude = elevation + surfaceSnapOffset;
                                if (autoSnapToAltitude == SnapType.Highest_Surface_Raycast || autoSnapToAltitude == SnapType.Highest_Surface_Elevation || ((autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Raycast || autoSnapToAltitude == SnapType.Prevent_Surface_Penetration_Elevation) && altitude < newAltitude))
                                    altitude = newAltitude;
                            }
                            else
                                elevation = 0.0f;
                        }

                        if (geoCoordinate.altitude != altitude)
                        {
                            geoCoordinate.altitude = altitude;
                            localPositionParam.SetValue(geoCoordinate);
                        }
                    }

                    if (autoAlignUpwards)
                    {
                        objectBase.ApplyAutoAlignToSurface(localPositionParam, localRotationParam, parentGeoAstroObject, PropertyDirty(nameof(upVector)));
                        if (localRotationParam.changed)
                            localRotationParam.SetValue(localRotationParam.value * QuaternionDouble.Euler(upVector));
                    }
                }

                return true;
            }
            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
#if UNITY_EDITOR
                snapToAltitudeTimer = null;
#endif
                return true;
            }
            return false;
        }
    }
}
