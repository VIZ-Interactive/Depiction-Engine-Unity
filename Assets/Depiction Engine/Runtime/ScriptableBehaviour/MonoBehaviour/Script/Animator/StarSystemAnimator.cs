// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
	[AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Animator/" + nameof(StarSystemAnimator))]
	[RequireComponent(typeof(StarSystem))]
    public class StarSystemAnimator : AnimatorBase
    {
		[SerializeField, Tooltip("How fast the animation should play.")]
		private float _speed;
		[SerializeField, Tooltip("When enabled the animation will play to infinity.")]
		private bool _infinity;
		[SerializeField, Tooltip("For how many days should the animation play.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _day;
		[SerializeField, Tooltip("For how many hours should the animation play.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _hour;
		[SerializeField, Tooltip("For how many minutes should the animation play.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _minute;
		[SerializeField, Tooltip("For how many seconds should the animation play.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _second;
		[SerializeField, Tooltip("For how many milliseconds should the animation play.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _millisecond;
#if UNITY_EDITOR
		[SerializeField, Button(nameof(StartTimeAnimationBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start animating the current '"+nameof(StarSystem)+"' time for the duration specified.")]
		private bool _startTimeAnimation;

		private void StartTimeAnimationBtn()
        {
            StartTimeAnimation();
		}

		protected override bool GetShowDuration()
		{
			return false;
		}

		protected bool GetEnableCustomTimeFields()
        {
			return !infinity;
		}
#endif

		protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

			InitValue(value => speed = value, 10000.0f, initializingContext);
			InitValue(value => infinity = value, true, initializingContext);
			InitValue(value => day = value, 0, initializingContext);
			InitValue(value => hour = value, 0, initializingContext);
			InitValue(value => minute = value, 0, initializingContext);
			InitValue(value => second = value, 0, initializingContext);
			InitValue(value => millisecond = value, 0, initializingContext);
		}

		/// <summary>
		/// How fast the animation should play.
		/// </summary>
		[Json]
		private float speed
		{
			get => _speed;
			set => SetValue(nameof(speed), value, ref _speed);
		}

		/// <summary>
		/// When enabled the animation will play to infinity.
		/// </summary>
		[Json]
		private bool infinity
		{
			get => _infinity;
			set => SetValue(nameof(infinity), value, ref _infinity);
		}

		/// <summary>
		/// For how many days should the animation play.
		/// </summary>
		[Json]
		private int day
		{
			get => _day;
			set => SetValue(nameof(day), value, ref _day);
		}

        /// <summary>
        /// For how many hours should the animation play.
        /// </summary>
        [Json]
		private int hour
		{
			get => _hour;
			set => SetValue(nameof(hour), value, ref _hour);
		}

        /// <summary>
        /// For how many minutes should the animation play.
        /// </summary>
        [Json]
		private int minute
		{
			get => _minute;
			set => SetValue(nameof(minute), value, ref _minute);
		}

        /// <summary>
        /// For how many seconds should the animation play.
        /// </summary>
        [Json]
		private int second
		{
			get => _second;
			set => SetValue(nameof(second), value, ref _second);
		}

        /// <summary>
        /// For how many milliseconds should the animation play.
        /// </summary>
        [Json]
		private int millisecond
		{
			get => _millisecond;
			set => SetValue(nameof(millisecond), value, ref _millisecond);
		}

		public TimeSpan GetDuration()
        {
			return new TimeSpan(day, hour, minute, second, millisecond);
        }

		private DateTime? _fromTime;
		private DateTime? _toTime;
		private DateTimeOffset? _startTime;
        /// <summary>
        /// Start animating the current <see cref="DepictionEngine.StarSystem"/> time for the duration specified.
        /// </summary>
        public void StartTimeAnimation()
		{
			StopAllAnimations();
			StarSystem starSystem = objectBase as StarSystem;
			if (starSystem != Disposable.NULL)
			{
				_startTime = DateTime.UtcNow;
				_fromTime = starSystem.GetTime();
				if (!infinity)
					_toTime = _fromTime.Value.Add(GetDuration());
			}
		}

        public override void StopAllAnimations()
        {
            base.StopAllAnimations();

            _fromTime = null;
            _toTime = null;
            _startTime = null;
        }

        protected override void UpdateAnimator()
        {
			base.UpdateAnimator();

            StarSystem starSystem = objectBase as StarSystem;
            if (starSystem != Disposable.NULL)
            {
                if (_fromTime.HasValue && _startTime.HasValue)
                {
                    TimeSpan durationSinceStart = DateTime.UtcNow - _startTime.Value;
                    DateTime currentTime = _fromTime.Value.Add(TimeSpan.FromMilliseconds(durationSinceStart.TotalMilliseconds * speed));
                    if (_toTime.HasValue && currentTime >= _toTime.Value)
                    {
                        currentTime = _toTime.Value;
                        StopAllAnimations();
                    }
                    starSystem.SetTime(currentTime);
                }
            }
        }
    }
}
