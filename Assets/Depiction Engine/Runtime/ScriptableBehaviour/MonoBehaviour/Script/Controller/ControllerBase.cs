// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
	[DisallowMultipleComponent]
	public class ControllerBase : Script
	{
        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            ForceUpdateTransform(true, true, true);
		}

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected override bool RemoveObjectBaseDelegate(Object objectBase)
		{
			if (base.RemoveObjectBaseDelegate(objectBase))
			{
				objectBase.TransformControllerCallback -= TransformControllerCallbackHandler;

				return true;
			}
			return false;
		}

		protected override bool AddObjectBaseDelegate(Object objectBase)
		{
			if (base.AddObjectBaseDelegate(objectBase))
			{
				objectBase.TransformControllerCallback += TransformControllerCallbackHandler;

				return true;
			}
			return false;
		}

		private void TransformControllerCallbackHandler(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
		{
			TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera);
		}

		protected virtual bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
		{
			bool isActive = initialized;

			if (isActive)
			{
				//The try catch is necessary because the controller might have been destroyed as a result of an undo or redo and we wont yet know at this point.
				try
				{
					if (isActiveAndEnabled)
						isActive = true;
				}
				catch (MissingReferenceException)
				{
					isActive = false;
				}
			}

            return isActive;
		}

		protected override void ActiveAndEnabledChanged(bool newValue, bool oldValue)
		{
			base.ActiveAndEnabledChanged(newValue, oldValue);

			if (newValue)
				ForceUpdateTransform(true, true, true);
		}

		public virtual void UpdateControllerTransform(Camera camera)
		{

		}

        public ComponentChangedPending forceUpdateTransformPending
		{
			get { return objectBase.forceUpdateTransformPending; }
        }

		public void ForceUpdateTransformPending(bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false)
		{
			if (initialized && objectBase != Disposable.NULL)
				objectBase.ForceUpdateTransformPending(localPositionChanged, localRotationChanged, localScaleChanged);
		}

		public bool ForceUpdateTransform(bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false, Camera camera = null)
		{
			if (initialized && objectBase != Disposable.NULL)
				return objectBase.ForceUpdateTransform(localPositionChanged, localRotationChanged, localScaleChanged, camera);
			
			return false;
		}
	}
}
