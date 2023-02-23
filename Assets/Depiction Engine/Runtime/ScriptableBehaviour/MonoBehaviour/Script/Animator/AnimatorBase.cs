// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
	[DisallowMultipleComponent]
	public class AnimatorBase : Script
    {
#if UNITY_EDITOR
		[SerializeField, Button(nameof(StopAllAnimationsBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Stop all playing animations that were started by this 'Animator'.")]
        private bool _stopAllAnimations;
#endif

		[SerializeField, Tooltip("The duration of the animation.")]
#if UNITY_EDITOR
		[ConditionalShow(nameof(GetShowDuration))]
#endif
		private float _duration;

#if UNITY_EDITOR
        private void StopAllAnimationsBtn()
        {
			StopAllAnimations();
        }

        protected virtual bool GetShowDuration()
		{
			return true;
		}
#endif

		protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

			InitValue(value => duration = value, GetDefaultDuration(), initializingContext);
		}

		protected override bool AddInstanceToManager()
		{
			return true;
		}

		protected virtual float GetDefaultDuration()
        {
			return 1.0f;
        }

		/// <summary>
		/// The duration of the animation.
		/// </summary>
		[Json]
		public float duration
        {
            get { return _duration; }
            set { SetValue(nameof(duration), value, ref _duration); }
        }

		/// <summary>
		/// Stop all playing animations that were started by this 'Animator'.
		/// </summary>
        public virtual void StopAllAnimations()
		{

		}

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                if (isActiveAndEnabled)
                    UpdateAnimator();

                return true;
            }
            return false;
        }

        protected virtual void UpdateAnimator()
        {
        }

        public override bool OnDisposing()
		{
			if (base.OnDisposing())
			{
				StopAllAnimations();

				return true;
			}
			return false;
		}
	}
}
