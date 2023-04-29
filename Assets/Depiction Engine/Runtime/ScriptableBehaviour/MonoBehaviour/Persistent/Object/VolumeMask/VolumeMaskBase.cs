// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Hide the geometry that lies inside its volume.
    /// </summary>
    public class VolumeMaskBase : Object, ICustomEffect
    {
        [BeginFoldout("Mask")]
        [SerializeField, Mask, Tooltip("The layers to be masked by this effect."), EndFoldout]
        private int _maskedLayers;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => maskedLayers = value, 0, initializingContext);
        }

        /// <summary>
        /// The layers to be masked by this effect.
        /// </summary>
        [Json]
        public int maskedLayers
        {
            get { return _maskedLayers;}
            set { SetValue(nameof(maskedLayers), value, ref _maskedLayers); }
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        public virtual int GetCustomEffectComputeBufferDataSize()
        {
            return 0;
        }

        public virtual int AddToComputeBufferData(int startIndex, float[] computeBufferData)
        {
            return GetCustomEffectComputeBufferDataSize();
        }

        public virtual bool IsInsideVolume(Vector3Double point)
        {
            return false;
        }
    }
}
