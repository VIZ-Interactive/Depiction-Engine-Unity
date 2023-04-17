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
        /// Do not snap to the ground. <br/><br/>
		/// <b><see cref="SeaLevel"/>:</b> <br/>
        /// Snap to altitude level zero. <br/><br/>
		/// <b><see cref="Terrain"/>:</b> <br/>
        /// Snap to the terrain elevation.
        /// </summary>
        public enum SnapType
        {
            None,
            SeaLevel,
            Terrain
        };

        public const double DEFAULT_GROUND_SNAP_OFFSET_VALUE = 0.0d;

		[BeginFoldout("Position")]
		[SerializeField, Tooltip("When enabled the '"+nameof(GeoCoordinateController)+"' will always position the object in a way so that it does not penetrate the ground.")]
		private bool _preventGroundPenetration;
		[SerializeField, Tooltip("When enabled the '"+nameof(GeoCoordinateController)+"' will always position the object above the ground.")]
		private SnapType _autoSnapToGround;
		[SerializeField, Tooltip("This value represents the distance from the ground at which the object will be positioned. Requires '"+nameof(autoSnapToGround)+"' enabled to take effect."), EndFoldout]
		private double _groundSnapOffset;

		[BeginFoldout("Rotation")]
		[SerializeField, Tooltip("When enabled the '"+nameof(GeoCoordinateController)+"' will always rotate the object so that it is pointing upwards.")]
		private bool _autoAlignUpwards;
		[SerializeField, Tooltip("An offset vector represented in degrees which is added to the surface up vector used by the '"+nameof(autoAlignUpwards)+"'."), EndFoldout]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableUpVector))]
#endif
		private Vector3 _upVector;

        private double _elevation;

        private double _radius;

        private Camera _filterTerrainCamera;

#if UNITY_EDITOR
		protected virtual bool GetEnableUpVector()
		{
			return autoAlignUpwards;
		}
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

            InitValue(value => preventGroundPenetration = value, true, initializingContext);
			InitValue(value => autoSnapToGround = value, SnapType.Terrain, initializingContext);
			InitValue(value => groundSnapOffset = value, DEFAULT_GROUND_SNAP_OFFSET_VALUE, initializingContext);
			InitValue(value => autoAlignUpwards = value, true, initializingContext);
			InitValue(value => upVector = value, Vector3.zero, initializingContext);
		}

		protected override bool Initialize(InitializationContext initializingContext)
		{
			if (base.Initialize(initializingContext))
			{
				InitElevation();

				return true;
			}
			return false;
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

        protected override bool RemoveParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
		{
			if (base.RemoveParentGeoAstroObjectDelegates(parentGeoAstroObject))
			{
				parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent -= ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                parentGeoAstroObject.TerrainGridMeshObjectAddedEvent -= ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;

                return true;
			}
			return false;
		}

		protected override bool AddParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
		{
			if (base.AddParentGeoAstroObjectDelegates(parentGeoAstroObject))
			{
				parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent += ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                parentGeoAstroObject.TerrainGridMeshObjectAddedEvent += ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;

                return true;
			}
			return false;
		}

        private void ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
        {
			if (transform != Disposable.NULL && RequiresElevation() && !forceUpdateTransformPending.localPositionChanged && grid2DIndex == (Vector2Int)MathPlus.GetIndexFromGeoCoordinate(transform.GetGeoCoordinate(), grid2DDimensions))
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

			if (name == nameof(GeoAstroObject.radius))
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

        private void InitElevation()
        {
			elevation = gameObject.GetComponent<TransformDouble>().GetGeoCoordinate().altitude;
        }

        public double elevation
        {
            get => _elevation;
            private set => SetElevation(value);
        }

        private bool SetElevation(double value)
        {
            if (_elevation == value)
                return false;

            _elevation = value;

            return true;
        }

        public Camera filterTerrainCamera
		{
            get => _filterTerrainCamera;
            set => _filterTerrainCamera = value;
		}

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.GeoCoordinateController"/> will always position the object in a way so that it does not penetrate the ground. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public bool preventGroundPenetration
		{
            get => _preventGroundPenetration;
			set
			{
				SetValue(nameof(preventGroundPenetration), value, ref _preventGroundPenetration, (newValue, oldValue) =>
				{
					if (initialized)
					{
						InitElevation();

						ForceUpdateTransform(true);
					}
				});
			}
		}

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.GeoCoordinateController"/> will always position the object above the ground. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public SnapType autoSnapToGround
		{
            get => _autoSnapToGround;
			set 
			{
				SetValue(nameof(autoSnapToGround), value, ref _autoSnapToGround, (newValue, oldValue) => 
				{
					if (initialized)
					{
						InitElevation();

						ForceUpdateTransform(true);
					}
				});
			}
		}

        /// <summary>
        /// This value represents the distance from the ground at which the object will be positioned. Requires <see cref="DepictionEngine.GeoCoordinateController.autoSnapToGround"/> enabled to take effect.
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public double groundSnapOffset
		{
            get => _groundSnapOffset;
			set
			{
				SetValue(nameof(groundSnapOffset), value, ref _groundSnapOffset, (newValue, oldValue) =>
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
                        ForceUpdateTransform(false, true);
				});
			}
		}

		private bool RequiresElevation()
		{
			return autoSnapToGround == SnapType.Terrain || preventGroundPenetration;
        }

        protected override bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (enabled &&  localPositionParam.isGeoCoordinate && parentGeoAstroObject != Disposable.NULL)
                {
                    if (localPositionParam.changed)
                    {
#if UNITY_EDITOR
                        bool mouseDown = Event.current != null && Event.current.button == 0;
                        isBeingMovedByUser = !SceneManager.IsEditorNamespace(GetType()) && SceneManager.IsUserChangeContext() && mouseDown;
                        if (!isBeingMovedByUser)
                        {
#endif
                            GeoCoordinate3Double geoCoordinate = localPositionParam.geoCoordinateValue;
                            if (!localPositionParam.isGeoCoordinate)
                                geoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromPoint(transform.parent != Disposable.NULL ? transform.parent.TransformPoint(localPositionParam.vector3DoubleValue) : localPositionParam.vector3DoubleValue);

                            double altitude = autoSnapToGround == SnapType.SeaLevel ? groundSnapOffset : geoCoordinate.altitude;

                            if (RequiresElevation())
                            {
								    if (parentGeoAstroObject.GetGeoCoordinateElevation(out double elevation, geoCoordinate, filterTerrainCamera))
									    SetElevation(elevation);

								    if (autoSnapToGround == SnapType.Terrain || (preventGroundPenetration && altitude < this.elevation))
									    altitude = this.elevation + groundSnapOffset;
						    }
                            else
                                SetElevation(0.0f);

                            if (geoCoordinate.altitude != altitude)
                            {
                                geoCoordinate.altitude = altitude;
                                localPositionParam.SetValue(geoCoordinate);
                            }
#if UNITY_EDITOR
                        }
#endif
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
                snapToGridTimer = null;
#endif
                return true;
            }
            return false;
        }
    }
}
