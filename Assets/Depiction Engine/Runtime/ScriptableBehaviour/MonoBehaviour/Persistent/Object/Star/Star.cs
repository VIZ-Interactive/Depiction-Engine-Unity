// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepictionEngine
{
    /// <summary>
    /// A light source, and lens flare effect, emitting light in all directions.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/" + nameof(Star))]
    [SelectionBase]
    public class Star : GeoAstroObject
    {
        [BeginFoldout("Star")]
        [SerializeField, Tooltip("The color of the light.")]
        private Color _color;
        [SerializeField, Tooltip("A multiplier used to increase the size of the lens flare.")]
        private float _lensFlareScale;
        [SerializeField, Tooltip("The intensity of a light is multiplied with the Light color.")]
        private float _intensity;
        [SerializeField, Tooltip("The range of the light.")]
        private float _range;
        [SerializeField, Tooltip("How this light casts shadows."), EndFoldout]
        private LightShadows _shadows;
        [BeginFoldout("LensFlare")]
        [SerializeField, Tooltip("When enabled, raycasting is used to (partially or completely) occlude the lens flare.")]
        private bool _useOcclusion;
        [SerializeField, Tooltip("Sets a radius multiplier around the light used to calculate the occlusion of the lens flare. If this area is half occluded by geometry, the intensity of the lens flare is cut by half.")]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLensFlareOcclusion))]
#endif
        private float _occlusionRadius;
        [SerializeField, Tooltip("Sets the number of random samples used inside the Occlusion Radius area. A higher sample count gives a smoother attenuation when occluded."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableLensFlareOcclusion))]
#endif
        private uint _occlusionSampleCount;

        private Light _lightInternal;
        private LensFlareComponentSRP _lensFlare;

#if UNITY_EDITOR
        protected override bool GetEnableSpherical()
        {
            return false;
        }

        protected override bool GetEnableReflectionProbe()
        {
            return false;
        }

        protected bool GetEnableLensFlareOcclusion()
        {
            return useOcclusion;
        }
