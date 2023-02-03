// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [DisallowMultipleComponent]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Effect/" + nameof(AtmosphereEffect))]
    [RequireComponent(typeof(GeoAstroObject))]
    public class AtmosphereEffect : EffectBase
    {
        public const double ATMOPSHERE_ALTITUDE_FACTOR = 1.025d; // Difference between inner and outer geoAstroObjectRadius. Must be 2.5%

        [BeginFoldout("Atmosphere")]
        [SerializeField, Tooltip("The amount of light reflected by the atmosphere.")]
        private float _sunBrightness;
        [SerializeField, Tooltip("The scattering of light off of the molecules of the air.")]
        private float _rayleighScattering;            
        [SerializeField, Tooltip("The elastic scattered light of particles that have a diameter similar to or larger than the wavelength of the incident light.")]
        private float _mieScattering;             
        [SerializeField, Tooltip("The asymmetrical nature of the mie phase.")]
        private float _miePhaseAsymmetryFactor;              
        [SerializeField, Tooltip("The altitude at which the atmosphere's average density is found.")]
        private float _scaleDepth;
        [SerializeField, Tooltip("The Wave length of the sun light."), EndFoldout]
        private Color _waveLength;

        private GlobalLoader _atmosphereLoader;

        protected override void Initialized(InstanceManager.InitializationContext initializingState)
        {
            base.Initialized(initializingState);
          
            string atmosphereLoadersName = "AtmosphereLoader";

            Transform atmosphereLoaderTransform = gameObject.transform.Find(atmosphereLoadersName);

            if (atmosphereLoaderTransform != null)
                atmosphereLoader = atmosphereLoaderTransform.GetSafeComponent<GlobalLoader>(initializingState);
            else
            {
                DatasourceRoot datasourceRoot = objectBase.CreateChild<DatasourceRoot>(atmosphereLoadersName, null, initializingState);
                datasourceRoot.dontSaveToScene = true;
                
                SerializableGuid atmosphereGridMeshObjectFallbackValuesId = SerializableGuid.NewGuid();
                
                atmosphereLoader = datasourceRoot.gameObject.AddSafeComponent<GlobalLoader>(initializingState);
                atmosphereLoader.autoUpdateInterval = 0.0f;
                atmosphereLoader.zoomRange = Vector2Int.zero;
                atmosphereLoader.fallbackValuesId = new List<SerializableGuid> { atmosphereGridMeshObjectFallbackValuesId };

                JSONObject json = new JSONObject();
                json[nameof(PropertyMonoBehaviour.id)] = JsonUtility.ToJson(atmosphereGridMeshObjectFallbackValuesId);
                FallbackValues atmosphereGridMeshObjectFallbackValues = datasourceRoot.gameObject.AddSafeComponent<FallbackValues>(initializingState, json);
                atmosphereGridMeshObjectFallbackValues.SetFallbackJsonFromType(typeof(AtmosphereGridMeshObject).FullName);
                atmosphereGridMeshObjectFallbackValues.SetProperty(nameof(AtmosphereGridMeshObject.dontSaveToScene), true);
            }

            if (atmosphereLoader != Disposable.NULL)
                atmosphereLoader.name = atmosphereLoadersName;

            if (atmosphereLoader != Disposable.NULL)
                atmosphereLoader.objectBase.isHiddenInHierarchy = true;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => sunBrightness = value, 200.0f, initializingState);
            InitValue(value => rayleighScattering = value, 0.001f, initializingState);
            InitValue(value => mieScattering = value, 0.008f, initializingState);
            InitValue(value => miePhaseAsymmetryFactor = value, -0.999f, initializingState);
            InitValue(value => scaleDepth = value, 0.17f, initializingState);
            InitValue(value => waveLength = value, new Color(0.8679245f, 0.7108141f, 0.5287087f), initializingState);
        }

        private GlobalLoader atmosphereLoader
        {
            get { return _atmosphereLoader; }
            set
            {
                if (Object.ReferenceEquals(_atmosphereLoader, value))
                    return;

                _atmosphereLoader = value;

                UpdateAtmosphereLoaderActive();
            }
        }

        /// <summary>
        /// The amount of light reflected by the atmosphere.
        /// </summary>
        [Json]
        public float sunBrightness
        {
            get { return _sunBrightness; }
            set { SetValue(nameof(sunBrightness), value, ref _sunBrightness); }
        }

        /// <summary>
        /// The scattering of light off of the molecules of the air.
        /// </summary>
        [Json]
        public float rayleighScattering
        {
            get { return _rayleighScattering; }
            set { SetValue(nameof(rayleighScattering), value, ref _rayleighScattering); }
        }

        /// <summary>
        /// The elastic scattered light of particles that have a diameter similar to or larger than the wavelength of the incident light.
        /// </summary>
        [Json]
        public float mieScattering
        {
            get { return _mieScattering; }
            set { SetValue(nameof(mieScattering), value, ref _mieScattering); }
        }

        /// <summary>
        /// The asymmetrical nature of the mie phase.
        /// </summary>
        [Json]
        public float miePhaseAsymmetryFactor
        {
            get { return _miePhaseAsymmetryFactor; }
            set 
            {
                if (value < -0.999f)
                    value = -0.0999f;
                if (value > 0.999f)
                    value = 0.0999f;
                SetValue(nameof(miePhaseAsymmetryFactor), value, ref _miePhaseAsymmetryFactor); 
            }
        }

        /// <summary>
        /// The altitude at which the atmosphere's average density is found.
        /// </summary>
        [Json]
        public float scaleDepth
        {
            get { return _scaleDepth; }
            set { SetValue(nameof(scaleDepth), value, ref _scaleDepth); }
        }

        /// <summary>
        /// The Wave length of the sun light.
        /// </summary>
        [Json]
        public Color waveLength
        {
            get { return _waveLength; }
            set { SetValue(nameof(waveLength), value, ref _waveLength); }
        }

        public double GetAtmosphereAltitude()
        {
            double geoAstroObjectRadius = geoAstroObject.GetScaledRadius();
            return (ATMOPSHERE_ALTITUDE_FACTOR * geoAstroObjectRadius) - geoAstroObjectRadius;
        }

        protected override void ActiveAndEnabledChanged(bool newValue, bool oldValue)
        {
            base.ActiveAndEnabledChanged(newValue, oldValue);

            UpdateAtmosphereLoaderActive();
        }

        private void UpdateAtmosphereLoaderActive()
        {
            if (atmosphereLoader != Disposable.NULL)
                atmosphereLoader.gameObject.SetActive(!IsDisposing() && activeAndEnabled);
        }

        public override bool OnDisposing()
        {
            if (base.OnDisposing())
            {
                UpdateAtmosphereLoaderActive();

                return true;
            }
            return false;
        }
    }
}