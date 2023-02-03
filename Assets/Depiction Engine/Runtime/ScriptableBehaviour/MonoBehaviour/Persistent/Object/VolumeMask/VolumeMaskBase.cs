﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class VolumeMaskBase : Object, ICustomEffect
    {
        [BeginFoldout("Mask")]
        [SerializeField, Mask, Tooltip("The layers to be masked by this effect."), EndFoldout]
        private int _maskedLayers;

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => maskedLayers = value, 0, initializingState);
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

        public virtual int GetCusomtEffectComputeBufferDataSize()
        {
            return 0;
        }

        public virtual int AddToComputeBufferData(int startIndex, float[] computeBufferData)
        {
            return GetCusomtEffectComputeBufferDataSize();
        }

        public virtual bool IsInsideVolume(Vector3Double point)
        {
            return false;
        }
    }
}