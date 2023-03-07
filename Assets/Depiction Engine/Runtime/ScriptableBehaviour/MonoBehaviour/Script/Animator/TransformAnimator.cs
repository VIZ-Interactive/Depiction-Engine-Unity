// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
	[AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Animator/" + nameof(TransformAnimator))]
    [RequireComponent(typeof(TransformDouble))]
	public class TransformAnimator : AnimatorBase
    {
		[SerializeField, Tooltip("The local position the transform should reach by the end of the animation.")]
#if UNITY_EDITOR
		[ConditionalShow(nameof(GetShowLocalPosition))]
#endif
		private Vector3Double _toLocalPosition;

		[SerializeField, Tooltip("The Geo Coordinate the transform should reach by the end of the animation.")]
#if UNITY_EDITOR
		[ConditionalShow(nameof(GetShowGeoCoordinate))]
#endif
		private GeoCoordinate3Double _toGeoCoordinate;

#if UNITY_EDITOR
		[SerializeField, Button(nameof(StartLocalPositionAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating towards the localPosition / geoCoordinate.")]
		private bool _startLocalPositionAnimation;
#endif
		[SerializeField, Tooltip("The local rotation the transform should reach by the end of the animation.")]
		private Vector3Double _toLocalRotation;
#if UNITY_EDITOR
		[SerializeField, Button(nameof(StartLocalRotationAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating towards the localRotation.")]
		private bool _startLocalRotationAnimation;
#endif
		[SerializeField, Tooltip("The local scale the transform should reach by the end of the animation.")]
		private Vector3Double _toLocalScale;
#if UNITY_EDITOR
		[SerializeField, Button(nameof(StartLocalScaleAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating towards the localScale.")]
		private bool _startLocalScaleAnimation;
#endif

		private bool _isGeoCoordinateTransform;
		private Tween _localPositionTween;
		private Tween _localRotationTween;
		private Tween _localScaleTween;

#if UNITY_EDITOR
		private void StartLocalPositionAnimationBtn()
		{
            if (transform != Disposable.NULL)
            {
                if (!transform.isGeoCoordinateTransform)
                    SetLocalPosition(_toLocalPosition);
                else
                    SetGeoCoordinate(_toGeoCoordinate);
            }
        }

        private void StartLocalRotationAnimationBtn()
        {
            SetLocalRotation(QuaternionDouble.Euler(_toLocalRotation));
        }

        private void StartLocalScaleAnimationBtn()
        {
            SetLocalScale(_toLocalScale);
        }

        private bool GetShowLocalPosition()
        {
			if (isFallbackValues)
				return true;
			return transform != Disposable.NULL ? !transform.isGeoCoordinateTransform : true;
		}

		private bool GetShowGeoCoordinate()
		{
			if (isFallbackValues)
				return true;
			return transform != Disposable.NULL ? transform.isGeoCoordinateTransform : false;
		}
#endif

		protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

			InitValue(value => toLocalPosition = value, Vector3Double.zero, initializingContext);
			InitValue(value => toGeoCoordinate = value, GeoCoordinate3Double.zero, initializingContext);
			InitValue(value => toLocalRotation = value, Vector3Double.zero, initializingContext);
			InitValue(value => toLocalScale = value, Vector3Double.one, initializingContext);
		}

        protected override bool TransformPropertyAssigned(IProperty property, string name, object newValue, object oldValue)
        {
			if (base.TransformPropertyAssigned(property, name, newValue, oldValue))
			{
				if (name == nameof(TransformBase.isGeoCoordinateTransform))
					StopAllAnimations();

				return true;
			}
			return false;
        }

		/// <summary>
		/// The local position the transform should reach by the end of the animation.
		/// </summary>
        [Json]
		private Vector3Double toLocalPosition
		{
			get { return _toLocalPosition; }
			set { SetValue(nameof(toLocalPosition), value, ref _toLocalPosition); }
		}

        /// <summary>
        /// The Geo Coordinate the transform should reach by the end of the animation.
        /// </summary>
        [Json]
		private GeoCoordinate3Double toGeoCoordinate
		{
			get { return _toGeoCoordinate; }
			set { SetValue(nameof(toGeoCoordinate), value, ref _toGeoCoordinate); }
		}

        /// <summary>
        /// The local rotation the transform should reach by the end of the animation.
        /// </summary>
        [Json]
		private Vector3Double toLocalRotation
		{
			get { return _toLocalRotation; }
			set { SetValue(nameof(toLocalRotation), value, ref _toLocalRotation); }
		}

        /// <summary>
        /// The local scale the transform should reach by the end of the animation.
        /// </summary>
        [Json]
		private Vector3Double toLocalScale
		{
			get { return _toLocalScale; }
			set { SetValue(nameof(toLocalScale), value, ref _toLocalScale); }
		}

        private Tween localPositionTween
		{
            get { return _localPositionTween; }
			set
			{
				if (Object.ReferenceEquals(_localPositionTween, value))
					return;

                DisposeManager.Dispose(_localPositionTween);

				_localPositionTween = value;
			}
		}

		/// <summary>
		/// Start animating towards the position.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="easing"></param>
		public void SetPosition(Vector3Double value, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalPosition(GetLocalPosition(value), duration, easing);
		}

        /// <summary>
        /// Start animating towards the position.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetPosition(Vector3Double value, float duration, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalPosition(GetLocalPosition(value), duration, (t) => { return t; }, easing);
		}

        /// <summary>
        /// Start animating towards the position.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetPosition(Vector3Double value, AnimationCurve curve)
		{
			SetLocalPosition(GetLocalPosition(value), duration, curve);
		}

        /// <summary>
        /// Start animating towards the position.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetPosition(Vector3Double value, float duration, AnimationCurve curve)
		{
			SetLocalPosition(GetLocalPosition(value), duration, (t) => { return curve.Evaluate(t); });
		}

		private Vector3Double GetLocalPosition(Vector3Double position)
        {
			return transform.parent != Disposable.NULL ? transform.parent.InverseTransformPoint(position) : position;
        }

        /// <summary>
        /// Start animating towards the localPosition.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetLocalPosition(Vector3Double value, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalPosition(value, duration, easing);
		}

        /// <summary>
        /// Start animating towards the localPosition.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetLocalPosition(Vector3Double value, float duration, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalPosition(value, duration, (t) => { return t; }, easing);
		}

        /// <summary>
        /// Start animating towards the localPosition.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetLocalPosition(Vector3Double value, AnimationCurve curve)
		{
			SetLocalPosition(value, duration, curve);
		}

        /// <summary>
        /// Start animating towards the localPosition.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetLocalPosition(Vector3Double value, float duration, AnimationCurve curve)
		{
			SetLocalPosition(value, duration, (t) => { return curve.Evaluate(t); });
		}

		private Vector3Double _fromLocalPosition;
		private void SetLocalPosition(Vector3Double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
		{
			if (transform != Disposable.NULL && !transform.isGeoCoordinateTransform && !Object.Equals(transform.localPosition, value))
			{
				_fromLocalPosition = transform.localPosition;

				localPositionTween = tweenManager.To(0.0f, 1.0f, duration, (t) =>
				{
					transform.localPosition = Vector3Double.Lerp(_fromLocalPosition, value, TCallback(t));
				}, () => { localPositionTween = null; }, null, easing);
			}
		}

        /// <summary>
        /// Start animating towards the geoCoordinate.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetGeoCoordinate(GeoCoordinate3Double value, EasingType easing = EasingType.QuartEaseOut)
		{
			SetGeoCoordinate(value, duration, easing);
		}

        /// <summary>
        /// Start animating towards the geoCoordinate.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetGeoCoordinate(GeoCoordinate3Double value, float duration, EasingType easing = EasingType.QuartEaseOut)
		{
			SetGeoCoordinate(value, duration, (t) => { return t; }, easing);
		}

        /// <summary>
        /// Start animating towards the geoCoordinate.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetGeoCoordinate(GeoCoordinate3Double value, AnimationCurve curve)
		{
			SetGeoCoordinate(value, duration, curve);
		}

        /// <summary>
        /// Start animating towards the geoCoordinate.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetGeoCoordinate(GeoCoordinate3Double value, float duration, AnimationCurve curve)
		{
			SetGeoCoordinate(value, duration, (t) => { return curve.Evaluate(t); });
		}

		private GeoCoordinate3Double _fromGeoCoordinate;
		private void SetGeoCoordinate(GeoCoordinate3Double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
		{
			if (transform != Disposable.NULL && transform.isGeoCoordinateTransform && !Object.Equals(transform.geoCoordinate, value))
			{
				bool wrap = IsSpherical();

				_fromGeoCoordinate = transform.geoCoordinate;

				double longitudeDelta = MathPlus.ClosestLongitudeDelta(_fromGeoCoordinate.longitude, value.longitude); 

				localPositionTween = tweenManager.To(0.0f, 1.0f, duration, (t) =>
				{
					t = TCallback(t);

					double longitude;
					if (wrap)
						longitude = MathPlus.ClampAngle180(_fromGeoCoordinate.longitude + MathPlus.Lerp(0.0d, longitudeDelta, t));
					else
						longitude = MathPlus.Lerp(_fromGeoCoordinate.longitude, value.longitude, t);

					transform.geoCoordinate = new GeoCoordinate3Double(MathPlus.Lerp(_fromGeoCoordinate.latitude, value.latitude, t), longitude, MathPlus.Lerp(_fromGeoCoordinate.altitude, value.altitude, t));
				
				}, () => { localPositionTween = null; }, null, easing);
			}
		}

        private Tween localRotationTween
		{
			get { return _localRotationTween; }
			set
			{
				if (Object.ReferenceEquals(_localRotationTween, value))
					return;

                DisposeManager.Dispose(_localRotationTween);

				_localRotationTween = value;
			}
		}

        /// <summary>
        /// Start animating towards the localRotation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetLocalRotation(QuaternionDouble value, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalRotation(value, duration, easing);
		}

        /// <summary>
        /// Start animating towards the localRotation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetLocalRotation(QuaternionDouble value, float duration, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalRotation(value, duration, (t) => { return t; }, easing);
		}

        /// <summary>
        /// Start animating towards the localRotation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetLocalRotation(QuaternionDouble value, AnimationCurve curve)
		{
			SetLocalRotation(value, duration, curve);
		}

        /// <summary>
        /// Start animating towards the localRotation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetLocalRotation(QuaternionDouble value, float duration, AnimationCurve curve)
		{
			SetLocalRotation(value, duration, (t) => { return curve.Evaluate(t); });
		}

		private QuaternionDouble _fromLocalRotation;
		private void SetLocalRotation(QuaternionDouble value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
		{
			if (!Object.Equals(transform.localRotation, value) && transform != Disposable.NULL)
			{
				_fromLocalRotation = transform.localRotation;

				localRotationTween = tweenManager.To(0.0f, 1.0f, duration, (t) =>
				{
					transform.localRotation = QuaternionDouble.Lerp(_fromLocalRotation, value, TCallback(t));
				}, null, () => { localRotationTween = null; }, easing);
			}
		}

        private Tween localScaleTween
		{
			get { return _localScaleTween; }
			set
			{
				if (Object.ReferenceEquals(_localScaleTween, value))
					return;

                DisposeManager.Dispose(_localScaleTween);

				_localScaleTween = value;
			}
		}

        /// <summary>
        /// Start animating towards the localScale.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetLocalScale(Vector3Double value, EasingType easing = EasingType.QuartEaseOut)
		{
			SetLocalScale(value, duration, easing);
		}

        /// <summary>
        /// Start animating towards the localScale.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetLocalScale(Vector3Double value, float duration, EasingType easing = EasingType.QuadEaseInOut)
		{
			SetLocalScale(value, duration, (t) => { return t; }, easing);
		}

        /// <summary>
        /// Start animating towards the localScale.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetLocalScale(Vector3Double value, AnimationCurve curve)
		{
			SetLocalScale(value, duration, curve);
		}

        /// <summary>
        /// Start animating towards the localScale.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetLocalScale(Vector3Double value, float duration, AnimationCurve curve)
		{
			SetLocalScale(value, duration, (t) => { return curve.Evaluate(t); });
		}

		private Vector3Double _fromLocalScale;
		private void SetLocalScale(Vector3Double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
		{
			if (!Object.Equals(transform.localScale, value) && transform != Disposable.NULL)
			{
				_fromLocalScale = transform.localScale;
				localScaleTween = tweenManager.To(0.0f, 1.0f, duration, (t) =>
				{
					transform.localScale = Vector3Double.Lerp(_fromLocalScale, value, TCallback(t));
				}, () => { localScaleTween = null; }, null, easing);
			}
		}

        public override void StopAllAnimations()
        {
            base.StopAllAnimations();

            localPositionTween = null;
            localRotationTween = null;
            localScaleTween = null;
        }
    }
}
