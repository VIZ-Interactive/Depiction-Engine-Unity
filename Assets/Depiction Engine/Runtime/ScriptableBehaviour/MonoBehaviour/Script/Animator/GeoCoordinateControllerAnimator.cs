// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Animator/" + nameof(GeoCoordinateControllerAnimator))]
    [RequireComponent(typeof(GeoCoordinateController))]
    public class GeoCoordinateControllerAnimator : AnimatorBase
    {
        [SerializeField, Tooltip("The distance to the ground that should be reached by the end of the animation.")]
		private double _toGroundSnapOffset;

#if UNITY_EDITOR
        [SerializeField, Button(nameof(StartGroundSnapOffsetAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start moving the transform.")]
		private bool _startGroundSnapOffsetAnimation;
#endif

        private Tween _groundSnapOffsetTween;

#if UNITY_EDITOR
        private void StartGroundSnapOffsetAnimationBtn()
		{
			SetGroundSnapOffset(toGroundSnapOffset);
		}
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => toGroundSnapOffset = value, GeoCoordinateController.DEFAULT_GROUND_SNAP_OFFSET_VALUE, initializingContext);
        }

        protected override float GetDefaultDuration()
        {
            return 0.3f;
        }

        private Tween groundSnapOffsetTween
        {
            get { return _groundSnapOffsetTween; }
            set
            {
                if (Object.ReferenceEquals(_groundSnapOffsetTween, value))
                    return;

                DisposeManager.Dispose(_groundSnapOffsetTween);

                _groundSnapOffsetTween = value;
            }
        }

        /// <summary>
        /// The distance to the ground that should be reached by the end of the animation.
        /// </summary>
        [Json]
        private double toGroundSnapOffset
        {
            get { return _toGroundSnapOffset; }
            set { SetValue(nameof(toGroundSnapOffset), value, ref _toGroundSnapOffset); }
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetGroundSnapOffset(double value, EasingType easing = EasingType.QuartEaseOut)
        {
            SetGroundSnapOffset(value, duration, easing);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetGroundSnapOffset(double value, float duration, EasingType easing = EasingType.QuartEaseOut)
        {
            SetGroundSnapOffset(value, duration, (t) => { return t; }, easing);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetGroundSnapOffset(double value, AnimationCurve curve)
        {
            SetGroundSnapOffset(value, duration, curve);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetGroundSnapOffset(double value, float duration, AnimationCurve curve)
        {
            SetGroundSnapOffset(value, duration, (t) => { return curve.Evaluate(t); });
        }

        private double _fromGroundSnapOffset;
        private void SetGroundSnapOffset(double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
        {
            GeoCoordinateController controller = objectBase.controller as GeoCoordinateController;
            if (controller != Disposable.NULL)
            {
                _fromGroundSnapOffset = controller.groundSnapOffset;
                groundSnapOffsetTween = tweenManager.To(0.0f, 1.0f, duration, (t) => { controller.groundSnapOffset = _fromGroundSnapOffset + (TCallback(t) * (value - _fromGroundSnapOffset)); }, null, () => { groundSnapOffsetTween = null; }, easing);
            }
        }

        public override void StopAllAnimations()
        {
            base.StopAllAnimations();

            groundSnapOffsetTween = null;
        }
    }
}
