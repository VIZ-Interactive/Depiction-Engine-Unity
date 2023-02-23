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
		private bool _autoAlignToSurface;
		[SerializeField, Tooltip("An offset vector represented in degrees which is added to the surface up vector used by the '"+nameof(autoAlignToSurface)+"'."), EndFoldout]
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
			return autoAlignToSurface;
		}
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

            InitValue(value => preventGroundPenetration = value, true, initializingContext);
			InitValue(value => autoSnapToGround = value, SnapType.Terrain, initializingContext);
			InitValue(value => groundSnapOffset = value, DEFAULT_GROUND_SNAP_OFFSET_VALUE, initializingContext);
			InitValue(value => autoAlignToSurface = value, true, initializingContext);
			InitValue(value => upVector = value, Vector3.zero, initializingContext);
		}

		protected override bool Initialize(InstanceManager.InitializationContext initializingContext)
		{
			if (base.Initialize(initializingContext))
			{
				InitElevation();

				return true;
			}
			return false;
		}

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
			if (RequiresElevation() && !forceUpdateTransformPending.localPositionChanged && grid2DIndex == (Vector2Int)MathPlus.GetIndexFromGeoCoordinate(transform.GetGeoCoordinate(), grid2DDimensions))
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
			elevation = GetComponent<TransformDouble>().GetGeoCoordinate().altitude;
        }

        public double elevation
        {
            get { return _elevation; }
            private set { SetElevation(value); }
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
			get { return _filterTerrainCamera; }
			set { _filterTerrainCamera = value; }
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
			get { return _preventGroundPenetration; }
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
			get { return _autoSnapToGround; }
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
			get { return _groundSnapOffset; }
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
		public bool autoAlignToSurface
		{
			get { return _autoAlignToSurface; }
			set
			{
				SetValue(nameof(autoAlignToSurface), value, ref _autoAlignToSurface, (newValue, oldValue) =>
				{
                    if (initialized)
                        ForceUpdateTransform(true);
				});
			}
		}

        /// <summary>
        /// An offset vector represented in degrees which is added to the surface up vector used by the <see cref="DepictionEngine.GeoCoordinateController.autoAlignToSurface"/>.
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public Vector3 upVector
		{
			get { return _upVector; }
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
                if (localPositionParam.isGeoCoordinate && parentGeoAstroObject != Disposable.NULL)
                {
                    if (localPositionParam.changed)
                    {
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
                    }

                    if (autoAlignToSurface)
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
    }
}
