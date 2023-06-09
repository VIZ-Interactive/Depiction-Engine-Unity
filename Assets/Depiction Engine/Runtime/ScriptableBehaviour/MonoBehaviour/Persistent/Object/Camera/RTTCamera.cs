// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    public class RTTCamera : Camera
    {
        protected override void InitializeCamera(InitializationContext initializingContext)
        {
            base.InitializeCamera(initializingContext);

            unityCamera.enabled = false;
        }

        public override int GetDefaultDistancePass()
        {
            return 0;
        }

        protected override bool GetDefaultPostProcessing()
        {
            return false;
        }

        protected override bool AddInstanceToManager()
        {
            return false;
        }

        protected override bool GetDefaultIsHiddenInHierarchy()
        {
            return true;
        }

        protected override bool GetDefaultDontSaveToScene()
        {
            return true;
        }

        public override bool IsReflectionObject()
        {
            return false;
        }

        public RenderTexture RenderToCubemap(CameraClearFlags clearFlags, Color backgroundColor, Material skyboxMaterial, int cullingMask, float nearClipPlane, float farClipPlane, Vector3 unityCameraPosition, Quaternion unityCameraRotation, RenderTexture cubemap, Action<UnityEngine.Camera> applyPropertiesToUnityCamera = null, Action<UnityEngine.Camera> resetUnityCamera = null, int faceMask = 63)
        {
            ApplyPropertiesToRTTUnityCamera(clearFlags, backgroundColor, skyboxMaterial, cullingMask, nearClipPlane, farClipPlane, unityCameraPosition, unityCameraRotation, applyPropertiesToUnityCamera);

            unityCamera.RenderToCubemap(cubemap, faceMask);
           
            ResetUnityCamera(resetUnityCamera);
         
            return cubemap;
        }

        public RenderTexture RenderToTexture(CameraClearFlags clearFlags, Color backgroundColor, Material skyboxMaterial, int cullingMask, float nearClipPlane, float farClipPlane, Vector3 unityCameraPosition, Quaternion unityCameraRotation, ScriptableRenderContext context, RenderTexture texture, Action<UnityEngine.Camera> applyPropertiesToUnityCamera = null, Action<UnityEngine.Camera> resetUnityCamera = null)
        {
            unityCamera.targetTexture = texture;

            ApplyPropertiesToRTTUnityCamera(clearFlags, backgroundColor, skyboxMaterial, cullingMask, nearClipPlane, farClipPlane, unityCameraPosition, unityCameraRotation, applyPropertiesToUnityCamera);

#pragma warning disable CS0618 // Type or member is obsolete
            UnityEngine.Rendering.Universal.UniversalRenderPipeline.RenderSingleCamera(context, unityCamera);
#pragma warning restore CS0618 // Type or member is obsolete

            ResetUnityCamera(resetUnityCamera);

            unityCamera.targetTexture = null;

            return texture;
        }

        private void ApplyPropertiesToRTTUnityCamera(CameraClearFlags clearFlags, Color backgroundColor, Material skyboxMaterial, int cullingMask, float nearClipPlane, float farClipPlane, Vector3 unityCameraPosition, Quaternion unityCameraRotation, Action<UnityEngine.Camera> applyPropertiesToUnityCamera)
        {
            unityCamera.enabled = true;

            unityCamera.clearFlags = clearFlags;
            unityCamera.backgroundColor = backgroundColor;
            skybox.material = skyboxMaterial;

            unityCamera.cullingMask = cullingMask;

            Camera.ApplyClipPlanePropertiesToUnityCamera(unityCamera, 0, nearClipPlane, farClipPlane);
         
            unityCamera.transform.SetPositionAndRotation(unityCameraPosition, unityCameraRotation);

            applyPropertiesToUnityCamera?.Invoke(unityCamera);

            transform.InitLastTransformFields();
        }

        public static void ModifyClipPlanesToIncludeAtmosphere(GeoAstroObject geoAstroObject, Vector3Double position, ref float far)
        {
            if (geoAstroObject != Disposable.NULL)
            {
                //Extend farClipPlane to make sure atmosphere is included in the reflection
                AtmosphereEffect atmosphereEffect = null;
                foreach (EffectBase effect in geoAstroObject.effects)
                {
                    if (effect is AtmosphereEffect)
                    {
                        atmosphereEffect = effect as AtmosphereEffect;
                        break;
                    }
                }
                if (atmosphereEffect != Disposable.NULL)
                {
                    double atmosphereDistance = (geoAstroObject.GetGeoCoordinateFromPoint(position).altitude + atmosphereEffect.GetAtmosphereAltitude()) * geoAstroObject.GetScale();
                    far = (float)atmosphereDistance;
                }
                else
                    far = 1000.0f;
            }
        }

        private void ResetUnityCamera(Action<UnityEngine.Camera> resetUnityCamera)
        {
            unityCamera.enabled = false;

            unityCamera.ResetWorldToCameraMatrix();
            unityCamera.ResetProjectionMatrix();
            unityCamera.ResetCullingMatrix();

            if (resetUnityCamera != null)
                resetUnityCamera(unityCamera);
        }
    }
}
