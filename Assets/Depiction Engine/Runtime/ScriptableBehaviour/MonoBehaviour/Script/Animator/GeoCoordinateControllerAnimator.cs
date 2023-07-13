// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Animator/" + nameof(GeoCoordinateControllerAnimator))]
    [RequireComponent(typeof(GeoCoordinateController))]
    public class GeoCoordinateControllerAnimator : AnimatorBase
    {
        [SerializeField, Tooltip("The distance to the surface that should be reached by the end of the animation.")]
		private double _toSurfaceSnapOffset;

#if UNITY_EDITOR
        [SerializeField, Button(nameof(StartSurfaceSnapOffsetAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start moving the transform.")]
		private bool _startSurfaceSnapOffsetAnimation;
#endif

        private Tween _surfaceSnapOffsetTween;

#if UNITY_EDITOR
        private void StartSurfaceSnapOffsetAnimationBtn()
		{
			SetSurfaceSnapOffset(_toSurfaceSnapOffset, easing);
		}
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => toSurfaceSnapOffset = value, GeoCoordinateController.DEFAULT_SURFACE_SNAP_OFFSET_VALUE, initializingContext);
        }

        protected override float GetDefaultDuration()
        {
            return 0.3f;
        }

        private Tween surfaceSnapOffsetTween
        {
            get => _surfaceSnapOffsetTween;
            set
            {
                if (Object.ReferenceEquals(_surfaceSnapOffsetTween, value))
                    return;

                DisposeManager.Dispose(_surfaceSnapOffsetTween);

                _surfaceSnapOffsetTween = value;
            }
        }

        /// <summary>
        /// The distance to the surface that should be reached by the end of the animation.
        /// </summary>
        [Json]
        private double toSurfaceSnapOffset
        {
            get => _toSurfaceSnapOffset;
            set => SetValue(nameof(toSurfaceSnapOffset), value, ref _toSurfaceSnapOffset);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="easing"></param>
        public void SetSurfaceSnapOffset(double value, EasingType easing = EasingType.QuartEaseOut)
        {
            SetSurfaceSnapOffset(value, duration, easing);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="easing"></param>
        public void SetSurfaceSnapOffset(double value, float duration, EasingType easing = EasingType.QuartEaseOut)
        {
            SetSurfaceSnapOffset(value, duration, (t) => { return t; }, easing);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="curve"></param>
        public void SetSurfaceSnapOffset(double value, AnimationCurve curve)
        {
            SetSurfaceSnapOffset(value, duration, curve);
        }

        /// <summary>
        /// Start moving the transform.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="curve"></param>
        public void SetSurfaceSnapOffset(double value, float duration, AnimationCurve curve)
        {
            SetSurfaceSnapOffset(value, duration, (t) => { return curve.Evaluate(t); });
        }

        private double _fromSurfaceSnapOffset;
        private void SetSurfaceSnapOffset(double value, float duration, Func<float, float> TCallback, EasingType easing = EasingType.Linear)
        {
            GeoCoordinateController controller = objectBase.controller as GeoCoordinateController;
            if (controller != Disposable.NULL)
            {
                _fromSurfaceSnapOffset = controller.surfaceSnapOffset;
                surfaceSnapOffsetTween = tweenManager.To(0.0f, 1.0f, duration, (t) => { controller.surfaceSnapOffset = _fromSurfaceSnapOffset + (TCallback(t) * (value - _fromSurfaceSnapOffset)); }, null, () => { surfaceSnapOffsetTween = null; }, easing);
            }
        }

        public override void StopAllAnimations()
        {
            base.StopAllAnimations();

            surfaceSnapOffsetTween = null;
        }
    }
}
