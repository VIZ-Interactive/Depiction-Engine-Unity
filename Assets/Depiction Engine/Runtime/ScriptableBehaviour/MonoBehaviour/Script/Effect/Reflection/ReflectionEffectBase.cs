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
        private Vector2Int _reflectionTextureSize;
        [SerializeField, Tooltip("When enabled shadows are included in the reflection render.")]
        private bool _renderShadows;
        [SerializeField, Mask, Tooltip("The objects within the ignoreLayers will be excluded from the reflection render."), EndFoldout]
        private int _ignoreLayers;

        private RenderTexture _reflectionTexture;

        private bool _reflectionTextureDirty;

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => reflectionTextureSize = value, new Vector2Int(512, 512), initializingState);
            InitValue(value => renderShadows = value, false, initializingState);
            InitValue(value => ignoreLayers = value, LayerMask.GetMask(new string[] { typeof(UIBase).Name, typeof(Star).Name, typeof(TerrainEdgeMeshRendererVisual).Name, typeof(TerrainGridMeshObject).Name, typeof(TerrainGridMeshObject).Name + InstanceManager.GLOBAL_LAYER, typeof(AtmosphereGridMeshObject).Name, "Ignore Render", "Ignore Raycast" }), initializingState);
        }

        protected override float GetDefaultAlpha()
        {
            return 0.1f;
        }

        /// <summary>
        /// The width and height of the reflection texture.
        /// </summary>
        [Json]
        public Vector2Int reflectionTextureSize
        {
            get { return _reflectionTextureSize; }
            set 
            {
                if (value.x < 2)
                    value.x = 2;
                if (value.y < 2)
                    value.y = 2;
                SetValue(nameof(reflectionTextureSize), value, ref _reflectionTextureSize); 
            }
        }

        /// <summary>
        /// When enabled shadows are included in the reflection render.
        /// </summary>
        [Json]
        public bool renderShadows
        {
            get { return _renderShadows; }
            set { SetValue(nameof(renderShadows), value, ref _renderShadows); }
        }

        /// <summary>
        /// The objects within the ignoreLayers will be excluded from the reflection render.
        /// </summary>
        [Json]
        public int ignoreLayers
        {
            get { return _ignoreLayers; }
            set { SetValue(nameof(ignoreLayers), value, ref _ignoreLayers); }
        }

        private RenderTexture reflectionTexture
        {
            get { return _reflectionTexture; }
            set
            {
                if (Object.ReferenceEquals(_reflectionTexture, value))
                    return;

                if (!Object.ReferenceEquals(_reflectionTexture, null))
                    DisposeManager.Dispose(_reflectionTexture);

                _reflectionTexture = value;
                _reflectionTextureDirty = true;
            }
        }

        protected override void ModifyClipPlanes(Camera camera, ref float near, ref float far)
        {
            near = 0.1f;
            RTTCamera.ModifyClipPlanesToIncludeAtmosphere(geoAstroObject, camera, ref far);
        }

        protected override void ApplyBackgroundToRTTUnityCamera(UnityEngine.Camera unityCamera, Camera copyFromCamera)
        {
            unityCamera.clearFlags = CameraClearFlags.SolidColor;
            unityCamera.backgroundColor = Color.black;
        }

        protected override bool ApplyPropertiesToRTTUnityCamera(UnityEngine.Camera unityCamera, Camera copyFromCamera, int cullingMask)
        {
            if (base.ApplyPropertiesToRTTUnityCamera(unityCamera, copyFromCamera, cullingMask))
            {
                UniversalAdditionalCameraData additionalData = unityCamera.GetUniversalAdditionalCameraData();
                additionalData.renderShadows = renderShadows;
                additionalData.requiresColorOption = CameraOverrideOption.On;
                return true;
            }
            return false;
        }

        public RenderTexture GetTexture(RTTCamera rttCamera, Camera camera, ScriptableRenderContext context)
        {
            if (reflectionTexture == null || reflectionTexture.width != reflectionTextureSize.x || reflectionTexture.height != reflectionTextureSize.y)
            {
                reflectionTexture = new RenderTexture(reflectionTextureSize.x, reflectionTextureSize.y, 16, RenderTextureFormat.ARGB32, 0);
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

        public override void OnDestroy()
        {
            base.OnDestroy();

            Dispose(_reflectionTexture);
        }
    }
}
