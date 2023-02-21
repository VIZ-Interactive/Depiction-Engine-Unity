// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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
#endif

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            InitLightAndLensFlare(initializingState);
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => color = value, new Color(1.0f, 0.9597525f, 0.9016172f), initializingState);
            InitValue(value => lensFlareScale = value, 1.3f, initializingState);
            InitValue(value => intensity = value, 1.8f, initializingState);
            InitValue(value => range = value, 1000000.0f, initializingState);
            InitValue(value => shadows = value, LightShadows.Soft, initializingState);
        }

        protected override void InitReflectionProbeObject()
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

        private void InitLightAndLensFlare(InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically)
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
                    GameObject go = new GameObject("Light");
                    go.transform.SetParent(gameObject.transform, false);

#if UNITY_EDITOR
                    Editor.UndoManager.RegisterCreatedObjectUndo(go, initializingState);
#endif

                    _lightInternal = go.AddSafeComponent<Light>(initializingState);
                    _lightInternal.type = LightType.Directional;

                    go.AddSafeComponent<UniversalAdditionalLightData>(initializingState);

                    _lensFlare = go.AddSafeComponent<LensFlareComponentSRP>(initializingState);
                    _lensFlare.lensFlareData = Resources.Load("LensFlare/Sun Lens Flare (SRP)") as LensFlareDataSRP;
                    _lensFlare.useOcclusion = false;
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
            get { return _color; }
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
            get { return _lensFlareScale; }
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
            get { return _intensity; }
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
            get { return _range; }
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
            get { return _shadows; }
            set 
            { 
                SetValue(nameof(shadows), value, ref _shadows, (newValue, oldValue) => 
                {
                    if (lightInternal != null)
                        lightInternal.shadows = value;
                }); 
            }
        }

        private float _lastLensFlareIntensity;
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

                if (!camera.orthographic)
                {
                    if (_lensFlareOcclusionRay == null)
                        _lensFlareOcclusionRay = new RayDouble();

                    _lensFlareOcclusionRay.origin = cameraPosition;
                    _lensFlareOcclusionRay.direction = -cameraToStarDirection;

                    if (PhysicsDouble.Raycast(_lensFlareOcclusionRay, out RaycastHitDouble hit, (float)Vector3Double.Distance(starPosition, cameraPosition)))
                        lensFlare.intensity = 0.0f;
                }
            }
        }

        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                if (lensFlare != null)
                    lensFlare.intensity = _lastLensFlareIntensity;

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
