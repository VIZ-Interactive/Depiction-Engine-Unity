// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Animator/" + nameof(TargetControllerAnimator))]
    [RequireComponent(typeof(TargetController))]
    public class TargetControllerAnimator : AnimatorBase
    {
		[SerializeField, Tooltip("The vector, in degrees pointing from the target towards the object, that should be reached by the end of the animation.")]
		private Vector3Double _toForwardVector;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(StartForwardVectorAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating towards the specified forward vector.")]
		private bool _startForwardVectorAnimation;
#endif
        [SerializeField, Tooltip("The distance from the target that should be reached by the end of the animation.")]
		private double _toDistance;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(StartDistanceAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating towards the specified distance.")]
		private bool _startDistanceAnimation;
#endif

        private Tween _forwardVectorTween;
        private Tween _distanceTween;

#if UNITY_EDITOR
        private void StartForwardVectorAnimationBtn()
		{
			SetForwardVector(_toForwardVector);
		}

        private void StartDistanceAnimationBtn()
		{
			SetDistance(_toDistance);
		}
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => toForwardVector = value, TargetController.DEFAULT_FORWARD_VECTOR_VALUE, initializingContext);
            InitValue(value => toDistance = value, TargetController.DEFAULT_DISTANCE_VALUE, initializingContext);
        }

        protected override float GetDefaultDuration()
        {
            return 0.3f;
        }

        /// <summary>
        /// The vector, in degrees pointing from the target towards the object, that should be reached by the end of the animation.
        /// </summary>
        [Json]
        private Vector3Double toForwardVector
        {
            get { return _toForwardVector; }
            set { SetValue(nameof(toForwardVector), value, ref _toForwardVector); }
        }

        /// <summary>
        /// The distance from the target that should be reached by the end of the animation.
        /// </summary>
        [Json]
        private double toDistance
        {
            get { return _toDistance; }
            set { SetValue(nameof(toDistance), value, ref _toDistance); }
        }

        private Tween forwardVectorTween
        {
            get { return _forwardVectorTween; }
            set
            {
                if (Object.ReferenceEquals(_forwardVectorTween, value))
                    return;

                DisposeManager.Dispose(_forwardVectorTween);

                _forwardVectorTween = value;
            }
        }

        /// <summary>
        /// Start animating towards the forward vector.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetForwardVector(Vector3Double value, EasingType easing = EasingType.QuartEaseOut)
        {
            SetForwardVector(value, duration, (t) => { return t; }, easing);
        }

        /// <summary>
        /// Start animating towards the forward vector.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetForwardVector(Vector3Double value, float duration, EasingType easing = EasingType.QuartEaseOut)
        {
            SetForwardVector(value, duration, (t) => { return t; }, easing);
        }

        /// <summary>
        /// Start animating towards the forward vector.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetForwardVector(Vector3Double value, AnimationCurve curve)
        {
            SetForwardVector(value, duration, curve);
        }

        /// <summary>
        /// Start animating towards the forward vector.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetForwardVector(Vector3Double value, float duration, AnimationCurve curve)
        {
            SetForwardVector(value, duration, (t) => { return curve.Evaluate(t); });
        }

        private Vector3Double _fromForwardVector;
        private void SetForwardVector(Vector3Double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
        {
            TargetController controller = objectBase.controller as TargetController;
            if (controller != Disposable.NULL)
            {
                _fromForwardVector = controller.forwardVector;
                forwardVectorTween = tweenManager.To(0.0f, 1.0f, duration, (t) => { controller.forwardVector = Vector3Double.Lerp(_fromForwardVector, value, TCallback(t)); }, null, () => { forwardVectorTween = null; }, easing);
            }
        }

        private Tween distanceTween
        {
            get { return _distanceTween; }
            set
            {
                if (Object.ReferenceEquals(_distanceTween, value))
                    return;

                DisposeManager.Dispose(_distanceTween);

                _distanceTween = value;
            }
        }

        /// <summary>
        /// Start animating towards the distance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetDistance(double value, EasingType easing = EasingType.QuartEaseOut)
        {
            SetDistance(value, duration, easing);
        }

        /// <summary>
        /// Start animating towards the distance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetDistance(double value, float duration, EasingType easing = EasingType.QuartEaseOut)
        {
            SetDistance(value, duration, (t) => { return t; }, easing);
        }

        /// <summary>
        /// Start animating towards the distance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetDistance(double value, AnimationCurve curve)
        {
            SetDistance(value, duration, curve);
        }

        /// <summary>
        /// Start animating towards the distance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetDistance(double value, float duration, AnimationCurve curve)
        {
            SetDistance(value, duration, (t) => { return curve.Evaluate(t); });
        }

        private double _fromDistance;
        private void SetDistance(double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
        {
            TargetController controller = objectBase.controller as TargetController;
            if (controller != Disposable.NULL)
            {
                _fromDistance = controller.distance;
                distanceTween = tweenManager.To(0.0f, 1.0f, duration, (t) => { controller.distance = _fromDistance + (TCallback(t) * (value - _fromDistance)); }, null, () => { distanceTween = null; }, easing);
            }
        }

        public override void StopAllAnimations()
        {
            base.StopAllAnimations();

            forwardVectorTween = null;
            distanceTween = null;
        }
    }
}