#endif

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            InitLightAndLensFlare(initializingContext);
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => color = value, new Color(1.0f, 0.9597525f, 0.9016172f), initializingContext);
            InitValue(value => lensFlareScale = value, 1.3f, initializingContext);
            InitValue(value => intensity = value, 1.8f, initializingContext);
            InitValue(value => range = value, 1000000.0f, initializingContext);
            InitValue(value => shadows = value, LightShadows.Soft, initializingContext);
            InitValue(value => useOcclusion = value, true, initializingContext);
            InitValue(value => occlusionRadius = value, 0.05f, initializingContext);
            InitValue(value => occlusionSampleCount = value, (uint)6, initializingContext);
        }

        protected override void InitReflectionProbeObject(InitializationContext initializingContext)
        {
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        protected override bool GetDefaultReflectionProbe()
        {
            return false;
        }

        private void InitLightAndLensFlare(InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            if (!isFallbackValues)
            {
                if (_lightInternal == null || _lensFlare == null)
                {
                    _lightInternal = gameObject.GetComponentInChildren<Light>(true);
                    _lensFlare = gameObject.GetComponentInChildren<LensFlareComponentSRP>(true);
                }

                if (_lightInternal == null)
                {
                    GameObject go = new("Light");
                    go.transform.SetParent(gameObject.transform, false);

#if UNITY_EDITOR
                    Editor.UndoManager.QueueRegisterCreatedObjectUndo(go, initializingContext);
#endif

                    _lightInternal = go.AddComponentInitialized<Light>(initializingContext);
                    _lightInternal.type = LightType.Directional;

                    go.AddComponentInitialized<UniversalAdditionalLightData>(initializingContext);

                    _lensFlare = go.AddComponentInitialized<LensFlareComponentSRP>(initializingContext);
                    _lensFlare.lensFlareData = Resources.Load("LensFlare/Sun Lens Flare (SRP)") as LensFlareDataSRP;
                }
            }
        }

        private Light lightInternal
        {
            get 
            {
                InitLightAndLensFlare();
                return _lightInternal; 
            }
        }

        private LensFlareComponentSRP lensFlare
        {
            get
            {
                InitLightAndLensFlare();
                return _lensFlare;
            }
        }

#if UNITY_EDITOR
        private UnityEngine.Object[] GetLightAdditionalRecordObjects()
        {
            if (lightInternal != null)
                return new UnityEngine.Object[] { lightInternal };
            return null;
        }
#endif

        /// <summary>
        /// The color of the light.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetLightAdditionalRecordObjects))]
#endif
        public Color color
        {
            get => _color;
            set
            {
                SetValue(nameof(color), value, ref _color, (newValue, oldValue) =>
                {
                    if (lightInternal != null)
                        lightInternal.color = value;
                });
            }
        }

        /// <summary>
        /// A multiplier used to increase the size of the lens flare.
        /// </summary>
        [Json]
        public float lensFlareScale
        {
            get => _lensFlareScale;
            set { SetValue(nameof(lensFlareScale), value, ref _lensFlareScale); }
        }

        /// <summary>
        /// The intensity of a light is multiplied with the Light color.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetLightAdditionalRecordObjects))]
#endif
        public float intensity
        {
            get => _intensity;
            set
            {
                SetValue(nameof(intensity), value, ref _intensity, (newValue, oldValue) =>
                {
                    if (lightInternal != null)
                        lightInternal.intensity = value;
                });
            }
        }

        /// <summary>
        /// The range of the light.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetLightAdditionalRecordObjects))]
#endif
        public float range
        {
            get => _range;
            set
            {
                SetValue(nameof(range), value, ref _range, (newValue, oldValue) =>
                {
                    if (lightInternal != null)
                        lightInternal.range = range;
                });
            }
        }

        /// <summary>
        /// How this light casts shadows.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetLightAdditionalRecordObjects))]
#endif
        public LightShadows shadows
        {
            get => _shadows;
            set 
            { 
                SetValue(nameof(shadows), value, ref _shadows, (newValue, oldValue) => 
                {
                    if (lightInternal != null)
                        lightInternal.shadows = value;
                }); 
            }
        }

        /// <summary>
        /// When enabled, raycasting is used to (partially or completely) occlude the lens flare.
        /// </summary>
        [Json]
        public bool useOcclusion
        {
            get => _useOcclusion;
            set => SetValue(nameof(useOcclusion), value, ref _useOcclusion);
        }

        /// <summary>
        /// Sets a radius multiplier around the light used to calculate the occlusion of the lens flare. If this area is half occluded by geometry, the intensity of the lens flare is cut by half.
        /// </summary>
        [Json]
        public float occlusionRadius
        {
            get => _occlusionRadius;
            set => SetValue(nameof(occlusionRadius), Mathf.Clamp(value, 0.0f, 5.0f), ref _occlusionRadius);
        }

        /// <summary>
        /// Sets the number of random samples used inside the Occlusion Radius area. A higher sample count gives a smoother attenuation when occluded.
        /// </summary>
        [Json]
        public uint occlusionSampleCount
        {
            get => _occlusionSampleCount;
            set => SetValue(nameof(occlusionSampleCount), Math.Clamp(value, 1, 32), ref _occlusionSampleCount);
        }

        private float _lastLensFlareIntensity;
        private bool _lastLensUseOcclusion;
        private RayDouble _lensFlareOcclusionRay;
        public void UpdateStar(Camera camera)
        {
            Vector3Double starPosition = transform.position;
            Vector3Double cameraPosition = camera.transform.position;
            Vector3Double cameraToStarDirection = (cameraPosition - starPosition).normalized;

            if (cameraToStarDirection != Vector3Double.zero)
                lightInternal.transform.localRotation = QuaternionDouble.Inverse(transform.rotation) * QuaternionDouble.LookRotation(cameraToStarDirection);

            LensFlareComponentSRP lensFlare = this.lensFlare;
            if (lensFlare != null)
            {
                lensFlare.scale = (float)(size / 8.0d / camera.GetDistanceScaleForCamera(gameObject.transform.position)) * lensFlareScale;

                _lastLensFlareIntensity = lensFlare.intensity;
                _lastLensUseOcclusion = lensFlare.useOcclusion;

                float intensity = 1.0f;

                if (useOcclusion && !camera.orthographic)
                {
                    float cameraStarDistance = (float)Vector3Double.Distance(starPosition, cameraPosition);
                    float occlusionRadius = cameraStarDistance * this.occlusionRadius * 0.1f;

                    _lensFlareOcclusionRay ??= new RayDouble();
                    _lensFlareOcclusionRay.origin = cameraPosition;

                    int unOccludedSampleCount = 0;
                    int sampleCount = (int)occlusionSampleCount;
                    float sampleAngle = 360.0f / sampleCount;
                  
                    for (int i = sampleCount - 1; i >= 0; i--)
                    {
                        _lensFlareOcclusionRay.direction = -(camera.gameObject.transform.position - lightInternal.transform.TransformPoint(new Vector3(occlusionRadius * Mathf.Sin(sampleAngle * i), occlusionRadius * Mathf.Cos(sampleAngle * i)))).normalized;
                        if (!PhysicsDouble.Raycast(_lensFlareOcclusionRay, out RaycastHitDouble hit, (float)cameraStarDistance))
                            unOccludedSampleCount++;
                    }

                    intensity = lensFlare.occlusionRemapCurve.Evaluate((float)unOccludedSampleCount / (float)sampleCount);
                }

                lensFlare.intensity = intensity;
                lensFlare.useOcclusion = false;
            }
        }

        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                LensFlareComponentSRP lensFlare = this.lensFlare;
                if (lensFlare != null)
                {
                    lensFlare.intensity = _lastLensFlareIntensity;
                    lensFlare.useOcclusion = _lastLensUseOcclusion;
                }

                return true;
            }
            return false;
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                if (lightInternal != null)
                {
                    //Prevent direct manipulation
                    lightInternal.transform.localPosition = Vector3.zero;
                }

                return true;
            }
            return false;
        }
    }
}
