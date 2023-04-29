// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
	/// <summary>
	/// Controls the XYZ components of the object transform.
	/// </summary>
	[AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Controller/" + nameof(XYZController))]
	public class XYZController : ControllerBase
	{
		[BeginFoldout("Position")]
		[SerializeField, Tooltip("Limits the object altitude to ground level.")]
		private bool _autoSnapToGround;
		[SerializeField, Tooltip("Limits the object altitude to ground level + offset. (Requires "+nameof(autoSnapToGround)+" enabled)."), EndFoldout]
		private double _groundOffset;

		protected override void InitializeSerializedFields(InitializationContext initializingContext)
		{
			base.InitializeSerializedFields(initializingContext);

			InitValue(value => autoSnapToGround = value, true, initializingContext);
			InitValue(value => groundOffset = value, 0.0d, initializingContext);
		}

		/// <summary>
		/// Limits the object altitude to ground level.
		/// </summary>
		[Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public bool autoSnapToGround
		{
			get => _autoSnapToGround;
			set 
			{
				SetValue(nameof(autoSnapToGround), value, ref _autoSnapToGround, (newValue, oldValue) =>
				{
					ForceUpdateTransform(true);
				});
			}
		}

        /// <summary>
        /// Limits the object altitude to ground level + offset. (Requires <see cref="DepictionEngine.XYZController.autoSnapToGround"/> enabled).
        /// </summary>
        [Json]
#if UNITY_EDITOR
		[RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
		public double groundOffset
		{
			get => _groundOffset;
			set
			{
				SetValue(nameof(groundOffset), value, ref _groundOffset, (newValue, oldValue) =>
				{
					ForceUpdateTransform(true);
				});
			}
		}

        protected override bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
			if (base.TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera))
			{
				if (!localPositionParam.isGeoCoordinate && localPositionParam.changed)
				{
					Vector3Double localPosition = localPositionParam.vector3DoubleValue;
					
					double y = localPosition.y;
					if (_autoSnapToGround)
						y = groundOffset;

					if (localPosition.y != y)
					{
						localPosition.y = y;
						localPositionParam.SetValue(localPosition);
					}
				}

				return true;
			}
			return false;
		}
	}
}
