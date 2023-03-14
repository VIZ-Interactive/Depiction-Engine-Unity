// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    public class EffectBase : Script
    {
        [BeginFoldout("Effect")]
        [SerializeField, Tooltip("How visible should the effect be."), EndFoldout]
        private float _alpha;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => alpha = value, GetDefaultAlpha(), initializingContext);
        }

        protected virtual float GetDefaultAlpha()
        {
            return 1.0f;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        /// <summary>
        /// How visible should the effect be.
        /// </summary>
        [Json]
        public float alpha
        {
            get { return _alpha; }
            set 
            {
                if (value < 0.0f)
                    value = 0.0f;
                SetValue(nameof(alpha), value, ref _alpha); 
            }
        }

        public GeoAstroObject geoAstroObject
        {
            get { return objectBase as GeoAstroObject; }
        }

        public virtual float GetAlpha()
        {
            return isActiveAndEnabled ? alpha : 0.0f;
        }

        protected RenderTexture RenderToTexture(RTTCamera rttCamera, Camera camera, ScriptableRenderContext context, RenderTexture texture, int cullingMask)
        {
           return rttCamera.RenderToTexture(camera, context, texture, (rttUnityCamera, camera) => 
           { 
               if (!ApplyPropertiesToRTTUnityCamera(rttUnityCamera, camera, cullingMask))
                   rttUnityCamera.cullingMask = 0;
           }, ResetRTTUnityCamera);
        }

        protected virtual bool ApplyPropertiesToRTTUnityCamera(UnityEngine.Camera rttUnityCamera, Camera camera, int cullingMask)
        {
            ApplyBackgroundToRTTUnityCamera(rttUnityCamera, camera);

            rttUnityCamera.fieldOfView = camera.fieldOfView;
            rttUnityCamera.aspect = camera.aspect;
            rttUnityCamera.orthographic = camera.orthographic;
            rttUnityCamera.orthographicSize = camera.orthographicSize;

            rttUnityCamera.cullingMask = cullingMask;

            float nearClipPlane = rttUnityCamera.nearClipPlane;
            float farClipPlane = rttUnityCamera.farClipPlane;
            ModifyClipPlanes(camera, ref nearClipPlane, ref farClipPlane);
            Camera.ApplyClipPlanePropertiesToUnityCamera(rttUnityCamera, 0, nearClipPlane, farClipPlane);

            return true;
        }

        protected virtual void ApplyBackgroundToRTTUnityCamera(UnityEngine.Camera rttUnityCamera, Camera camera)
        {
           
        }

        protected virtual void ModifyClipPlanes(Camera camera, ref float near, ref float far)
        {
            
        }

        protected virtual void ResetRTTUnityCamera(UnityEngine.Camera rttUnityCamera)
        {
            
        }
    }
}
