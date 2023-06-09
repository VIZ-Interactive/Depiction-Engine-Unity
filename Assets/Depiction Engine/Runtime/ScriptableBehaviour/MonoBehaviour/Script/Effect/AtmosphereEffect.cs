// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [DisallowMultipleComponent]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Effect/" + nameof(AtmosphereEffect))]
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

        private GlobalLoader _atmosphereGlobalLoader;

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);
          
            string atmosphereLoadersName = "AtmosphereLoader";

            Transform atmosphereLoaderTransform = gameObject.transform.Find(atmosphereLoadersName);

            if (atmosphereLoaderTransform != null)
                atmosphereGlobalLoader = atmosphereLoaderTransform.gameObject.GetComponentInitialized<GlobalLoader>(initializingContext);
            else
            {
                DatasourceRoot datasourceRoot = objectBase.CreateChild<DatasourceRoot>(atmosphereLoadersName, null, initializingContext);
                datasourceRoot.dontSaveToScene = true;
                
                SerializableGuid atmosphereGridMeshObjectFallbackValuesId = SerializableGuid.NewGuid();
                
                atmosphereGlobalLoader = datasourceRoot.AddComponent<GlobalLoader>(initializingContext);
                atmosphereGlobalLoader.autoUpdateInterval = 0.0f;
                atmosphereGlobalLoader.minMaxZoom = Vector2Int.zero;
                atmosphereGlobalLoader.fallbackValuesId = new List<SerializableGuid> { atmosphereGridMeshObjectFallbackValuesId };

                JSONObject json = new()
                {
                    [nameof(PropertyMonoBehaviour.id)] = JsonUtility.ToJson(atmosphereGridMeshObjectFallbackValuesId)
                };
                FallbackValues atmosphereGridMeshObjectFallbackValues = datasourceRoot.AddComponent<FallbackValues>(initializingContext, json);
                atmosphereGridMeshObjectFallbackValues.SetFallbackJsonFromType(typeof(AtmosphereGridMeshObject).FullName);
                atmosphereGridMeshObjectFallbackValues.SetProperty(nameof(AtmosphereGridMeshObject.dontSaveToScene), true);
            }

            if (atmosphereGlobalLoader != Disposable.NULL)
                atmosphereGlobalLoader.name = atmosphereLoadersName;

            if (atmosphereGlobalLoader != Disposable.NULL)
                atmosphereGlobalLoader.objectBase.isHiddenInHierarchy = true;

            if (atmosphereGlobalLoader.transform.childCount == 0)
                atmosphereGlobalLoader.LoadAll();
            atmosphereGlobalLoader.enabled = false;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => sunBrightness = value, 200.0f, initializingContext);
            InitValue(value => rayleighScattering = value, 0.005f, initializingContext);
            InitValue(value => mieScattering = value, 0.002f, initializingContext);
            InitValue(value => miePhaseAsymmetryFactor = value, -0.999f, initializingContext);
            InitValue(value => scaleDepth = value, 0.1f, initializingContext);
            InitValue(value => waveLength = value, new Color(1.0f, 0.8407589f, 0.6469002f), initializingContext);
        }

        private GlobalLoader atmosphereGlobalLoader
        {
            get => _atmosphereGlobalLoader;
            set
            {
                if (Object.ReferenceEquals(_atmosphereGlobalLoader, value))
                    return;

                _atmosphereGlobalLoader = value;
            }
        }

        /// <summary>
        /// The amount of light reflected by the atmosphere.
        /// </summary>
        [Json]
        public float sunBrightness
        {
            get => _sunBrightness;
            set => SetValue(nameof(sunBrightness), value, ref _sunBrightness);
        }

        /// <summary>
        /// The scattering of light off of the molecules of the air.
        /// </summary>
        [Json]
        public float rayleighScattering
        {
            get => _rayleighScattering;
            set => SetValue(nameof(rayleighScattering), value, ref _rayleighScattering);
        }

        /// <summary>
        /// The elastic scattered light of particles that have a diameter similar to or larger than the wavelength of the incident light.
        /// </summary>
        [Json]
        public float mieScattering
        {
            get => _mieScattering;
            set => SetValue(nameof(mieScattering), value, ref _mieScattering);
        }

        /// <summary>
        /// The asymmetrical nature of the mie phase.
        /// </summary>
        [Json]
        public float miePhaseAsymmetryFactor
        {
            get => _miePhaseAsymmetryFactor;
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
            get => _scaleDepth;
            set => SetValue(nameof(scaleDepth), value, ref _scaleDepth);
        }

        /// <summary>
        /// The Wave length of the sun light.
        /// </summary>
        [Json]
        public Color waveLength
        {
            get => _waveLength;
            set => SetValue(nameof(waveLength), value, ref _waveLength);
        }

        public double GetAtmosphereAltitude()
        {
            double geoAstroObjectRadius = geoAstroObject.GetScaledRadius();
            return (ATMOPSHERE_ALTITUDE_FACTOR * geoAstroObjectRadius) - geoAstroObjectRadius;
        }
    }
}