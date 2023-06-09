// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepictionEngine
{
    public class ReflectionEffectBase : EffectBase
    {
        [BeginFoldout("Reflection")]
        [SerializeField, Tooltip("The width and height of the reflection texture.")]
        private int _reflectionTextureSize;
        [SerializeField, Tooltip("When enabled shadows are included in the reflection render.")]
        private bool _renderShadows;
        [SerializeField, Mask, Tooltip("The objects within the ignoreLayers will be excluded from the reflection render."), EndFoldout]
        private int _ignoreLayers;

        [SerializeField, HideInInspector]
        private RenderTexture _reflectionTexture;

        private bool _reflectionTextureDirty;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                _reflectionTexture = null;

            InitValue(value => reflectionTextureSize = value, 512, initializingContext);
            InitValue(value => renderShadows = value, false, initializingContext);
            InitValue(value => ignoreLayers = value, LayerMask.GetMask(new string[] { typeof(UIBase).Name, typeof(Star).Name, typeof(TerrainEdgeMeshRendererVisual).Name, typeof(TerrainGridMeshObject).Name, typeof(TerrainGridMeshObject).Name + InstanceManager.GLOBAL_LAYER, typeof(AtmosphereGridMeshObject).Name, "Ignore Render", "Ignore Raycast" }), initializingContext);
        }

        protected override float GetDefaultAlpha()
        {
            return 0.1f;
        }

        /// <summary>
        /// The width and height of the reflection texture.
        /// </summary>
        [Json]
        public int reflectionTextureSize
        {
            get => _reflectionTextureSize;
            set => SetValue(nameof(reflectionTextureSize),Mathf.Clamp(value, 2, 2048), ref _reflectionTextureSize); 
        }

        /// <summary>
        /// When enabled shadows are included in the reflection render.
        /// </summary>
        [Json]
        public bool renderShadows
        {
            get => _renderShadows;
            set => SetValue(nameof(renderShadows), value, ref _renderShadows);
        }

        /// <summary>
        /// The objects within the ignoreLayers will be excluded from the reflection render.
        /// </summary>
        [Json]
        public int ignoreLayers
        {
            get => _ignoreLayers;
            set => SetValue(nameof(ignoreLayers), value, ref _ignoreLayers);
        }

        private RenderTexture reflectionTexture
        {
            get => _reflectionTexture;
            set
            {
                if (Object.ReferenceEquals(_reflectionTexture, value))
                    return;

                DisposeManager.Dispose(_reflectionTexture);

                _reflectionTexture = value;
                _reflectionTextureDirty = true;
            }
        }

        protected override void ModifyClipPlanes(Camera camera, ref float near, ref float far)
        {
            near = 0.1f;
            RTTCamera.ModifyClipPlanesToIncludeAtmosphere(geoAstroObject, camera.transform.position, ref far);
        }

        protected override void ApplyBackgroundToRTTUnityCamera(UnityEngine.Camera unityCamera, Camera copyFromCamera)
        {
            unityCamera.clearFlags = CameraClearFlags.SolidColor;
            unityCamera.backgroundColor = Color.black;
        }

        protected override bool ApplyPropertiesToRTTUnityCamera(UnityEngine.Camera rttUnityCamera, Camera copyFromCamera, int cullingMask)
        {
            if (base.ApplyPropertiesToRTTUnityCamera(rttUnityCamera, copyFromCamera, cullingMask))
            {
                UniversalAdditionalCameraData additionalData = rttUnityCamera.GetUniversalAdditionalCameraData();
                additionalData.renderShadows = renderShadows;
                additionalData.requiresColorOption = CameraOverrideOption.On;
                return true;
            }
            return false;
        }

        public RenderTexture GetTexture(RTTCamera rttCamera, Camera camera, ScriptableRenderContext context)
        {
            int textureSize = Mathf.ClosestPowerOfTwo(reflectionTextureSize);
            if (reflectionTexture == null || reflectionTexture.width != textureSize || reflectionTexture.height != textureSize)
            {
                reflectionTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32, 0);
                reflectionTexture.useMipMap = false;
                reflectionTexture.name = GetTextureName() + id;
                reflectionTexture.isPowerOfTwo = true;
            }

            if (_reflectionTextureDirty)
            {
                _reflectionTextureDirty = false;
                RenderToTexture(rttCamera, camera, context, reflectionTexture, ~ignoreLayers);
            }

            return reflectionTexture;
        }

        public virtual string GetTextureName()
        {
            return "";
        }

        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                _reflectionTextureDirty = true;

                return true;
            }
            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (disposeContext != DisposeContext.Programmatically_Pool)
                    DisposeManager.Dispose(_reflectionTexture, disposeContext);

                return true;
            }
            return false;
        }
    }
}
